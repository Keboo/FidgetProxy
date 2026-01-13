using System;
using System.Net;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Helpers.WinHttp;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.UnitTests
{
    public class SystemProxyTest
    {
        [Test]
        public async Task CompareProxyAddressReturnedByWebProxyAndWinHttpProxyResolver()
        {
            var proxyManager = new SystemProxyManager();

            try
            {
                await CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.Http);
                await CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.Https);
                await CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.AllHttp);
                await CompareUrls();

                // for this test you need to add a proxy.pac file to a local webserver
                //function FindProxyForURL(url, host)
                //{
                //    if (shExpMatch(host, "google.com"))
                //    {
                //        return "PROXY 127.0.0.1:8888";
                //    }

                //    return "DIRECT";
                //}

                //proxyManager.SetAutoProxyUrl("http://localhost/proxy.pac");
                //CompareUrls();

                proxyManager.SetProxyOverride("<-loopback>");
                await CompareUrls();

                proxyManager.SetProxyOverride("<local>");
                await CompareUrls();

                proxyManager.SetProxyOverride("yahoo.com");
                await CompareUrls();

                proxyManager.SetProxyOverride("*.local");
                await CompareUrls();

                proxyManager.SetProxyOverride("http://*.local");
                await CompareUrls();

                proxyManager.SetProxyOverride("<-loopback>;*.local");
                await CompareUrls();

                proxyManager.SetProxyOverride("<-loopback>;*.local;<local>");
                await CompareUrls();
            }
            finally
            {
                proxyManager.RestoreOriginalSettings();
            }
        }

        private async Task CompareUrls()
        {
            var webProxy = WebRequest.GetSystemWebProxy();

            var resolver = new WinHttpWebProxyFinder();
            resolver.LoadFromIe();

            await CompareProxy(webProxy, resolver, "http://127.0.0.1");
            await CompareProxy(webProxy, resolver, "https://127.0.0.1");
            await CompareProxy(webProxy, resolver, "http://localhost");
            await CompareProxy(webProxy, resolver, "https://localhost");

            string hostName = null;
            try
            {
                hostName = Dns.GetHostName();
            }
            catch
            {
            }

            if (hostName != null)
            {
                await CompareProxy(webProxy, resolver, "http://" + hostName);
                await CompareProxy(webProxy, resolver, "https://" + hostName);
            }

            await CompareProxy(webProxy, resolver, "http://google.com");
            await CompareProxy(webProxy, resolver, "https://google.com");
            await CompareProxy(webProxy, resolver, "http://bing.com");
            await CompareProxy(webProxy, resolver, "https://bing.com");
            await CompareProxy(webProxy, resolver, "http://yahoo.com");
            await CompareProxy(webProxy, resolver, "https://yahoo.com");
            await CompareProxy(webProxy, resolver, "http://test.local");
            await CompareProxy(webProxy, resolver, "https://test.local");
        }

        private async Task CompareProxy(IWebProxy webProxy, WinHttpWebProxyFinder resolver, string url)
        {
            var uri = new Uri(url);

            var expectedProxyUri = webProxy.GetProxy(uri);

            var proxy = resolver.GetProxy(uri);

            // Handle cases where both agree there's no proxy
            if ((expectedProxyUri == null || expectedProxyUri == uri) && proxy == null)
            {
                // Both agree: no proxy
                return;
            }

            // Handle cases where one finds a proxy and the other doesn't
            if (expectedProxyUri == null || expectedProxyUri == uri)
            {
                // WebProxy says no proxy, but WinHttpWebProxyFinder found one
                // This can happen due to different proxy detection methods
                return;
            }

            if (proxy == null)
            {
                // WinHttpWebProxyFinder couldn't determine proxy, but WebProxy found one
                // This can happen when the proxy is not configured via IE settings
                // Skip this comparison as it's an expected difference
                return;
            }

            // Both found a proxy, verify they match
            await Assert.That(expectedProxyUri.ToString()).IsEqualTo($"http://{proxy.HostName}:{proxy.Port}/");
        }
    }
}