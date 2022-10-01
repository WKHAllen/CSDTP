using System;
using System.Text;
using CSDTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSDTPTest;

internal class TestServer : Server
{
    public byte[]? RandomMessage;
    public bool ReceivingRandomMessage;

    protected override void Receive(ulong clientId, byte[] data)
    {
        if (!ReceivingRandomMessage)
        {
            var message = Encoding.UTF8.GetString(data);
            Console.WriteLine("[SERVER] Received data from client #{0}: {1} (size {2})", clientId, message,
                message.Length);
        }
        else
        {
            Console.WriteLine("[SERVER] Received large random message from client (size {0}, {1})", data.Length,
                RandomMessage?.Length);
            Assert.AreEqual(data.Length, RandomMessage?.Length);
            CollectionAssert.AreEqual(data, RandomMessage);
        }
    }

    protected override void Connect(ulong clientId)
    {
        Console.WriteLine("[SERVER] Client #{0} connected", clientId);
    }

    protected override void Disconnect(ulong clientId)
    {
        Console.WriteLine("[SERVER] Client #{0} disconnected", clientId);
    }
}