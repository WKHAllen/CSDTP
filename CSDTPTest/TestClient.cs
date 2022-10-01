using System;
using System.Text;
using CSDTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSDTPTest;

internal class TestClient : Client
{
    public byte[]? RandomMessage;
    public bool ReceivingRandomMessage;

    protected override void Receive(byte[] data)
    {
        if (!ReceivingRandomMessage)
        {
            var message = Encoding.UTF8.GetString(data);
            Console.WriteLine("[CLIENT] Received data from server: {0} (size {1})", message, message.Length);
        }
        else
        {
            Console.WriteLine("[CLIENT] Received large random message from server (size {0}, {1})", data.Length,
                RandomMessage?.Length);
            Assert.AreEqual(data.Length, RandomMessage?.Length);
            CollectionAssert.AreEqual(data, RandomMessage);
        }
    }

    protected override void Disconnected()
    {
        Console.WriteLine("[CLIENT] Unexpectedly disconnected from server");
    }
}