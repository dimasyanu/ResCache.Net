using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace ResCache.Net;

public class TrackingOutputCachePolicy(
    CacheTracker tracker,
    string policyName,
    TimeSpan ttl
) : IOutputCachePolicy
{
    private readonly CacheTracker _tracker = tracker;
    private readonly string _policyName = policyName;
    private readonly TimeSpan _ttl = ttl;

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var attempt = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attempt;
        context.AllowCacheStorage = attempt;
        context.AllowLocking = true;
        context.CacheVaryByRules.QueryKeys = "*";
        context.ResponseExpirationTimeSpan = _ttl;

        var path = context.HttpContext.Request.Path.Value ?? "";
        var query = context.HttpContext.Request.QueryString.Value ?? "";
        context.Tags.Add(path + query);

        AppendResponseHeaders(context);

        return ValueTask.CompletedTask;
    }

    // Cache hit
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        if (context.HttpContext.Items["__CacheStatus"] is CacheStatus status)
            status.IsHit = true;

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        // Only track if caching is actually going to happen
        if (context.AllowCacheStorage) {
            var path = context.HttpContext.Request.Path.Value ?? "";
            var query = context.HttpContext.Request.QueryString.Value ?? "";
            _tracker.Track(path + query, _policyName, _ttl);
        }
        return ValueTask.CompletedTask;
    }

    private void AppendResponseHeaders(OutputCacheContext context)
    {
        var cacheStatus = new CacheStatus();
        context.HttpContext.Response.OnStarting(() => {
            var response = context.HttpContext.Response;
            // Headers are not yet sent — safe to write in both HTTP/1.1 and HTTP/2
            response.Headers["X-Cache"] = cacheStatus.IsHit ? "HIT" : "MISS";

            if (!cacheStatus.IsHit) return Task.CompletedTask;

            var path = context.HttpContext.Request.Path.Value ?? "";
            var query = context.HttpContext.Request.QueryString.Value ?? "";
            var entry = _tracker.Get(path + query);
            if (entry is not null) {
                var age = (int)(DateTimeOffset.UtcNow - entry.CachedAt).TotalSeconds;
                var remaining = (int)(entry.ExpiredAt - DateTimeOffset.UtcNow).TotalSeconds;
                response.Headers["X-Cache-Age"] = age.ToString();
                response.Headers["X-Cache-Ttl"] = remaining.ToString();
            }

            return Task.CompletedTask;
        });

        // Store the flag in HttpContext.Items so ServeFromCacheAsync can reach it
        context.HttpContext.Items["__CacheStatus"] = cacheStatus;
    }

    // Only cache GET/HEAD, unauthenticated requests (mirrors DefaultPolicy)
    private bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            return false;

        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var ignoreAuth = config.GetSection("AppConfig").GetValue<bool?>("IgnoreAuthorizationHeader") ?? false;
        if (!ignoreAuth && !StringValues.IsNullOrEmpty(request.Headers.Authorization))
            return false;

        return true;
    }
}

public class CacheStatus
{
    public bool IsHit { get; set; }
}