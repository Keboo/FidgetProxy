# Keboo.FidgetProxy

A command-line HTTP traffic debugging proxy tool that captures and logs all HTTP/HTTPS traffic passing through it.

## Features

- **System Proxy Integration**: Automatically configures itself as the Windows system proxy
- **Traffic Logging**: Logs all HTTP requests and responses (including headers and body) to individual text files
- **Background Operation**: Runs as a background process with start/stop commands
- **GRPC Control**: Internal GRPC server for process communication and control
- **SSL/TLS Support**: Intercepts and logs HTTPS traffic using certificate generation

## Installation

Install as a .NET global tool:

```bash
dotnet tool install --global Keboo.FidgetProxy
```

Or build from source:

```bash
dotnet build
dotnet pack
dotnet tool install --global --add-source ./nupkg Keboo.FidgetProxy
```

## Usage

### Start the Proxy

Start the proxy server in the background:

```bash
fidgetproxy start
```

By default, logs are written to `%USERPROFILE%\.fidgetproxy\logs`. You can specify a custom output directory:

```bash
fidgetproxy start --output-directory C:\Logs\HttpTraffic
```

Or use the short form:

```bash
fidgetproxy start -o C:\Logs\HttpTraffic
```

### Stop the Proxy

Stop the running proxy server:

```bash
fidgetproxy stop
```

### Clean Up

If the proxy crashes or processes become orphaned, use the clean command to terminate any stray processes and restore system proxy settings:

```bash
fidgetproxy clean
```

This command will:
- Terminate any running FidgetProxy processes
- Remove PID files
- Restore Windows system proxy settings to their defaults

### Viewing Logs

Traffic logs are written as individual text files in the output directory with the format:

```
yyyyMMdd_HHmmss_fff_NNNNNN_METHOD_HOST_request.txt
yyyyMMdd_HHmmss_fff_NNNNNN_METHOD_HOST_response.txt
```

For example:
```
20260112_143052_123_000001_GET_example.com_request.txt
20260112_143052_456_000001_GET_example.com_response.txt
```

Each file contains:
- Timestamp
- HTTP method and URL (for requests) or status code (for responses)
- All HTTP headers
- Full body content

## How It Works

1. **Start Command**: Spawns a new background process running the hidden `run` command, waits for the proxy to be ready via GRPC, then exits
2. **Run Command** (Hidden): 
   - Starts the ProxyServer and configures it as the Windows system proxy
   - Subscribes to HTTP request/response events
   - Logs all traffic to the output directory  
   - Hosts a GRPC server on a named pipe for control communication
   - Keeps the process alive until shutdown is requested
3. **Stop Command**: Connects to the GRPC server and sends a shutdown request, which gracefully stops the proxy and restores system proxy settings

## Architecture

- **ProxyServer**: Based on Keboo.Web.Proxy library for HTTP/HTTPS interception
- **GRPC Communication**: Named pipe-based GRPC for inter-process communication
- **Process Tracking**: PID file mechanism to track running proxy instances
- **Traffic Logger**: Asynchronous logging of all HTTP traffic to individual files

## Requirements

- .NET 10.0 or later
- Windows (for system proxy integration)
- Administrator privileges may be required for certificate installation

## Development

Build the project:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

## License

See LICENSE.txt for details.


### Parameters
[Default template options](https://learn.microsoft.com/dotnet/core/tools/dotnet-new#options)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--pipeline` | CI/CD provider to use. Options: `github`, `azuredevops`, `none` | `github` |
| `--sln` | Use legacy .sln format instead of .slnx format | `false` |
| `--tests` | Testing framework to use. Options: `xunit`, `mstest`, `tunit`, `none` | `xunit` |

**Example with Azure DevOps:**
```cli
> dotnet new keboo.console --pipeline azuredevops
```

**Example with no CI/CD pipeline:**
```cli
> dotnet new keboo.console --pipeline none
```

**Example with legacy .sln format:**
```cli
> dotnet new keboo.console --sln true
```

**Example with MSTest:**
```cli
> dotnet new keboo.console --tests mstest
```

**Example with no tests:**
```cli
> dotnet new keboo.console --tests none
```

## Updating .NET Version

This template uses a `global.json` file to specify the required .NET SDK version. To update the .NET SDK version:

1. Update the `global.json` file in the solution root
2. Update the `<TargetFramework>` in the `csproj` files.

## Key Features

### Build Customization
[Docs](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory?view=vs-2022&WT.mc_id=DT-MVP-5003472)

### Centralized Package Management
[Docs](https://learn.microsoft.com/nuget/consume-packages/Central-Package-Management?WT.mc_id=DT-MVP-5003472)

### NuGet package source mapping
[Docs](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping?WT.mc_id=DT-MVP-5003472)

### GitHub Actions / Azure DevOps Pipeline
Build, test, and code coverage reporting included. Use `--pipeline` parameter to choose between GitHub Actions (default) or Azure DevOps Pipelines.

### Solution File Format (slnx)
By default, this template uses the new `.slnx` (XML-based solution) format introduced in .NET 9. This modern format is more maintainable and easier to version control compared to the legacy `.sln` format.

[Blog: Introducing slnx support in the dotnet CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/?WT.mc_id=DT-MVP-5003472)  
[Docs: dotnet sln command](https://learn.microsoft.com/dotnet/core/tools/dotnet-sln?WT.mc_id=DT-MVP-5003472)

If you need to use the legacy `.sln` format, use the `--sln true` parameter when creating the template.