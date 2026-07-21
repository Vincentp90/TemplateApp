using Application;
using Application.Contracts;
using Application.Queries;
using Application.UseCases;
using Application.UseCases.AppListing;
using Application.UseCases.Auction;
using Application.UseCases.Auth;
using Application.UseCases.User;
using Application.UseCases.Wishlist;
using Infrastructure.Persistence;
using Infrastructure.Persistence.AppListings;
using Infrastructure.Persistence.Auctions;
using Infrastructure.Persistence.Users;
using Infrastructure.Persistence.Wishlist;
using Infrastructure.ReadAdapters;
using Infrastructure.Messaging;
using CrossService.Messaging;
using RabbitMQ.Client;
using Domain;
using Domain.Helpers;
using Domain.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System;
using System.Text;
using WishlistApi.Controllers;
using WishlistApi.Helpers;
using WishlistApi.HostedServices;
using Infrastructure.ExternalServices;
using Infrastructure.SharedDb;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 500000; // If we count size 1 as 100 B, 500000 is ~50 MB
});
builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WishlistAPI", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token",
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});



var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins")?.Split(',') ?? new string[0];
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCORS", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<WishlistDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")).UseSnakeCaseNamingConvention());
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WishlistDbContext>());
builder.Services.AddScoped<ISteamApiClient, SteamApiClient>();
builder.Services.AddHostedService<SteamUpdaterService>();

builder.Services.AddScoped<IAppListingRepository, AppListingRepository>();
builder.Services.AddScoped<IWishlistItemRepository, WishlistItemRepository>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IAuctionReadModel, AuctionReadAdapter>();
builder.Services.AddScoped<IUserReadModel, UserReadAdapter>();

// Use case registrations
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
builder.Services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
builder.Services.AddScoped<IGetUserProfileUseCase, GetUserProfileUseCase>();
builder.Services.AddScoped<IUpdateUserProfileUseCase, UpdateUserProfileUseCase>();
builder.Services.AddScoped<IGetPaginatedUsersUseCase, GetPaginatedUsersUseCase>();
builder.Services.AddScoped<IGetWishlistUseCase, GetWishlistUseCase>();
builder.Services.AddScoped<IAddWishlistItemUseCase, AddWishlistItemUseCase>();
builder.Services.AddScoped<IDeleteWishlistItemUseCase, DeleteWishlistItemUseCase>();
builder.Services.AddScoped<IGetWishlistStatsUseCase, GetWishlistStatsUseCase>();
builder.Services.AddScoped<IPublishBackfillEventUseCase, PublishBackfillEventUseCase>();
builder.Services.AddScoped<ISetAlertRuleUseCase, SetAlertRuleUseCase>();
builder.Services.AddScoped<IDeleteAlertRuleUseCase, DeleteAlertRuleUseCase>();
builder.Services.AddScoped<ISearchAppListingsUseCase, SearchAppListingsUseCase>();
builder.Services.AddScoped<IGetRandomAppListingUseCase, GetRandomAppListingUseCase>();
builder.Services.AddScoped<EnsureAppListingsPopulatedUseCase, EnsureAppListingsPopulatedUseCase>();
builder.Services.AddScoped<IPlaceBidUseCase, PlaceBidUseCase>();
builder.Services.AddScoped<ISimulateBidUseCase, SimulateBidUseCase>();
builder.Services.AddScoped<IStartNextAuctionUseCase, StartNextAuctionUseCase>();
builder.Services.AddScoped<IEnsureAppListingsPopulatedUseCase, EnsureAppListingsPopulatedUseCase>();

// RabbitMQ infrastructure
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();

// Create a shared RabbitMQ connection and channel pool
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = sp.GetRequiredService<IRabbitMqConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<ChannelPool>(sp =>
    new ChannelPool(sp.GetRequiredService<IConnection>()));

// Exchange initializer for one-shot setup
builder.Services.AddSingleton<ExchangeInitializer>();

// Event publisher uses the shared channel pool
builder.Services.AddScoped<IEventPublisher>(sp =>
    new RabbitMqEventPublisher(
        sp.GetRequiredService<ChannelPool>(),
        "wishlist.events"));

// Initialize exchanges at startup
using var initScope = builder.Services.BuildServiceProvider().CreateScope();
var initPool = initScope.ServiceProvider.GetRequiredService<ChannelPool>();
var initInitializer = new ExchangeInitializer(initPool);
await initInitializer.InitializeAsync(
    new[] { new ExchangeDeclaration { ExchangeName = "wishlist.events", Type = "fanout", Durable = true } },
    Enumerable.Empty<QueueDeclaration>(),
    Enumerable.Empty<QueueBinding>());

// SteamTracker proxy for alert management
builder.Services.AddHttpClient<ISteamTrackerAlertProxy, SteamTrackerAlertProxy>();

// Named HttpClient for the prices passthrough endpoint
builder.Services.AddHttpClient("SteamTracker", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("SteamTrackerUri")!);
});


builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddHostedService<AuctionBackgroundService>();

string jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new Exception("Missing Jwt:Key in appsettings.json");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["auth_token"];
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "WishlistApp",
            ValidAudience = "WishlistApp_audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07")
    {
        // Tables already exist from a previous migration — ignore
        Console.WriteLine($"Migration skipped (tables already exist): {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
        throw;
    }
}

app.UseCors("RestrictedCORS");
app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AuctionHub>("/auctionHub");

app.Run();
