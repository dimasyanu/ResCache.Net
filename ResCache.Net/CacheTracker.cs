using System.Collections.Concurrent;

namespace ResCache.Net;

public class CacheTracker
{
    private readonly ConcurrentDictionary<string, CachedPathEntry> _entries = new();

    public void Track(string path, string policy, TimeSpan ttl)
    {
        var now = DateTimeOffset.UtcNow;
        _entries[path] = new CachedPathEntry {
            Path = path,
            PolicyName = policy,
            CachedAt = now,
            ExpiredAt = now.Add(ttl)
        };
    }

    public IReadOnlyCollection<CachedPathEntry> GetAll()
    {
        // Purge expired entries
        var expired = _entries
            .Where(x => x.Value.ExpiredAt <= DateTimeOffset.UtcNow)
            .Select(x => x.Key)
            .ToList();

        foreach (var key in expired) {
            _entries.TryRemove(key, out _);
        }

        return _entries.Values.ToList();
    }

    public CachedPathEntry? Get(string path)
    {
        _entries.TryGetValue(path, out var entry);
        if (entry is not null && entry.ExpiredAt < DateTimeOffset.UtcNow) {
            _entries.TryRemove(path, out _);
            return null;
        }
        return entry;
    }

    public void Invalidate(string path) => _entries.TryRemove(path, out _);
    public void InvalidateAll() => _entries.Clear();
}

public class CachedPathEntry
{
    public string Path { get; set; } = "";
    public string PolicyName { get; set; } = "";
    public DateTimeOffset CachedAt { get; set; }
    public DateTimeOffset ExpiredAt { get; set; }
}