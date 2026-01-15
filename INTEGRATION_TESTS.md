# Integration Tests for FidgetProxy

This document describes the integration tests for FidgetProxy and how they work.

## Overview

Integration tests have been added to verify end-to-end functionality of FidgetProxy. These tests run on both **Windows** and **Linux** in the build pipeline to ensure cross-platform compatibility.

## Test Coverage

The integration tests verify the following functionality:

### 1. Proxy Lifecycle
- **ProxyCanStartAndStop**: Verifies that the proxy server can be started and stopped successfully
- Tests that `IsRunning` property reflects the correct state

### 2. File Logging
- **ProxyWritesRequestAndResponseFiles**: Verifies that HTTP traffic is logged to individual files
  - Confirms that request files contain expected headers (Method, URL, etc.)
  - Confirms that response files contain expected headers (Status Code, etc.)
  - Validates the file naming pattern: `yyyyMMdd_HHmmss_fff_NNNNNN_METHOD_HOST_request.txt`

### 3. URL Filtering
- **UrlFilterExcludesMatchingUrls**: Verifies that URL filters work correctly
  - Adds filters to exclude specific URL patterns
  - Makes requests to both filtered and non-filtered URLs
  - Confirms that filtered URLs are NOT logged to files
- **UrlFilterManagerAddsAndRemovesFilters**: Tests the URL filter manager API
  - Verifies adding/removing filters
  - Tests pattern matching logic

### 4. Process Filtering
- **ProcessFilterIncludesOnlyMatchingProcesses**: Verifies process-based filtering
  - Adds filters to include only specific processes
  - Confirms that only matching processes are logged
- **ProcessFilterManagerAddsAndRemovesFilters**: Tests the process filter manager API
  - Verifies adding/removing filters
  - Tests wildcard matching for process names

### 5. Multiple Filters
- **MultipleFiltersWorkTogether**: Verifies that multiple filters can be combined
  - Tests multiple URL filters applied simultaneously
  - Confirms all filters are active and working

## Running the Tests

### Locally

To run the integration tests locally:

```bash
# From the repository root
cd Keboo.FidgetProxy.Tests
dotnet run --configuration Release
```

Or to run all tests including unit tests:

```bash
# From the repository root
dotnet test --configuration Release
```

### In CI/CD

The integration tests run automatically on every push and pull request via GitHub Actions:
- Tests run on **Ubuntu (Linux)** and **Windows** using a matrix build
- All tests must pass on both platforms before the build can proceed
- Code coverage is collected on Ubuntu

## Test Design

### Cross-Platform Compatibility

The tests are designed to work on both Windows and Linux:
- **No system proxy changes**: Tests use `setAsSystemProxy: false` to avoid platform-specific behavior
- **Explicit proxy configuration**: Tests configure HttpClient to use the proxy explicitly via `HttpClientHandler.Proxy`
- **Temporary directories**: Each test uses a unique temporary directory for log files
- **Port selection**: Each test uses a different port to avoid conflicts when tests run in parallel

### Test Isolation

Each test:
- Creates a unique temporary output directory
- Uses a different proxy port (8081, 8082, 8083, etc.)
- Cleans up resources in the `Cleanup` method
- Is independent and can run in any order

### Minimal External Dependencies

Tests are designed to minimize external dependencies:
- Tests primarily verify the proxy infrastructure itself
- External HTTP requests (e.g., to httpbin.org) are wrapped in try-catch to handle network failures gracefully
- Tests focus on verifying that the proxy and filter mechanisms work, regardless of external connectivity

## Key Changes Made

### 1. ProxyServerManager Update

Added a `setAsSystemProxy` parameter to `ProxyServerManager.StartAsync`:

```csharp
public async Task StartAsync(string outputDirectory, int port = 8080, bool setAsSystemProxy = true)
```

This allows tests to start the proxy without modifying system settings, which:
- Avoids permission issues
- Works on both Windows and Linux
- Prevents tests from interfering with the host system

### 2. Platform-Specific Behavior

The `ProxyServerManager` now checks the platform before setting system proxy:

```csharp
if (setAsSystemProxy && OperatingSystem.IsWindows())
{
    _proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
}
```

### 3. CI/CD Pipeline

The GitHub Actions workflow now includes:
- An `integration-tests` job that runs on both Ubuntu and Windows
- A matrix build strategy to test both platforms simultaneously
- The existing `build` job depends on successful completion of integration tests

## Future Enhancements

Potential areas for future test improvements:
- Add tests for HTTPS traffic interception
- Add tests for certificate handling
- Add performance/load tests
- Add tests for gRPC control API
- Add tests for concurrent requests
- Add tests for edge cases (malformed URLs, large payloads, etc.)

## Troubleshooting

### Tests Failing Locally

If tests fail locally:
1. Ensure no other process is using ports 8081-8084
2. Check that you have permissions to create files in the temp directory
3. Verify .NET 10.0 SDK is installed
4. Try running tests individually to isolate the issue

### Tests Timing Out

If tests timeout:
- Some tests make real HTTP requests which may be slow or blocked by firewalls
- The tests are designed to gracefully handle network failures
- Ensure your network allows outbound HTTP/HTTPS connections

### Platform-Specific Failures

If tests pass on one platform but not another:
- Check for platform-specific file path issues
- Verify port availability on the failing platform
- Review error messages for platform-specific system calls
