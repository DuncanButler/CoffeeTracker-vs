using CoffeeTracker.Data;
using CoffeeTracker.ApiService.Endpoints;
using CoffeeTracker.ApiService.Interfaces;
using CoffeeTracker.ApiService.Repositories;
using CoffeeTracker.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// This approach makes the WebApplicationFactory work with integration tests
namespace CoffeeTracker.ApiService;
public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();
        
        // Add PostgreSQL DbContext using Aspire service discovery with improved connection settings
        builder.AddNpgsqlDbContext<WeatherDbContext>("weatherdb", 
            configureDbContextOptions: options =>
            {
                // Configure PostgreSQL-specific options with improved reliability
                options.UseNpgsql(npgsqlBuilder =>
                {
                    // Enable retry on failure with more attempts and longer intervals
                    npgsqlBuilder.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    
                    // Set appropriate timeout to avoid stream reading errors
                    npgsqlBuilder.CommandTimeout(30);
                    
                    // Explicitly set the migrations history table schema to public
                    npgsqlBuilder.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            });
        
        // Configure JWT authentication
        builder.Services.AddAuthentication(options => 
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
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
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultDevelopmentKeyThatShouldBeReplaced"))
            };
        });
        
        // Add authorization policies
        builder.Services.AddAuthorization(options =>
        {
            // Policy for website access
            options.AddPolicy("WebClientPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("ClientType", "WebApplication");
            });
            
            // Policy for internal API access (service-to-service)
            options.AddPolicy("InternalApiPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("ClientType", "InternalService");
            });
        });
        
        // add service
        builder.Services.AddScoped<IWeatherService, WeatherService>();
        builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        
        // Add services to the container.
        builder.Services.AddProblemDetails();
          // Add health checks
        var healthChecks = builder.Services.AddHealthChecks()
            // Add database health check with more detailed reporting
            .AddDbContextCheck<WeatherDbContext>("weatherdb_ef_check", 
                tags: new[] { "database", "ef" },
                customTestQuery: async (context, cancellationToken) => 
                    await context.Forecasts.AnyAsync(cancellationToken: cancellationToken));
        
        // Only add PostgreSQL connection check when not in Testing environment
        if (builder.Environment.EnvironmentName != "Testing") 
        {
            var connectionString = builder.Configuration.GetConnectionString("weatherdb");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecks.AddNpgSql(connectionString,
                    name: "weatherdb_connection",
                    tags: new[] { "database", "postgres" },
                    timeout: TimeSpan.FromSeconds(5));
            }
        }
        
        // Add Swagger services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
          var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();
        
        // Configure Swagger middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // Add authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Map authentication endpoints
        app.MapAuthEndpoints();
        
        // Map weather endpoint (with authorization)
        app.MapWeatherEndpoints();        // Add health check endpoint with detailed results
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) => 
            {
                context.Response.ContentType = "application/json";
                
                var result = new 
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new 
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.ToString()
                    }),
                    totalDuration = report.TotalDuration.ToString()
                };
                
                await System.Text.Json.JsonSerializer.SerializeAsync(
                    context.Response.Body, result);
            }
        });
        
        // map default extensions from service defaults
        app.MapDefaultEndpoints();
        
        app.Run();
    }
}
