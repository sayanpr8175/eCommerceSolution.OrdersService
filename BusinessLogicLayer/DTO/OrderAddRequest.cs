namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record OrderAddRequest (Guid UserId, DateTime OrderDate, List<OrderItemAddRequest> OrderItems)
{
    public OrderAddRequest() : this(default, default, default)
    {
    }
}
