using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using server.Data;

namespace server.tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeTimeProvider TimeProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's ApplicationDbContext registration
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
            
            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(System.Data.Common.DbConnection));
            if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);

            // Add ApplicationDbContext options directly to avoid running AddDbContext configuration from Program.cs
            services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("InMemoryDbForTesting")
                .Options);

            // Replace TimeProvider with FakeTimeProvider
            var timeProviderDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(TimeProvider));
            if (timeProviderDescriptor != null)
            {
                services.Remove(timeProviderDescriptor);
            }
            services.AddSingleton<TimeProvider>(TimeProvider);

            // Add custom test authentication
            services.AddAuthentication(TestAuthHandler.DefaultScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.DefaultScheme, options => { });
        });
    }
}
