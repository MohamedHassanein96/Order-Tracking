using StackExchange.Redis;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;

public record OrderMessage(string Type, int OrderId);

namespace OrderWorker
{
    internal class Program
    {
        static async Task Main()
        {
            var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
            var db = redis.GetDatabase();

            // 👇 إنشاء الـ Consumer Group مرة واحدة
            try
            {
                // key, groupName, position, createStream
                await db.StreamCreateConsumerGroupAsync(
                    "orders-stream",    // اسم الـ Stream
                    "orders-group",     // اسم الـ Group
                    "$",                // يبدأ بالرسائل الجديدة
                    createStream: true  // لو الـ Stream مش موجود ينشأه
                );
                Console.WriteLine("✅ Consumer group created.");
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                Console.WriteLine("ℹ Consumer group already exists.");
            }

            Console.WriteLine("🚀 Worker started. Listening to Redis Stream...");

            while (true)
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
                    await Task.Delay(1000);
                    continue;
                }

                foreach (var entry in entries)
                {
                    var message = entry.Values.First(v => v.Name == "message").Value.ToString();
                    var data = JsonSerializer.Deserialize<OrderMessage>(message);

                    Console.WriteLine($"📦 Received {data.Type} for OrderId: {data.OrderId}");

                    // الاعتراف بالرسالة بعد تنفيذها
                    await db.StreamAcknowledgeAsync("orders-stream", "orders-group", entry.Id);
                }
            }
        }
    }
}
