namespace Order_Tracking.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.OrderCreated;
        public decimal TotalAmount { get; set; }
        public string Address { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int CustomerId { get; set; }
        public User Customer { get; set; } = default!;
        public ICollection<OrderItem> OrderItems { get; set; } = [];

    }
    public enum OrderStatus
    {
        OrderCreated,
        OutForDelivery,
        Delivered
    }
}
