
using Microsoft.Extensions.Hosting;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceBus;
public class ServiceBusProductNameUpdateHostedService : IHostedService
{
    private readonly IServiceBusConsumer _productNameUpdateConsumer;

    public ServiceBusProductNameUpdateHostedService(IServiceBusConsumer consumer)
    {
        _productNameUpdateConsumer = consumer;
        
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _productNameUpdateConsumer.ConsumeAsync();

       
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _productNameUpdateConsumer.Dispose();

        return Task.CompletedTask;
    }
}
