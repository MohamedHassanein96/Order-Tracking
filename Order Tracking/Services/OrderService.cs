using Microsoft.EntityFrameworkCore;
using Order_Tracking.Consts;

namespace Order_Tracking.Services
{
    public class OrderService(ApplicationDbContext _context) : IOrderService
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
                OrderItems = request.OrderItemsRequest.Select(i => new OrderItems
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderEvent = new OrderEvents
            {
                OrderId = order.Id,
                Status = OrderStatus.OrderCreated,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                CustomerId = request.CustomerId
            };


            _context.OrderEvents.Add(orderEvent);
            await _context.SaveChangesAsync();

            #region old
            //var db = _redis.GetDatabase();
            //var message = JsonSerializer.Serialize(new { Type = "OrderCreated" , OrderId = order.Id, order.CustomerId });
            //await db.StreamAddAsync(RedisConsts.OrdersStream, new NameValueEntry[] { new(nameof(message), message) }); 
            #endregion

            return true;

        }

        public async Task<bool> UpdateStatusAsync(int id , UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;



            if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
                return false; 

          
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();



            // STEP 2 — Add Event to OrderEvents table
            var orderEvent = new OrderEvents
            {
                OrderId = order.Id,
                Status = request.Status,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false,
                CustomerId = request.CustomerId
            };


            _context.OrderEvents.Add(orderEvent);

            await _context.SaveChangesAsync();
            #region Old
            //await _context.SaveChangesAsync();
            //string messageType = request.Status switch
            //{
            //    OrderStatus.OutForDelivery => "OutForDelivery",
            //    OrderStatus.Delivered => "Delivered",
            //    _ => null!
            //};

            //if (messageType != null)
            //{
            //    var db = _redis.GetDatabase();
            //    var orderMessage = new OrderMessage(
            //                                        Type: messageType,
            //                                        OrderId: order.Id,
            //                                        CustomerId: request.CustomerId);

            //    var message = JsonSerializer.Serialize(orderMessage);
            //    await db.StreamAddAsync(RedisConsts.OrdersStream, new NameValueEntry[] { new (nameof(message), message) } );
            //} 
            #endregion

            return true;

        }
        public async Task<IEnumerable<OrderTrackingResponse>> GetActiveOrdersAsync(int id)
        {
            return await _context.Orders
                        .Where(x => x.CustomerId == id &&
                            (
                                (x.Status == OrderStatus.Delivered &&
                                 x.UpdatedAt.HasValue &&
                                 EF.Functions.DateDiffDay(x.UpdatedAt.Value, DateTime.UtcNow) <= 3)
                                ||
                                (x.Status != OrderStatus.Delivered)
                            )
                        ).Select(x => new OrderTrackingResponse(x.Id, x.Status, x.UpdatedAt, x.UpdatedAt))
                        .ToListAsync();
        }
    }
}
