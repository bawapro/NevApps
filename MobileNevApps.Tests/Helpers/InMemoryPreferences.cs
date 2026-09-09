using Microsoft.Maui.Storage;

namespace MobileNevApps.Tests.Helpers;

internal sealed class InMemoryPreferences : IPreferences
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    public bool ContainsKey(string key, string? sharedName = null)
        => _values.ContainsKey(PrefKey(key, sharedName));

    public void Remove(string key, string? sharedName = null)
        => _values.Remove(PrefKey(key, sharedName));

    public void Clear(string? sharedName = null)
    {
        if (sharedName is null)
        {
            _values.Clear();
            return;
        }

        var prefix = sharedName + ":";
        foreach (var key in _values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            _values.Remove(key);
    }

    public void Set<T>(string key, T value, string? sharedName = null)
        => _values[PrefKey(key, sharedName)] = value;

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        if (_values.TryGetValue(PrefKey(key, sharedName), out var value) && value is T typed)
            return typed;

        return defaultValue;
    }

    private static string PrefKey(string key, string? sharedName)
        => sharedName is null ? key : $"{sharedName}:{key}";
}
