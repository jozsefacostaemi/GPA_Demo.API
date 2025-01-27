using Notification.Lib;
using Web.Core.Business.API.Enums;
using Microsoft.AspNetCore.SignalR;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications
{
    public class NotificationRepository
    {
        private readonly IHubContext<EventHub> _hubContext;
        public NotificationRepository(IHubContext<EventHub> hubContext) =>
            _hubContext = hubContext;

        /* Función que envia a todos los clientes (Broadcast) */
        public async Task SendBroadcastAsync(NotificationEventCodeEnum eventCode, object? payload = null) =>
            await _hubContext.Clients.All.SendAsync(eventCode.ToString(), payload);

        /* Función que envia a un grupo específico */
        public async Task SendToGroupAsync(string groupName, NotificationEventCodeEnum eventCode, object? payload = null) =>
           await _hubContext.Clients.Group(groupName).SendAsync(eventCode.ToString(), payload);

        /* Función que envia a un usuario específico */
        public async Task SendToUserAsync(string userId, NotificationEventCodeEnum eventCode, object? payload = null) =>
            await _hubContext.Clients.User(userId).SendAsync(eventCode.ToString(), payload);

        /* Función que envia a todos excepto ciertos clientes */
        public async Task SendToAllExceptAsync(NotificationEventCodeEnum eventCode, object? payload = null, params string[] excludedConnectionIds) =>
            await _hubContext.Clients.AllExcept(excludedConnectionIds).SendAsync(eventCode.ToString(), payload);
    }
}
