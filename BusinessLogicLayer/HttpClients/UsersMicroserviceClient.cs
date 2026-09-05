using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroserviceClient> _logger;
    public UsersMicroserviceClient(HttpClient httpClient, ILogger<UsersMicroserviceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userID)
    {

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"/api/users/{userID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
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
