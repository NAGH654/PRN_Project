using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Entities.Enums;

namespace Repositories.Data
{
	public static class DbSeeder
	{
		public static async Task SeedDefaultsAsync(AppDbContext db, CancellationToken ct = default)
		{
			await db.Database.MigrateAsync(ct);

			// Users
			if (!await db.Users.AnyAsync(ct))
			{
				var admin = new User
				{
					Username = "admin",
					Email = "admin@swd392.test",
					PasswordHash = "",
					Role = UserRole.Admin,
					IsActive = true
				};

				var lecturer = new User
				{
					Username = "lecturer",
					Email = "lecturer@swd392.test",
					PasswordHash = "",
					Role = UserRole.Examiner,
					IsActive = true
				};

				await db.Users.AddRangeAsync(new[] { admin, lecturer }, ct);
				await db.SaveChangesAsync(ct);
			}

			// Subject
			var subject = await db.Subjects.FirstOrDefaultAsync(s => s.SubjectCode == "PRN232", ct);
			if (subject == null)
			{
				subject = new Subject
				{
					SubjectCode = "PRN232",
					SubjectName = "Programming in .NET",
					Description = "Seed subject",
					CreatedAt = DateTime.UtcNow
				};
				await db.Subjects.AddAsync(subject, ct);
				await db.SaveChangesAsync(ct);
			}

			// Semester
			var semester = await db.Semesters.FirstOrDefaultAsync(s => s.SemesterCode == "FA25", ct);
			if (semester == null)
			{
				semester = new Semester
				{
					SemesterCode = "FA25",
					SemesterName = "Fall 2025",
					StartDate = DateTime.UtcNow.AddMonths(-1),
					EndDate = DateTime.UtcNow.AddMonths(1),
					IsActive = true
				};
				await db.Semesters.AddAsync(semester, ct);
				await db.SaveChangesAsync(ct);
			}

			// Admin and Lecturer for references
			var adminUser = await db.Users.FirstAsync(u => u.Username == "admin", ct);
			var lecturerUser = await db.Users.FirstAsync(u => u.Username == "lecturer", ct);

			// Exam
			var exam = await db.Exams.FirstOrDefaultAsync(e => e.ExamName == "PRN232 Final Exam", ct);
			if (exam == null)
			{
				exam = new Exam
				{
					SubjectId = subject.SubjectId,
					SemesterId = semester.SemesterId,
					ExamName = "PRN232 Final Exam",
					ExamDate = DateTime.UtcNow.Date,
					DurationMinutes = 120,
					TotalMarks = 100,
					CreatedBy = adminUser.UserId,
					CreatedAt = DateTime.UtcNow
				};
				await db.Exams.AddAsync(exam, ct);
				await db.SaveChangesAsync(ct);
			}

			// Rubrics (only seed if none)
			if (!await db.Rubrics.AnyAsync(r => r.ExamId == exam.ExamId, ct))
			{
				var rubrics = new List<Rubric>
				{
					new Rubric { ExamId = exam.ExamId, Criteria = "Functionality", MaxPoints = 40, Description = "Features work as expected" },
					new Rubric { ExamId = exam.ExamId, Criteria = "Code Quality", MaxPoints = 30, Description = "Structure, readability, practices" },
					new Rubric { ExamId = exam.ExamId, Criteria = "Documentation", MaxPoints = 30, Description = "Readme, comments" }
				};
				await db.Rubrics.AddRangeAsync(rubrics, ct);
				await db.SaveChangesAsync(ct);
			}

			// Exam Session
			var session = await db.ExamSessions.FirstOrDefaultAsync(s => s.ExamId == exam.ExamId && s.IsActive, ct);
			if (session == null)
			{
				session = new ExamSession
				{
					ExamId = exam.ExamId,
					SessionName = "Session A",
					StartTime = DateTime.UtcNow.AddHours(-1),
					EndTime = DateTime.UtcNow.AddHours(3),
					IsActive = true
				};
				await db.ExamSessions.AddAsync(session, ct);
				await db.SaveChangesAsync(ct);
			}

			// Assign lecturer to exam
			if (!await db.ExaminerAssignments.AnyAsync(a => a.ExamId == exam.ExamId && a.ExaminerId == lecturerUser.UserId, ct))
			{
				await db.ExaminerAssignments.AddAsync(new ExaminerAssignment
				{
					ExamId = exam.ExamId,
					ExaminerId = lecturerUser.UserId,
					AssignedBy = adminUser.UserId,
					AssignedAt = DateTime.UtcNow
				}, ct);
				await db.SaveChangesAsync(ct);
			}

			// Seed sample submissions with grades and a violation so OData has data
			if (!await db.Submissions.AnyAsync(s => s.SessionId == session.SessionId, ct))
			{
				var rnd = new Random(1735);
				var submissions = new List<Submission>();
				for (int i = 0; i < 8; i++)
				{
					var sid = $"SE{100000 + i}";
					var sname = $"Student{i + 1}";
					var sub = new Submission
					{
						SessionId = session.SessionId,
						StudentId = sid,
						StudentName = sname,
						FileName = $"{sid}_PRN232_{sname}.docx",
						FilePath = $"jobs/{Guid.NewGuid()}/extract/{sid}_PRN232_{sname}.docx",
						FileSize = 1024 + i,
						ContentHash = Guid.NewGuid().ToString("N"),
						SubmissionTime = DateTime.UtcNow.AddMinutes(-30 + i * 3),
						Status = SubmissionStatus.Pending
					};
					submissions.Add(sub);
				}
				await db.Submissions.AddRangeAsync(submissions, ct);
				await db.SaveChangesAsync(ct);

				// Add a naming violation for the first submission
				var firstSub = submissions.First();
				await db.Violations.AddAsync(new Violation
				{
					SubmissionId = firstSub.SubmissionId,
					ViolationType = ViolationType.Naming,
					Severity = ViolationSeverity.Warning,
					Description = "Invalid naming convention in demo seed",
					DetectedAt = DateTime.UtcNow
				}, ct);
				await db.SaveChangesAsync(ct);

				// Seed grades per rubric from lecturer
				var examRubrics = await db.Rubrics.Where(r => r.ExamId == exam.ExamId).ToListAsync(ct);
				var gradeRows = new List<Grade>();
				foreach (var sub in submissions)
				{
					foreach (var r in examRubrics)
					{
						var points = Math.Round(Math.Min(r.MaxPoints, (decimal)(rnd.NextDouble() * (double)r.MaxPoints)), 1);
						gradeRows.Add(new Grade
						{
							SubmissionId = sub.SubmissionId,
							ExaminerId = lecturerUser.UserId,
							RubricId = r.RubricId,
							Points = points,
							Comments = "Seed grade",
							GradedAt = DateTime.UtcNow,
							IsFinal = true
						});
					}
				}
				await db.Grades.AddRangeAsync(gradeRows, ct);
				await db.SaveChangesAsync(ct);
			}
		}
	}
}


