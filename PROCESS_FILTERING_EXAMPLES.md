# Process Filtering Examples

This document provides practical examples of using process filtering with FidgetProxy to capture traffic from specific applications.

## Quick Start

Start the proxy and log only specific processes:

```bash
# Log only Chrome browser traffic
fidgetproxy start -p "chrome"

# Log only a specific application
fidgetproxy start -p "myapp.exe"

# Multiple applications
fidgetproxy start -p "chrome" -p "firefox" -p "msedge"
```

## How Process Filtering Works

- **Inclusion-based**: When you specify process filters, ONLY those processes will be logged
- **No filters = Log everything**: If no process filters are specified, all processes are logged
- **Local only**: Process filtering only works for traffic from the local machine
- **Remote traffic**: Traffic from remote machines is always included (can't determine process)

## Real-World Scenarios

### Web Browser Debugging

Debug a specific browser:

```bash
# Only Chrome
fidgetproxy start -p "chrome"

# Chrome and all helpers
fidgetproxy start -p "chrome*"

# Multiple browsers
fidgetproxy start -p "chrome" -p "firefox" -p "msedge"
```

### Application Development

Focus on your app's traffic:

```bash
# Your application only
fidgetproxy start -p "MyApp"

# Your app and test runner
fidgetproxy start -p "MyApp" -p "testrunner"

# Development tools
fidgetproxy start -p "node" -p "npm"
```

### Testing Specific Services

```bash
# Only PowerShell scripts
fidgetproxy start -p "powershell"

# Specific service
fidgetproxy start -p "WindowsService.exe"

# Process by PID (useful for debugging a specific instance)
fidgetproxy start -p "12345"
```

## Pattern Matching

### Exact Name Matching

```bash
# Matches chrome.exe only
-p "chrome"

# Matches exactly firefox.exe
-p "firefox"
```

### Wildcard Patterns

```bash
# All Chrome processes (chrome.exe, chrome-helper.exe, etc.)
-p "chrome*"

# Any process with 'test' in the name
-p "*test*"

# Processes starting with 'app'
-p "app*"

# Single character wildcard
-p "chrome?"  # Matches chrome1, chromeX, etc.
```

### PID Matching

```bash
# Specific process ID
-p "1234"

# Multiple PIDs
-p "1234" -p "5678"
```

## Combining Filters

### Process + URL Filtering

```bash
# Chrome traffic, excluding CDNs
fidgetproxy start -p "chrome" -e "*.cdn.*" -e "*.cloudflare.com"

# Your app, only API calls
fidgetproxy start -p "myapp" -e "*/static/*" -e "*.js" -e "*.css"
```

### Multiple Processes + URL Filters

```bash
fidgetproxy start \
  -p "chrome" -p "firefox" \
  -e "*.googleapis.com" \
  -e "*/analytics/*"
```

### Process Filtering + Custom Output

```bash
# Separate logs for different apps
fidgetproxy start -p "myapp" -o "C:\Logs\MyApp"

# Then in another terminal
fidgetproxy start -p "chrome" -o "C:\Logs\Chrome"
```

## Advanced Use Cases

### Debugging Multi-Process Applications

Many apps use multiple processes. Use wildcards to capture all:

```bash
# Electron apps (main + renderer processes)
fidgetproxy start -p "electron*"

# Visual Studio Code (multiple processes)
fidgetproxy start -p "code*"

# Chrome (browser + helpers + extensions)
fidgetproxy start -p "chrome*"
```

### Isolating Test Traffic

```bash
# Only automated test runner
fidgetproxy start -p "testrunner" -o "C:\Logs\TestRun"

# Multiple test tools
fidgetproxy start -p "jest" -p "mocha" -p "playwright"
```

### Service Monitoring

```bash
# Windows Service
fidgetproxy start -p "MyService.exe"

# Background worker
fidgetproxy start -p "worker*"
```

### Development Environment

```bash
# Node.js development server
fidgetproxy start -p "node"

# .NET application
fidgetproxy start -p "dotnet"

# Python scripts
fidgetproxy start -p "python*"
```

## Troubleshooting

### Process Not Being Logged

1. **Check process name**: Use Task Manager to verify the exact process name
2. **Try with wildcard**: Use `*processname*` to be more inclusive
3. **Check if remote**: Remote connections can't be filtered by process
4. **Verify process is running**: Start your app after starting the proxy

### Finding Process Names

```powershell
# PowerShell: List all processes
Get-Process | Select-Object ProcessName, Id | Format-Table

# Find specific process
Get-Process | Where-Object {$_.ProcessName -like "*chrome*"}
```

### Testing Your Filters

```bash
# Start with verbose logging (no filters) to see all processes
fidgetproxy start

# Review the logs to see "Client Process:" values
# Then restart with specific filters
fidgetproxy stop
fidgetproxy start -p "chrome"
```

## Tips and Best Practices

1. **Start Broad**: Begin with `*appname*` and narrow down if needed
2. **Use Process Names, Not PIDs**: PIDs change on restart; names are stable
3. **Combine with URL Filters**: For laser-focused traffic capture
4. **Test First**: Run without filters to identify process names
5. **Monitor Output**: Check console output to verify filters are working

## Log Format

When process filtering is active, logs show:

```
=== HTTP REQUEST ===
Time: 2026-01-15 14:30:52.123
Proxy Process: Keboo.FidgetProxy (PID: 8888)
Client Process: chrome (PID: 1234)
Method: GET
URL: https://example.com/api
...
```

The **Client Process** field shows which application made the request.

## Common Scenarios Quick Reference

| Scenario | Command |
|----------|---------|
| Debug Chrome only | `fidgetproxy start -p "chrome"` |
| Your app only | `fidgetproxy start -p "myapp"` |
| Multiple browsers | `fidgetproxy start -p "chrome" -p "firefox" -p "edge"` |
| Node.js development | `fidgetproxy start -p "node"` |
| Specific PID | `fidgetproxy start -p "1234"` |
| Wildcard match | `fidgetproxy start -p "*test*"` |
| App + no CDNs | `fidgetproxy start -p "myapp" -e "*.cdn.*"` |
