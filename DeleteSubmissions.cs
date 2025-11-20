using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

// Simple script to delete all submissions
// Run this with: dotnet script DeleteSubmissions.cs

// Note: This is a standalone script. You'll need to reference the StorageService project
// Or run it as a console app

// For now, let's create a simple SQL script that can be run directly
Console.WriteLine("To delete submissions, run this SQL:");
Console.WriteLine(@"
USE AssignmentGradingDb;
GO

DELETE FROM Storage.SubmissionImage;
DELETE FROM Storage.Violation;
DELETE FROM Storage.SubmissionFile;
DELETE FROM Storage.Submission;
GO
");

