using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GiaLaiOCOP.Api.Data;

namespace GiaLaiOCOP.Api.Tests.Integration
{
    /// <summary>
    /// WebApplicationFactory cho integration tests
    /// </summary>
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        // Use a unique database name per factory instance to avoid conflicts between test classes
        private readonly string _databaseName = "TestDb_" + Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
                    .ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove Npgsql services if any
                var npgsqlServices = services.Where(s =>
                    (s.ServiceType.FullName?.Contains("Npgsql") == true) ||
                    (s.ImplementationType?.FullName?.Contains("Npgsql") == true) ||
                    (s.ImplementationInstance?.GetType().FullName?.Contains("Npgsql") == true))
                    .ToList();

                foreach (var service in npgsqlServices)
                {
                    services.Remove(service);
                }

                // Add in-memory database with a unique name per factory instance
                // This ensures each test class has its own isolated database
                var databaseName = _databaseName;
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });

            builder.UseEnvironment("Testing");
        }
    }
}
