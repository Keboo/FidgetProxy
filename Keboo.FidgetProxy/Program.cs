using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Http;
using System.Security.Principal;
using Grpc.Core;
using Grpc.Net.Client;
using Keboo.Web.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Keboo.FidgetProxy;

/// <summary>
/// Factory for creating IPC connections for gRPC (named pipes on Windows, Unix domain sockets on Linux/macOS)
/// </summary>
public class IpcConnectionFactory
{
    private readonly string _endpoint;

    public IpcConnectionFactory(string endpoint)
    {
        _endpoint = endpoint;
    }

    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows())
        {
            var clientStream = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _endpoint,
                direction: PipeDirection.InOut,
                options: PipeOptions.WriteThrough | PipeOptions.Asynchronous,
                impersonationLevel: TokenImpersonationLevel.Anonymous);

            try
            {
                await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return clientStream;
            }
            catch
            {
                clientStream.Dispose();
                throw;
            }
        }
        else
        {
            var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.Unix, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Unspecified);
            try
            {
                var endpoint = new System.Net.Sockets.UnixDomainSocketEndPoint(_endpoint);
                await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = BuildCommandLine();
        return rootCommand.Parse(args).InvokeAsync();
    }

    private static string GetIpcEndpoint()
    {
        if (OperatingSystem.IsWindows())
        {
            return ProcessTracker.GetNamedPipeName();
        }
        else
        {
            // On Linux/macOS, use Unix domain socket in temp directory
            var pipeName = ProcessTracker.GetNamedPipeName();
            return Path.Combine(Path.GetTempPath(), $"fidgetproxy-{pipeName}.sock");
        }
    }

    public static RootCommand BuildCommandLine()
    {
        // Root command
        RootCommand rootCommand = new("HTTP traffic debugging proxy tool");

        // Common option for output directory  
        string defaultOutputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fidgetproxy", "logs");
        Option<string> outputDirOption = new("--output-directory")
        {
            Description = "Directory where HTTP traffic logs will be written"
        };
        outputDirOption.Aliases.Add("-o");

        // Start command
        Command startCommand = new("start", "Start the proxy server in the background");
        startCommand.Options.Add(outputDirOption);
        startCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var outputDir = parseResult.GetValue(outputDirOption) ?? defaultOutputDir;
            return await StartCommandAsync(outputDir, cancellationToken);
        });

        // Stop command
        Command stopCommand = new("stop", "Stop the running proxy server");
        stopCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            return await StopCommandAsync(cancellationToken);
        });

        // Clean command
        Command cleanCommand = new("clean", "Terminate any stray processes and clean up system proxy settings");
        cleanCommand.SetAction((ParseResult parseResult) =>
        {
            return CleanCommand();
        });

        // Run command (hidden)
        Command runCommand = new("run", "Run the proxy server (internal use)");
        runCommand.Hidden = true;
        runCommand.Options.Add(outputDirOption);
        runCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var outputDir = parseResult.GetValue(outputDirOption) ?? defaultOutputDir;
            return await RunCommandAsync(outputDir, cancellationToken);
        });

        rootCommand.Subcommands.Add(startCommand);
        rootCommand.Subcommands.Add(stopCommand);
        rootCommand.Subcommands.Add(cleanCommand);
        rootCommand.Subcommands.Add(runCommand);

        return rootCommand;
    }

    private static async Task<int> StartCommandAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        try
        {
            // Check if proxy is already running
            if (ProcessTracker.IsProxyRunning())
            {
                Console.Error.WriteLine("Proxy server is already running");
                return 1;
            }

            // Get the path to the current executable
            var currentProcess = Process.GetCurrentProcess();
            var exePath = currentProcess.MainModule?.FileName ?? Environment.ProcessPath;
            
            if (string.IsNullOrEmpty(exePath))
            {
                Console.Error.WriteLine("Could not determine executable path");
                return 1;
            }

            // Capture output from the spawned process
            var stdoutCapture = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var stderrCapture = new System.Collections.Concurrent.ConcurrentQueue<string>();

            // Start a new process with the run command
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"run --output-directory \"{outputDirectory}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                Console.Error.WriteLine("Failed to start proxy process");
                return 1;
            }

            // Capture output asynchronously
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stdoutCapture.Enqueue(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    stderrCapture.Enqueue(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Give the process a moment to initialize
            await Task.Delay(1000);

            // Wait for the proxy to be ready by connecting to the GRPC server
            var endpoint = GetIpcEndpoint();
            var connectionFactory = new IpcConnectionFactory(endpoint);
            var channel = GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        ConnectCallback = connectionFactory.ConnectAsync
                    }
                });

            var client = new ProxyControl.ProxyControlClient(channel);

            // Poll for ready state with timeout
            var timeout = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            var lastException = (Exception?)null;

            while (DateTime.UtcNow - startTime < timeout)
            {
                // Check if process has exited
                if (process.HasExited)
                {
                    Console.Error.WriteLine($"Proxy process exited unexpectedly with code {process.ExitCode}");
                    PrintCapturedOutput(stdoutCapture, stderrCapture);
                    return 1;
                }

                try
                {
                    var response = await client.GetStatusAsync(
                        new GetStatusRequest(),
                        deadline: DateTime.UtcNow.AddSeconds(2),
                        cancellationToken: cancellationToken);

                    if (response.IsRunning)
                    {
                        Console.WriteLine("Proxy server started successfully");
                        Console.WriteLine();
                        Console.WriteLine("Note: Most applications will automatically detect the proxy change.");
                        Console.WriteLine("      If an application doesn't pick up the proxy, try restarting it.");
                        Console.WriteLine("      Use tools like curl with -x http://localhost:8080 to test immediately.");
                        return 0;
                    }
                }
                catch (RpcException ex)
                {
                    // Server not ready yet, wait and retry
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(500, cancellationToken);
            }

            Console.Error.WriteLine("Timeout waiting for proxy server to start");
            if (lastException != null)
            {
                Console.Error.WriteLine($"Last error: {lastException.Message}");
            }
            PrintCapturedOutput(stdoutCapture, stderrCapture);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error starting proxy: {ex.Message}");
            return 1;
        }
    }

    private static void PrintCapturedOutput(
        System.Collections.Concurrent.ConcurrentQueue<string> stdout,
        System.Collections.Concurrent.ConcurrentQueue<string> stderr)
    {
        if (stdout.Count > 0 || stderr.Count > 0)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("=== Output from proxy process ===");
            
            if (stdout.Count > 0)
            {
                Console.Error.WriteLine("--- STDOUT ---");
                while (stdout.TryDequeue(out var line))
                {
                    Console.Error.WriteLine(line);
                }
            }

            if (stderr.Count > 0)
            {
                Console.Error.WriteLine("--- STDERR ---");
                while (stderr.TryDequeue(out var line))
                {
                    Console.Error.WriteLine(line);
                }
            }
            
            Console.Error.WriteLine("=================================");
        }
    }

    private static async Task<int> StopCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if proxy is running
            if (!ProcessTracker.IsProxyRunning())
            {
                Console.Error.WriteLine("No proxy server is running");
                return 1;
            }

            // Connect to the GRPC server
            var endpoint = GetIpcEndpoint();
            var connectionFactory = new IpcConnectionFactory(endpoint);
            var channel = GrpcChannel.ForAddress(
                "http://localhost",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        ConnectCallback = connectionFactory.ConnectAsync
                    }
                });

            var client = new ProxyControl.ProxyControlClient(channel);

            // Send shutdown command
            var response = await client.ShutdownAsync(
                new ShutdownRequest(),
                deadline: DateTime.UtcNow.AddSeconds(10),
                cancellationToken: cancellationToken);

            if (response.Success)
            {
                Console.WriteLine("Proxy server stopped successfully");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Failed to stop proxy: {response.Message}");
                return 1;
            }
        }
        catch (RpcException ex)
        {
            Console.Error.WriteLine($"Failed to connect to proxy server: {ex.Status.Detail}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error stopping proxy: {ex.Message}");
            return 1;
        }
    }

    private static int CleanCommand()
    {
        try
        {
            Console.WriteLine("Cleaning up FidgetProxy...");

            // Kill any running proxy processes
            Console.WriteLine("Terminating any running proxy processes...");
            ProcessTracker.KillStrayProcesses();

            // Restore system proxy settings
            Console.WriteLine("Restoring system proxy settings...");
            try
            {
                // Create a temporary ProxyServer instance to access system proxy restoration
                using var tempProxy = new ProxyServer();
                tempProxy.DisableAllSystemProxies();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not restore system proxy settings: {ex.Message}");
            }

            Console.WriteLine("Cleanup complete.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during cleanup: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunCommandAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        try
        {
            // Check if already running
            if (ProcessTracker.IsProxyRunning())
            {
                Console.Error.WriteLine("Proxy server is already running");
                return 1;
            }

            // Write PID file
            ProcessTracker.WritePidFile();

            // Ensure cleanup on exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                ProcessTracker.RemovePidFile();
            };

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                ProcessTracker.RemovePidFile();
            };

            // Create the proxy manager
            var proxyManager = new ProxyServerManager();

            // Build and configure the web host for GRPC
            var builder = WebApplication.CreateBuilder();
            
            // Configure Kestrel to listen on platform-specific IPC endpoint
            var endpoint = GetIpcEndpoint();
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (OperatingSystem.IsWindows())
                {
                    options.ListenNamedPipe(endpoint, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }
                else
                {
                    // On Linux/macOS, use Unix domain socket
                    options.ListenUnixSocket(endpoint, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }
            });

            // Add GRPC services
            builder.Services.AddGrpc();
            builder.Services.AddSingleton(proxyManager);

            var app = builder.Build();
            app.MapGrpcService<ProxyControlService>();

            // Start the proxy server
            await proxyManager.StartAsync(outputDirectory);
            Console.WriteLine($"Proxy server is running. Logs will be written to: {outputDirectory}");
            Console.WriteLine("Press Ctrl+C to stop...");

            // Run the GRPC server
            await app.RunAsync(cancellationToken);

            // Cleanup
            await proxyManager.StopAsync();
            ProcessTracker.RemovePidFile();

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Proxy server shutting down...");
            ProcessTracker.RemovePidFile();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error running proxy: {ex.Message}");
            ProcessTracker.RemovePidFile();
            return 1;
        }
    }
}