using System;
using System.Net;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using Titanium.Web.Proxy.Models;

namespace Titanium.Web.Proxy.UnitTests
{
    public class ProxyServerTests
    {
        [Test]
        public async Task
            GivenOneEndpointIsAlreadyAddedToAddress_WhenAddingNewEndpointToExistingAddress_ThenExceptionIsThrown()
        {
            // Arrange
            var proxy = new ProxyServer();
            const int port = 9999;
            var firstIpAddress = IPAddress.Parse("127.0.0.1");
            var secondIpAddress = IPAddress.Parse("127.0.0.1");
            proxy.AddEndPoint(new ExplicitProxyEndPoint(firstIpAddress, port, false));

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                proxy.AddEndPoint(new ExplicitProxyEndPoint(secondIpAddress, port, false));
                await Task.CompletedTask;
            });

            // Assert
            await Assert.That(exception.Message).Contains("Cannot add another endpoint to same port");
        }

        [Test]
        public async Task
            GivenOneEndpointIsAlreadyAddedToAddress_WhenAddingNewEndpointToExistingAddress_ThenTwoEndpointsExists()
        {
            // Arrange
            var proxy = new ProxyServer();
            const int port = 9999;
            var firstIpAddress = IPAddress.Parse("127.0.0.1");
            var secondIpAddress = IPAddress.Parse("192.168.1.1");
            proxy.AddEndPoint(new ExplicitProxyEndPoint(firstIpAddress, port, false));

            // Act
            proxy.AddEndPoint(new ExplicitProxyEndPoint(secondIpAddress, port, false));

            // Assert
            await Assert.That(proxy.ProxyEndPoints.Count).IsEqualTo(2);
        }

        [Test]
        public async Task GivenOneEndpointIsAlreadyAddedToPort_WhenAddingNewEndpointToExistingPort_ThenExceptionIsThrown()
        {
            // Arrange
            var proxy = new ProxyServer();
            const int port = 9999;
            proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, port, false));

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, port, false));
                await Task.CompletedTask;
            });

            // Assert
            await Assert.That(exception.Message).Contains("Cannot add another endpoint to same port");
        }

        [Test]
        public async Task
            GivenOneEndpointIsAlreadyAddedToZeroPort_WhenAddingNewEndpointToExistingPort_ThenTwoEndpointsExists()
        {
            // Arrange
            var proxy = new ProxyServer();
            const int port = 0;
            proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, port, false));

            // Act
            proxy.AddEndPoint(new ExplicitProxyEndPoint(IPAddress.Loopback, port, false));

            // Assert
            await Assert.That(proxy.ProxyEndPoints.Count).IsEqualTo(2);
        }
    }
}