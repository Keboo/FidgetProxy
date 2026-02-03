using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Keboo.Web.Proxy.Helpers;

[SupportedOSPlatform("windows")]
internal partial class NativeMethods
{
    // Keeps it from getting garbage collected
    internal static ConsoleEventDelegate? Handler;

    [DllImport("wininet.dll")]
    internal static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer,
        int dwBufferLength);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

    /// <summary>
    ///     <see href="https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getsystemmetrics" />
    /// </summary>
    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int nIndex);

    // Pinvoke
    internal delegate bool ConsoleEventDelegate(int eventType);
}