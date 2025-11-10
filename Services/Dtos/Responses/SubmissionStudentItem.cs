using System;

namespace Services.Dtos.Responses
{
    public class SubmissionStudentItem
    {
        public Guid SubmissionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
    }
}


