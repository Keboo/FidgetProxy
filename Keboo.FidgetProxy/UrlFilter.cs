using System.Text.RegularExpressions;

namespace Keboo.FidgetProxy;

/// <summary>
/// Represents a URL filter pattern with wildcard support
/// </summary>
public class UrlFilter
{
    private readonly Regex _regex;
    
    public string Pattern { get; }

    /// <summary>
    /// Creates a new URL filter with wildcard pattern support.
    /// Supports * for any characters and ? for single character.
    /// Examples: 
    ///   - *.example.com - matches any subdomain of example.com
    ///   - */api/* - matches any URL containing /api/
    ///   - https://example.com/* - matches all URLs under example.com
    /// </summary>
    public UrlFilter(string pattern)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        
        // Convert wildcard pattern to regex
        // Escape special regex characters except * and ?
        var regexPattern = Regex.Escape(pattern)
            .Replace(@"\*", ".*")  // * matches any characters
            .Replace(@"\?", ".");   // ? matches single character
        
        // Add anchors to match the entire URL
        regexPattern = "^" + regexPattern + "$";
        
        _regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Checks if the given URL matches this filter pattern
    /// </summary>
    public bool IsMatch(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }
        
        return _regex.IsMatch(url);
    }

    public override string ToString() => Pattern;
}
