using System.Text.Json.Serialization;
using HotelPMS.API.Data;
using HotelPMS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelPMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(o =>
                o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
            builder.Services.AddScoped<AnalyticsService>();

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    // Перечисления (RoomStatus, PaymentType) передаются строками: "Free", "Cash" и т.д.
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Описание API: /openapi/v1.json (встроенный генератор .NET)
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                // Применяем миграции автоматически (сначала создайте миграцию Initial)
                using (var scope = app.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
                }
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
