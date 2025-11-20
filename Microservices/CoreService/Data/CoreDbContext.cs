using Microsoft.EntityFrameworkCore;
using CoreService.Entities;

namespace CoreService.Data;

public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<RubricItem> RubricItems { get; set; }
    public DbSet<ExamSession> ExamSessions { get; set; }
    public DbSet<ExaminerAssignment> ExaminerAssignments { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<RubricScore> RubricScores { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Exam>()
            .HasOne(e => e.Subject)
            .WithMany(s => s.Exams)
            .HasForeignKey(e => e.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Exam>()
            .HasOne(e => e.Semester)
            .WithMany(s => s.Exams)
            .HasForeignKey(e => e.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RubricItem>()
            .HasOne(r => r.Exam)
            .WithMany(e => e.RubricItems)
            .HasForeignKey(r => r.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamSession>()
            .HasOne(es => es.Exam)
            .WithMany(e => e.ExamSessions)
            .HasForeignKey(es => es.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExaminerAssignment>()
            .HasOne(ea => ea.ExamSession)
            .WithMany(es => es.ExaminerAssignments)
            .HasForeignKey(ea => ea.ExamSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Exam)
            .WithMany(e => e.Grades)
            .HasForeignKey(g => g.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RubricScore>()
            .HasOne(rs => rs.RubricItem)
            .WithMany()
            .HasForeignKey(rs => rs.RubricItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<Semester>()
            .HasIndex(s => s.Code)
            .IsUnique();

        modelBuilder.Entity<Grade>()
            .HasIndex(g => new { g.ExamId, g.StudentId });

        modelBuilder.Entity<RubricScore>()
            .HasIndex(rs => new { rs.SubmissionId, rs.RubricItemId, rs.GradedBy });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityType, a.EntityId });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);
    }
}
