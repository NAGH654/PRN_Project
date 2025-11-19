using Microsoft.EntityFrameworkCore;
using StorageService.Entities;

namespace StorageService.Data;

public class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options)
    {
    }

    public DbSet<Submission> Submissions { get; set; }
    public DbSet<SubmissionFile> SubmissionFiles { get; set; }
    public DbSet<Violation> Violations { get; set; }
    public DbSet<SubmissionImage> SubmissionImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<SubmissionFile>()
            .HasOne(sf => sf.Submission)
            .WithMany(s => s.Files)
            .HasForeignKey(sf => sf.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Violation>()
            .HasOne(v => v.Submission)
            .WithMany(s => s.Violations)
            .HasForeignKey(v => v.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubmissionImage>()
            .HasOne(si => si.Submission)
            .WithMany(s => s.Images)
            .HasForeignKey(si => si.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<Submission>()
            .HasIndex(s => new { s.StudentId, s.ExamId });

        modelBuilder.Entity<Submission>()
            .HasIndex(s => s.Status);

        modelBuilder.Entity<Submission>()
            .HasIndex(s => s.SubmittedAt);

        modelBuilder.Entity<SubmissionFile>()
            .HasIndex(sf => sf.FileHash);

        modelBuilder.Entity<Violation>()
            .HasIndex(v => new { v.SubmissionId, v.IsResolved });

        modelBuilder.Entity<Violation>()
            .HasIndex(v => v.Severity);
    }
}
