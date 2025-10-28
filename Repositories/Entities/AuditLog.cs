using System;

namespace Repositories.Entities
{
	public class AuditLog
	{
		public Guid LogId { get; set; }
		public Guid? UserId { get; set; }
		public string Action { get; set; } = string.Empty;
		public string EntityType { get; set; } = string.Empty;
		public Guid EntityId { get; set; }
		public string? Details { get; set; }
		public DateTime Timestamp { get; set; }

		public User? User { get; set; }
	}
}


