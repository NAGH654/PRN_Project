using API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Services.Dtos.Responses;
using Services.Interfaces;

namespace API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifySubmissionUploadedAsync(SubmissionUploadedNotificationDto notification, Guid? examId = null)
        {
            try
            {
                notification.EventType = "SubmissionUploaded";
                notification.Timestamp = DateTime.UtcNow;
                notification.Message = $"New submission uploaded: {notification.StudentId} (Total: {notification.TotalSubmissions})";

                // Notify managers
                await _hubContext.Clients.Group("managers").SendAsync("SubmissionUploaded", notification);

                // Notify examiners assigned to the exam
                if (examId.HasValue)
                {
                    await _hubContext.Clients.Group($"exam_{examId.Value}").SendAsync("SubmissionUploaded", notification);
                }

                // Notify all examiners
                await _hubContext.Clients.Group("examiners").SendAsync("SubmissionUploaded", notification);

                _logger.LogInformation("Notification sent: SubmissionUploaded for SubmissionId {SubmissionId}", notification.SubmissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SubmissionUploaded notification");
            }
        }

        public async Task NotifySubmissionGradedAsync(SubmissionGradedNotificationDto notification, Guid? examId = null)
        {
            try
            {
                notification.EventType = "SubmissionGraded";
                notification.Timestamp = DateTime.UtcNow;
                notification.Message = $"Submission graded by {notification.ExaminerName} for student {notification.StudentId}";

                // Notify managers
                await _hubContext.Clients.Group("managers").SendAsync("SubmissionGraded", notification);

                // Notify other examiners for the same exam
                if (examId.HasValue)
                {
                    await _hubContext.Clients.Group($"exam_{examId.Value}").SendAsync("SubmissionGraded", notification);
                }

                // Notify all examiners
                await _hubContext.Clients.Group("examiners").SendAsync("SubmissionGraded", notification);

                _logger.LogInformation("Notification sent: SubmissionGraded for SubmissionId {SubmissionId} by {Examiner}", 
                    notification.SubmissionId, notification.ExaminerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SubmissionGraded notification");
            }
        }

        public async Task NotifyViolationDetectedAsync(ViolationDetectedNotificationDto notification, Guid? examId = null)
        {
            try
            {
                notification.EventType = "ViolationDetected";
                notification.Timestamp = DateTime.UtcNow;
                notification.Message = $"Violation detected ({notification.Severity}): {notification.ViolationType} - {notification.Description}";

                // Notify moderators (highest priority)
                await _hubContext.Clients.Group("moderators").SendAsync("ViolationDetected", notification);

                // Notify managers
                await _hubContext.Clients.Group("managers").SendAsync("ViolationDetected", notification);

                // Notify examiners for the exam
                if (examId.HasValue)
                {
                    await _hubContext.Clients.Group($"exam_{examId.Value}").SendAsync("ViolationDetected", notification);
                }

                // If severity is Error, notify all examiners
                if (notification.Severity == "Error")
                {
                    await _hubContext.Clients.Group("examiners").SendAsync("ViolationDetected", notification);
                }

                _logger.LogInformation("Notification sent: ViolationDetected for SubmissionId {SubmissionId}, Type: {Type}, Severity: {Severity}", 
                    notification.SubmissionId, notification.ViolationType, notification.Severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ViolationDetected notification");
            }
        }

        public async Task NotifyUserAsync(string userId, NotificationDto notification)
        {
            try
            {
                notification.Timestamp = DateTime.UtcNow;
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("Notification", notification);
                _logger.LogInformation("Notification sent to user {UserId}: {EventType}", userId, notification.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        public async Task NotifyGroupAsync(string groupName, NotificationDto notification)
        {
            try
            {
                notification.Timestamp = DateTime.UtcNow;
                await _hubContext.Clients.Group(groupName).SendAsync("Notification", notification);
                _logger.LogInformation("Notification sent to group {GroupName}: {EventType}", groupName, notification.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to group {GroupName}", groupName);
            }
        }
    }
}

