using CoreService.Entities;
using CoreService.Repositories;
using CoreService.DTOs;
using CoreService.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;
    private readonly IExamRepository _examRepository;
    private readonly CoreDbContext _context;
    private readonly ILogger<GradeService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GradeService(
        IGradeRepository gradeRepository,
        IExamRepository examRepository,
        CoreDbContext context,
        ILogger<GradeService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _gradeRepository = gradeRepository;
        _examRepository = examRepository;
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;

        // Configure Mapster mappings
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        TypeAdapterConfig<RubricScore, RubricScoreDetail>
            .NewConfig()
            .Map(dest => dest.Criteria, src => src.RubricItem.Criteria)
            .Map(dest => dest.Description, src => src.RubricItem.Description)
            .Map(dest => dest.MaxPoints, src => src.RubricItem.MaxPoints);
    }

    // New rubric-based grading methods (Use Case 3)
    public async Task<GradingResponse> GradeSubmissionAsync(GradingRequest request)
    {
        // Validate submission exists and get exam info
        var submission = await ValidateSubmissionAsync(request.SubmissionId);
        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam {submission.ExamId} not found");
        }

        // Validate examiner is assigned to this exam
        await ValidateExaminerAssignmentAsync(exam.Id, request.GradedBy);

        // Validate rubric scores
        await ValidateRubricScoresAsync(request, exam);

        // Check if this is first or second grading
        var existingGrades = await _context.RubricScores
            .Where(rs => rs.SubmissionId == request.SubmissionId)
            .Include(rs => rs.RubricItem)
            .ToListAsync();

        var isFirstGrading = !existingGrades.Any();

        // Save rubric scores
        var rubricScores = new List<RubricScore>();
        foreach (var scoreRequest in request.RubricScores)
        {
            var rubricScore = new RubricScore
            {
                SubmissionId = request.SubmissionId,
                RubricItemId = scoreRequest.RubricItemId,
                GradedBy = request.GradedBy,
                Points = scoreRequest.Points,
                Comments = scoreRequest.Comments,
                GradedAt = DateTime.UtcNow
            };
            rubricScores.Add(rubricScore);
        }

        await _context.RubricScores.AddRangeAsync(rubricScores);
        await _context.SaveChangesAsync();

        // Calculate grading status and check for moderator review
        var gradingStatus = await DetermineGradingStatusAsync(request.SubmissionId, isFirstGrading);

        // Update submission status in StorageService
        await UpdateSubmissionStatusAsync(request.SubmissionId, gradingStatus);

        // Calculate response
        var totalScore = rubricScores.Sum(rs => rs.Points);
        var maxScore = exam.RubricItems.Sum(ri => ri.MaxPoints);

        var response = new GradingResponse
        {
            SubmissionId = request.SubmissionId,
            GradedBy = request.GradedBy,
            ExaminerName = await GetExaminerNameAsync(request.GradedBy),
            GradedAt = DateTime.UtcNow,
            TotalScore = totalScore,
            MaxScore = maxScore,
            RubricScores = rubricScores.Adapt<List<RubricScoreDetail>>(),
            Status = gradingStatus.Status,
            RequiresModeratorReview = gradingStatus.RequiresModeratorReview,
            ModeratorReviewReason = gradingStatus.ModeratorReviewReason
        };

        _logger.LogInformation("Submission {SubmissionId} graded by {ExaminerId}. Status: {Status}, Score: {Score}/{Max}",
            request.SubmissionId, request.GradedBy, gradingStatus.Status, totalScore, maxScore);

        return response;
    }

    public async Task<GradingResponse?> GetSubmissionGradesAsync(Guid submissionId)
    {
        var rubricScores = await _context.RubricScores
            .Where(rs => rs.SubmissionId == submissionId)
            .Include(rs => rs.RubricItem)
            .OrderBy(rs => rs.GradedAt)
            .ToListAsync();

        if (!rubricScores.Any()) return null;

        // Group by examiner to handle multiple gradings
        var examinerGroups = rubricScores.GroupBy(rs => rs.GradedBy).ToList();

        // For now, return the most recent grading
        var latestGrading = examinerGroups.OrderByDescending(g => g.Max(rs => rs.GradedAt)).First();
        var latestScores = latestGrading.ToList();

        var exam = await GetExamFromSubmissionAsync(submissionId);
        var totalScore = latestScores.Sum(rs => rs.Points);
        var maxScore = exam.RubricItems.Sum(ri => ri.MaxPoints);

        return new GradingResponse
        {
            SubmissionId = submissionId,
            GradedBy = latestGrading.Key,
            ExaminerName = await GetExaminerNameAsync(latestGrading.Key),
            GradedAt = latestScores.Max(rs => rs.GradedAt),
            TotalScore = totalScore,
            MaxScore = maxScore,
            RubricScores = latestScores.Adapt<List<RubricScoreDetail>>(),
            Status = GradingStatus.FirstGrading // Simplified for now
        };
    }

    public async Task<List<AssignedExamResponse>> GetAssignedExamsAsync(Guid examinerId)
    {
        // Get all exam assignments for this examiner
        var assignments = await _context.ExaminerAssignments
            .AsNoTracking()
            .Where(ea => ea.ExaminerId == examinerId)
            .Include(ea => ea.ExamSession!)
                .ThenInclude(es => es.Exam!)
                    .ThenInclude(e => e.Subject)
            .Include(ea => ea.ExamSession!)
                .ThenInclude(es => es.Exam!)
                    .ThenInclude(e => e.Semester)
            .Include(ea => ea.ExamSession!)
                .ThenInclude(es => es.Exam!)
                    .ThenInclude(e => e.ExamSessions)
            .ToListAsync();

        var result = new List<AssignedExamResponse>();
        foreach (var assignment in assignments)
        {
            var exam = assignment.ExamSession?.Exam;
            if (exam == null) continue;

            // Get submission statistics from StorageService
            var submissions = await GetSubmissionsFromStorageServiceAsync(exam.Id);

            // Count graded submissions by this examiner
            var myGradedCount = await _context.RubricScores
                .AsNoTracking()
                .Where(rs => rs.GradedBy == examinerId &&
                            exam.RubricItems.Any(ri => ri.Id == rs.RubricItemId))
                .Select(rs => rs.SubmissionId)
                .Distinct()
                .CountAsync();

            result.Add(new AssignedExamResponse
            {
                ExamId = exam.Id,
                ExamName = exam.Title,
                SubjectId = exam.SubjectId,
                SubjectName = exam.Subject?.Name ?? string.Empty,
                SemesterId = exam.SemesterId,
                SemesterName = exam.Semester?.Code ?? string.Empty,
                TotalSubmissions = submissions.Count,
                PendingSubmissions = submissions.Count(s => s.Status == "Pending"),
                ProcessingSubmissions = submissions.Count(s => s.Status == "Processing"),
                GradedSubmissions = submissions.Count(s => s.Status == "Graded"),
                MyGradedSubmissions = myGradedCount
            });
        }

        return result;
    }

    public async Task<SubmissionDetailsResponse?> GetSubmissionDetailsAsync(Guid submissionId, Guid examinerId)
    {
        // Verify examiner is assigned
        if (!await IsExaminerAssignedAsync(submissionId, examinerId))
        {
            return null;
        }

        // Get submission details from StorageService
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null) return null;

        // Get exam details
        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null) return null;

        // Get all graders for this submission
        var allGraders = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .Select(rs => rs.GradedBy)
            .Distinct()
            .ToListAsync();

        var isSecondGrader = allGraders.Count == 1 && !allGraders.Contains(examinerId);

        // Get my existing grades
        var myGrades = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId && rs.GradedBy == examinerId)
            .Include(rs => rs.RubricItem)
            .ToListAsync();

        // Build rubrics with existing grades
        var rubrics = exam.RubricItems.OrderBy(ri => ri.DisplayOrder).Select(ri =>
        {
            var myGrade = myGrades.FirstOrDefault(g => g.RubricItemId == ri.Id);
            return new RubricGradingItemDto
            {
                RubricId = ri.Id,
                Criteria = ri.Criteria,
                Description = ri.Description,
                MaxPoints = ri.MaxPoints,
                Points = myGrade?.Points,
                Comments = myGrade?.Comments
            };
        }).ToList();

        // Build existing grades (all examiners)
        var allRubricScores = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .Include(rs => rs.RubricItem)
            .ToListAsync();

        var existingGrades = allRubricScores.Select(rs => new GradeResponseDto
        {
            GradeId = rs.Id,
            SubmissionId = rs.SubmissionId,
            ExaminerId = rs.GradedBy,
            ExaminerName = "Examiner", // Would get from IdentityService
            RubricId = rs.RubricItemId,
            RubricCriteria = rs.RubricItem?.Criteria ?? string.Empty,
            Points = rs.Points,
            MaxPoints = rs.RubricItem?.MaxPoints ?? 0,
            Comments = rs.Comments,
            GradedAt = rs.GradedAt,
            IsFinal = rs.IsFinal
        }).ToList();

        // Get violations from StorageService
        var violations = await GetViolationsFromStorageServiceAsync(submissionId);

        // Build file download URL
        var fileDownloadUrl = $"{_configuration["Services:StorageService"]}/api/submissions/{submissionId}/download";

        return new SubmissionDetailsResponse
        {
            SubmissionId = submissionId,
            SessionId = submission.SessionId,
            ExamId = exam.Id,
            ExamName = exam.Title,
            StudentId = submission.StudentId,
            StudentName = submission.StudentName,
            FileName = submission.FileName,
            FileDownloadUrl = fileDownloadUrl,
            SubmissionTime = submission.SubmissionTime,
            Status = submission.Status,
            Violations = violations,
            Rubrics = rubrics,
            ExistingGrades = existingGrades,
            CanEdit = !myGrades.Any(g => g.IsFinal),
            IsSecondGrader = isSecondGrader
        };
    }

    public async Task<List<RubricResponseDto>> GetExamRubricsAsync(Guid examId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null) return new List<RubricResponseDto>();

        return exam.RubricItems.OrderBy(ri => ri.DisplayOrder).Select(ri => new RubricResponseDto
        {
            RubricId = ri.Id,
            ExamId = examId,
            Criteria = ri.Criteria,
            Description = ri.Description,
            MaxPoints = ri.MaxPoints
        }).ToList();
    }

    public async Task<GradeResponseDto?> UpdateGradeAsync(Guid gradeId, UpdateGradeRequest request, Guid examinerId)
    {
        var rubricScore = await _context.RubricScores
            .Include(rs => rs.RubricItem)
            .FirstOrDefaultAsync(rs => rs.Id == gradeId);

        if (rubricScore == null) return null;

        // Verify it belongs to this examiner
        if (rubricScore.GradedBy != examinerId)
        {
            throw new UnauthorizedAccessException("Cannot update grade from another examiner");
        }

        // Check if editable (not finalized)
        if (rubricScore.IsFinal)
        {
            throw new InvalidOperationException("Cannot update finalized grade");
        }

        // Validate points
        if (request.Points > rubricScore.RubricItem!.MaxPoints)
        {
            throw new InvalidOperationException(
                $"Points ({request.Points}) exceed maximum ({rubricScore.RubricItem.MaxPoints}) for rubric: {rubricScore.RubricItem.Criteria}");
        }
        if (request.Points < 0)
        {
            throw new InvalidOperationException("Points cannot be negative");
        }

        // Update grade
        rubricScore.Points = request.Points;
        rubricScore.Comments = request.Comments;
        rubricScore.GradedAt = DateTime.UtcNow;

        // If this is part of double grading, recalculate and check for review
        var submissionId = rubricScore.SubmissionId;
        var allGraders = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .Select(rs => rs.GradedBy)
            .Distinct()
            .CountAsync();

        if (allGraders >= 2)
        {
            var requiresReview = await RequiresModeratorReviewAsync(submissionId);
            if (requiresReview)
            {
                // Update submission status to flagged (would need to call StorageService)
                await UpdateSubmissionStatusInStorageServiceAsync(submissionId, "Flagged");
            }
        }

        await _context.SaveChangesAsync();

        return new GradeResponseDto
        {
            GradeId = rubricScore.Id,
            SubmissionId = rubricScore.SubmissionId,
            ExaminerId = rubricScore.GradedBy,
            RubricId = rubricScore.RubricItemId,
            RubricCriteria = rubricScore.RubricItem.Criteria,
            Points = rubricScore.Points,
            MaxPoints = rubricScore.RubricItem.MaxPoints,
            Comments = rubricScore.Comments,
            GradedAt = rubricScore.GradedAt,
            IsFinal = rubricScore.IsFinal
        };
    }

    public async Task<GradingResultResponse> MarkZeroDueToViolationsAsync(Guid submissionId, MarkZeroRequest request, Guid examinerId)
    {
        // Verify examiner is assigned
        if (!await IsExaminerAssignedAsync(submissionId, examinerId))
        {
            throw new UnauthorizedAccessException("Examiner is not assigned to grade this submission");
        }

        // Get submission and exam details
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null)
        {
            throw new KeyNotFoundException("Submission not found");
        }

        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null)
        {
            throw new InvalidOperationException("Exam not found");
        }

        // Verify it has violations
        var violations = await GetViolationsFromStorageServiceAsync(submissionId);
        if (!violations.Any())
        {
            throw new InvalidOperationException("Submission has no violations. Cannot mark as zero.");
        }

        // Check if examiner already has grades - if so, delete them
        var existingGrades = await _context.RubricScores
            .Where(rs => rs.SubmissionId == submissionId && rs.GradedBy == examinerId)
            .ToListAsync();

        foreach (var existingGrade in existingGrades)
        {
            if (existingGrade.IsFinal)
            {
                throw new InvalidOperationException("Cannot mark zero: submission already has finalized grades");
            }
            _context.RubricScores.Remove(existingGrade);
        }

        // Create zero grades for all rubrics with justification
        var createdGrades = new List<RubricScore>();
        var comments = $"Zero score due to violations: {string.Join("; ", violations.Select(v => $"{v.ViolationType}: {v.Description}"))}. Examiner justification: {request.Reason}";

        foreach (var rubricItem in exam.RubricItems)
        {
            var grade = new RubricScore
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                RubricItemId = rubricItem.Id,
                GradedBy = examinerId,
                Points = 0,
                Comments = comments,
                GradedAt = DateTime.UtcNow,
                IsFinal = false
            };
            _context.RubricScores.Add(grade);
            createdGrades.Add(grade);
        }

        // Update submission status to Flagged for moderator review
        await UpdateSubmissionStatusInStorageServiceAsync(submissionId, "Flagged");

        await _context.SaveChangesAsync();

        return new GradingResultResponse
        {
            Success = true,
            Message = "Submission marked as zero due to violations. Awaiting moderator review.",
            SubmissionId = submissionId,
            NewStatus = "Flagged",
            RequiresModeratorReview = true,
            CreatedGrades = createdGrades.Select(g => new GradeResponseDto
            {
                GradeId = g.Id,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.GradedBy,
                RubricId = g.RubricItemId,
                Points = g.Points,
                Comments = g.Comments,
                GradedAt = g.GradedAt,
                IsFinal = g.IsFinal
            }).ToList()
        };
    }

    public async Task<GradingStatusResponse?> GetGradingStatusAsync(Guid submissionId)
    {
        // Get submission details
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null) return null;

        // Get exam details
        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null) return null;

        // Get all rubric scores for this submission
        var allRubricScores = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .ToListAsync();

        var allGraders = allRubricScores.Select(rs => rs.GradedBy).Distinct().ToList();
        var hasViolations = (await GetViolationsFromStorageServiceAsync(submissionId)).Any();

        // Calculate max possible score
        var maxPossibleScore = exam.RubricItems.Sum(ri => ri.MaxPoints);

        // Get grader summaries
        var graderSummaries = allGraders.Select(examinerId =>
        {
            var graderGrades = allRubricScores.Where(rs => rs.GradedBy == examinerId).ToList();
            return new ExaminerGradingSummaryDto
            {
                ExaminerId = examinerId,
                ExaminerName = "Examiner", // Would get from IdentityService
                TotalScore = graderGrades.Sum(rs => rs.Points),
                RubricsGraded = graderGrades.Count,
                LastGradedAt = graderGrades.Any() ? graderGrades.Max(rs => rs.GradedAt) : null
            };
        }).ToList();

        var averageScore = await CalculateAverageScoreAsync(submissionId);
        var requiresReview = await RequiresModeratorReviewAsync(submissionId);

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

    public async Task<bool> IsExaminerAssignedAsync(Guid submissionId, Guid examinerId)
    {
        // Get submission details
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null) return false;

        // Check if examiner is assigned to the exam
        var isAssigned = await _context.ExaminerAssignments
            .AsNoTracking()
            .AnyAsync(ea => ea.ExamSession.ExamId == submission.ExamId && ea.ExaminerId == examinerId);

        return isAssigned;
    }

    public async Task<bool> ValidateAllRubricsGradedAsync(Guid submissionId, Guid examinerId)
    {
        // Get submission and exam details
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null) return false;

        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null) return false;

        var examRubricIds = exam.RubricItems.Select(ri => ri.Id).ToHashSet();
        var gradedRubricIds = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId && rs.GradedBy == examinerId)
            .Select(rs => rs.RubricItemId)
            .ToListAsync();

        return examRubricIds.SetEquals(gradedRubricIds);
    }

    public async Task<decimal?> CalculateAverageScoreAsync(Guid submissionId)
    {
        var graderScores = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .GroupBy(rs => rs.GradedBy)
            .Select(g => new { ExaminerId = g.Key, TotalScore = g.Sum(rs => rs.Points) })
            .ToListAsync();

        if (graderScores.Count < 2)
        {
            return null; // Need at least 2 graders to calculate average
        }

        var average = graderScores.Average(s => s.TotalScore);
        return Math.Round(average, 2);
    }

    public async Task<bool> RequiresModeratorReviewAsync(Guid submissionId)
    {
        // Get all grades grouped by examiner
        var graderScores = await _context.RubricScores
            .AsNoTracking()
            .Where(rs => rs.SubmissionId == submissionId)
            .GroupBy(rs => rs.GradedBy)
            .Select(g => new { ExaminerId = g.Key, TotalScore = g.Sum(rs => rs.Points) })
            .ToListAsync();

        if (graderScores.Count != 2)
        {
            return false; // Only check when exactly 2 graders
        }

        // Get max possible score
        var submission = await GetSubmissionFromStorageServiceAsync(submissionId);
        if (submission == null) return false;

        var exam = await _examRepository.GetByIdAsync(submission.ExamId);
        if (exam == null) return false;

        var maxPossibleScore = exam.RubricItems.Sum(ri => ri.MaxPoints);
        if (maxPossibleScore == 0) return false;

        var scores = graderScores.Select(s => s.TotalScore).OrderBy(s => s).ToList();
        var score1 = scores[0];
        var score2 = scores[1];

        // Calculate difference as percentage of max possible score
        var difference = Math.Abs(score2 - score1);
        var differencePercentage = (difference / maxPossibleScore) * 100;

        // Flag for review if difference > 20%
        return differencePercentage > 20;
    }

    public async Task<ExamSubmissionsResponse> GetExamSubmissionsAsync(Guid examId, Guid examinerId, int pageNumber = 1, int pageSize = 20)
    {
        // Validate exam exists
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam {examId} not found");
        }

        // Get submissions for this exam from StorageService
        // In a real implementation, this would call StorageService API
        var submissions = await GetSubmissionsFromStorageServiceAsync(examId);

        // Get grading information for each submission
        var submissionIds = submissions.Select(s => s.SubmissionId).ToList();
        var rubricScores = await _context.RubricScores
            .Where(rs => submissionIds.Contains(rs.SubmissionId))
            .ToListAsync();

        // Group rubric scores by submission
        var gradingBySubmission = rubricScores
            .GroupBy(rs => rs.SubmissionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build response items
        var items = new List<SubmissionListItemDto>();
        foreach (var submission in submissions)
        {
            var submissionRubricScores = gradingBySubmission.GetValueOrDefault(submission.SubmissionId, new List<RubricScore>());

            // Group by examiner
            var examinerGroups = submissionRubricScores.GroupBy(rs => rs.GradedBy).ToList();

            // Check if current examiner has graded
            var isGradedByMe = examinerGroups.Any(g => g.Key == examinerId);
            var isGradedByOthers = examinerGroups.Any(g => g.Key != examinerId);
            var gradingCount = examinerGroups.Count;

            // Determine grading status
            var gradingStatus = DetermineSubmissionGradingStatus(submissionRubricScores, examinerId);

            // Get current score (most recent grading)
            decimal? currentScore = null;
            if (examinerGroups.Any())
            {
                var latestGrading = examinerGroups.OrderByDescending(g => g.Max(rs => rs.GradedAt)).First();
                currentScore = latestGrading.Sum(rs => rs.Points);
            }

            items.Add(new SubmissionListItemDto
            {
                SubmissionId = submission.SubmissionId,
                SessionId = submission.SessionId,
                StudentId = submission.StudentId,
                StudentName = submission.StudentName,
                FileName = submission.FileName,
                SubmissionTime = submission.SubmissionTime,
                Status = submission.Status,
                HasViolations = submission.HasViolations,
                IsGradedByMe = isGradedByMe,
                IsGradedByOthers = isGradedByOthers,
                GradingCount = gradingCount,
                RequiresDoubleGrading = exam.RubricItems.Any(), // Simplified - all exams require double grading if they have rubrics
                CurrentScore = currentScore,
                GradingStatus = gradingStatus
            });
        }

        // Apply pagination
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var paginatedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ExamSubmissionsResponse
        {
            Items = paginatedItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<IEnumerable<GradingResponse>> GetExaminerAssignedSubmissionsAsync(Guid examinerId)
    {
        // Get exams assigned to this examiner
        var assignedExams = await _context.ExaminerAssignments
            .Where(ea => ea.ExaminerId == examinerId && ea.IsActive)
            .Select(ea => ea.ExamSession.ExamId)
            .Distinct()
            .ToListAsync();

        if (!assignedExams.Any()) return new List<GradingResponse>();

        // Get submissions for these exams that need grading
        // This would require calling StorageService to get submissions
        // For now, return empty list as this needs cross-service communication
        return new List<GradingResponse>();
    }

    public async Task<bool> FinalizeGradesAsync(Guid submissionId, Guid moderatorId)
    {
        // Mark grades as finalized by moderator
        var rubricScores = await _context.RubricScores
            .Where(rs => rs.SubmissionId == submissionId)
            .ToListAsync();

        if (!rubricScores.Any()) return false;

        // Update submission status to finalized
        await UpdateSubmissionStatusAsync(submissionId, new GradingStatusResult { Status = GradingStatus.Finalized });

        _logger.LogInformation("Grades finalized for submission {SubmissionId} by moderator {ModeratorId}",
            submissionId, moderatorId);

        return true;
    }

    public async Task<IEnumerable<GradingResponse>> GetSubmissionsRequiringReviewAsync()
    {
        // Get submissions that require moderator review
        // This would need to query StorageService for submissions with specific status
        return new List<GradingResponse>();
    }

    // Helper methods
    private async Task<SubmissionInfo> ValidateSubmissionAsync(Guid submissionId)
    {
        var storageServiceUrl = _configuration["Services:StorageService"];
        var response = await _httpClient.GetAsync($"{storageServiceUrl}/api/submissions/{submissionId}");

        if (!response.IsSuccessStatusCode)
        {
            throw new KeyNotFoundException($"Submission {submissionId} not found");
        }

        // Parse response to get submission details
        // For now, assume submission exists and return mock data
        return new SubmissionInfo { ExamId = Guid.NewGuid(), Status = "Pending" };
    }

    private class SubmissionInfo
    {
        public Guid ExamId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private async Task ValidateExaminerAssignmentAsync(Guid examId, Guid examinerId)
    {
        var isAssigned = await _context.ExaminerAssignments
            .AnyAsync(ea => ea.ExamSession.ExamId == examId &&
                           ea.ExaminerId == examinerId &&
                           ea.IsActive);

        if (!isAssigned)
        {
            throw new InvalidOperationException($"Examiner {examinerId} is not assigned to exam {examId}");
        }
    }

    private async Task ValidateRubricScoresAsync(GradingRequest request, Exam exam)
    {
        var rubricItemIds = request.RubricScores.Select(rs => rs.RubricItemId).ToList();
        var validRubrics = await _context.RubricItems
            .Where(ri => ri.ExamId == exam.Id && rubricItemIds.Contains(ri.Id))
            .ToListAsync();

        if (validRubrics.Count() != request.RubricScores.Count)
        {
            throw new ArgumentException("Invalid rubric items specified");
        }

        foreach (var scoreRequest in request.RubricScores)
        {
            var rubric = validRubrics.First(ri => ri.Id == scoreRequest.RubricItemId);
            if (scoreRequest.Points < 0 || scoreRequest.Points > rubric.MaxPoints)
            {
                throw new ArgumentException($"Points for rubric '{rubric.Criteria}' must be between 0 and {rubric.MaxPoints}");
            }
        }
    }

    private async Task<GradingStatusResult> DetermineGradingStatusAsync(Guid submissionId, bool isFirstGrading)
    {
        var rubricScores = await _context.RubricScores
            .Where(rs => rs.SubmissionId == submissionId)
            .ToListAsync();

        var examinerCount = rubricScores.Select(rs => rs.GradedBy).Distinct().Count();

        if (examinerCount == 1)
        {
            return new GradingStatusResult { Status = GradingStatus.FirstGrading };
        }

        // Check for significant differences between examiners
        var examinerGroups = rubricScores.GroupBy(rs => rs.GradedBy).ToList();
        if (examinerGroups.Count() >= 2)
        {
            var scores1 = examinerGroups[0].Sum(rs => rs.Points);
            var scores2 = examinerGroups[1].Sum(rs => rs.Points);
            var difference = Math.Abs(scores1 - scores2);
            var average = (scores1 + scores2) / 2;

            if (difference > average * 0.2m) // 20% difference threshold
            {
                return new GradingStatusResult
                {
                    Status = GradingStatus.AwaitingModeratorReview,
                    RequiresModeratorReview = true,
                    ModeratorReviewReason = $"Score difference of {difference:F2} points between examiners"
                };
            }
        }

        return new GradingStatusResult { Status = GradingStatus.Finalized };
    }

    private async Task UpdateSubmissionStatusAsync(Guid submissionId, GradingStatusResult gradingStatus)
    {
        var storageServiceUrl = _configuration["Services:StorageService"];
        var statusUpdate = new
        {
            SubmissionId = submissionId,
            Status = gradingStatus.Status.ToString(),
            RequiresModeratorReview = gradingStatus.RequiresModeratorReview
        };

        // This would make an HTTP call to StorageService to update submission status
        // For now, just log it
        _logger.LogInformation("Would update submission {SubmissionId} status to {Status}",
            submissionId, gradingStatus.Status);
    }

    private async Task<Exam> GetExamFromSubmissionAsync(Guid submissionId)
    {
        // Get exam ID from submission (would need StorageService call)
        // For now, get from existing rubric scores
        var examId = await _context.RubricScores
            .Where(rs => rs.SubmissionId == submissionId)
            .Select(rs => rs.RubricItem.ExamId)
            .FirstOrDefaultAsync();

        return await _examRepository.GetByIdAsync(examId) ?? throw new KeyNotFoundException("Exam not found");
    }

    private async Task<string> GetExaminerNameAsync(Guid examinerId)
    {
        // This would call IdentityService to get examiner name
        // For now, return placeholder
        return $"Examiner {examinerId}";
    }

    private async Task<List<StorageSubmissionDto>> GetSubmissionsFromStorageServiceAsync(Guid examId)
    {
        // In a real implementation, this would make an HTTP call to StorageService
        // For now, return mock data to demonstrate the functionality
        var storageServiceUrl = _configuration["Services:StorageService"];

        // Mock implementation - in reality this would call:
        // GET {storageServiceUrl}/api/submissions/exam/{examId}

        return new List<StorageSubmissionDto>
        {
            new StorageSubmissionDto
            {
                SubmissionId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                StudentId = "ST001",
                StudentName = "John Doe",
                FileName = "assignment.docx",
                SubmissionTime = DateTime.Now.AddDays(-1),
                Status = "Submitted",
                HasViolations = false
            },
            new StorageSubmissionDto
            {
                SubmissionId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                StudentId = "ST002",
                StudentName = "Jane Smith",
                FileName = "homework.pdf",
                SubmissionTime = DateTime.Now.AddDays(-2),
                Status = "Graded",
                HasViolations = true
            }
        };
    }

    private GradingStatus DetermineSubmissionGradingStatus(List<RubricScore> rubricScores, Guid currentExaminerId)
    {
        if (!rubricScores.Any())
        {
            return GradingStatus.NotGraded;
        }

        // Check if marked as zero (all rubrics have 0 points and contain violation text)
        var allZeroWithViolationText = rubricScores.All(rs =>
            rs.Points == 0 && rs.Comments != null &&
            rs.Comments.Contains("Zero score due to violations"));

        if (allZeroWithViolationText)
        {
            return GradingStatus.MarkedZero;
        }

        var examinerGroups = rubricScores.GroupBy(rs => rs.GradedBy).ToList();
        var examinerCount = examinerGroups.Count;

        if (examinerCount == 1)
        {
            return GradingStatus.FirstGrading;
        }

        // Check for significant differences between examiners (>20% of max score)
        if (examinerGroups.Count >= 2)
        {
            var scores = examinerGroups.Select(g => g.Sum(rs => rs.Points)).OrderBy(s => s).ToList();
            var score1 = scores[0];
            var score2 = scores[1];
            var difference = Math.Abs(score2 - score1);

            // Get max possible score for this submission
            var maxScore = rubricScores.First().RubricItem?.MaxPoints ?? 0;
            var totalRubrics = examinerGroups.First().Count();
            var maxPossibleScore = maxScore * totalRubrics;

            if (maxPossibleScore > 0)
            {
                var differencePercentage = (difference / maxPossibleScore) * 100;
                if (differencePercentage > 20)
                {
                    return GradingStatus.AwaitingModeratorReview;
                }
            }
        }

        return GradingStatus.Finalized;
    }

    private class GradingStatusResult
    {
        public GradingStatus Status { get; set; }
        public bool RequiresModeratorReview { get; set; }
        public string? ModeratorReviewReason { get; set; }
    }

    private async Task<StorageSubmissionDto?> GetSubmissionFromStorageServiceAsync(Guid submissionId)
    {
        // In a real implementation, this would make an HTTP call to StorageService
        // GET {storageServiceUrl}/api/submissions/{submissionId}

        // Mock implementation for demonstration
        return new StorageSubmissionDto
        {
            SubmissionId = submissionId,
            SessionId = Guid.NewGuid(),
            StudentId = "ST001",
            StudentName = "John Doe",
            FileName = "assignment.docx",
            SubmissionTime = DateTime.Now.AddDays(-1),
            Status = "Submitted",
            HasViolations = false,
            ExamId = Guid.NewGuid() // Would be retrieved from StorageService
        };
    }

    private async Task<List<ViolationItemDto>> GetViolationsFromStorageServiceAsync(Guid submissionId)
    {
        // In a real implementation, this would make an HTTP call to StorageService
        // GET {storageServiceUrl}/api/submissions/{submissionId}/violations

        // Mock implementation for demonstration
        return new List<ViolationItemDto>
        {
            new ViolationItemDto
            {
                ViolationId = Guid.NewGuid(),
                ViolationType = "FileSize",
                Description = "File size exceeds limit",
                Severity = "Medium",
                DetectedAt = DateTime.Now.AddHours(-1)
            }
        };
    }

    private async Task UpdateSubmissionStatusInStorageServiceAsync(Guid submissionId, string status)
    {
        // In a real implementation, this would make an HTTP call to StorageService
        // PUT {storageServiceUrl}/api/submissions/{submissionId}/status
        // Body: { "status": status }

        // Mock implementation - in real scenario this would be an HTTP call
        _logger.LogInformation("Would update submission {SubmissionId} status to {Status}", submissionId, status);
    }

    private class StorageSubmissionDto
    {
        public Guid SubmissionId { get; set; }
        public Guid SessionId { get; set; }
        public Guid ExamId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasViolations { get; set; }
    }

    public async Task<Grade?> GetByIdAsync(Guid id)
    {
        return await _gradeRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId)
    {
        return await _gradeRepository.GetByExamIdAsync(examId);
    }

    public async Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId)
    {
        return await _gradeRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<Grade> CreateOrUpdateGradeAsync(Guid examId, Guid studentId, decimal score, string? feedback, Guid gradedBy)
    {
        // Validate exam exists
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            _logger.LogWarning("Exam {ExamId} not found", examId);
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        // Validate score
        if (score < 0 || score > exam.TotalMarks)
        {
            _logger.LogWarning("Invalid score {Score} for exam {ExamId} with max {MaxScore}", score, examId, exam.TotalMarks);
            throw new ArgumentException($"Score must be between 0 and {exam.TotalMarks}", nameof(score));
        }

        // Check if grade already exists
        var existingGrade = await _gradeRepository.GetByExamAndStudentAsync(examId, studentId);

        if (existingGrade != null)
        {
            // Update existing grade
            existingGrade.Score = score;
            existingGrade.MaxScore = exam.TotalMarks;
            existingGrade.Feedback = feedback;
            existingGrade.GradedBy = gradedBy;
            existingGrade.GradedAt = DateTime.UtcNow;

            var updated = await _gradeRepository.UpdateAsync(existingGrade);
            _logger.LogInformation("Grade updated for student {StudentId} in exam {ExamId}", studentId, examId);
            
            return updated;
        }
        else
        {
            // Create new grade
            var grade = new Grade
            {
                ExamId = examId,
                StudentId = studentId,
                Score = score,
                MaxScore = exam.TotalMarks,
                Feedback = feedback,
                GradedBy = gradedBy,
                GradedAt = DateTime.UtcNow
            };

            var created = await _gradeRepository.CreateAsync(grade);
            _logger.LogInformation("Grade created for student {StudentId} in exam {ExamId}", studentId, examId);
            
            return created;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _gradeRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Grade {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Grade {Id} not found for deletion", id);
        }
        
        return deleted;
    }
}
