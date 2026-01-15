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

    public override Task<AddFilterResponse> AddFilter(AddFilterRequest request, ServerCallContext context)
    {
        try
        {
            var added = _proxyManager.FilterManager.AddFilter(request.Pattern);
            return Task.FromResult(new AddFilterResponse
            {
                Success = added,
                Message = added 
                    ? $"Filter '{request.Pattern}' added successfully" 
                    : $"Filter '{request.Pattern}' already exists"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AddFilterResponse
            {
                Success = false,
                Message = $"Failed to add filter: {ex.Message}"
            });
        }
    }

    public override Task<RemoveFilterResponse> RemoveFilter(RemoveFilterRequest request, ServerCallContext context)
    {
        try
        {
            var removed = _proxyManager.FilterManager.RemoveFilter(request.Pattern);
            return Task.FromResult(new RemoveFilterResponse
            {
                Success = removed,
                Message = removed
                    ? $"Filter '{request.Pattern}' removed successfully"
                    : $"Filter '{request.Pattern}' not found"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RemoveFilterResponse
            {
                Success = false,
                Message = $"Failed to remove filter: {ex.Message}"
            });
        }
    }

    public override Task<ListFiltersResponse> ListFilters(ListFiltersRequest request, ServerCallContext context)
    {
        var filters = _proxyManager.FilterManager.GetFilters();
        var response = new ListFiltersResponse();
        response.Patterns.AddRange(filters);
        return Task.FromResult(response);
    }

    public override Task<ClearFiltersResponse> ClearFilters(ClearFiltersRequest request, ServerCallContext context)
    {
        try
        {
            var count = _proxyManager.FilterManager.Count;
            _proxyManager.FilterManager.ClearFilters();
            return Task.FromResult(new ClearFiltersResponse
            {
                Success = true,
                Message = $"Cleared {count} filter(s)",
                ClearedCount = count
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ClearFiltersResponse
            {
                Success = false,
                Message = $"Failed to clear filters: {ex.Message}",
                ClearedCount = 0
            });
        }
    }

    public override Task<AddProcessFilterResponse> AddProcessFilter(AddProcessFilterRequest request, ServerCallContext context)
    {
        try
        {
            var added = _proxyManager.ProcessFilterManager.AddFilter(request.Pattern);
            return Task.FromResult(new AddProcessFilterResponse
            {
                Success = added,
                Message = added 
                    ? $"Process filter '{request.Pattern}' added successfully" 
                    : $"Process filter '{request.Pattern}' already exists"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AddProcessFilterResponse
            {
                Success = false,
                Message = $"Failed to add process filter: {ex.Message}"
            });
        }
    }

    public override Task<RemoveProcessFilterResponse> RemoveProcessFilter(RemoveProcessFilterRequest request, ServerCallContext context)
    {
        try
        {
            var removed = _proxyManager.ProcessFilterManager.RemoveFilter(request.Pattern);
            return Task.FromResult(new RemoveProcessFilterResponse
            {
                Success = removed,
                Message = removed
                    ? $"Process filter '{request.Pattern}' removed successfully"
                    : $"Process filter '{request.Pattern}' not found"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new RemoveProcessFilterResponse
            {
                Success = false,
                Message = $"Failed to remove process filter: {ex.Message}"
            });
        }
    }

    public override Task<ListProcessFiltersResponse> ListProcessFilters(ListProcessFiltersRequest request, ServerCallContext context)
    {
        var filters = _proxyManager.ProcessFilterManager.GetFilters();
        var response = new ListProcessFiltersResponse();
        response.Patterns.AddRange(filters);
        return Task.FromResult(response);
    }

    public override Task<ClearProcessFiltersResponse> ClearProcessFilters(ClearProcessFiltersRequest request, ServerCallContext context)
    {
        try
        {
            var count = _proxyManager.ProcessFilterManager.Count;
            _proxyManager.ProcessFilterManager.ClearFilters();
            return Task.FromResult(new ClearProcessFiltersResponse
            {
                Success = true,
                Message = $"Cleared {count} process filter(s)",
                ClearedCount = count
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ClearProcessFiltersResponse
            {
                Success = false,
                Message = $"Failed to clear process filters: {ex.Message}",
                ClearedCount = 0
            });
        }
    }
}
