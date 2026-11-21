using Microsoft.AspNetCore.SignalR;

namespace NotificationService;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Client mới đã kết nối: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
}