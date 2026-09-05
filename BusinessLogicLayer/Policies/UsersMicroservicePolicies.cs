
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;


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

        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromMinutes(2),

            onBreak: (outcome, timespan) =>
            {
                _logger.LogInformation($"Circuit breaker is open for {timespan.TotalSeconds} minutes, due to consecutive 3 failures." +
                    $" The subsequent requests will be blocked");

            }, onReset : () =>
            {
                _logger.LogInformation($"Circuit breaker is closed, subsequent requests will be allowed");
            });

            return policy;
        }

        public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(1500));

            return policy;
        }
    }
}
