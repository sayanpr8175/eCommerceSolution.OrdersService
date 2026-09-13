
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
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
    private readonly IDistributedCache _cache;
    public RabbitMQProductNameUpdateConsumer(
        IConfiguration configuration, 
        ILogger<RabbitMQProductNameUpdateConsumer> logger,
        IDistributedCache cache)
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

        _cache = cache;

    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }



    public void Consume()
    {
        var headers = new Dictionary<string, object>()
        {
           { "x-match", "all"},
           {"event", "product.update" },
           {"RowCount", 1 }
        };

        //string routingKey = "product.update.name";
        // string routingKey = "product.#";
        //string routingKey = "product.update.*";
        string queueName = "orders.products.update.name.queue";

        string exchangeName = _configuration["RabbitMQ_Products_Exchange"]!;

        _channel.ExchangeDeclare(exchange: exchangeName,
            //type: ExchangeType.Direct,
            //type: ExchangeType.Topic,
            type: ExchangeType.Headers,
            durable: true);

        // Creating the messge queue
        _channel.QueueDeclare(queue:queueName,
            durable: true, 
            exclusive: false,
            autoDelete:false,
            arguments: null);

        // Bind the queue to the msg channel

        //_channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);

        // updated consumer for headers
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: string.Empty, arguments: headers);

        EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (sender, args) =>
        {
            byte[] body = args.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);

            ProductDTO? productNameMessage = JsonSerializer.Deserialize<ProductDTO>(message);

            // Update Product cache
            await HandleProductUpdation(productNameMessage);

        };

        _channel.BasicConsume(queue: queueName, consumer: consumer, autoAck: true);

    }

    private async Task HandleProductUpdation(ProductDTO productDTO)
    {
        string productObj = JsonSerializer.Serialize(productDTO);
        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(300));

        string cacheKeyForWrite = $"product:{productDTO.ProductID}";
        await _cache.SetStringAsync(cacheKeyForWrite, productObj, options);

        _logger.LogInformation($"Product Name has been updated:  {productDTO.ProductID}, " +
                $" New Name: {productDTO.ProductName}");
    }
}

