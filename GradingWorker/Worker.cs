using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GradingWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _rabbitUri;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _rabbitUri = config["RabbitMQ:Uri"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitUri) };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Lắng nghe hàng đợi bài nộp mới
        channel.QueueDeclare(queue: "submission_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        // Khai báo hàng đợi kết quả chấm xong
        channel.QueueDeclare(queue: "graded_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"[Worker] Nhận yêu cầu chấm bài: {messageJson}");

            var data = JsonSerializer.Deserialize<SubmissionMessage>(messageJson);
            if (data != null)
            {
                // Giả lập thời gian máy chấm bài mất 3 giây
                await Task.Delay(3000, stoppingToken);

                // Quy tắc chấm tự động giả lập theo độ dài bài làm
                double score = data.Content.Length >= 20 ? 9.0 : 4.5;
                string feedback = data.Content.Length >= 20
                    ? "Bài làm đầy đủ ý, trình bày rõ ràng!"
                    : "Nội dung bài làm quá ngắn, cần bổ sung thêm.";

                // Cập nhật điểm, nhận xét và trạng thái vào Database
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GradingDbContext>();
                var submission = await db.Submissions.FindAsync(data.SubmissionId);

                if (submission != null)
                {
                    submission.Status = "Graded";
                    submission.Score = score;
                    submission.Feedback = feedback;
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"[Worker] Đã chấm xong bài #{submission.Id} -> Điểm: {score}");

                    // Phát sự kiện hoàn thành sang graded_queue để NotificationService báo cho sinh viên
                    var gradedEvent = JsonSerializer.Serialize(new
                    {
                        SubmissionId = submission.Id,
                        StudentId = submission.StudentId,
                        AssignmentId = submission.AssignmentId,
                        Status = submission.Status,
                        Score = submission.Score,
                        Feedback = submission.Feedback
                    });
                    var eventBody = Encoding.UTF8.GetBytes(gradedEvent);
                    channel.BasicPublish(exchange: "", routingKey: "graded_queue", basicProperties: null, body: eventBody);
                }
            }

            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: "submission_queue", autoAck: false, consumer: consumer);

        // Giữ Worker chạy liên tục
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public record SubmissionMessage(int SubmissionId, int StudentId, int AssignmentId, string Content);