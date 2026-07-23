using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using SteamTracker.Application.Ports;
using SteamTracker.Application.UseCases;
using SteamTracker.Domain.Services;
using SteamTracker.Infrastructure;
using SteamTracker.Infrastructure.Data;
using SteamTracker.Worker;

var hostBuilder = Host.CreateApplicationBuilder(args);

// Infrastructure (registers Func<Task<IConnection>> using RabbitMQ:HostName etc.)
hostBuilder.Services.AddInfrastructure(hostBuilder.Configuration);

// Resolve IConnection from the infrastructure's properly-configured factory
hostBuilder.Services.AddSingleton<IConnection>(sp =>
    sp.GetRequiredService<Func<Task<IConnection>>>().Invoke().GetAwaiter().GetResult());

// Application — use cases
hostBuilder.Services.AddScoped<ISetAlertRuleUseCase, SetAlertRuleUseCase>();
hostBuilder.Services.AddScoped<IDeleteAlertRuleUseCase, DeleteAlertRuleUseCase>();
hostBuilder.Services.AddScoped<IProcessPriceCheckUseCase>(sp =>
    new ProcessPriceCheckUseCase(
        sp.GetRequiredService<IGameRepository>(),
        sp.GetRequiredService<IAlertRuleRepository>(),
        sp.GetRequiredService<INotificationPublisher>(),
        sp.GetRequiredService<PriceAlertEvaluator>(),
        hostBuilder.Configuration));
hostBuilder.Services.AddScoped<IHandleWishlistItemAddedUseCase, HandleWishlistItemAddedUseCase>();
hostBuilder.Services.AddScoped<IHandleWishlistItemRemovedUseCase, HandleWishlistItemRemovedUseCase>();
hostBuilder.Services.AddSingleton<PriceAlertEvaluator>();

// Background workers
hostBuilder.Services.AddHostedService<PriceCheckScheduler>();
hostBuilder.Services.AddHostedService<PriceCheckWorker>();
hostBuilder.Services.AddHostedService<WishlistSyncWorker>();

var host = hostBuilder.Build();

// Apply migrations at startup
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SteamTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

host.Run();
