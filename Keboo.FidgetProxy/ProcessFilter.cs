using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Keboo.FidgetProxy;

/// <summary>
/// Represents a process filter pattern with wildcard support
/// </summary>
public class ProcessFilter
{
    private readonly Regex? _nameRegex;
    private readonly int? _exactPid;
    
    public string Pattern { get; }

    /// <summary>
    /// Creates a new process filter with wildcard pattern support or PID matching.
    /// Supports * for any characters and ? for single character in process names.
    /// Examples: 
    ///   - "chrome" - matches chrome.exe
    ///   - "chrome*" - matches chrome.exe, chrome-helper.exe
    ///   - "1234" - matches PID 1234
    ///   - "*test*" - matches any process with 'test' in the name
    /// </summary>
    public ProcessFilter(string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        
        // Check if pattern is a PID (all digits)
        if (int.TryParse(pattern, out var pid))
        {
            _exactPid = pid;
        }
        else
        {
            // Convert wildcard pattern to regex for process name matching
            var regexPattern = Regex.Escape(pattern)
                .Replace(@"\*", ".*")  // * matches any characters
                .Replace(@"\?", ".");   // ? matches single character
            
            // Add anchors to match the entire process name (case-insensitive)
            regexPattern = "^" + regexPattern + "$";
            
            _nameRegex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Checks if the given process ID and name match this filter pattern
    /// </summary>
    public bool IsMatch(int processId, string? processName)
    {
        // Match by PID if this filter is a PID filter
        if (_exactPid.HasValue)
        {
            return processId == _exactPid.Value;
        }
        
        // Match by process name
        if (!string.IsNullOrEmpty(processName) && _nameRegex != null)
        {
            // Remove .exe extension if present for matching
            var nameWithoutExtension = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName.Substring(0, processName.Length - 4)
                : processName;
            
            return _nameRegex.IsMatch(processName) || _nameRegex.IsMatch(nameWithoutExtension);
        }
        
        return false;
    }

    /// <summary>
    /// Checks if the given process matches this filter
    /// </summary>
    public bool IsMatch(Process process)
    {
        if (process == null)
        {
            return false;
        }
        
        try
        {
            return IsMatch(process.Id, process.ProcessName);
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Pattern;
}
