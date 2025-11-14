using Microsoft.EntityFrameworkCore;
using IdentityService.Entities;

namespace IdentityService.Data
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Use schema for shared database approach
            modelBuilder.HasDefaultSchema("Identity");

            // Users configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                
                entity.Property(e => e.UserId)
                    .HasDefaultValueSql("newsequentialid()");
                
                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .IsRequired();
                
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("getutcdate()");
                
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.RefreshToken)
                    .HasMaxLength(500);

                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Identity_Users_Role", 
                    "[Role] IN ('Admin','Manager','Moderator','Examiner')"));
            });
        }
    }
}
