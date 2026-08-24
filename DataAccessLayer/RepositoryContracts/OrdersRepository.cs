using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using MongoDB.Driver;


namespace eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;

public class OrdersRepository : IOrdersRepository
{
    private readonly IMongoCollection<Order> _orders;
    private readonly string collectionName = "orders";

    public OrdersRepository(IMongoDatabase mongoDatabase)
    {
         _orders =  mongoDatabase.GetCollection<Order>(collectionName);
    }

    public Task<Order?> AddOrder(Order order)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteOrder(Guid orderID)
    {
        throw new NotImplementedException();
    }

    public Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Order>> GetOrders()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        throw new NotImplementedException();
    }

    public Task<Order?> UpdateOrder(Order order)
    {
        throw new NotImplementedException();
    }
}
