using Grpc.Core;
using Keboo.Web.Proxy;
using Microsoft.Extensions.Hosting;

namespace Keboo.FidgetProxy;

/// <summary>
/// GRPC service for controlling the proxy server
/// </summary>
public class ProxyControlService : ProxyControl.ProxyControlBase
{
    private readonly ProxyServerManager _proxyManager;
    private readonly IHostApplicationLifetime _lifetime;

    public ProxyControlService(ProxyServerManager proxyManager, IHostApplicationLifetime lifetime)
    {
        _proxyManager = proxyManager;
        _lifetime = lifetime;
    }

    public override Task<GetStatusResponse> GetStatus(GetStatusRequest request, ServerCallContext context)
    {
        var response = new GetStatusResponse
        {
            IsRunning = _proxyManager.IsRunning,
            Message = _proxyManager.IsRunning ? "Proxy is running" : "Proxy is not running",
            ActiveConnections = _proxyManager.ActiveConnections
        };

        return Task.FromResult(response);
    }

    public override async Task<ShutdownResponse> Shutdown(ShutdownRequest request, ServerCallContext context)
    {
        try
        {
            await _proxyManager.StopAsync();
            
            // Trigger application shutdown
            _lifetime.StopApplication();
            
            return new ShutdownResponse
            {
                Success = true,
                Message = "Proxy server stopped successfully"
            };
        }
        catch (Exception ex)
        {
            return new ShutdownResponse
            {
                Success = false,
                Message = $"Failed to stop proxy: {ex.Message}"
            };
        }
    }
}
