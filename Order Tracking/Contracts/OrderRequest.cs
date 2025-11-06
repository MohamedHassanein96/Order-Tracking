namespace Order_Tracking.Contracts
{
    public record OrderRequest(int CustomerId,string Address,IEnumerable<OrderItemsRequest> OrderItemsRequest);
    
    
}
