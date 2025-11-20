# PowerShell script to delete all submissions from database

$connectionString = "Server=localhost,1433;Database=AssignmentGradingDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"

$sql = @"
USE AssignmentGradingDb;

-- Delete in correct order to avoid foreign key violations
DELETE FROM Storage.SubmissionImage;
DELETE FROM Storage.Violation;
DELETE FROM Storage.SubmissionFile;
DELETE FROM Storage.Submission;

-- Show remaining counts
SELECT 'Submissions' AS TableName, COUNT(*) AS Count FROM Storage.Submission
UNION ALL
SELECT 'SubmissionFiles', COUNT(*) FROM Storage.SubmissionFile
UNION ALL
SELECT 'SubmissionImages', COUNT(*) FROM Storage.SubmissionImage
UNION ALL
SELECT 'Violations', COUNT(*) FROM Storage.Violation;
"@

# Try using sqlcmd if available
$sqlcmdPath = "sqlcmd"
if (Get-Command $sqlcmdPath -ErrorAction SilentlyContinue) {
    Write-Host "Using sqlcmd to delete submissions..."
    $sql | sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd" -C
} else {
    Write-Host "sqlcmd not found. Trying docker exec..."
    # Use docker exec to run sqlcmd inside the container
    $sql | docker exec -i assignment_grading_db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Trying alternative path..."
        $sql | docker exec -i assignment_grading_db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C
    }
}

Write-Host "Done!"

