using System.Collections.Concurrent;
using System.Diagnostics;

namespace Keboo.FidgetProxy;

/// <summary>
/// Manages a dynamic collection of process filters
/// </summary>
public class ProcessFilterManager
{
    private readonly ConcurrentDictionary<string, ProcessFilter> _filters = new();

    /// <summary>
    /// Adds a new process filter pattern
    /// </summary>
    /// <param name="pattern">Wildcard pattern or PID to filter processes (e.g., "chrome*", "1234")</param>
    /// <returns>True if the filter was added, false if it already exists</returns>
    public bool AddFilter(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Pattern cannot be null or whitespace", nameof(pattern));
        }

        var filter = new ProcessFilter(pattern);
        return _filters.TryAdd(pattern, filter);
    }

    /// <summary>
    /// Adds multiple process filter patterns
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
    /// Removes a process filter pattern
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
    /// Checks if a process should be filtered (i.e., matches any filter pattern)
    /// Returns true if there are NO filters (include all), or if the process matches any filter
    /// </summary>
    /// <param name="processId">The process ID</param>
    /// <param name="processName">The process name (optional)</param>
    /// <returns>True if the process should be included in logging</returns>
    public bool ShouldIncludeProcess(int processId, string? processName = null)
    {
        // If no filters are configured, include all processes
        if (_filters.IsEmpty)
        {
            return true;
        }

        // If processId is invalid (e.g., -1 for remote connections), include it
        if (processId < 0)
        {
            return true;
        }

        // Check if any filter matches the process
        return _filters.Values.Any(filter => filter.IsMatch(processId, processName));
    }

    /// <summary>
    /// Checks if a process should be filtered
    /// </summary>
    public bool ShouldIncludeProcess(Process process)
    {
        if (process == null)
        {
            return true; // Include if we can't determine
        }

        try
        {
            return ShouldIncludeProcess(process.Id, process.ProcessName);
        }
        catch
        {
            return true; // Include if we can't read process info
        }
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
