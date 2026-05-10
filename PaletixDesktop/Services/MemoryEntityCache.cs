using System;
using System.Collections.Concurrent;

namespace PaletixDesktop.Services
{
    public sealed class MemoryEntityCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

        public void Set<T>(string key, T value)
        {
            _entries[key] = new CacheEntry(value, DateTimeOffset.UtcNow);
        }

        public bool TryGet<T>(string key, out T? value)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.Value is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private sealed record CacheEntry(object? Value, DateTimeOffset CachedAt);
    }
}
