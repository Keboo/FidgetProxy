using System.Diagnostics;
using System.Text;
using Keboo.Web.Proxy.EventArguments;

namespace Keboo.FidgetProxy;

/// <summary>
/// Logs HTTP requests and responses to individual text files
/// </summary>
public class HttpTrafficLogger
{
    private readonly string _outputDirectory;
    private readonly ProcessFilterManager _processFilterManager;
    private int _requestCounter = 0;

    public HttpTrafficLogger(string outputDirectory, ProcessFilterManager processFilterManager)
    {
        _outputDirectory = outputDirectory;
        _processFilterManager = processFilterManager;
        Directory.CreateDirectory(outputDirectory);
    }

    public async Task LogRequestAsync(SessionEventArgs session)
    {
        // Get client process information
        var (clientPid, clientProcessName) = GetClientProcessInfo(session);
        
        // Check if this process should be included
        if (!_processFilterManager.ShouldIncludeProcess(clientPid, clientProcessName))
        {
            return; // Skip logging for filtered processes
        }
        
        var counter = Interlocked.Increment(ref _requestCounter);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var method = session.HttpClient.Request.Method;
        var sanitizedUrl = SanitizeFileName(session.HttpClient.Request.RequestUri.Host);
        
        var fileName = $"{timestamp}_{counter:D6}_{method}_{sanitizedUrl}_request.txt";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var clientProcessInfo = clientPid > 0
            ? $"{clientProcessName ?? "Unknown"} (PID: {clientPid})"
            : "Remote/Unknown";

        var sb = new StringBuilder();
        sb.AppendLine($"=== HTTP REQUEST ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Proxy Process: {GetProcessInfo()}");
        sb.AppendLine($"Client Process: {clientProcessInfo}");
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
        // Get client process information
        var (clientPid, clientProcessName) = GetClientProcessInfo(session);
        
        // Check if this process should be included
        if (!_processFilterManager.ShouldIncludeProcess(clientPid, clientProcessName))
        {
            return; // Skip logging for filtered processes
        }
        
        var counter = _requestCounter; // Use same counter as request
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var method = session.HttpClient.Request.Method;
        var sanitizedUrl = SanitizeFileName(session.HttpClient.Request.RequestUri.Host);
        
        var fileName = $"{timestamp}_{counter:D6}_{method}_{sanitizedUrl}_response.txt";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var clientProcessInfo = clientPid > 0
            ? $"{clientProcessName ?? "Unknown"} (PID: {clientPid})"
            : "Remote/Unknown";

        var sb = new StringBuilder();
        sb.AppendLine($"=== HTTP RESPONSE ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Proxy Process: {GetProcessInfo()}");
        sb.AppendLine($"Client Process: {clientProcessInfo}");
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

    private static string GetProcessInfo()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return $"{process.ProcessName} (PID: {process.Id})";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static (int processId, string? processName) GetClientProcessInfo(SessionEventArgs session)
    {
        try
        {
            // Get the client process ID from the session
            var processId = session.HttpClient.ProcessId.Value;
            
            // If it's a valid local process, get its name
            if (processId > 0)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    return (processId, process.ProcessName);
                }
                catch
                {
                    // Process may have exited, return just the ID
                    return (processId, null);
                }
            }
            
            return (processId, null);
        }
        catch
        {
            return (-1, null);
        }
    }
}
