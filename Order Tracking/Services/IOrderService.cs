namespace Order_Tracking.Services
{
    public interface IOrderService
    {
        Task<bool> AddAsync(OrderRequest request); 
        Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusRequest request);
        public Task<IEnumerable<OrderTrackingResponse>> GetActiveOrdersAsync(int id);

    }
}
