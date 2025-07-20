using DataAccess;
using DataAccess.AppListings;
using DataAccess.Wishlist;
using Microsoft.EntityFrameworkCore;
using System;
using WishlistApi.HostedServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins")?.Split(',') ?? new string[0];
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCORS", policy =>
    {
        policy.WithOrigins(corsOrigins)//TODO read more what is recommended prod CORS config
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<WishlistDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")).UseSnakeCaseNamingConvention());

builder.Services.AddHostedService<SteamUpdaterService>();

builder.Services.AddScoped<AppListingDA, AppListingDA>();//TODO interface and unit test
builder.Services.AddScoped<WishlistItemDA, WishlistItemDA>();

var app = builder.Build();
app.UseCors("RestrictedCORS");
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
