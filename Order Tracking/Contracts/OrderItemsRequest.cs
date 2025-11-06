namespace Order_Tracking.Contracts
{
    public record OrderItemsRequest(int ProductId, int Quantity, decimal UnitPrice);

}
