

namespace Order_Tracking
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy = null;
            });


            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();


            builder.Services.AddScoped<IOrderService, OrderService>();

            // register Hub
            builder.Services.AddSignalR();

            builder.Services.AddCors();


            //Worker HostedService
            builder.Services.AddHostedService<OrderWorkerService>();

         

            var app = builder.Build();

            app.UseRouting(); // ✅ لازم قبل UseCors

            app.UseCors(builder =>
            {
                builder
                    .WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });




            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            // URL of ordersHub to enable clients connecting with this hub
            app.MapHub<OrdersHub>("/ordersHub");


            app.Run();
        }
    }
}
