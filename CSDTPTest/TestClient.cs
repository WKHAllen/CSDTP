using System.Collections.Generic;
using CSDTP;

namespace CSDTPTest;

internal class TestClient<S, R> : Client<S, R>
{
    public TestClient(int receiveCount, int disconnectedCount)
    {
        ReceiveCount = receiveCount;
        DisconnectedCount = disconnectedCount;
    }

    public int ReceiveCount { get; private set; }
    public int DisconnectedCount { get; private set; }

    public List<R> Received { get; } = new();

    public bool EventsDone()
    {
        return ReceiveCount == 0 && DisconnectedCount == 0;
    }

    protected override void Receive(R data)
    {
        ReceiveCount--;
        Received.Add(data);
    }

    protected override void Disconnected()
    {
        DisconnectedCount--;
    }
}