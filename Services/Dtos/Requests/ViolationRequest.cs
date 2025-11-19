using Repositories.Entities.Enums;

namespace Services.Dtos.Requests
{
    public class ViolationRequest
    {
        public ViolationType ViolationType { get; set; }
        public string Description { get; set; } = string.Empty;
        public ViolationSeverity Severity { get; set; } = ViolationSeverity.Warning;
    }
}

