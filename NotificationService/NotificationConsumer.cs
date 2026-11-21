using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService;

public class NotificationConsumer : BackgroundService
{
    private readonly ILogger<NotificationConsumer> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly string _rabbitUri;

    public NotificationConsumer(ILogger<NotificationConsumer> logger, IHubContext<NotificationHub> hubContext, IConfiguration config)
    {
        _logger = logger;
        _hubContext = hubContext;
        _rabbitUri = config["RabbitMQ:Uri"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitUri) };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "graded_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"[Notification] Đã chấm xong, bắn realtime tới sinh viên: {messageJson}");

            // Gửi thông báo realtime qua SignalR tới tất cả client đang kết nối
            await _hubContext.Clients.All.SendAsync("ReceiveGradingResult", messageJson, cancellationToken: stoppingToken);

            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: "graded_queue", autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}