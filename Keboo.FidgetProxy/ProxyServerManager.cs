using System.Net;
using Keboo.Web.Proxy;
using Keboo.Web.Proxy.Models;

namespace Keboo.FidgetProxy;

/// <summary>
/// Manages the lifecycle of the proxy server
/// </summary>
public class ProxyServerManager : IDisposable
{
    private ProxyServer? _proxyServer;
    private HttpTrafficLogger? _logger;
    private readonly object _lock = new();
    private bool _disposed = false;

    public bool IsRunning => _proxyServer?.ProxyRunning ?? false;
    public int ActiveConnections => _proxyServer?.ClientConnectionCount ?? 0;

    public async Task StartAsync(string outputDirectory, int port = 8080)
    {
        if (_proxyServer != null)
        {
            throw new InvalidOperationException("Proxy server is already running");
        }

        lock (_lock)
        {
            // Create the logger
            _logger = new HttpTrafficLogger(outputDirectory);

            // Create and configure the proxy server
            _proxyServer = new ProxyServer(
                userTrustRootCertificate: true,
                machineTrustRootCertificate: false,
                trustRootCertificateAsAdmin: false);

            // Subscribe to request/response events
            _proxyServer.BeforeRequest += OnBeforeRequest;
            _proxyServer.BeforeResponse += OnBeforeResponse;

            // Add an explicit proxy endpoint
            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, port, true);
            _proxyServer.AddEndPoint(explicitEndPoint);

            // Start the proxy
            _proxyServer.Start(changeSystemProxySettings: true);
            
            // Set as system proxy (for both HTTP and HTTPS)
            _proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_proxyServer == null)
        {
            return;
        }

        lock (_lock)
        {
            // Unsubscribe from events
            if (_proxyServer != null)
            {
                _proxyServer.BeforeRequest -= OnBeforeRequest;
                _proxyServer.BeforeResponse -= OnBeforeResponse;

                // Stop the proxy (this will restore system proxy settings)
                _proxyServer.Stop();
                _proxyServer.Dispose();
                _proxyServer = null;
            }

            _logger = null;
        }

        await Task.CompletedTask;
    }

    private async Task OnBeforeRequest(object? sender, Keboo.Web.Proxy.EventArguments.SessionEventArgs e)
    {
        if (_logger != null)
        {
            try
            {
                await _logger.LogRequestAsync(e);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error logging request: {ex.Message}");
            }
        }
    }

    private async Task OnBeforeResponse(object? sender, Keboo.Web.Proxy.EventArguments.SessionEventArgs e)
    {
        if (_logger != null)
        {
            try
            {
                await _logger.LogResponseAsync(e);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error logging response: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
