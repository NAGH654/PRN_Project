-- Delete submissions and related data
-- Order matters due to foreign key constraints

USE AssignmentGradingDb;
GO

-- Delete SubmissionImages first (references Submission)
DELETE FROM Storage.SubmissionImage;
GO

-- Delete Violations (references Submission)
DELETE FROM Storage.Violation;
GO

-- Delete SubmissionFiles (references Submission)
DELETE FROM Storage.SubmissionFile;
GO

-- Delete Submissions
DELETE FROM Storage.Submission;
GO

-- Show remaining counts
SELECT 
    'Submissions' AS TableName, COUNT(*) AS Count FROM Storage.Submission
UNION ALL
SELECT 
    'SubmissionFiles', COUNT(*) FROM Storage.SubmissionFile
UNION ALL
SELECT 
    'SubmissionImages', COUNT(*) FROM Storage.SubmissionImage
UNION ALL
SELECT 
    'Violations', COUNT(*) FROM Storage.Violation;
GO

