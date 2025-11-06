using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using Order_Tracking.Hubs;

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
                await db.StreamCreateConsumerGroupAsync("orders-stream", "orders-group", "$", createStream: true);
                Console.WriteLine("Consumer group created.");
            }
            catch (StackExchange.Redis.RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                Console.WriteLine("Consumer group already exists.");
            }

            Console.WriteLine("Worker started. Listening to Redis Stream...");

            while (!stoppingToken.IsCancellationRequested)
            {
                var entries = await db.StreamReadGroupAsync(
                    key: "orders-stream",
                    groupName: "orders-group",
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

                    Console.WriteLine($"Received {data.Type} for OrderId: {data.OrderId}");

                    // إرسال الإشعار للـ Hub
                    await _hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", $"{data.Type} for OrderId: {data.OrderId}");

                    await db.StreamAcknowledgeAsync("orders-stream", "orders-group", entry.Id);
                }
            }
        }
    }

    public record OrderMessage(string Type, int OrderId);
}
