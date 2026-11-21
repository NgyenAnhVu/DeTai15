using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace SubmissionAPI.Services;

public class RabbitMqPublisher
{
    private readonly string _uri;

    public RabbitMqPublisher(IConfiguration config)
    {
        _uri = config["RabbitMQ:Uri"]!;
    }

    public void PublishSubmission(object message)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_uri) };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "submission_queue",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: "",
                             routingKey: "submission_queue",
                             basicProperties: null,
                             body: body);
    }
}