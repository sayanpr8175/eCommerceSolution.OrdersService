
namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceBus;

public interface IServiceBusConsumer : IDisposable
{

    Task ConsumeAsync();

}
