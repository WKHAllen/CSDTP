using System.Collections.Generic;
using CSDTP;

namespace CSDTPTest;

internal class TestServer<S, R> : Server<S, R>
{
    public bool ReplyWithStringLength = false;

    public TestServer(int receiveCount, int connectCount, int disconnectCount)
    {
        ReceiveCount = receiveCount;
        ConnectCount = connectCount;
        DisconnectCount = disconnectCount;
    }

    public int DisconnectCount { get; private set; }
    public int ReceiveCount { get; private set; }
    public int ConnectCount { get; private set; }

    public List<R> Received { get; } = new();
    public List<ulong> ReceivedClientIDs { get; } = new();
    public List<ulong> ConnectClientIDs { get; } = new();
    public List<ulong> DisconnectClientIDs { get; } = new();

    public bool EventsDone()
    {
        return ReceiveCount == 0 && ConnectCount == 0 && DisconnectCount == 0;
    }

    private static T UnsafeCast<F, T>(F from)
    {
        return Util.Deserialize<T>(Util.Serialize(from));
    }

    protected override void Receive(ulong clientId, R data)
    {
        ReceiveCount--;
        Received.Add(data);
        ReceivedClientIDs.Add(clientId);

        if (ReplyWithStringLength)
        {
            var strData = UnsafeCast<R, string>(data);
            var strLen = strData.Length;
            var sendLen = UnsafeCast<int, S>(strLen);
            Send(clientId, sendLen);
        }
    }

    protected override void Connect(ulong clientId)
    {
        ConnectCount--;
        ConnectClientIDs.Add(clientId);
    }

    protected override void Disconnect(ulong clientId)
    {
        DisconnectCount--;
        DisconnectClientIDs.Add(clientId);
    }
}