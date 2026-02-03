using System.Net.Http;
using Keboo.Web.Proxy.IntegrationTests.Helpers;
using Keboo.Web.Proxy.IntegrationTests.Setup;

namespace Keboo.Web.Proxy.IntegrationTests;

public class TestSuite
{
    private readonly TestServer server;

    public TestSuite(bool requireMutualTls = false)
    {
        var dummyProxy = new ProxyServer();
        var serverCertificate = dummyProxy.CertificateManager.CreateServerCertificate("localhost").Result
            ?? throw new InvalidOperationException("Failed to create certificate");
        server = new TestServer(serverCertificate, requireMutualTls);
    }

    public TestServer GetServer()
    {
        return server;
    }

    public static ProxyServer GetProxy(ProxyServer? upStreamProxy = null)
    {
        if (upStreamProxy != null)
        {
            return new TestProxyServer(false, upStreamProxy).ProxyServer;
        }

        return new TestProxyServer(false).ProxyServer;
    }

    public static ProxyServer GetReverseProxy(ProxyServer? upStreamProxy = null)
    {
        if (upStreamProxy != null)
        {
            return new TestProxyServer(true, upStreamProxy).ProxyServer;
        }

        return new TestProxyServer(true).ProxyServer;
    }

    public static HttpClient GetClient(ProxyServer proxyServer, bool enableBasicProxyAuthorization = false)
    {
        return TestHelper.GetHttpClient(proxyServer.ProxyEndPoints[0].Port, enableBasicProxyAuthorization);
    }

    public static HttpClient GetReverseProxyClient()
    {
        return TestHelper.GetHttpClient();
    }
}
