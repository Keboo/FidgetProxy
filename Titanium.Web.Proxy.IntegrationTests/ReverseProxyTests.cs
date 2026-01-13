using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.IntegrationTests;

[NotInParallel]
public class ReverseProxyTests
{
    [Test]
    public async Task Smoke_Test_Http_To_Http_Reverse_Proxy()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetReverseProxy();
        proxy.BeforeRequest += async (sender, e) =>
        {
            e.HttpClient.Request.Url = server.ListeningHttpUrl;
            await Task.FromResult(0);
        };

        var client = testSuite.GetReverseProxyClient();

        var response = await client.PostAsync(new Uri($"http://localhost:{proxy.ProxyEndPoints[0].Port}"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Smoke_Test_Https_To_Http_Reverse_Proxy()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetReverseProxy();
        proxy.BeforeRequest += async (sender, e) =>
        {
            e.HttpClient.Request.Url = server.ListeningHttpUrl;
            await Task.FromResult(0);
        };

        var client = testSuite.GetReverseProxyClient();

        var response = await client.PostAsync(new Uri($"https://localhost:{proxy.ProxyEndPoints[0].Port}"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Smoke_Test_Http_To_Https_Reverse_Proxy()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetReverseProxy();
        proxy.BeforeRequest += async (sender, e) =>
        {
            e.HttpClient.Request.Url = server.ListeningHttpsUrl;
            await Task.FromResult(0);
        };

        var client = testSuite.GetReverseProxyClient();

        var response = await client.PostAsync(new Uri($"http://localhost:{proxy.ProxyEndPoints[0].Port}"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Smoke_Test_Https_To_Https_Reverse_Proxy()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetReverseProxy();
        proxy.BeforeRequest += async (sender, e) =>
        {
            e.HttpClient.Request.Url = server.ListeningHttpsUrl;
            await Task.FromResult(0);
        };

        var client = testSuite.GetReverseProxyClient();

        var response = await client.PostAsync(new Uri($"https://localhost:{proxy.ProxyEndPoints[0].Port}"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Smoke_Test_Https_To_Https_Reverse_Proxy_Tunnel_Without_Decryption()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetReverseProxy();
        var endpoint =
            proxy.ProxyEndPoints.Where(x => x is TransparentProxyEndPoint).First() as TransparentProxyEndPoint;

        endpoint.BeforeSslAuthenticate += async (sender, e) =>
        {
            e.DecryptSsl = false;
            e.ForwardHttpsPort = server.HttpsListeningPort;
        };

        var client = testSuite.GetReverseProxyClient();

        var response = await client.PostAsync(new Uri($"https://localhost:{proxy.ProxyEndPoints[0].Port}"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }
}
