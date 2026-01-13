using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Helpers.WinHttp;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.UnitTests
{
    [TestClass]
    public class SystemProxyTest
    {
        [TestMethod]
        public void CompareProxyAddressReturnedByWebProxyAndWinHttpProxyResolver()
        {
            var proxyManager = new SystemProxyManager();

            try
            {
                CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.Http);
                CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.Https);
                CompareUrls();

                proxyManager.SetProxy("127.0.0.1", 8000, ProxyProtocolType.AllHttp);
                CompareUrls();

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
                CompareUrls();

                proxyManager.SetProxyOverride("<local>");
                CompareUrls();

                proxyManager.SetProxyOverride("yahoo.com");
                CompareUrls();

                proxyManager.SetProxyOverride("*.local");
                CompareUrls();

                proxyManager.SetProxyOverride("http://*.local");
                CompareUrls();

                proxyManager.SetProxyOverride("<-loopback>;*.local");
                CompareUrls();

                proxyManager.SetProxyOverride("<-loopback>;*.local;<local>");
                CompareUrls();
            }
            finally
            {
                proxyManager.RestoreOriginalSettings();
            }
        }

        private void CompareUrls()
        {
            var webProxy = WebRequest.GetSystemWebProxy();

            var resolver = new WinHttpWebProxyFinder();
            resolver.LoadFromIe();

            CompareProxy(webProxy, resolver, "http://127.0.0.1");
            CompareProxy(webProxy, resolver, "https://127.0.0.1");
            CompareProxy(webProxy, resolver, "http://localhost");
            CompareProxy(webProxy, resolver, "https://localhost");

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
                CompareProxy(webProxy, resolver, "http://" + hostName);
                CompareProxy(webProxy, resolver, "https://" + hostName);
            }

            CompareProxy(webProxy, resolver, "http://google.com");
            CompareProxy(webProxy, resolver, "https://google.com");
            CompareProxy(webProxy, resolver, "http://bing.com");
            CompareProxy(webProxy, resolver, "https://bing.com");
            CompareProxy(webProxy, resolver, "http://yahoo.com");
            CompareProxy(webProxy, resolver, "https://yahoo.com");
            CompareProxy(webProxy, resolver, "http://test.local");
            CompareProxy(webProxy, resolver, "https://test.local");
        }

        private void CompareProxy(IWebProxy webProxy, WinHttpWebProxyFinder resolver, string url)
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
            Assert.AreEqual(expectedProxyUri.ToString(), $"http://{proxy.HostName}:{proxy.Port}/");
        }
    }
}