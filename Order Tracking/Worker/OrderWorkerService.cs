using Microsoft.AspNetCore.SignalR;
using Order_Tracking.Consts;
using Order_Tracking.Hubs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Order_Tracking.Services
{
    public class OrderWorkerService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<OrdersHub> _hubContext;

        public OrderWorkerService(IConnectionMultiplexer redis, IHubContext<OrdersHub> hubContext)
        {
            _redis = redis;
            _hubContext = hubContext;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var db = _redis.GetDatabase();

            try
            {
                await db.StreamCreateConsumerGroupAsync(RedisConsts.OrdersStream, RedisConsts.OrdersGroup, "$", createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
            }


            while (!stoppingToken.IsCancellationRequested)
            {
                var entries = await db.StreamReadGroupAsync(
                    key: RedisConsts.OrdersStream,
                    groupName: RedisConsts.OrdersGroup,
                    consumerName: "worker-1",
                    count: 1,
                    noAck: false
                );

                if (entries.Length == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    var message = entry.Values.First(v => v.Name == "message").Value.ToString();
                    var data = JsonSerializer.Deserialize<OrderMessage>(message);

                    if (data.Type == "OrderCreated")
                    {
                        await _hubContext.Clients.Group(SignalRGroups.Admins)
                            .SendAsync("ReceiveOrderUpdate", $"Order Created: OrderId {data.OrderId}", cancellationToken: stoppingToken);
                    }


                    if (data.Type == "OutForDelivery")
                    {
                        await _hubContext.Clients.Groups($"user-{data.CustomerId}", SignalRGroups.Delivery)
                            .SendAsync("ReceiveOrderUpdate", $"{data.Type} for OrderId: {data.OrderId}", cancellationToken: stoppingToken);
                    }

                    if (data.Type == "Delivered")
                    {
                        await _hubContext.Clients.Groups($"user-{data.CustomerId}", SignalRGroups.Admins)
                            .SendAsync("ReceiveOrderUpdate", $"{data.Type} for OrderId: {data.OrderId}", cancellationToken: stoppingToken);
                    }


                    await db.StreamAcknowledgeAsync(RedisConsts.OrdersStream, RedisConsts.OrdersGroup, entry.Id);
                }
            }
        }
    }

    public record OrderMessage(string Type, int OrderId, int CustomerId);
}
