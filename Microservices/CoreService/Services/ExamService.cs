using CoreService.DTOs;
using CoreService.Entities;
using CoreService.Repositories;
using Mapster;

namespace CoreService.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IExamSessionRepository _examSessionRepository;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamRepository examRepository,
        ISubjectRepository subjectRepository,
        ISemesterRepository semesterRepository,
        IExamSessionRepository examSessionRepository,
        ILogger<ExamService> logger)
    {
        _examRepository = examRepository;
        _subjectRepository = subjectRepository;
        _semesterRepository = semesterRepository;
        _examSessionRepository = examSessionRepository;
        _logger = logger;

        // Configure Mapster mappings
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        TypeAdapterConfig<Exam, ExamDto>
            .NewConfig()
            .Map(dest => dest.SubjectName, src => src.Subject != null ? src.Subject.Name : string.Empty)
            .Map(dest => dest.SemesterCode, src => src.Semester != null ? src.Semester.Code : string.Empty)
            .Map(dest => dest.RubricItems, src => src.RubricItems.Adapt<List<RubricItemDto>>())
            .Map(dest => dest.ExamSessionsCount, src => src.ExamSessions.Count);

        TypeAdapterConfig<RubricItem, RubricItemDto>
            .NewConfig();
    }

    public async Task<ExamDto?> GetByIdAsync(Guid id)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        return exam?.Adapt<ExamDto>();
    }

    public async Task<IEnumerable<ExamDto>> GetAllAsync()
    {
        var exams = await _examRepository.GetAllAsync();
        return exams.Adapt<IEnumerable<ExamDto>>();
    }

    public async Task<IEnumerable<ExamDto>> GetBySubjectIdAsync(Guid subjectId)
    {
        var exams = await _examRepository.GetBySubjectIdAsync(subjectId);
        return exams.Adapt<IEnumerable<ExamDto>>();
    }

    public async Task<IEnumerable<ExamDto>> GetBySemesterIdAsync(Guid semesterId)
    {
        var exams = await _examRepository.GetBySemesterIdAsync(semesterId);
        return exams.Adapt<IEnumerable<ExamDto>>();
    }

    public async Task<ExamDto> CreateAsync(string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks)
    {
        // Check for duplicate title
        if (await _examRepository.TitleExistsAsync(title))
        {
            throw new InvalidOperationException($"Exam with title '{title}' already exists");
        }

        // Validate subject exists
        var subjectExists = await _subjectRepository.ExistsAsync(subjectId);
        if (!subjectExists)
        {
            _logger.LogWarning("Subject {SubjectId} not found", subjectId);
            throw new KeyNotFoundException($"Subject with ID {subjectId} not found");
        }

        // Validate duration
        if (durationMinutes < 15 || durationMinutes > 360)
        {
            _logger.LogWarning("Invalid duration: {Duration}", durationMinutes);
            throw new ArgumentException("Duration must be between 15 and 360 minutes", nameof(durationMinutes));
        }

        // Validate total marks
        if (totalMarks <= 0 || totalMarks > 1000)
        {
            _logger.LogWarning("Invalid total marks: {TotalMarks}", totalMarks);
            throw new ArgumentException("Total marks must be between 0 and 1000", nameof(totalMarks));
        }

        var exam = new Exam
        {
            Title = title,
            Description = description,
            SubjectId = subjectId,
            SemesterId = semesterId,
            ExamDate = examDate,
            DurationMinutes = durationMinutes,
            TotalMarks = totalMarks
        };

        var created = await _examRepository.CreateAsync(exam);
        _logger.LogInformation("Exam {Title} created successfully", title);

        return created.Adapt<ExamDto>();
    }

    public async Task<ExamDto> UpdateAsync(Guid id, string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam == null)
        {
            _logger.LogWarning("Exam {Id} not found", id);
            throw new KeyNotFoundException($"Exam with ID {id} not found");
        }

        // Validate subject exists
        var subjectExists = await _subjectRepository.ExistsAsync(subjectId);
        if (!subjectExists)
        {
            _logger.LogWarning("Subject {SubjectId} not found", subjectId);
            throw new KeyNotFoundException($"Subject with ID {subjectId} not found");
        }

        // Validate semester exists and exam date is within semester
        var semester = await _semesterRepository.GetByIdAsync(semesterId);
        if (semester == null)
        {
            _logger.LogWarning("Semester {SemesterId} not found", semesterId);
            throw new KeyNotFoundException($"Semester with ID {semesterId} not found");
        }

        if (examDate < semester.StartDate || examDate > semester.EndDate)
        {
            _logger.LogWarning("Exam date {ExamDate} is outside semester period {StartDate} - {EndDate}",
                examDate, semester.StartDate, semester.EndDate);
            throw new ArgumentException("Exam date must be within the selected semester period", nameof(examDate));
        }

        // Validate duration (Use Case specifies 30-300 minutes)
        if (durationMinutes < 30 || durationMinutes > 300)
        {
            _logger.LogWarning("Invalid duration: {Duration}", durationMinutes);
            throw new ArgumentException("Duration must be between 30 and 300 minutes", nameof(durationMinutes));
        }

        // Validate total marks
        if (totalMarks <= 0 || totalMarks > 1000)
        {
            _logger.LogWarning("Invalid total marks: {TotalMarks}", totalMarks);
            throw new ArgumentException("Total marks must be between 0 and 1000", nameof(totalMarks));
        }

        exam.Title = title;
        exam.Description = description;
        exam.SubjectId = subjectId;
        exam.SemesterId = semesterId;
        exam.ExamDate = examDate;
        exam.DurationMinutes = durationMinutes;
        exam.TotalMarks = totalMarks;

        var updated = await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Exam {Id} updated successfully", id);

        return updated.Adapt<ExamDto>();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _examRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Exam {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Exam {Id} not found for deletion", id);
        }
        
        return deleted;
    }

    // Rubric Management
    public async Task<RubricItemDto> AddRubricItemAsync(Guid examId, string criteria, string? description, decimal maxPoints)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        if (exam.Status != "Draft")
        {
            throw new InvalidOperationException("Cannot modify rubrics for non-draft exams");
        }

        // Get next display order
        var existingRubrics = exam.RubricItems;
        var nextOrder = existingRubrics.Any() ? existingRubrics.Max(r => r.DisplayOrder) + 1 : 1;

        // Validate that adding this rubric won't exceed exam total marks
        var currentTotalRubricPoints = existingRubrics.Sum(r => r.MaxPoints);
        if (currentTotalRubricPoints + maxPoints > exam.TotalMarks)
        {
            throw new InvalidOperationException($"Adding this rubric would exceed the exam's total marks. Current total: {currentTotalRubricPoints}, Adding: {maxPoints}, Exam total: {exam.TotalMarks}");
        }

        var rubricItem = new RubricItem
        {
            ExamId = examId,
            Criteria = criteria,
            Description = description,
            MaxPoints = maxPoints,
            DisplayOrder = nextOrder
        };

        // Note: This would need to be saved via repository
        // For now, we'll assume the rubric is added to the exam's collection
        exam.RubricItems.Add(rubricItem);

        await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Rubric item '{Criteria}' added to exam {ExamId}", criteria, examId);

        return rubricItem.Adapt<RubricItemDto>();
    }

    public async Task<RubricItemDto> UpdateRubricItemAsync(Guid examId, Guid rubricItemId, string criteria, string? description, decimal maxPoints)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        if (exam.Status != "Draft")
        {
            throw new InvalidOperationException("Cannot modify rubrics for non-draft exams");
        }

        var rubricItem = exam.RubricItems.FirstOrDefault(r => r.Id == rubricItemId);
        if (rubricItem == null)
        {
            throw new KeyNotFoundException($"Rubric item with ID {rubricItemId} not found in exam {examId}");
        }

        // Calculate new total: current total - old maxPoints + new maxPoints
        var currentTotalRubricPoints = exam.RubricItems.Sum(r => r.MaxPoints);
        var newTotalRubricPoints = currentTotalRubricPoints - rubricItem.MaxPoints + maxPoints;

        // Validate that the new total won't exceed exam total marks
        if (newTotalRubricPoints > exam.TotalMarks)
        {
            throw new InvalidOperationException($"Updating this rubric would exceed the exam's total marks. New total: {newTotalRubricPoints}, Exam total: {exam.TotalMarks}");
        }

        // Update the rubric item
        rubricItem.Criteria = criteria;
        rubricItem.Description = description;
        rubricItem.MaxPoints = maxPoints;

        await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Rubric item {RubricItemId} updated in exam {ExamId}", rubricItemId, examId);

        return rubricItem.Adapt<RubricItemDto>();
    }

    public async Task<IEnumerable<RubricItemDto>> AddRubricItemsAsync(Guid examId, IEnumerable<(string Criteria, string? Description, decimal MaxPoints)> rubricItems)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        if (exam.Status != "Draft")
        {
            throw new InvalidOperationException("Cannot modify rubrics for non-draft exams");
        }

        var rubricItemList = rubricItems.ToList();
        if (!rubricItemList.Any())
        {
            throw new ArgumentException("At least one rubric item must be provided", nameof(rubricItems));
        }

        // Get next display order
        var existingRubrics = exam.RubricItems;
        var nextOrder = existingRubrics.Any() ? existingRubrics.Max(r => r.DisplayOrder) + 1 : 1;

        // Calculate total points that would be added
        var totalPointsToAdd = rubricItemList.Sum(r => r.MaxPoints);

        // Validate that adding all rubrics won't exceed exam total marks
        var currentTotalRubricPoints = existingRubrics.Sum(r => r.MaxPoints);
        if (currentTotalRubricPoints + totalPointsToAdd > exam.TotalMarks)
        {
            throw new InvalidOperationException($"Adding these rubrics would exceed the exam's total marks. Current total: {currentTotalRubricPoints}, Adding: {totalPointsToAdd}, Exam total: {exam.TotalMarks}");
        }

        var addedRubrics = new List<RubricItem>();

        foreach (var (criteria, description, maxPoints) in rubricItemList)
        {
            var rubricItem = new RubricItem
            {
                ExamId = examId,
                Criteria = criteria,
                Description = description,
                MaxPoints = maxPoints,
                DisplayOrder = nextOrder++
            };

            exam.RubricItems.Add(rubricItem);
            addedRubrics.Add(rubricItem);
        }

        await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Added {Count} rubric items to exam {ExamId}", rubricItemList.Count, examId);

        return addedRubrics.Select(r => r.Adapt<RubricItemDto>());
    }

    public async Task<bool> RemoveRubricItemAsync(Guid examId, Guid rubricItemId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        if (exam.Status != "Draft")
        {
            throw new InvalidOperationException("Cannot modify rubrics for non-draft exams");
        }

        var rubricItem = exam.RubricItems.FirstOrDefault(r => r.Id == rubricItemId);
        if (rubricItem == null)
        {
            return false;
        }

        exam.RubricItems.Remove(rubricItem);
        await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Rubric item {RubricItemId} removed from exam {ExamId}", rubricItemId, examId);

        return true;
    }

    public async Task<ExamDto> PublishExamAsync(Guid examId)
    {
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        if (exam.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft exams can be published");
        }

        // Validate rubrics exist
        if (!exam.RubricItems.Any())
        {
            throw new InvalidOperationException("Cannot publish exam without rubric items");
        }

        // Validate total rubric points equal exam total marks
        var totalRubricPoints = exam.RubricItems.Sum(r => r.MaxPoints);
        if (totalRubricPoints != exam.TotalMarks)
        {
            throw new InvalidOperationException($"Total rubric points ({totalRubricPoints}) must equal exam total marks ({exam.TotalMarks})");
        }

        exam.Status = "Active";
        exam.UpdatedAt = DateTime.UtcNow;

        // Create default exam session when publishing
        var examSession = new ExamSession
        {
            ExamId = examId,
            SessionName = $"{exam.Title} - Main Session",
            ScheduledDate = exam.ExamDate,
            Location = "Default Location",
            MaxStudents = 100, // Default value
            IsActive = true
        };

        await _examSessionRepository.CreateAsync(examSession);
        _logger.LogInformation("Created exam session {SessionId} for published exam {ExamId}", examSession.Id, examId);

        var updated = await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Exam {ExamId} published successfully", examId);

        return updated.Adapt<ExamDto>();
    }

    // Examiner Assignment
    public async Task<ExaminerAssignment> AssignExaminerAsync(Guid examSessionId, Guid examinerId, string role = "Examiner")
    {
        // Validate exam session exists
        var examSession = await _examSessionRepository.GetByIdAsync(examSessionId);
        if (examSession == null)
        {
            throw new KeyNotFoundException($"Exam session with ID {examSessionId} not found");
        }

        // Check if examiner is already assigned to this session
        var existingAssignment = examSession.ExaminerAssignments?.FirstOrDefault(ea => ea.ExaminerId == examinerId);
        if (existingAssignment != null)
        {
            throw new InvalidOperationException($"Examiner {examinerId} is already assigned to this exam session");
        }

        // Validate maximum examiners per exam (Use Case specifies max 5)
        var examAssignments = examSession.ExaminerAssignments?.Count ?? 0;
        if (examAssignments >= 5)
        {
            throw new InvalidOperationException("Maximum 5 examiners can be assigned to an exam");
        }

        var assignment = new ExaminerAssignment
        {
            ExamSessionId = examSessionId,
            ExaminerId = examinerId,
            Role = role,
            IsActive = true
        };

        // Note: This would need to be saved via repository
        // For now, we'll assume the assignment is added to the session's collection
        if (examSession.ExaminerAssignments == null)
        {
            examSession.ExaminerAssignments = new List<ExaminerAssignment>();
        }
        examSession.ExaminerAssignments.Add(assignment);

        await _examSessionRepository.UpdateAsync(examSession);
        _logger.LogInformation("Examiner {ExaminerId} assigned to exam session {SessionId} with role {Role}", examinerId, examSessionId, role);

        return assignment;
    }

    public async Task<bool> RemoveExaminerAssignmentAsync(Guid examSessionId, Guid examinerId)
    {
        var examSession = await _examSessionRepository.GetByIdAsync(examSessionId);
        if (examSession == null)
        {
            throw new KeyNotFoundException($"Exam session with ID {examSessionId} not found");
        }

        var assignment = examSession.ExaminerAssignments?.FirstOrDefault(ea => ea.ExaminerId == examinerId);
        if (assignment == null)
        {
            return false;
        }

        examSession.ExaminerAssignments?.Remove(assignment);
        await _examSessionRepository.UpdateAsync(examSession);
        _logger.LogInformation("Examiner {ExaminerId} removed from exam session {SessionId}", examinerId, examSessionId);

        return true;
    }
}
