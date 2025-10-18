using Microsoft.EntityFrameworkCore;
using Repositories.Entities;

namespace Repositories.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentKeyword> AssignmentKeywords => Set<AssignmentKeyword>();
        public DbSet<RubricItem> RubricItems => Set<RubricItem>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<SubmissionFile> SubmissionFiles => Set<SubmissionFile>();
        public DbSet<Check> Checks => Set<Check>();
        public DbSet<Score> Scores => Set<Score>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobItem> JobItems => Set<JobItem>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Student>().HasIndex(x => x.Code).IsUnique();
            b.Entity<Assignment>().HasIndex(x => new { x.ClassId, x.Code }).IsUnique();
            b.Entity<Submission>().HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
            b.Entity<Score>().HasIndex(x => x.SubmissionId).IsUnique();

            b.Entity<Submission>().Property(x => x.Status).HasConversion<int>();
            b.Entity<Job>().Property(x => x.Status).HasConversion<int>();
            b.Entity<Job>().Property(x => x.Kind).HasConversion<int>();
        }
    }
}
