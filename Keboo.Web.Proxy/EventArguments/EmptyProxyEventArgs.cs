using Keboo.Web.Proxy.Network.Tcp;

namespace Keboo.Web.Proxy.EventArguments;

public class EmptyProxyEventArgs : ProxyEventArgsBase
{
    internal EmptyProxyEventArgs(ProxyServer server, TcpClientConnection clientConnection) : base(server,
        clientConnection)
    {
    }
}