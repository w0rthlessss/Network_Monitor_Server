
using Microsoft.EntityFrameworkCore;
using Network_Monitor_API.Data;

namespace Network_Monitor_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<MainDBContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5433;Database=connectionsDb;Username=connectionsDbUser;Password=connectionsDbPassword123"));
            builder.Services.AddDbContext<SystemUsageDbContext>(options =>
                options.UseNpgsql("Host=localhost;Port=5434;Database=sysUsageDb;Username=sysUsageDbuser;Password=sysUsageDbPassword123"));


            builder.Services.AddScoped<Network_Monitor_API.Services.AlertsService>();
            builder.Services.AddScoped<Network_Monitor_API.Services.ConnectionService>();
            builder.Services.AddScoped<Network_Monitor_API.Services.PredictionService>();
            builder.Services.AddScoped<Network_Monitor_API.Services.SystemUsageService>();
            builder.Services.AddScoped<Network_Monitor_API.Services.ModelService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            using (var scope = app.Services.CreateScope())
            {
                var mainDbContext = scope.ServiceProvider.GetRequiredService<MainDBContext>();
                mainDbContext.Database.Migrate();
                var sysUsageDbContext = scope.ServiceProvider.GetRequiredService<SystemUsageDbContext>();
                sysUsageDbContext.Database.Migrate();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
