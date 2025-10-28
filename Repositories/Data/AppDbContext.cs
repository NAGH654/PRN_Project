using Microsoft.EntityFrameworkCore;
using Repositories.Entities;

namespace Repositories.Data
{
	public class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
	{
		public DbSet<User> Users => Set<User>();
		public DbSet<Subject> Subjects => Set<Subject>();
		public DbSet<Semester> Semesters => Set<Semester>();
		public DbSet<Exam> Exams => Set<Exam>();
		public DbSet<Rubric> Rubrics => Set<Rubric>();
		public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
		public DbSet<Submission> Submissions => Set<Submission>();
		public DbSet<Violation> Violations => Set<Violation>();
		public DbSet<SubmissionImage> SubmissionImages => Set<SubmissionImage>();
		public DbSet<Grade> Grades => Set<Grade>();
		public DbSet<ExaminerAssignment> ExaminerAssignments => Set<ExaminerAssignment>();
		public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

		protected override void OnModelCreating(ModelBuilder b)
		{
			// Users
			b.Entity<User>(e =>
			{
				e.HasKey(x => x.UserId);
				e.Property(x => x.UserId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.Username).HasMaxLength(50).IsRequired();
				e.Property(x => x.Email).HasMaxLength(100).IsRequired();
				e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
				e.Property(x => x.Role).HasMaxLength(20).IsRequired();
				e.Property(x => x.IsActive).HasDefaultValue(true);
				e.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()");
				e.Property(x => x.UpdatedAt).HasDefaultValueSql("getutcdate()");
				e.HasIndex(x => x.Username).IsUnique();
				e.HasIndex(x => x.Email).IsUnique();
				e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Admin','Manager','Moderator','Examiner')"));
			});

			// Subjects
			b.Entity<Subject>(e =>
			{
				e.HasKey(x => x.SubjectId);
				e.Property(x => x.SubjectId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.SubjectCode).HasMaxLength(20).IsRequired();
				e.Property(x => x.SubjectName).HasMaxLength(100).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);
				e.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()");
				e.HasIndex(x => x.SubjectCode).IsUnique();
			});

			// Semesters
			b.Entity<Semester>(e =>
			{
				e.HasKey(x => x.SemesterId);
				e.Property(x => x.SemesterId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.SemesterCode).HasMaxLength(20).IsRequired();
				e.Property(x => x.SemesterName).HasMaxLength(100).IsRequired();
				e.Property(x => x.IsActive).HasDefaultValue(true);
				e.HasIndex(x => x.SemesterCode).IsUnique();
			});

			// Exams
			b.Entity<Exam>(e =>
			{
				e.HasKey(x => x.ExamId);
				e.Property(x => x.ExamId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.ExamName).HasMaxLength(200).IsRequired();
				e.Property(x => x.TotalMarks).HasPrecision(5, 2).IsRequired();
				e.Property(x => x.CreatedAt).HasDefaultValueSql("getutcdate()");
				e.HasOne(x => x.Subject).WithMany(x => x.Exams).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
				e.HasOne(x => x.Semester).WithMany(x => x.Exams).HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);
				e.HasOne(x => x.CreatedByUser).WithMany(x => x.CreatedExams).HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
			});

			// Rubrics
			b.Entity<Rubric>(e =>
			{
				e.HasKey(x => x.RubricId);
				e.Property(x => x.RubricId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.Criteria).HasMaxLength(200).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500);
				e.Property(x => x.MaxPoints).HasPrecision(5, 2).IsRequired();
				e.HasOne(x => x.Exam).WithMany(x => x.Rubrics).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
			});

			// Exam Sessions
			b.Entity<ExamSession>(e =>
			{
				e.HasKey(x => x.SessionId);
				e.Property(x => x.SessionId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.SessionName).HasMaxLength(100).IsRequired();
				e.Property(x => x.IsActive).HasDefaultValue(true);
				e.HasOne(x => x.Exam).WithMany(x => x.Sessions).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
			});

			// Submissions
			b.Entity<Submission>(e =>
			{
				e.HasKey(x => x.SubmissionId);
				e.Property(x => x.SubmissionId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.StudentId).HasMaxLength(50).IsRequired();
				e.Property(x => x.StudentName).HasMaxLength(100);
				e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
				e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
				e.Property(x => x.ContentHash).HasMaxLength(128);
				e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
				e.Property(x => x.SubmissionTime).HasDefaultValueSql("getutcdate()");
				e.ToTable(t => t.HasCheckConstraint("CK_Submissions_Status", "[Status] IN ('Pending','Processing','Graded','Flagged')"));
				e.HasOne(x => x.Session).WithMany(x => x.Submissions).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
			});

			// Violations
			b.Entity<Violation>(e =>
			{
				e.HasKey(x => x.ViolationId);
				e.Property(x => x.ViolationId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.ViolationType).HasMaxLength(50).IsRequired();
				e.Property(x => x.Description).HasMaxLength(500).IsRequired();
				e.Property(x => x.Severity).HasMaxLength(20).HasDefaultValue("Warning");
				e.Property(x => x.DetectedAt).HasDefaultValueSql("getutcdate()");
				e.ToTable(t => t.HasCheckConstraint("CK_Violations_Severity", "[Severity] IN ('Warning','Error')"));
				e.HasOne(x => x.Submission).WithMany(x => x.Violations).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
			});

			// Submission Images
			b.Entity<SubmissionImage>(e =>
			{
				e.HasKey(x => x.ImageId);
				e.Property(x => x.ImageId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.ImageName).HasMaxLength(255).IsRequired();
				e.Property(x => x.ImagePath).HasMaxLength(500).IsRequired();
				e.Property(x => x.ExtractedAt).HasDefaultValueSql("getutcdate()");
				e.HasOne(x => x.Submission).WithMany(x => x.Images).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
			});

			// Grades
				b.Entity<Grade>(e =>
			{
				e.HasKey(x => x.GradeId);
				e.Property(x => x.GradeId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.Points).HasPrecision(5, 2).IsRequired();
				e.Property(x => x.Comments).HasMaxLength(500);
				e.Property(x => x.GradedAt).HasDefaultValueSql("getutcdate()");
				e.Property(x => x.IsFinal).HasDefaultValue(false);
				e.HasIndex(x => new { x.SubmissionId, x.ExaminerId, x.RubricId }).IsUnique();
				e.HasOne(x => x.Submission).WithMany(x => x.Grades).HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
				e.HasOne(x => x.Examiner).WithMany(x => x.GivenGrades).HasForeignKey(x => x.ExaminerId).OnDelete(DeleteBehavior.Restrict);
				e.HasOne(x => x.Rubric).WithMany(x => x.Grades).HasForeignKey(x => x.RubricId).OnDelete(DeleteBehavior.Cascade);
			});

			// Examiner Assignments
			b.Entity<ExaminerAssignment>(e =>
			{
				e.HasKey(x => x.AssignmentId);
				e.Property(x => x.AssignmentId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.AssignedAt).HasDefaultValueSql("getutcdate()");
				e.HasIndex(x => new { x.ExamId, x.ExaminerId }).IsUnique();
				e.HasOne(x => x.Exam).WithMany(x => x.ExaminerAssignments).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
				e.HasOne(x => x.Examiner).WithMany(x => x.ExaminerAssignments).HasForeignKey(x => x.ExaminerId).OnDelete(DeleteBehavior.Restrict);
				e.HasOne(x => x.AssignedByUser).WithMany(x => x.AssignedExaminerAssignments).HasForeignKey(x => x.AssignedBy).OnDelete(DeleteBehavior.Restrict);
			});

			// Audit Logs
			b.Entity<AuditLog>(e =>
			{
				e.HasKey(x => x.LogId);
				e.Property(x => x.LogId).HasDefaultValueSql("newsequentialid()");
				e.Property(x => x.Action).HasMaxLength(100).IsRequired();
				e.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
				e.Property(x => x.Details).HasMaxLength(1000);
				e.Property(x => x.Timestamp).HasDefaultValueSql("getutcdate()");
				e.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
			});
		}
	}
}
