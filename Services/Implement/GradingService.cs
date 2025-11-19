using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Entities.Enums;
using Services.Dtos.Requests;
using Services.Dtos.Responses;
using Services.Interfaces;

namespace Services.Implement
{
    public class GradingService : IGradingService
    {
        private readonly AppDbContext _db;

        public GradingService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AssignedExamResponse>> GetAssignedExamsAsync(Guid examinerId, CancellationToken ct = default)
        {
            var assignments = await _db.ExaminerAssignments
                .AsNoTracking()
                .Where(ea => ea.ExaminerId == examinerId)
                .Include(ea => ea.Exam!)
                    .ThenInclude(e => e.Subject!)
                .Include(ea => ea.Exam!)
                    .ThenInclude(e => e.Semester!)
                .Include(ea => ea.Exam!)
                    .ThenInclude(e => e.Sessions)
                        .ThenInclude(s => s.Submissions)
                            .ThenInclude(sub => sub.Grades)
                .ToListAsync(ct);

            var result = assignments.Select(ea =>
            {
                var exam = ea.Exam!;
                var submissions = exam.Sessions.SelectMany(s => s.Submissions).ToList();
                
                return new AssignedExamResponse
                {
                    ExamId = exam.ExamId,
                    ExamName = exam.ExamName,
                    SubjectId = exam.SubjectId,
                    SubjectName = exam.Subject?.SubjectName ?? string.Empty,
                    SemesterId = exam.SemesterId,
                    SemesterName = exam.Semester?.SemesterName ?? string.Empty,
                    TotalSubmissions = submissions.Count,
                    PendingSubmissions = submissions.Count(s => s.Status == SubmissionStatus.Pending),
                    ProcessingSubmissions = submissions.Count(s => s.Status == SubmissionStatus.Processing),
                    GradedSubmissions = submissions.Count(s => s.Status == SubmissionStatus.Graded),
                    MyGradedSubmissions = submissions.Count(s => s.Grades.Any(g => g.ExaminerId == examinerId))
                };
            }).ToList();

            return result;
        }

        public async Task<PagedSubmissionResponse> GetExamSubmissionsAsync( Guid examId, Guid examinerId, GetSubmissionsQuery query, CancellationToken ct = default)
        {
            // Verify examiner is assigned to this exam
            var isAssigned = await _db.ExaminerAssignments
                .AsNoTracking()
                .AnyAsync(ea => ea.ExamId == examId && ea.ExaminerId == examinerId, ct);
            
            if (!isAssigned)
            {
                throw new UnauthorizedAccessException("Examiner is not assigned to this exam");
            }

            // Get session IDs for this exam
            var sessionIds = await _db.ExamSessions
                .AsNoTracking()
                .Where(s => s.ExamId == examId)
                .Select(s => s.SessionId)
                .ToListAsync(ct);

            // Build query
            var submissionsQuery = _db.Submissions
                .AsNoTracking()
                .Where(s => sessionIds.Contains(s.SessionId))
                .Include(s => s.Grades)
                .Include(s => s.Violations)
                .AsQueryable();

            // Filter by status
            if (query.Status.HasValue)
            {
                submissionsQuery = submissionsQuery.Where(s => s.Status == query.Status.Value);
            }

            // Filter by assigned to examiner (submissions where examiner hasn't graded yet)
            if (query.AssignedToMe)
            {
                submissionsQuery = submissionsQuery.Where(s => 
                    !s.Grades.Any(g => g.ExaminerId == examinerId));
            }

            // Get total count
            var totalCount = await submissionsQuery.CountAsync(ct);

            // Apply pagination
            var submissions = await submissionsQuery
                .OrderBy(s => s.StudentId)
                .ThenBy(s => s.StudentName)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var items = submissions.Select(s =>
            {
                var myGrades = s.Grades.Where(g => g.ExaminerId == examinerId).ToList();
                var otherGrades = s.Grades.Where(g => g.ExaminerId != examinerId).ToList();
                var allGraders = s.Grades.Select(g => g.ExaminerId).Distinct().ToList();
                var hasViolations = s.Violations.Any();

                return new SubmissionListItemResponse
                {
                    SubmissionId = s.SubmissionId,
                    SessionId = s.SessionId,
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    FileName = s.FileName,
                    SubmissionTime = s.SubmissionTime,
                    Status = s.Status,
                    HasViolations = hasViolations,
                    IsGradedByMe = myGrades.Any(),
                    IsGradedByOthers = otherGrades.Any(),
                    GradingCount = allGraders.Count,
                    RequiresDoubleGrading = hasViolations || allGraders.Count == 1
                };
            }).ToList();

            return new PagedSubmissionResponse
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            };
        }

        public async Task<SubmissionGradingDetailsResponse?> GetSubmissionDetailsAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default)
        {
            // Verify examiner is assigned
            if (!await IsExaminerAssignedAsync(submissionId, examinerId, ct))
            {
                return null;
            }

            // Load submission with all related data
            var submission = await _db.Submissions
                .AsNoTracking()
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Rubric!)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Examiner!)
                .Include(s => s.Violations)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);

            if (submission == null || submission.Session?.Exam == null)
            {
                return null;
            }

            var exam = submission.Session.Exam;
            var allGraders = submission.Grades.Select(g => g.ExaminerId).Distinct().ToList();
            var isSecondGrader = allGraders.Count == 1 && !allGraders.Contains(examinerId);

            // Get my existing grades
            var myGrades = submission.Grades.Where(g => g.ExaminerId == examinerId).ToList();

            // Build rubrics with existing grades
            var rubrics = exam.Rubrics.OrderBy(r => r.RubricId).Select(r =>
            {
                var myGrade = myGrades.FirstOrDefault(g => g.RubricId == r.RubricId);
                return new RubricGradingItemResponse
                {
                    RubricId = r.RubricId,
                    Criteria = r.Criteria,
                    Description = r.Description,
                    MaxPoints = r.MaxPoints,
                    Points = myGrade?.Points,
                    Comments = myGrade?.Comments
                };
            }).ToList();

            // Build existing grades (all examiners)
            var existingGrades = submission.Grades.Select(g => new GradeResponse
            {
                GradeId = g.GradeId,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.ExaminerId,
                ExaminerName = g.Examiner?.Username,
                RubricId = g.RubricId,
                RubricCriteria = g.Rubric?.Criteria ?? string.Empty,
                Points = g.Points,
                MaxPoints = g.Rubric?.MaxPoints ?? 0,
                Comments = g.Comments,
                GradedAt = g.GradedAt,
                IsFinal = g.IsFinal
            }).ToList();

            // Build violations
            var violations = submission.Violations.Select(v => new ViolationItemResponse
            {
                ViolationId = v.ViolationId,
                ViolationType = v.ViolationType.ToString(),
                Description = v.Description,
                Severity = v.Severity,
                DetectedAt = v.DetectedAt
            }).ToList();

            // Build file download URL (assuming API route structure)
            var fileDownloadUrl = $"/api/submissions/{submissionId}/download"; // Or actual file serving endpoint

            return new SubmissionGradingDetailsResponse
            {
                SubmissionId = submission.SubmissionId,
                SessionId = submission.SessionId,
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                StudentId = submission.StudentId,
                StudentName = submission.StudentName,
                FileName = submission.FileName,
                FileDownloadUrl = fileDownloadUrl,
                SubmissionTime = submission.SubmissionTime,
                Status = submission.Status,
                Violations = violations,
                Rubrics = rubrics,
                ExistingGrades = existingGrades,
                CanEdit = !myGrades.Any(g => g.IsFinal), // Can edit if not finalized
                IsSecondGrader = isSecondGrader
            };
        }

        public async Task<List<RubricResponse>> GetExamRubricsAsync(Guid examId, CancellationToken ct = default)
        {
            return await _db.Rubrics
                .AsNoTracking()
                .Where(r => r.ExamId == examId)
                .OrderBy(r => r.RubricId)
                .Select(r => new RubricResponse
                {
                    RubricId = r.RubricId,
                    ExamId = r.ExamId,
                    Criteria = r.Criteria,
                    Description = r.Description,
                    MaxPoints = r.MaxPoints
                })
                .ToListAsync(ct);
        }

        public async Task<GradingResultResponse> SubmitGradesAsync(GradingRequests request, Guid examinerId, CancellationToken ct = default)
        {
            // Verify examiner is assigned
            if (!await IsExaminerAssignedAsync(request.SubmissionId, examinerId, ct))
            {
                throw new UnauthorizedAccessException("Examiner is not assigned to grade this submission");
            }

            // Load submission with tracking for update
            var submission = await _db.Submissions
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId, ct);

            if (submission == null)
            {
                throw new KeyNotFoundException("Submission not found");
            }

            if (submission.Session?.Exam == null)
            {
                throw new InvalidOperationException("Submission exam not found");
            }

            // Validate status
            if (submission.Status != SubmissionStatus.Pending && submission.Status != SubmissionStatus.Processing)
            {
                throw new InvalidOperationException($"Cannot grade submission with status: {submission.Status}");
            }

            var exam = submission.Session.Exam;
            var rubrics = exam.Rubrics.ToList();

            // Validate exam has rubrics defined
            if (rubrics.Count == 0)
            {
                throw new InvalidOperationException("Cannot grade submission: Exam has no rubrics defined");
            }

            // Validate all rubrics are provided
            var providedRubricIds = request.Grades.Select(g => g.RubricId).ToHashSet();
            var requiredRubricIds = rubrics.Select(r => r.RubricId).ToHashSet();

            if (!providedRubricIds.SetEquals(requiredRubricIds))
            {
                var missing = requiredRubricIds.Except(providedRubricIds).ToList();
                throw new InvalidOperationException($"Missing grades for rubrics: {string.Join(", ", missing)}");
            }

            // Validate points
            foreach (var gradeDto in request.Grades)
            {
                var rubric = rubrics.First(r => r.RubricId == gradeDto.RubricId);
                if (gradeDto.Points > rubric.MaxPoints)
                {
                    throw new InvalidOperationException(
                        $"Points ({gradeDto.Points}) exceed maximum ({rubric.MaxPoints}) for rubric: {rubric.Criteria}");
                }
                if (gradeDto.Points < 0)
                {
                    throw new InvalidOperationException("Points cannot be negative");
                }
            }

            // Get existing grades for this examiner and check garder has graded before
            var existingGrades = submission.Grades.Where(g => g.ExaminerId == examinerId).ToList();

            // Create grades
            var createdGrades = new List<Grade>();
            foreach (var gradeDto in request.Grades)
            {
                var existingGrade = existingGrades.FirstOrDefault(g => g.RubricId == gradeDto.RubricId);
                
                if (existingGrade != null)
                {
                    // Update existed grade (only if not finalized)
                    if (existingGrade.IsFinal)
                    {
                        throw new InvalidOperationException("Cannot update finalized grade");
                    }
                    existingGrade.Points = gradeDto.Points;
                    existingGrade.Comments = gradeDto.Comments;
                    existingGrade.GradedAt = DateTime.UtcNow;
                    createdGrades.Add(existingGrade);
                }
                else
                {
                    // Create new grade
                    var newGrade = new Grade
                    {
                        GradeId = Guid.NewGuid(),
                        SubmissionId = request.SubmissionId,
                        ExaminerId = examinerId,
                        RubricId = gradeDto.RubricId,
                        Points = gradeDto.Points,
                        Comments = gradeDto.Comments,
                        GradedAt = DateTime.UtcNow,
                        IsFinal = false
                    };
                    _db.Grades.Add(newGrade);
                    createdGrades.Add(newGrade);
                }
            }

            // Check examiner hien tai cham submission nay truoc do hay chua (excluding new grades)
            var hasExistingGradesForThisExaminer = existingGrades.Any();
            var otherGradersBefore = submission.Grades //Lấy list examiner khác đã chấm submission này trước khi submit lần này
                .Where(g => g.ExaminerId != examinerId)
                .Select(g => g.ExaminerId)
                .Distinct()
                .ToList();
            var wasCompletingDoubleGrading = otherGradersBefore.Count == 1 && !hasExistingGradesForThisExaminer;
            
            var result = new GradingResultResponse
            {
                Success = true,
                Message = "Grades submitted successfully",
                SubmissionId = request.SubmissionId,
                CreatedGrades = createdGrades.Select(g => new GradeResponse
                {
                    GradeId = g.GradeId,
                    SubmissionId = g.SubmissionId,
                    ExaminerId = g.ExaminerId,
                    RubricId = g.RubricId,
                    Points = g.Points,
                    Comments = g.Comments,
                    GradedAt = g.GradedAt,
                    IsFinal = g.IsFinal
                }).ToList()
            };

            await _db.SaveChangesAsync(ct);

            // Đếm số grader sau khi submit
            var allGradersAfter = await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == request.SubmissionId)
                .Select(g => g.ExaminerId)
                .Distinct()
                .CountAsync(ct);

            // If completed double grading OR updating in a double-graded submission, calculate average and check for moderator review
            if (wasCompletingDoubleGrading || allGradersAfter >= 2)
            {
                var averageScore = await CalculateAverageScoreAsync(request.SubmissionId, ct);
                result.AverageScore = averageScore;

                var requiresReview = await RequiresModeratorReviewAsync(request.SubmissionId, ct);
                result.RequiresModeratorReview = requiresReview;

                // Calculate score difference if 2 graders
                if (allGradersAfter == 2)
                {
                    var graderScores = await _db.Grades
                        .AsNoTracking()
                        .Where(g => g.SubmissionId == request.SubmissionId)
                        .GroupBy(g => g.ExaminerId)
                        .Select(g => new { ExaminerId = g.Key, TotalScore = g.Sum(gr => gr.Points) })
                        .ToListAsync(ct);

                    if (graderScores.Count == 2)
                    {
                        var scores = graderScores.Select(s => s.TotalScore).OrderBy(s => s).ToList();
                        result.ScoreDifference = Math.Abs(scores[1] - scores[0]);
                    }
                }

                // Update submission status (only if we completed double grading or if status needs change)
                if (wasCompletingDoubleGrading)
                {
                    if (requiresReview)
                    {
                        submission.Status = SubmissionStatus.Flagged;
                        result.NewStatus = SubmissionStatus.Flagged;
                        result.Message = "Grades submitted. Submission flagged for moderator review due to significant score difference.";
                    }
                    else
                    {
                        submission.Status = SubmissionStatus.Graded;
                        result.NewStatus = SubmissionStatus.Graded;
                        result.Message = "Grades submitted. Double grading complete.";
                    }
                    
                    await _db.SaveChangesAsync(ct);
                }
                else if (allGradersAfter >= 2)
                {
                    // Updating existing grades - recalculate but don't change status automatically
                    result.NewStatus = submission.Status;
                    if (requiresReview && submission.Status != SubmissionStatus.Flagged)
                    {
                        submission.Status = SubmissionStatus.Flagged;
                        result.NewStatus = SubmissionStatus.Flagged;
                        await _db.SaveChangesAsync(ct);
                    }
                }
            }
            else
            {
                result.NewStatus = submission.Status;
            }           
            return result;
        }

        public async Task<GradeResponse?> UpdateGradeAsync(Guid gradeId, UpdateGradeRequest request, Guid examinerId, CancellationToken ct = default)
        {
            var grade = await _db.Grades
                .Include(g => g.Rubric!)
                .Include(g => g.Submission!)
                    .ThenInclude(s => s.Session!)
                        .ThenInclude(ses => ses.Exam!)
                .FirstOrDefaultAsync(g => g.GradeId == gradeId, ct);

            if (grade == null)
            {
                return null;
            }

            // Verify it belongs to this examiner
            if (grade.ExaminerId != examinerId)
            {
                throw new UnauthorizedAccessException("Cannot update grade from another examiner");
            }

            // Check if editable (not finalized)
            if (grade.IsFinal)
            {
                throw new InvalidOperationException("Cannot update finalized grade");
            }

            // Validate points
            if (request.Points > grade.Rubric!.MaxPoints)
            {
                throw new InvalidOperationException(
                    $"Points ({request.Points}) exceed maximum ({grade.Rubric.MaxPoints}) for rubric: {grade.Rubric.Criteria}");
            }
            if (request.Points < 0)
            {
                throw new InvalidOperationException("Points cannot be negative");
            }

            // Update grade
            grade.Points = request.Points;
            grade.Comments = request.Comments;
            grade.GradedAt = DateTime.UtcNow;

            // If this is part of double grading, recalculate and check for review
            var submission = grade.Submission!;
            var allGraders = await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == submission.SubmissionId)
                .Select(g => g.ExaminerId)
                .Distinct()
                .CountAsync(ct);

            if (allGraders >= 2)
            {
                var requiresReview = await RequiresModeratorReviewAsync(submission.SubmissionId, ct);
                if (requiresReview)
                {
                    submission.Status = SubmissionStatus.Flagged;
                }
                else if (submission.Status == SubmissionStatus.Flagged)
                {
                    submission.Status = SubmissionStatus.Graded;
                }
            }

            await _db.SaveChangesAsync(ct);

            return new GradeResponse
            {
                GradeId = grade.GradeId,
                SubmissionId = grade.SubmissionId,
                ExaminerId = grade.ExaminerId,
                RubricId = grade.RubricId,
                RubricCriteria = grade.Rubric.Criteria,
                Points = grade.Points,
                MaxPoints = grade.Rubric.MaxPoints,
                Comments = grade.Comments,
                GradedAt = grade.GradedAt,
                IsFinal = grade.IsFinal
            };
        }

        public async Task<GradingResultResponse> MarkZeroDueToViolationsAsync(MarkZeroRequest request, Guid examinerId, CancellationToken ct = default)
        {
            // Verify examiner is assigned
            if (!await IsExaminerAssignedAsync(request.SubmissionId, examinerId, ct))
            {
                throw new UnauthorizedAccessException("Examiner is not assigned to grade this submission");
            }

            // Load submission
            var submission = await _db.Submissions
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .Include(s => s.Violations)
                .Include(s => s.Grades)
                .FirstOrDefaultAsync(s => s.SubmissionId == request.SubmissionId, ct);

            if (submission == null)
            {
                throw new KeyNotFoundException("Submission not found");
            }

            if (submission.Session?.Exam == null)
            {
                throw new InvalidOperationException("Submission exam not found");
            }

            // Verify it has violations
            if (!submission.Violations.Any())
            {
                throw new InvalidOperationException("Submission has no violations. Cannot mark as zero.");
            }

            var exam = submission.Session.Exam;
            var rubrics = exam.Rubrics.ToList();

            // Validate exam has rubrics defined
            if (rubrics.Count == 0)
            {
                throw new InvalidOperationException("Cannot mark zero: Exam has no rubrics defined");
            }

            // Check if examiner already has grades - if so, delete them
            var existingGrades = submission.Grades.Where(g => g.ExaminerId == examinerId).ToList();
            foreach (var existingGrade in existingGrades)
            {
                if (existingGrade.IsFinal)
                {
                    throw new InvalidOperationException("Cannot mark zero: submission already has finalized grades");
                }
                _db.Grades.Remove(existingGrade);
            }

            // Create zero grades for all rubrics with justification
            var createdGrades = new List<Grade>();
            var violationSummary = string.Join("; ", submission.Violations.Select(v => $"{v.ViolationType}: {v.Description}"));
            var comments = $"Zero score due to violations: {violationSummary}. Examiner justification: {request.Reason}";

            foreach (var rubric in rubrics)
            {
                var grade = new Grade
                {
                    GradeId = Guid.NewGuid(),
                    SubmissionId = request.SubmissionId,
                    ExaminerId = examinerId,
                    RubricId = rubric.RubricId,
                    Points = 0,
                    Comments = comments,
                    GradedAt = DateTime.UtcNow,
                    IsFinal = false
                };
                _db.Grades.Add(grade);
                createdGrades.Add(grade);
            }

            // Update submission status to Flagged for moderator review
            submission.Status = SubmissionStatus.Flagged;

            await _db.SaveChangesAsync(ct);

            return new GradingResultResponse
            {
                Success = true,
                Message = "Submission marked as zero due to violations. Awaiting moderator review.",
                SubmissionId = request.SubmissionId,
                NewStatus = SubmissionStatus.Flagged,
                RequiresModeratorReview = true,
                CreatedGrades = createdGrades.Select(g => new GradeResponse
                {
                    GradeId = g.GradeId,
                    SubmissionId = g.SubmissionId,
                    ExaminerId = g.ExaminerId,
                    RubricId = g.RubricId,
                    Points = g.Points,
                    Comments = g.Comments,
                    GradedAt = g.GradedAt,
                    IsFinal = g.IsFinal
                }).ToList()
            };
        }

        public async Task<List<GradeResponse>> GetSubmissionGradesAsync(Guid submissionId, CancellationToken ct = default)
        {
            return await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == submissionId)
                .Include(g => g.Examiner!)
                .Include(g => g.Rubric!)
                .OrderBy(g => g.ExaminerId)
                .ThenBy(g => g.RubricId)
                .Select(g => new GradeResponse
                {
                    GradeId = g.GradeId,
                    SubmissionId = g.SubmissionId,
                    ExaminerId = g.ExaminerId,
                    ExaminerName = g.Examiner!.Username,
                    RubricId = g.RubricId,
                    RubricCriteria = g.Rubric!.Criteria,
                    Points = g.Points,
                    MaxPoints = g.Rubric.MaxPoints,
                    Comments = g.Comments,
                    GradedAt = g.GradedAt,
                    IsFinal = g.IsFinal
                })
                .ToListAsync(ct);
        }

        public async Task<GradingStatusResponse?> GetGradingStatusAsync(Guid submissionId, CancellationToken ct = default)
        {
            var submission = await _db.Submissions
                .AsNoTracking()
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .Include(s => s.Violations)
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Examiner!)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);

            if (submission == null || submission.Session?.Exam == null)
            {
                return null;
            }

            var exam = submission.Session.Exam;
            var allGraders = submission.Grades.Select(g => g.ExaminerId).Distinct().ToList();
            var hasViolations = submission.Violations.Any();

            // Calculate max possible score
            var maxPossibleScore = exam.Rubrics.Sum(r => r.MaxPoints);

            // Get grader summaries
            var graderSummaries = allGraders.Select(exId =>
            {
                var graderGrades = submission.Grades.Where(g => g.ExaminerId == exId).ToList();
                return new ExaminerGradingSummary
                {
                    ExaminerId = exId,
                    ExaminerName = graderGrades.FirstOrDefault()?.Examiner?.Username,
                    TotalScore = graderGrades.Sum(g => g.Points),
                    RubricsGraded = graderGrades.Count,
                    LastGradedAt = graderGrades.Any() ? graderGrades.Max(g => g.GradedAt) : null
                };
            }).ToList();

            var averageScore = await CalculateAverageScoreAsync(submissionId, ct);
            var requiresReview = await RequiresModeratorReviewAsync(submissionId, ct);

            return new GradingStatusResponse
            {
                SubmissionId = submissionId,
                ExaminerCount = allGraders.Count,
                RequiresDoubleGrading = hasViolations || allGraders.Count == 1,
                IsDoubleGradingComplete = allGraders.Count >= 2,
                AverageScore = averageScore,
                MaxPossibleScore = maxPossibleScore,
                RequiresModeratorReview = requiresReview,
                Status = submission.Status,
                ExaminerSummaries = graderSummaries
            };
        }

        public async Task<bool> IsExaminerAssignedAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default)
        {
            var submission = await _db.Submissions
                .AsNoTracking()
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);

            if (submission?.Session?.Exam == null)
            {
                return false;
            }

            return await _db.ExaminerAssignments
                .AsNoTracking()
                .AnyAsync(ea => ea.ExamId == submission.Session.Exam.ExamId && ea.ExaminerId == examinerId, ct);
        }

        public async Task<bool> ValidateAllRubricsGradedAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default)
        {
            var submission = await _db.Submissions
                .AsNoTracking()
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);

            if (submission?.Session?.Exam == null)
            {
                return false;
            }

            var examRubricIds = submission.Session.Exam.Rubrics.Select(r => r.RubricId).ToHashSet();
            var gradedRubricIds = await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == submissionId && g.ExaminerId == examinerId)
                .Select(g => g.RubricId)
                .ToListAsync(ct);

            return examRubricIds.SetEquals(gradedRubricIds);
        }

        public async Task<decimal?> CalculateAverageScoreAsync(Guid submissionId, CancellationToken ct = default)
        {
            var graderScores = await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == submissionId)
                .GroupBy(g => g.ExaminerId)
                .Select(g => new { ExaminerId = g.Key, TotalScore = g.Sum(gr => gr.Points) })
                .ToListAsync(ct);

            if (graderScores.Count < 2)
            {
                return null; // Need at least 2 graders to calculate average
            }

            var average = graderScores.Average(s => s.TotalScore);
            return Math.Round(average, 2);
        }

        public async Task<bool> RequiresModeratorReviewAsync(Guid submissionId, CancellationToken ct = default)
        {
            // Get all grades grouped by examiner
            var graderScores = await _db.Grades
                .AsNoTracking()
                .Where(g => g.SubmissionId == submissionId)
                .GroupBy(g => g.ExaminerId)
                .Select(g => new { ExaminerId = g.Key, TotalScore = g.Sum(gr => gr.Points) })
                .ToListAsync(ct);

            if (graderScores.Count != 2)
            {
                return false; // Only check when exactly 2 graders
            }

            // Get max possible score
            var submission = await _db.Submissions
                .AsNoTracking()
                .Include(s => s.Session!)
                    .ThenInclude(ses => ses.Exam!)
                        .ThenInclude(e => e.Rubrics)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);

            if (submission?.Session?.Exam == null)
            {
                return false;
            }

            var maxPossibleScore = submission.Session.Exam.Rubrics.Sum(r => r.MaxPoints);
            if (maxPossibleScore == 0)
            {
                return false;
            }

            var scores = graderScores.Select(s => s.TotalScore).OrderBy(s => s).ToList();
            var score1 = scores[0];
            var score2 = scores[1];

            // Calculate difference as percentage of max possible score
            var difference = Math.Abs(score2 - score1);
            var differencePercentage = (difference / maxPossibleScore) * 100;

            // Flag for review if difference > 20%
            return differencePercentage > 20;
        }
    }
}

