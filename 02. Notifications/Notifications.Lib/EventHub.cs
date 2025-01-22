using Microsoft.AspNetCore.SignalR;

namespace Notification.Lib;

public class EventHub : Hub
{
    /* Función que envia mensaje a todos los clientes */
    public async Task SendBroadcast(string eventCode, object? payload = null) => await Clients.All.SendAsync("ReceiveEvent", eventCode, payload);

    /* Función que envia mensaje a envia a un grupo específico */
    public async Task SendToGroup(string groupName, string eventCode, object? payload = null) => await Clients.Group(groupName).SendAsync("ReceiveEvent", eventCode, payload);

    /* Función que envia mensaje a envia a un usuario especifico */
    public async Task SendToUser(string userId, string eventCode, object? payload = null) => await Clients.User(userId).SendAsync("ReceiveEvent", eventCode, payload);

    /* Función que envia a todos excepto a ciertos clientes */
    public async Task SendToAllExcept(string eventCode, object? payload, params string[] excludedConnectionIds) => await Clients.AllExcept(excludedConnectionIds).SendAsync("ReceiveEvent", eventCode, payload);

}