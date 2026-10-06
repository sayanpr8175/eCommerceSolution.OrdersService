
namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceBus;

public class ServiceBusPublisher : IServiceBusPublisher
{
    public Task Publish<T>(string topicName, Dictionary<string, object> headers, T message)
    {
        throw new NotImplementedException();
    }
}
