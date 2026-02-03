using System;
using System.Net;
using System.Net.Http;

namespace Keboo.Web.Proxy.IntegrationTests.Helpers;

public static class TestHelper
{
    public static HttpClient GetHttpClient(int localProxyPort,
        bool enableBasicProxyAuthorization = false)
    {
        var proxy = new TestProxy($"http://localhost:{localProxyPort}", enableBasicProxyAuthorization);

        var handler = new HttpClientHandler 
        { 
            Proxy = proxy, 
            UseProxy = true,
            // Accept self-signed certificates generated for testing. This is safe in integration tests
            // where we control both the client and server, but should not be used in production code.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        return new HttpClient(handler);
    }

    public static HttpClient GetHttpClient()
    {
        var handler = new HttpClientHandler
        {
            // Accept self-signed certificates generated for testing. This is safe in integration tests
            // where we control both the client and server, but should not be used in production code.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler);
    }

    public class TestProxy : IWebProxy
    {
        public TestProxy(string proxyUri, bool enableAuthorization)
            : this(new Uri(proxyUri))
        {
            if (enableAuthorization)
            {
                Credentials = new NetworkCredential("test", "Test56");
            }
        }

        private TestProxy(Uri proxyUri)
        {
            ProxyUri = proxyUri;
        }

        public Uri ProxyUri { get; set; }
        public ICredentials? Credentials { get; set; }

        public Uri GetProxy(Uri destination)
        {
            return ProxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
}
