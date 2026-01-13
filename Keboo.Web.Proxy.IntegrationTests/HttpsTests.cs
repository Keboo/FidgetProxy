using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Keboo.Web.Proxy.IntegrationTests;

[NotInParallel]
public class HttpsTests
{
    [Test]
    public async Task Can_Handle_Https_Request()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetProxy();
        var client = testSuite.GetClient(proxy);

        var response = await client.PostAsync(new Uri(server.ListeningHttpsUrl),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Can_Handle_Https_Fake_Tunnel_Request()
    {
        var testSuite = new TestSuite();

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetProxy();
        proxy.BeforeRequest += async (sender, e) =>
        {
            e.HttpClient.Request.Url = server.ListeningHttpUrl;
            await Task.FromResult(0);
        };

        var client = testSuite.GetClient(proxy);

        var response = await client.PostAsync(new Uri($"https://{Guid.NewGuid().ToString()}.com"),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }

    [Test]
    public async Task Can_Handle_Https_Mutual_Tls_Request()
    {
        var testSuite = new TestSuite(true);

        var server = testSuite.GetServer();
        server.HandleRequest(context =>
        {
            return context.Response.WriteAsync("I am server. I received your greetings.");
        });

        var proxy = testSuite.GetProxy();
        var clientCert = proxy.CertificateManager.CreateCertificate("client.com", false);

        proxy.ClientCertificateSelectionCallback += async (sender, e) =>
        {
            e.ClientCertificate = clientCert;
            await Task.CompletedTask;
        };

        var client = testSuite.GetClient(proxy);

        var response = await client.PostAsync(new Uri(server.ListeningHttpsUrl),
            new StringContent("hello server. I am a client."));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("I am server. I received your greetings.");
    }
}
