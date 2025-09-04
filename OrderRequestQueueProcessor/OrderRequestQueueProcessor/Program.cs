using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderRequestQueueProcessor;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Data;
using OrderRequestQueueProcessor.Services;
using Serilog;
using Microsoft.EntityFrameworkCore;


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting OrderRequestQueueProcessor host...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // 👈 Hook Serilog into the host
        .ConfigureServices((context, services) =>
        {
            services.Configure<AppSettings>(context.Configuration.GetSection("AppSettings"));

            services.AddSingleton<IQueueRepository, OracleQueueRepository>();
            services.AddSingleton<IOrderRequestHandler, OrderRequestHandler>();
            services.AddHostedService<QueueProcessingService>();
            services.AddDbContext<OrderRequestDbContext>((sp, options) =>
            {
                var appSettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                options.UseOracle(appSettings.OracleConnectionString); // this should compile now
            });
            services.AddScoped<IOrderRequestService, OrderRequestService>();

        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception during startup.");
}
finally
{
    Log.CloseAndFlush();
}
