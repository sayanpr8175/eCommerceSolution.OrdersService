using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;


namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductNameUpdateConsumer : IDisposable, IRabbitMQProductNameUpdateConsumer
{
    private readonly IConfiguration _configuration;

    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQProductNameUpdateConsumer> _logger;
    public RabbitMQProductNameUpdateConsumer(IConfiguration configuration, ILogger<RabbitMQProductNameUpdateConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;

        string hostName = _configuration["RabbitMQ_HostName"];
        string userName = _configuration["RabbitMQ_UserName"];
        string password = _configuration["RabbitMQ_Password"];
        string port = _configuration["RabbitMQ_Port"];

        ConnectionFactory connectionFactory = new ConnectionFactory()
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            Port = Convert.ToInt32(port)
        };

        _connection = connectionFactory.CreateConnection();

        _channel = _connection.CreateModel();

    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }



    public void Consume()
    {
        string routingKey = "product.update.name";
        string queueName = "orders.products.update.name.queue";

        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;

        _channel.ExchangeDeclare(exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true);

        // Creating the messge queue
        _channel.QueueDeclare(queue:queueName,
            durable: true, 
            exclusive: false,
            autoDelete:false,
            arguments: null);

        // Bind the queue to the msg channel

        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);

        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            ProductNameUpdateMessage? productNameMessage = JsonSerializer.Deserialize<ProductNameUpdateMessage>(message);

            _logger.LogInformation($"Product Name has been updated:  {productNameMessage.ProductID}, " +
                $" New Name{productNameMessage.NewName}");

        };

        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);

    }
}

