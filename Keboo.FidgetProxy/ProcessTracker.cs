using System.Diagnostics;

namespace Keboo.FidgetProxy;

/// <summary>
/// Manages process tracking via PID file
/// </summary>
public class ProcessTracker
{
    private static readonly string PidFilePath = Path.Combine(
        Path.GetTempPath(), 
        "fidgetproxy.pid");

    public static void WritePidFile()
    {
        var pid = Environment.ProcessId;
        File.WriteAllText(PidFilePath, pid.ToString());
    }

    public static void RemovePidFile()
    {
        if (File.Exists(PidFilePath))
        {
            try
            {
                File.Delete(PidFilePath);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    public static int? GetRunningProxyProcessId()
    {
        if (!File.Exists(PidFilePath))
        {
            return null;
        }

        try
        {
            var pidText = File.ReadAllText(PidFilePath);
            if (int.TryParse(pidText, out int pid))
            {
                // Check if the process is actually running
                try
                {
                    var process = Process.GetProcessById(pid);
                    if (process.HasExited)
                    {
                        // Process has exited, remove stale PID file
                        RemovePidFile();
                        return null;
                    }
                    return pid;
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist, remove stale PID file
                    RemovePidFile();
                    return null;
                }
            }
        }
        catch
        {
            // If we can't read or parse the file, treat it as no running process
            RemovePidFile();
        }

        return null;
    }

    public static bool IsProxyRunning()
    {
        return GetRunningProxyProcessId() != null;
    }

    public static string GetNamedPipeName()
    {
        return "fidgetproxy-control";
    }

    public static void KillStrayProcesses()
    {
        // First try to get the process from PID file
        var pid = GetRunningProxyProcessId();
        if (pid.HasValue)
        {
            try
            {
                var process = Process.GetProcessById(pid.Value);
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000); // Wait up to 5 seconds
                }
            }
            catch
            {
                // Process might have already exited
            }
        }

        // Also search for any fidgetproxy processes that might be running
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var processes = Process.GetProcessesByName("fidgetproxy");
            
            foreach (var process in processes)
            {
                try
                {
                    // Don't kill ourselves
                    if (process.Id != currentProcess.Id)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch
                {
                    // Process might have already exited or we don't have permission
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch
        {
            // Ignore errors when searching for processes
        }

        // Always remove the PID file
        RemovePidFile();
    }
}
