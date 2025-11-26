using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Order_Tracking.Consts;
using Order_Tracking.Hubs;

namespace Order_Tracking.Services
{
    public class OrderWorkerService : BackgroundService
    {
        private readonly IHubContext<OrdersHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;

        public OrderWorkerService(IHubContext<OrdersHub> hubContext, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var pendingEvents = await _context.OrderEvents
                        .Where(e => !e.IsProcessed)
                        .ToListAsync(stoppingToken);

                    foreach (var orderEvent in pendingEvents)
                    {
                        string message = orderEvent.Status switch
                        {
                            OrderStatus.OrderCreated => $"Order {orderEvent.OrderId}, Status: Order in Preparation",
                            OrderStatus.OutForDelivery => $"Order {orderEvent.OrderId}, Status: Out For Delivery",
                            OrderStatus.Delivered => $"Order {orderEvent.OrderId}, Status: has been Delivered",
                            _ => $"Order {orderEvent.OrderId} updated: {orderEvent.Status}"
                        };

                        if (orderEvent.Status == OrderStatus.OrderCreated)
                        {
                            await _hubContext.Clients.Group(SignalRGroups.Admins)
                                .SendAsync("ReceiveOrderUpdate", $"{message} ", cancellationToken: stoppingToken);
                                Console.WriteLine($" OrderId {orderEvent.OrderId}");
                        }


                        if (orderEvent.Status == OrderStatus.OutForDelivery)
                        {
                            await _hubContext.Clients.Groups($"user-{orderEvent.CustomerId}", SignalRGroups.Delivery)
                                .SendAsync("ReceiveOrderUpdate", $"{message} ", cancellationToken: stoppingToken);
                        }

                        if (orderEvent.Status == OrderStatus.Delivered)
                        {
                            await _hubContext.Clients.Groups($"user-{orderEvent.CustomerId}", SignalRGroups.Admins)
                                .SendAsync("ReceiveOrderUpdate", $"{message} ", cancellationToken: stoppingToken);
                        }
                       

                        orderEvent.IsProcessed = true;
                    }

                    await _context.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
