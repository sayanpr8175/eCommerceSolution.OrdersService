using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;
public interface IProductsMicroservicePolicies
{
    IAsyncPolicy<HttpResponseMessage> GetFallBackPolicy();
    IAsyncPolicy<HttpResponseMessage> GetBulkHeadIsolationPolicy();

}
