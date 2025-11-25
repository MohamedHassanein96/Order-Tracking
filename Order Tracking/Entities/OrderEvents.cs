namespace Order_Tracking.Entities
{
    public class OrderEvents
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsProcessed { get; set; }
        public int CustomerId { get; set; }

    }
}
