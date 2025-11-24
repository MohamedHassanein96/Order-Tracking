namespace Order_Tracking.Contracts
{
    public record OrderTrackingResponse(int OrderId, OrderStatus Status, DateTime? LastUpdatedAt, DateTime? DeliveredAt);
   
}
