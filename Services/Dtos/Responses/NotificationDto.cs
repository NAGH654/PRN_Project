namespace Services.Dtos.Responses
{
    public class NotificationDto
    {
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class SubmissionUploadedNotificationDto : NotificationDto
    {
        public Guid SubmissionId { get; set; }
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
    }

    public class SubmissionGradedNotificationDto : NotificationDto
    {
        public Guid SubmissionId { get; set; }
        public Guid ExamId { get; set; }
        public string ExaminerName { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class ViolationDetectedNotificationDto : NotificationDto
    {
        public Guid SubmissionId { get; set; }
        public Guid ViolationId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }
}

