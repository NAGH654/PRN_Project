using Microsoft.EntityFrameworkCore;
using Repositories.Entities.Enum;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Repositories.Data
{
    public static class AppDbSeeder
    {
        public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
        {
            // Tự migrate DB nếu chưa có
            await db.Database.MigrateAsync(ct);

            // ===== Users (Admin + Lecturer) =====
            if (!await db.Users.AnyAsync(ct))
            {
                var admin = new User { Email = "admin@swd392.test", FullName = "System Admin", Role = UserRole.Admin };
                var lecturer = new User { Email = "lecturer@swd392.test", FullName = "Main Lecturer", Role = UserRole.Lecturer };
                await db.Users.AddRangeAsync(new[] { admin, lecturer }, ct);
                await db.SaveChangesAsync(ct);
            }
            var lecturerId = await db.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .Select(u => u.Id).FirstAsync(ct);

            // ===== Course =====
            var course = await db.Courses.FirstOrDefaultAsync(c => c.Code == "SWD392", ct);
            if (course is null)
            {
                course = new Course { Code = "SWD392", Name = "Software Architecture & Design" };
                db.Courses.Add(course);
                await db.SaveChangesAsync(ct);
            }

            // ===== Class =====
            var cls = await db.Classes.FirstOrDefaultAsync(c =>
                c.CourseId == course.Id && c.Name == "SWD392_SU25_E1" && c.Term == "SU25", ct);
            if (cls is null)
            {
                cls = new Class { CourseId = course.Id, Name = "SWD392_SU25_E1", Term = "SU25", LecturerId = lecturerId };
                db.Classes.Add(cls);
                await db.SaveChangesAsync(ct);
            }

            // ===== Assignment (PE) =====
            var assign = await db.Assignments.FirstOrDefaultAsync(a => a.ClassId == cls.Id && a.Code == "PE", ct);
            if (assign is null)
            {
                assign = new Assignment
                {
                    ClassId = cls.Id,
                    Code = "PE",
                    Name = "Practical Exam",
                    NamingRegex = @"^SWD392_PE_SU\d+_SE(?<sid>\d{5,})_(?<name>.+)\.docx$",
                    KeywordsJson = JsonSerializer.Serialize(new[] { "Part 1", "Part 2", "Part 3", "REST API", "ERD", "Design Pattern" }),
                    RubricJson = JsonSerializer.Serialize(new { filename = 1, keywords = 2, parts = new { p1 = 5, p2 = 10, p3 = 10 } })
                };
                db.Assignments.Add(assign);

                // (tuỳ chọn) thêm keyword/rubric chi tiết
                db.AssignmentKeywords.AddRange(
                    new AssignmentKeyword { Assignment = assign, Phrase = "Part 1", Weight = 1, IsRequired = true },
                    new AssignmentKeyword { Assignment = assign, Phrase = "Part 2", Weight = 1, IsRequired = true },
                    new AssignmentKeyword { Assignment = assign, Phrase = "REST API", Weight = 1 },
                    new AssignmentKeyword { Assignment = assign, Phrase = "ERD", Weight = 1 },
                    new AssignmentKeyword { Assignment = assign, Phrase = "Design Pattern", Weight = 1 }
                );
                db.RubricItems.AddRange(
                    new RubricItem { Assignment = assign, Code = "P1", Title = "Part 1 - Data analysis & design", MaxPoints = 5, AutoEvaluated = false },
                    new RubricItem { Assignment = assign, Code = "P2", Title = "Part 2 - REST API design", MaxPoints = 10, AutoEvaluated = false },
                    new RubricItem { Assignment = assign, Code = "P3", Title = "Part 3 - Design Pattern", MaxPoints = 10, AutoEvaluated = false }
                );
                await db.SaveChangesAsync(ct);
            }

            // ===== Students + Enrollments (mẫu) =====
            if (!await db.Students.AnyAsync(ct))
            {
                var studs = new List<Student>
            {
                new() { Code = "SE170283", FullName = "Dao Duong Hung Anh" },
                new() { Code = "SE182347", FullName = "Nguyen Van A" },
                new() { Code = "SE184425", FullName = "Tran Thi B" },
                new() { Code = "SE173528", FullName = "Le Van C" },
                new() { Code = "SE182326", FullName = "Pham Thi D" }
            };
                await db.Students.AddRangeAsync(studs, ct);
                await db.SaveChangesAsync(ct);

                var enrolls = studs.Select(s => new Enrollment { ClassId = cls.Id, StudentId = s.Id }).ToList();
                await db.Enrollments.AddRangeAsync(enrolls, ct);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
