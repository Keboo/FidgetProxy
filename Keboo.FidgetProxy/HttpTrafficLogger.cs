using System.Text;
using Keboo.Web.Proxy.EventArguments;

namespace Keboo.FidgetProxy;

/// <summary>
/// Logs HTTP requests and responses to individual text files
/// </summary>
public class HttpTrafficLogger
{
    private readonly string _outputDirectory;
    private int _requestCounter = 0;

    public HttpTrafficLogger(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(outputDirectory);
    }

    public async Task LogRequestAsync(SessionEventArgs session)
    {
        var counter = Interlocked.Increment(ref _requestCounter);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var method = session.HttpClient.Request.Method;
        var sanitizedUrl = SanitizeFileName(session.HttpClient.Request.RequestUri.Host);
        
        var fileName = $"{timestamp}_{counter:D6}_{method}_{sanitizedUrl}_request.txt";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine($"=== HTTP REQUEST ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Method: {session.HttpClient.Request.Method}");
        sb.AppendLine($"URL: {session.HttpClient.Request.Url}");
        sb.AppendLine($"HTTP Version: {session.HttpClient.Request.HttpVersion}");
        sb.AppendLine();
        
        sb.AppendLine("--- Headers ---");
        foreach (var header in session.HttpClient.Request.Headers)
        {
            sb.AppendLine($"{header.Name}: {header.Value}");
        }
        sb.AppendLine();

        if (session.HttpClient.Request.HasBody)
        {
            sb.AppendLine("--- Body ---");
            try
            {
                var body = await session.GetRequestBodyAsString();
                sb.AppendLine(body);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Error reading body: {ex.Message}]");
            }
        }
        else
        {
            sb.AppendLine("--- No Body ---");
        }

        var logContent = sb.ToString();
        await File.WriteAllTextAsync(filePath, logContent);
        
        // Also write to console
        Console.WriteLine(logContent);
        Console.WriteLine();
    }

    public async Task LogResponseAsync(SessionEventArgs session)
    {
        var counter = _requestCounter; // Use same counter as request
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var method = session.HttpClient.Request.Method;
        var sanitizedUrl = SanitizeFileName(session.HttpClient.Request.RequestUri.Host);
        
        var fileName = $"{timestamp}_{counter:D6}_{method}_{sanitizedUrl}_response.txt";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine($"=== HTTP RESPONSE ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Status Code: {session.HttpClient.Response.StatusCode}");
        sb.AppendLine($"Status Description: {session.HttpClient.Response.StatusDescription}");
        sb.AppendLine($"HTTP Version: {session.HttpClient.Response.HttpVersion}");
        sb.AppendLine();
        
        sb.AppendLine("--- Headers ---");
        foreach (var header in session.HttpClient.Response.Headers)
        {
            sb.AppendLine($"{header.Name}: {header.Value}");
        }
        sb.AppendLine();

        if (session.HttpClient.Response.HasBody)
        {
            sb.AppendLine("--- Body ---");
            try
            {
                var body = await session.GetResponseBodyAsString();
                sb.AppendLine(body);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Error reading body: {ex.Message}]");
            }
        }
        else
        {
            sb.AppendLine("--- No Body ---");
        }

        var logContent = sb.ToString();
        await File.WriteAllTextAsync(filePath, logContent);
        
        // Also write to console
        Console.WriteLine(logContent);
        Console.WriteLine();
    }

    private static string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        
        // Limit length to avoid excessively long filenames
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }
        
        return sanitized;
    }
}
