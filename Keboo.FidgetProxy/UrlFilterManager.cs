using System.Collections.Concurrent;

namespace Keboo.FidgetProxy;

/// <summary>
/// Manages a dynamic collection of URL filters
/// </summary>
public class UrlFilterManager
{
    private readonly ConcurrentDictionary<string, UrlFilter> _filters = new();

    /// <summary>
    /// Adds a new URL filter pattern
    /// </summary>
    /// <param name="pattern">Wildcard pattern to filter URLs (e.g., *.example.com, */api/*)</param>
    /// <returns>True if the filter was added, false if it already exists</returns>
    public bool AddFilter(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern cannot be null or whitespace", nameof(pattern));
        }

        var filter = new UrlFilter(pattern);
        return _filters.TryAdd(pattern, filter);
    }

    /// <summary>
    /// Adds multiple URL filter patterns
    /// </summary>
    public void AddFilters(IEnumerable<string> patterns)
    {
        if (patterns == null)
        {
            throw new ArgumentNullException(nameof(patterns));
        }

        foreach (var pattern in patterns)
        {
            AddFilter(pattern);
        }
    }

    /// <summary>
    /// Removes a URL filter pattern
    /// </summary>
    /// <returns>True if the filter was removed, false if it didn't exist</returns>
    public bool RemoveFilter(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return _filters.TryRemove(pattern, out _);
    }

    /// <summary>
    /// Removes all filters
    /// </summary>
    public void ClearFilters()
    {
        _filters.Clear();
    }

    /// <summary>
    /// Checks if a URL should be filtered (i.e., matches any filter pattern)
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <returns>True if the URL matches any filter and should be excluded</returns>
    public bool ShouldFilter(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Check if any filter matches the URL
        return _filters.Values.Any(filter => filter.IsMatch(url));
    }

    /// <summary>
    /// Gets all active filter patterns
    /// </summary>
    public IReadOnlyCollection<string> GetFilters()
    {
        return _filters.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the count of active filters
    /// </summary>
    public int Count => _filters.Count;
}
