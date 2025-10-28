using Microsoft.EntityFrameworkCore;
using Repositories.Entities;

namespace Repositories.Data
{
	public static class DbSeeder
	{
		public static async Task SeedDefaultsAsync(AppDbContext db, CancellationToken ct = default)
		{
			await db.Database.MigrateAsync(ct);

			if (!await db.Users.AnyAsync(ct))
			{
				var admin = new User
				{
					Username = "admin",
					Email = "admin@swd392.test",
					PasswordHash = "",
					Role = "Admin",
					IsActive = true
				};

				var lecturer = new User
				{
					Username = "lecturer",
					Email = "lecturer@swd392.test",
					PasswordHash = "",
					Role = "Examiner",
					IsActive = true
				};

				await db.Users.AddRangeAsync(new[] { admin, lecturer }, ct);
				await db.SaveChangesAsync(ct);
			}
		}
	}
}


