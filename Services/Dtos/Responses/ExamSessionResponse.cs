using System;

namespace Services.Dtos.Responses
{
    public class ExamSessionResponse
    {
        public Guid SessionId { get; set; }
        public Guid ExamId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public string ExamName { get; set; } = string.Empty;
    }
}


