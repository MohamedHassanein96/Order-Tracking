namespace Order_Tracking.Services
{
    public class OrderService(ApplicationDbContext _context, IConnectionMultiplexer _redis) : IOrderService
    {
        public async Task<bool> AddAsync(OrderRequest request)
        {
            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer == null)
                return false; 
            
                var order = new Order
                {
                    CustomerId = request.CustomerId,
                    Address = request.Address,
                    TotalAmount = request.OrderItemsRequest.Sum(i => i.Quantity * i.UnitPrice),
                    OrderItems = request.OrderItemsRequest.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                }; 

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();


            var db = _redis.GetDatabase();
            var message = JsonSerializer.Serialize(new { Type = "OrderCreated", OrderId = order.Id });
            await db.StreamAddAsync("orders-stream", new NameValueEntry[] { new("message", message) });




            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id , UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            // تحقق من صحة القيمة
            if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
                return false; // أو BadRequest في الـ Controller

            // تحديث الحالة في DB
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // تحديد نوع الرسالة حسب الحالة
            string messageType = request.Status switch
            {
                OrderStatus.OutForDelivery => "OutForDelivery",
                OrderStatus.Delivered => "Delivered",
                _ => null
            !};

            // إرسال الرسالة للـ Worker عبر Redis Stream فقط لو الحالة معروفة
            if (messageType != null)
            {
                var db = _redis.GetDatabase();
                var message = JsonSerializer.Serialize(new { Type = messageType, OrderId = order.Id });
                await db.StreamAddAsync("orders-stream", new NameValueEntry[] { new("message", message) });
            }

            return true;

        }
    }
}
