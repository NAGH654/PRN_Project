using System;
using System.Collections.Generic;
using Repositories.Entities.Enums;

namespace Services.Dtos.Responses
{
    public class AssignedExamResponse
    {
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public int PendingSubmissions { get; set; }
        public int ProcessingSubmissions { get; set; }
        public int GradedSubmissions { get; set; }
        public int MyGradedSubmissions { get; set; }
    }

    public class SubmissionListItemResponse
    {
        public Guid SubmissionId { get; set; }
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public SubmissionStatus Status { get; set; }
        public bool HasViolations { get; set; }
        public bool IsGradedByMe { get; set; }
        public bool IsGradedByOthers { get; set; }
        public int GradingCount { get; set; } // Number of examiners who have graded
        public bool RequiresDoubleGrading { get; set; }
    }

    public class PagedSubmissionResponse
    {
        public List<SubmissionListItemResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class SubmissionGradingDetailsResponse
    {
        public Guid SubmissionId { get; set; }
        public Guid SessionId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileDownloadUrl { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public SubmissionStatus Status { get; set; }
        public List<ViolationItemResponse> Violations { get; set; } = new();
        public List<RubricGradingItemResponse> Rubrics { get; set; } = new();
        public List<GradeResponse> ExistingGrades { get; set; } = new();
        public bool CanEdit { get; set; } // If grades are editable
        public bool IsSecondGrader { get; set; } // If this is the second examiner grading
    }

    public class RubricGradingItemResponse
    {
        public Guid RubricId { get; set; }
        public string Criteria { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxPoints { get; set; }
        public decimal? Points { get; set; } // Points given by current examiner
        public string? Comments { get; set; } // Comments by current examiner
    }

    public class RubricResponse
    {
        public Guid RubricId { get; set; }
        public Guid ExamId { get; set; }
        public string Criteria { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MaxPoints { get; set; }
    }

    public class ViolationItemResponse
    {
        public Guid ViolationId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ViolationSeverity Severity { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    public class GradeResponse
    {
        public Guid GradeId { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid ExaminerId { get; set; }
        public string? ExaminerName { get; set; }
        public Guid RubricId { get; set; }
        public string RubricCriteria { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public decimal MaxPoints { get; set; }
        public string? Comments { get; set; }
        public DateTime GradedAt { get; set; }
        public bool IsFinal { get; set; }
    }

    public class GradingResultResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid SubmissionId { get; set; }
        public SubmissionStatus NewStatus { get; set; }
        public bool RequiresModeratorReview { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal? ScoreDifference { get; set; } // If two graders, the difference
        public List<GradeResponse> CreatedGrades { get; set; } = new();
    }

    public class GradingStatusResponse
    {
        public Guid SubmissionId { get; set; }
        public int ExaminerCount { get; set; } // Number of examiners who have graded
        public bool RequiresDoubleGrading { get; set; }
        public bool IsDoubleGradingComplete { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal? MaxPossibleScore { get; set; }
        public bool RequiresModeratorReview { get; set; }
        public SubmissionStatus Status { get; set; }
        public List<ExaminerGradingSummary> ExaminerSummaries { get; set; } = new();
    }

    public class ExaminerGradingSummary
    {
        public Guid ExaminerId { get; set; }
        public string? ExaminerName { get; set; }
        public decimal TotalScore { get; set; }
        public int RubricsGraded { get; set; }
        public DateTime? LastGradedAt { get; set; }
    }
}
