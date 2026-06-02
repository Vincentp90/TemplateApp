using Application;
using BenchmarkDotNet.Attributes;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Security.Cryptography;
using Xunit;
using Tests.Helpers;
using WishlistApi.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace Benchmarks;

[MemoryDiagnoser]
public class UserContextBenchmarks
{
    private ApiFactory? _factory;
    private IUserContext? _userContext;
    private IMemoryCache? _memoryCache;
    private IHttpContextAccessor? _httpContextAccessor;
    private IServiceScope? _rootScope;
    private ClaimsPrincipal? _userPrincipal;
    private IServiceScope? _iterationScope;

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        _factory = new ApiFactory();
        await _factory.InitializeAsync();

        Guid _externalUserId = Guid.NewGuid();

        await _factory.SeedAsync(async sp =>
        {
            var passwordHash = HashPassword("benchmarkpass", out var salt);
            var efUser = new Infrastructure.Persistence.Users.User
            {
                UUID = _externalUserId,
                Username = "benchmarkuser",
                PasswordHash = passwordHash,
                PasswordSalt = salt,
            };
            sp.GetRequiredService<WishlistDbContext>().Users.Add(efUser);

            var efDetails = new Infrastructure.Persistence.Users.UserDetails { UserID = efUser.ID, User = efUser };
            sp.GetRequiredService<WishlistDbContext>().UserDetails.Add(efDetails);

            await sp.GetRequiredService<WishlistDbContext>().SaveChangesAsync();
        });

        _rootScope = _factory.Services.CreateScope();
        _httpContextAccessor = _rootScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _externalUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Cookies");
        _userPrincipal = new ClaimsPrincipal(identity);

        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = _userPrincipal
        };

        
        _memoryCache = _rootScope.ServiceProvider.GetRequiredService<IMemoryCache>();
        _userContext = _rootScope.ServiceProvider.GetRequiredService<IUserContext>();
        await _userContext.GetIdAsync(); // prime _cachedId
    }

    [IterationSetup(Target = nameof(GetIdAsync_CacheMiss))]
    public void ClearCacheForMiss()
    {
        // Compacting by 1.00 (100%) forces the memory cache to drop its entries
        if (_memoryCache is MemoryCache concreteCache)
        {
            concreteCache.Compact(1.00);
        }
        else
        {
            throw new Exception("_memoryCache is not a MemoryCache");
        }
        
        _httpContextAccessor!.HttpContext = new DefaultHttpContext { User = _userPrincipal! };

        // Reset scope
        _iterationScope = _rootScope!.ServiceProvider.CreateScope();
        var context = _iterationScope.ServiceProvider.GetRequiredService<IUserContext>();
        _userContext = context;
    }    

    [IterationSetup(Target = nameof(GetIdAsync_MemoryCacheHit))]
    public void ClearScopedContextCache()
    {
        _httpContextAccessor!.HttpContext = new DefaultHttpContext { User = _userPrincipal! };
        
        // Refresh usercontext so we test memorycache but skip the scoped usercontext cache
        _iterationScope = _rootScope!.ServiceProvider.CreateScope();
        var context = _iterationScope.ServiceProvider.GetRequiredService<IUserContext>();
        _userContext = context;
    }

    // Cleanup scope for CacheMiss, MemoryCacheHit in order to bypass IUserContext scoped cache
    [IterationCleanup(Targets = new[] { nameof(GetIdAsync_CacheMiss), nameof(GetIdAsync_MemoryCacheHit) })]
    public void CleanupIteration()
    {
        // Safely dispose of the scope after the benchmark iteration completes
        _iterationScope?.Dispose();
        _iterationScope = null;
        _userContext = null;
    }

    [Benchmark]
    public async Task GetIdAsync_CacheMiss()
    {
        await _userContext!.GetIdAsync();
    }

    [Benchmark]
    public async Task GetIdAsync_MemoryCacheHit()
    {
        await _userContext!.GetIdAsync();
    }

    // No setup because we check the IUserContext field cached hit, that was already initialized in global setup
    [Benchmark]
    public async Task GetIdAsync_UserContextFieldCacheHit()
    {
        await _userContext!.GetIdAsync();
    }

    [GlobalCleanup]
    public async Task GlobalCleanupAsync()
    {
        _rootScope?.Dispose();
        if (_factory is IAsyncLifetime asyncLifetime)
            await asyncLifetime.DisposeAsync();
        else
            _factory?.Dispose();
    }

    private static byte[] HashPassword(string password, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(16);
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
    }
}
