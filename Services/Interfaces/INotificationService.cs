using Services.Dtos.Responses;

namespace Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifySubmissionUploadedAsync(SubmissionUploadedNotificationDto notification, Guid? examId = null);
        Task NotifySubmissionGradedAsync(SubmissionGradedNotificationDto notification, Guid? examId = null);
        Task NotifyViolationDetectedAsync(ViolationDetectedNotificationDto notification, Guid? examId = null);
        Task NotifyUserAsync(string userId, NotificationDto notification);
        Task NotifyGroupAsync(string groupName, NotificationDto notification);
    }
}


