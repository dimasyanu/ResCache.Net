using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ResCache.Net.Controllers;

[ApiController]
[Route("Api/[controller]")]
public class CacheController(CacheTracker tracker, IOutputCacheStore cacheStore) : ControllerBase
{
    private readonly CacheTracker _tracker = tracker;
    private readonly IOutputCacheStore _cacheStore = cacheStore;

    [HttpGet("Entries")]
    public IActionResult GetEntries()
    {
        var entries = _tracker.GetAll();
        return Ok(entries);
    }

    [HttpPost("Invalidate")]
    public async Task<IActionResult> Invalidate([FromQuery] string path)
    {
        // Support wildcard invalidation with '*' suffix (e.g. /Api/Cache/Invalidate?path=/products/*)
        if (path.EndsWith('*')) {
            var prefix = path.TrimEnd('*');
            var entries = _tracker.GetAll().Where(e => e.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var entry in entries) {
                await _cacheStore.EvictByTagAsync(entry.Path, HttpContext.RequestAborted);
                _tracker.Invalidate(entry.Path);
            }
            return NoContent();
        }

        await _cacheStore.EvictByTagAsync(path, HttpContext.RequestAborted);
        _tracker.Invalidate(path);
        return NoContent();
    }

    [HttpPost("InvalidateAll")]
    public async Task<IActionResult> InvalidateAll()
    {
        await _cacheStore.EvictByTagAsync("all", HttpContext.RequestAborted);
        _tracker.InvalidateAll();
        return NoContent();
    }
}
