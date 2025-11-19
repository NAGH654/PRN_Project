using CoreService.Entities;
using CoreService.Repositories;

namespace CoreService.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;
    private readonly IExamRepository _examRepository;
    private readonly ILogger<GradeService> _logger;

    public GradeService(
        IGradeRepository gradeRepository,
        IExamRepository examRepository,
        ILogger<GradeService> logger)
    {
        _gradeRepository = gradeRepository;
        _examRepository = examRepository;
        _logger = logger;
    }

    public async Task<Grade?> GetByIdAsync(Guid id)
    {
        return await _gradeRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId)
    {
        return await _gradeRepository.GetByExamIdAsync(examId);
    }

    public async Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId)
    {
        return await _gradeRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<Grade> CreateOrUpdateGradeAsync(Guid examId, Guid studentId, decimal score, string? feedback, Guid gradedBy)
    {
        // Validate exam exists
        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
        {
            _logger.LogWarning("Exam {ExamId} not found", examId);
            throw new KeyNotFoundException($"Exam with ID {examId} not found");
        }

        // Validate score
        if (score < 0 || score > exam.TotalMarks)
        {
            _logger.LogWarning("Invalid score {Score} for exam {ExamId} with max {MaxScore}", score, examId, exam.TotalMarks);
            throw new ArgumentException($"Score must be between 0 and {exam.TotalMarks}", nameof(score));
        }

        // Check if grade already exists
        var existingGrade = await _gradeRepository.GetByExamAndStudentAsync(examId, studentId);

        if (existingGrade != null)
        {
            // Update existing grade
            existingGrade.Score = score;
            existingGrade.MaxScore = exam.TotalMarks;
            existingGrade.Feedback = feedback;
            existingGrade.GradedBy = gradedBy;
            existingGrade.GradedAt = DateTime.UtcNow;

            var updated = await _gradeRepository.UpdateAsync(existingGrade);
            _logger.LogInformation("Grade updated for student {StudentId} in exam {ExamId}", studentId, examId);
            
            return updated;
        }
        else
        {
            // Create new grade
            var grade = new Grade
            {
                ExamId = examId,
                StudentId = studentId,
                Score = score,
                MaxScore = exam.TotalMarks,
                Feedback = feedback,
                GradedBy = gradedBy,
                GradedAt = DateTime.UtcNow
            };

            var created = await _gradeRepository.CreateAsync(grade);
            _logger.LogInformation("Grade created for student {StudentId} in exam {ExamId}", studentId, examId);
            
            return created;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _gradeRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Grade {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Grade {Id} not found for deletion", id);
        }
        
        return deleted;
    }
}
