using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceBus;
public interface IServiceBusPublisher
{
    Task Publish<T>(string topicName, Dictionary<string, object> headers, T message);
}
