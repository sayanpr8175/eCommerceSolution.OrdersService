
using Azure.Messaging.ServiceBus;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceBus;

public class ServiceBusProductUpdateConsumer : IServiceBusConsumer
{
    private readonly ServiceBusProcessor _serviceBusProcessor;

    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<ServiceBusProductUpdateConsumer> _logger;
    private readonly IConfiguration _configuration;

    //private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusProductUpdateConsumer(IDistributedCache distributedCache,
        ILogger<ServiceBusProductUpdateConsumer> logger,
        ServiceBusClient serviceBusClient,
        IConfiguration configuration)
    {
        //_serviceBusProcessor = serviceBusProcessor;
        _distributedCache = distributedCache;
        _logger = logger;
        _configuration = configuration;
        //_serviceBusClient = serviceBusClient;

        _serviceBusProcessor = serviceBusClient.CreateProcessor(_configuration["ServiceBus:ServiceBus_ProductsTopic"],
            _configuration["ServiceBus:ServiceBus_ProductsTopic_Subscription"],
            new ServiceBusProcessorOptions()
        {
            AutoCompleteMessages = false
        });

        _serviceBusProcessor.ProcessMessageAsync += _serviceBusProcessor_ProcessMessageAsync;

        _serviceBusProcessor.ProcessErrorAsync += _serviceBusProcessor_ProcessErrorAsync;
    }

    private async Task _serviceBusProcessor_ProcessMessageAsync(ProcessMessageEventArgs arg)
    {
        string messgaeBodyJson = arg.Message.Body.ToString();
        ProductDTO? productdto = JsonSerializer.Deserialize<ProductDTO>(messgaeBodyJson);

        if(productdto!=null)
        {
            await HandleProductUpdation(productdto);
        }

        await arg.CompleteMessageAsync(arg.Message);
    }

    private async Task HandleProductUpdation(ProductDTO productDTO)
    {

        _logger.LogInformation($"Product Name has been updated:  {productDTO.ProductID}, " +
                $" New Name: {productDTO.ProductName} (Servicebus notification)");

        string productObj = JsonSerializer.Serialize(productDTO);
        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(300));

        string cacheKeyForWrite = $"product:{productDTO.ProductID}";
        await _distributedCache.SetStringAsync(cacheKeyForWrite, productObj, options);

        
    }

    private Task _serviceBusProcessor_ProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        throw new NotImplementedException();
    }



    public async Task ConsumeAsync()
    {
        await _serviceBusProcessor.StartProcessingAsync();
    }

    public async void Dispose()
    {
        await _serviceBusProcessor.DisposeAsync();
    }
}
