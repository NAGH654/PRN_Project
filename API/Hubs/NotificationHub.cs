using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            // Auto-subscribe users to role-based groups
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                switch (role)
                {
                    case "Manager":
                    case "Admin":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "managers");
                        break;
                    case "Moderator":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "moderators");
                        await Groups.AddToGroupAsync(Context.ConnectionId, "managers"); // Moderators also get manager notifications
                        break;
                    case "Examiner":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "examiners");
                        break;
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            // Remove from role-based groups
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                switch (role)
                {
                    case "Manager":
                    case "Admin":
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "managers");
                        break;
                    case "Moderator":
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "moderators");
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "managers");
                        break;
                    case "Examiner":
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "examiners");
                        break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to notifications for a specific exam
        /// </summary>
        public async Task SubscribeToExam(Guid examId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"exam_{examId}");
        }

        /// <summary>
        /// Unsubscribe from notifications for a specific exam
        /// </summary>
        public async Task UnsubscribeFromExam(Guid examId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exam_{examId}");
        }

        /// <summary>
        /// Subscribe to notifications for managers (all exam events)
        /// </summary>
        public async Task SubscribeToManagerNotifications()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "managers");
        }

        /// <summary>
        /// Subscribe to notifications for moderators (violations and zero-score submissions)
        /// </summary>
        public async Task SubscribeToModeratorNotifications()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "moderators");
        }

        /// <summary>
        /// Subscribe to notifications for examiners (assigned exams)
        /// </summary>
        public async Task SubscribeToExaminerNotifications()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "examiners");
        }
    }
}

