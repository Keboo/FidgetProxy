# URL Filtering Examples

This document provides practical examples of using URL filtering with FidgetProxy.

## Quick Start

Start the proxy with basic filtering:

```bash
# Exclude all Google Analytics traffic
fidgetproxy start -e "*.google-analytics.com"

# Exclude multiple CDN providers
fidgetproxy start -e "*.cloudflare.com" -e "*.fastly.net" -e "*.akamai.net"

# Exclude API endpoints you don't care about
fidgetproxy start -e "*/health" -e "*/metrics" -e "*/telemetry/*"
```

## Real-World Scenarios

### Web Development

Filter out common noise during development:

```bash
fidgetproxy start \
  -e "*/hot-update/*" \
  -e "*webpack*" \
  -e "*/sockjs-node/*" \
  -e "*.map"
```

### API Testing

Focus only on your API by excluding third-party services:

```bash
fidgetproxy start \
  -e "*.googleapis.com" \
  -e "*.facebook.com" \
  -e "*.twitter.com" \
  -e "*.analytics.*"
```

### Performance Testing

Exclude static resources to focus on dynamic content:

```bash
fidgetproxy start \
  -e "*.jpg" \
  -e "*.png" \
  -e "*.gif" \
  -e "*.css" \
  -e "*.js" \
  -e "*.woff*"
```

### Browser Extension Development

Filter out browser internal traffic:

```bash
fidgetproxy start \
  -e "*chrome-extension://*" \
  -e "*moz-extension://*" \
  -e "*edge://*" \
  -e "*.safebrowsing.*"
```

## Pattern Matching Guide

### Domain Filtering

```bash
# Specific domain
-e "https://example.com/*"

# All subdomains
-e "*.example.com"

# Multiple TLDs
-e "*.example.*"

# Specific subdomain
-e "api.example.com/*"
```

### Path Filtering

```bash
# Specific path segment
-e "*/admin/*"

# API versions
-e "*/v1/*"
-e "*/v2/*"

# File types
-e "*.pdf"
-e "*.zip"
-e "*.mp4"

# Query parameters (matches entire URL)
-e "*?utm_*"
```

### Protocol Filtering

```bash
# Only HTTPS
-e "https://*"

# Only HTTP
-e "http://*"

# WebSocket
-e "ws://*"
-e "wss://*"
```

### Advanced Patterns

```bash
# Localhost on any port
-e "*localhost*"
-e "*127.0.0.1*"

# Development environments
-e "*dev.example.com/*"
-e "*staging.example.com/*"

# Health checks and monitoring
-e "*/health"
-e "*/healthz"
-e "*/ready"
-e "*/metrics"

# Common tracking/analytics
-e "*/track/*"
-e "*/pixel/*"
-e "*analytics*"
-e "*tracking*"
```

## Combining with Output Directory

```bash
# Save production API traffic only
fidgetproxy start \
  -o "C:\Logs\ProductionAPI" \
  -e "*.cdn.*" \
  -e "*.static.*" \
  -e "*/assets/*"
```

## Tips and Best Practices

1. **Start Broad, Then Narrow**: Begin with general filters and refine as you see what traffic appears
2. **Use Wildcards Liberally**: Patterns like `*analytics*` catch more variations than exact matches
3. **Test Your Patterns**: Start the proxy and check if unwanted traffic still appears in logs
4. **Performance**: More filters = faster logging (less disk I/O)
5. **Debugging**: Remove all filters to see everything, then add them back selectively

## Common Use Cases

### Filter Everything Except Your App

```bash
# Include only your domain (by excluding everything else)
fidgetproxy start \
  -e "*.google.com" \
  -e "*.microsoft.com" \
  -e "*.cloudflare.com"
  # ... add more as needed
```

### Focus on Errors

Since filters only affect logging (not proxying), you could start with broad filters and investigate when you see errors in your app.

### Mobile App Debugging

```bash
fidgetproxy start \
  -e "*.apple.com" \
  -e "*.icloud.com" \
  -e "*.crashlytics.com"
```

## Verification

After starting with filters, check the console output - filtered URLs won't appear there either.

```bash
# Start with filters
fidgetproxy start -e "*.googleapis.com"

# Visit a webpage that uses Google APIs
# You should see reduced console output and fewer log files
```
