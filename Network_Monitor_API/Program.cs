using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Network_Monitor_API.Data;
using Network_Monitor_API.Hubs;
using Network_Monitor_API.Models.MainDBModels;
using Network_Monitor_API.Services;

namespace Network_Monitor_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<MainDBContext>(options =>
                options.UseNpgsql(builder.Configuration["DB_MAIN_CONNECTION_STRING"]));
            builder.Services.AddDbContext<SystemUsageDbContext>(options =>
                options.UseNpgsql(builder.Configuration["DB_SYS_USAGE_CONNECTION_STRING"]));

            builder.Services.AddScoped<AlertsService>();
            builder.Services.AddScoped<ConnectionService>();
            builder.Services.AddScoped<PredictionService>();
            builder.Services.AddScoped<SystemUsageService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<UserService>();

            builder.Services.AddHttpClient<PythonServiceClient>(client =>
            {
                client.BaseAddress = new Uri(
                    builder.Configuration["PythonService:BaseUrl"]
                    ?? "http://localhost:8000");
            });
            builder.Services.AddScoped<ModelService>();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
                    };
                    // WebSocket не может передавать заголовки, поэтому SignalR отправляет токен через query string
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                                context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddSignalR();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

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
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<MonitorHub>("/hubs/monitor");
            app.Run();
        }
    }
}
