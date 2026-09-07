using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public ProductsMicroserviceClient(HttpClient httpClient,
        ILogger<ProductsMicroserviceClient> logger,
        IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<ProductDTO?> GetProductByProductID(Guid productID)
    {

        try
        {
            //Fetching from redis
            // Key: product:productID
            // Value {ProductName : "..."}

            string cacheKey = $"product:{productID}";
            string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);

            if(cachedProduct!=null)
            {
                ProductDTO? productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);

                return productFromCache;
            }

            HttpResponseMessage response = await _httpClient.GetAsync($"/api/products/search/product-id/{productID}");

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
                    throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                }
            }

            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

            if (product == null)
            {
                throw new ArgumentException("Invalid Product ID");
            }

            string productObj = JsonSerializer.Serialize(product);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30))
                .SetSlidingExpiration(TimeSpan.FromSeconds(10));

            string cacheKeyForWrite = $"product:{productID}";
            await _distributedCache.SetStringAsync(cacheKeyForWrite, productObj, options);

            return product;
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogError(ex, "Bulkhead isolation blocks the" +
                " request since the request queue is full");

            return new ProductDTO(ProductID: Guid.Empty,
                ProductName: "Services Unavilable (Bulkhead isolation blocked)",
                Category: "Services Unavilable (Bulkhead isolation blocked)",
                UnitPrice: 0,
                QuantityInStock: 0);
        }

        

    }
}
