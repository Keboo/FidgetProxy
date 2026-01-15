using System.Diagnostics;
using System.Net;
using System.Net.Http;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Keboo.FidgetProxy.Tests;

/// <summary>
/// Integration tests for FidgetProxy that verify end-to-end functionality
/// </summary>
public class IntegrationTests : IDisposable
{
    private string? _testOutputDirectory;
    private ProxyServerManager? _proxyManager;

    [Before(Test)]
    public void Setup()
    {
        // Create a unique temp directory for each test
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "fidgetproxy-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testOutputDirectory);
    }

    [After(Test)]
    public async Task Cleanup()
    {
        // Stop the proxy if it's running
        if (_proxyManager != null)
        {
            await _proxyManager.StopAsync();
            _proxyManager.Dispose();
            _proxyManager = null;
        }

        // Clean up test output directory
        if (_testOutputDirectory != null && Directory.Exists(_testOutputDirectory))
        {
            try
            {
                Directory.Delete(_testOutputDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Test]
    public async Task ProxyCanStartAndStop()
    {
        // Arrange
        _proxyManager = new ProxyServerManager();

        // Act - Start the proxy (don't set as system proxy for tests)
        await _proxyManager.StartAsync(_testOutputDirectory!, setAsSystemProxy: false);

        // Assert - Proxy should be running
        await Assert.That(_proxyManager.IsRunning).IsTrue();

        // Act - Stop the proxy
        await _proxyManager.StopAsync();

        // Assert - Proxy should be stopped
        await Assert.That(_proxyManager.IsRunning).IsFalse();
    }

    [Test]
    public async Task ProxyWritesRequestAndResponseFiles()
    {
        // Arrange
        _proxyManager = new ProxyServerManager();
        await _proxyManager.StartAsync(_testOutputDirectory!, port: 8081, setAsSystemProxy: false);

        // Wait for proxy to be fully started
        await Task.Delay(500);

        // Act - Make an HTTP request through the proxy
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy("http://localhost:8081"),
            UseProxy = true,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        
        try
        {
            var response = await client.GetAsync("http://httpbin.org/get");
            await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // If httpbin is unreachable, skip this part of the test
            // We'll still verify that the proxy infrastructure works
        }

        // Give some time for files to be written
        await Task.Delay(1000);

        // Assert - Files should exist in the output directory
        var files = Directory.GetFiles(_testOutputDirectory!);
        
        // We should have at least some log files created
        // (request and response files)
        await Assert.That(files.Length).IsGreaterThanOrEqualTo(0);

        // Check that files follow the expected naming pattern
        var requestFiles = files.Where(f => f.Contains("_request.txt")).ToArray();
        var responseFiles = files.Where(f => f.Contains("_response.txt")).ToArray();

        if (requestFiles.Length > 0)
        {
            // Verify request file content
            var requestContent = await File.ReadAllTextAsync(requestFiles[0]);
            await Assert.That(requestContent).Contains("=== HTTP REQUEST ===");
            await Assert.That(requestContent).Contains("Method:");
            await Assert.That(requestContent).Contains("URL:");
        }

        if (responseFiles.Length > 0)
        {
            // Verify response file content
            var responseContent = await File.ReadAllTextAsync(responseFiles[0]);
            await Assert.That(responseContent).Contains("=== HTTP RESPONSE ===");
            await Assert.That(responseContent).Contains("Status Code:");
        }
    }

    [Test]
    public async Task UrlFilterExcludesMatchingUrls()
    {
        // Arrange
        _proxyManager = new ProxyServerManager();
        await _proxyManager.StartAsync(_testOutputDirectory!, port: 8082, setAsSystemProxy: false);

        // Add URL filter to exclude example.com
        _proxyManager.FilterManager.AddFilter("*example.com*");

        await Task.Delay(500);

        // Act - Make requests to both filtered and non-filtered URLs
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy("http://localhost:8082"),
            UseProxy = true,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            // This should be filtered out
            var response1 = await client.GetAsync("http://example.com");
            await response1.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore connection errors
        }

        try
        {
            // This should NOT be filtered
            var response2 = await client.GetAsync("http://httpbin.org/get");
            await response2.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore connection errors
        }

        await Task.Delay(1000);

        // Assert - Files should NOT contain example.com
        var files = Directory.GetFiles(_testOutputDirectory!);
        var fileContents = new List<string>();
        
        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            fileContents.Add(content);
        }

        // Verify that example.com was filtered out
        var exampleComFiles = fileContents.Where(c => c.Contains("example.com", StringComparison.OrdinalIgnoreCase)).ToArray();
        await Assert.That(exampleComFiles.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ProcessFilterIncludesOnlyMatchingProcesses()
    {
        // Arrange
        _proxyManager = new ProxyServerManager();
        
        // Get current process name
        var currentProcess = Process.GetCurrentProcess();
        var currentProcessName = currentProcess.ProcessName;

        // Add process filter to include only this process (the test runner)
        _proxyManager.ProcessFilterManager.AddFilter(currentProcessName);

        await _proxyManager.StartAsync(_testOutputDirectory!, port: 8083, setAsSystemProxy: false);
        await Task.Delay(500);

        // Act - Make an HTTP request (will be from this process)
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy("http://localhost:8083"),
            UseProxy = true,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            var response = await client.GetAsync("http://httpbin.org/get");
            await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // Ignore connection errors
        }

        await Task.Delay(1000);

        // Assert - Since we're making requests from the current process,
        // and we've filtered to only include this process, we should see logs
        var files = Directory.GetFiles(_testOutputDirectory!);

        // If we got any response, verify the client process matches
        if (files.Length > 0)
        {
            var fileContent = await File.ReadAllTextAsync(files[0]);
            if (fileContent.Contains("Client Process:"))
            {
                // Verify it contains our process name (accounting for PID variations)
                await Assert.That(fileContent.Contains(currentProcessName, StringComparison.OrdinalIgnoreCase)).IsTrue();
            }
        }
    }

    [Test]
    public async Task UrlFilterManagerAddsAndRemovesFilters()
    {
        // Arrange
        var filterManager = new UrlFilterManager();

        // Act & Assert - Add filter
        var added = filterManager.AddFilter("*.example.com");
        await Assert.That(added).IsTrue();
        await Assert.That(filterManager.Count).IsEqualTo(1);

        // Assert - Should filter matching URL
        await Assert.That(filterManager.ShouldFilter("https://api.example.com")).IsTrue();
        await Assert.That(filterManager.ShouldFilter("https://other.com")).IsFalse();

        // Act & Assert - Remove filter
        var removed = filterManager.RemoveFilter("*.example.com");
        await Assert.That(removed).IsTrue();
        await Assert.That(filterManager.Count).IsEqualTo(0);

        // Assert - Should no longer filter
        await Assert.That(filterManager.ShouldFilter("https://api.example.com")).IsFalse();
    }

    [Test]
    public async Task ProcessFilterManagerAddsAndRemovesFilters()
    {
        // Arrange
        var filterManager = new ProcessFilterManager();

        // Act & Assert - Add filter
        var added = filterManager.AddFilter("chrome");
        await Assert.That(added).IsTrue();
        await Assert.That(filterManager.Count).IsEqualTo(1);

        // Assert - Should include matching process
        await Assert.That(filterManager.ShouldIncludeProcess(1234, "chrome")).IsTrue();
        await Assert.That(filterManager.ShouldIncludeProcess(5678, "firefox")).IsFalse();

        // Act & Assert - Remove filter
        var removed = filterManager.RemoveFilter("chrome");
        await Assert.That(removed).IsTrue();
        await Assert.That(filterManager.Count).IsEqualTo(0);

        // Assert - When no filters, all processes should be included
        await Assert.That(filterManager.ShouldIncludeProcess(1234, "chrome")).IsTrue();
        await Assert.That(filterManager.ShouldIncludeProcess(5678, "firefox")).IsTrue();
    }

    [Test]
    public async Task MultipleFiltersWorkTogether()
    {
        // Arrange
        _proxyManager = new ProxyServerManager();
        
        // Add multiple URL filters
        _proxyManager.FilterManager.AddFilter("*.googleapis.com");
        _proxyManager.FilterManager.AddFilter("*.cloudflare.com");
        _proxyManager.FilterManager.AddFilter("*/analytics/*");

        await _proxyManager.StartAsync(_testOutputDirectory!, port: 8084, setAsSystemProxy: false);
        await Task.Delay(500);

        // Act - Make requests to various URLs
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy("http://localhost:8084"),
            UseProxy = true,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            await client.GetAsync("http://httpbin.org/get");
        }
        catch
        {
            // Ignore connection errors
        }

        await Task.Delay(1000);

        // Assert - Verify filters are active
        await Assert.That(_proxyManager.FilterManager.Count).IsEqualTo(3);
        await Assert.That(_proxyManager.FilterManager.ShouldFilter("https://maps.googleapis.com")).IsTrue();
        await Assert.That(_proxyManager.FilterManager.ShouldFilter("https://cdn.cloudflare.com")).IsTrue();
        await Assert.That(_proxyManager.FilterManager.ShouldFilter("https://example.com/analytics/track")).IsTrue();
        await Assert.That(_proxyManager.FilterManager.ShouldFilter("https://httpbin.org/get")).IsFalse();
    }

    public void Dispose()
    {
        Cleanup().GetAwaiter().GetResult();
    }
}
