using System;

namespace Services.Dtos.Responses
{
    public class SubmissionReportODataRow
    {
        public Guid SubmissionId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public DateTime SubmissionTime { get; set; }
        public bool HasViolations { get; set; }
        public int ViolationCount { get; set; }
        public decimal TotalAverageScore { get; set; }
    }
}


