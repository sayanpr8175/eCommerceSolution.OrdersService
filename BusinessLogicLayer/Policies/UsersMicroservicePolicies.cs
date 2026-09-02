
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;


namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class UsersMicroservicePolicies : IUsersMicroservicePolicies
    {
        private readonly ILogger<UsersMicroservicePolicies> _logger;

        public UsersMicroservicePolicies(ILogger<UsersMicroservicePolicies> logger)
        {
            _logger = logger;
        }
        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {

            AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
            .WaitAndRetryAsync(retryCount: 5,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, Context) =>
            {
                _logger.LogInformation($"Retry attempt : {retryAttempt} after {timespan.TotalSeconds} seconds.");
            });

            return policy;


        }
    }
}
