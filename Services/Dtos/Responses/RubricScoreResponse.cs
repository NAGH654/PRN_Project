using System;

namespace Services.Dtos.Responses
{
    public class RubricScoreResponse
    {
        public Guid RubricId { get; set; }
        public string Criteria { get; set; } = string.Empty;
        public decimal MaxPoints { get; set; }
        public decimal AveragePoints { get; set; }
    }
}


