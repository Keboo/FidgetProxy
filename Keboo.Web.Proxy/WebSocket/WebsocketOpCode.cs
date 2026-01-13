namespace Keboo.Web.Proxy;

public enum WebsocketOpCode : byte
{
    Continuation,
    Text,
    Binary,
    ConnectionClose = 8,
    Ping,
    Pong
}