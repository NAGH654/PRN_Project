using Repositories.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Dtos.Requests
{
    public class GradingRequests
    {
        [Required]
        public Guid SubmissionId { get; set; }

        [Required]
        public List<RubricGradeDto> Grades { get; set; } = new();
    }
    public class RubricGradeDto
    {
        [Required]
        public Guid RubricId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Points { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }
    public class UpdateGradeRequest
    {
        [Required]
        public Guid GradeId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Points { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }
    }

    public class MarkZeroRequest
    {
        [Required]
        public Guid SubmissionId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public List<Guid> ViolationIds { get; set; } = new();
    }

    public class GetSubmissionsQuery
    {
        public SubmissionStatus? Status { get; set; }
        public bool AssignedToMe { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
