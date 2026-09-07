using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;
    public UsersMicroserviceClient(HttpClient httpClient,
        ILogger<UsersMicroserviceClient> logger,
        IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userID)
    {

        string cacheKey = $"user:{userID}";
        string? cachedUser = await _distributedCache.GetStringAsync(cacheKey);

        if(cachedUser!=null)
        {
            UserDTO? userObj = JsonSerializer.Deserialize<UserDTO>(cachedUser);
            return userObj;
        }

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/users/{userID}");

            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    UserDTO? fallBackUser = await response.Content.ReadFromJsonAsync<UserDTO>();

                    if(fallBackUser == null)
                    {
                        throw new BadHttpRequestException("fallback user failed!");
                    }

                    return fallBackUser;
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad Request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    //throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");

                    // Sending fault data in case of fail

                    return new UserDTO(
                        PersonName: "Temporarily not available",
                        Email: "Temporarily down!",
                        Gender: "Temporarily unavilable",
                        UserId: Guid.Empty
                        );

                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid User ID");
            }

            string cacheKeyForWrite = $"user:{userID}";
            string userJson = JsonSerializer.Serialize(user);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
                .SetSlidingExpiration(TimeSpan.FromSeconds(100));

            await _distributedCache.SetStringAsync(cacheKeyForWrite, userJson, options);

            return user;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Request failed because of " +
                "Circuit breaker is in Open State");

             return new UserDTO(
                    PersonName: "Temporarily unavailable (Circuit breaker)",
                    Email: "Temporarily unavailable (Circuit breaker)",
                    Gender: "Temporarily unavailable (Circuit breaker)",
                    UserId: Guid.Empty
             );

        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Timeout occurred while fetching user data. Returning dummy data.");

            return new UserDTO(
                   PersonName: "Temporarily unavailable (Timeout)",
                   Email: "Temporarily not available (Timeout)",
                   Gender: "Temporarily not available (Timeout)",
                   UserId: Guid.Empty
            );

        }


    }

}
