
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System;


namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies
{
    public class PollyPolicies : IPollyPolicies
    {
        private readonly ILogger<UsersMicroservicePolicies> _logger;

        public PollyPolicies(ILogger<UsersMicroservicePolicies> logger)
        {
            _logger = logger;
        }

        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy( int _retryCount)
        {

            AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
            .WaitAndRetryAsync(retryCount: _retryCount,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, Context) =>
            {
                _logger.LogInformation($"Retry attempt : {retryAttempt} after {timespan.TotalSeconds} seconds.");
            });

            return policy;

        }

        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int 
            handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(resp => !resp.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
            durationOfBreak: durationOfBreak,

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

        public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
        {
            AsyncTimeoutPolicy<HttpResponseMessage> policy = 
                Policy.TimeoutAsync<HttpResponseMessage>(timeout);

            return policy;
        }

       

    }
}
