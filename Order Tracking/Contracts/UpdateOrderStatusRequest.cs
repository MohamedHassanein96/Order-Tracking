namespace Order_Tracking.Contracts
{
    public record UpdateOrderStatusRequest(OrderStatus Status , int CustomerId);
    
}
