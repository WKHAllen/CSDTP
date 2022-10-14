using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CSDTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSDTPTest;

[TestClass]
public class Test
{
    private const int WaitTime = 100;
    private readonly Random _random = new();

    [TestMethod]
    public void TestUtil()
    {
        // Generate various random values
        var randomBool = _random.Next() % 2 == 0;
        var randomInt = _random.Next();
        var randomLong = _random.NextInt64();
        var randomFloat = _random.NextSingle();
        var randomDouble = _random.NextDouble();
        var randomBytes = new byte[8];
        _random.NextBytes(randomBytes);
        var randomString = randomBytes.ToString();

        // Test serialization and deserialization
        Assert.AreEqual(Util.Deserialize<bool>(Util.Serialize(randomBool)), randomBool);
        Assert.AreEqual(Util.Deserialize<int>(Util.Serialize(randomInt)), randomInt);
        Assert.AreEqual(Util.Deserialize<long>(Util.Serialize(randomLong)), randomLong);
        Assert.AreEqual(Util.Deserialize<float>(Util.Serialize(randomFloat)), randomFloat);
        Assert.AreEqual(Util.Deserialize<double>(Util.Serialize(randomDouble)), randomDouble);
        CollectionAssert.AreEqual(Util.Deserialize<byte[]>(Util.Serialize(randomBytes)), randomBytes);
        Assert.AreEqual(Util.Deserialize<string>(Util.Serialize(randomString)), randomString);

        // Test message size encoding
        CollectionAssert.AreEqual(Util.EncodeMessageSize(0), new byte[] { 0, 0, 0, 0, 0 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(1), new byte[] { 0, 0, 0, 0, 1 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(255), new byte[] { 0, 0, 0, 0, 255 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(256), new byte[] { 0, 0, 0, 1, 0 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(257), new byte[] { 0, 0, 0, 1, 1 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(4311810305), new byte[] { 1, 1, 1, 1, 1 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(4328719365), new byte[] { 1, 2, 3, 4, 5 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(47362409218), new byte[] { 11, 7, 5, 3, 2 });
        CollectionAssert.AreEqual(Util.EncodeMessageSize(1099511627775), new byte[] { 255, 255, 255, 255, 255 });

        // Test message size decoding
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 0, 0, 0, 0, 0 }), 0ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 0, 0, 0, 0, 1 }), 1ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 0, 0, 0, 0, 255 }), 255ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 0, 0, 0, 1, 0 }), 256ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 0, 0, 0, 1, 1 }), 257ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 1, 1, 1, 1, 1 }), 4311810305ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 1, 2, 3, 4, 5 }), 4328719365ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 11, 7, 5, 3, 2 }), 47362409218ul);
        Assert.AreEqual(Util.DecodeMessageSize(new byte[] { 255, 255, 255, 255, 255 }), 1099511627775ul);
    }

    [TestMethod]
    public void TestCrypto()
    {
        // TODO: test crypto
    }

    [TestMethod]
    public void TestServerServe()
    {
        // Create server
        var s = new TestServer<int, string>(0, 0, 0);
        Assert.IsFalse(s.IsServing());

        // Start server
        s.Start();
        Assert.IsTrue(s.IsServing());
        Thread.Sleep(WaitTime);

        // Check server address info
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);

        // Stop server
        s.Stop();
        Assert.IsFalse(s.IsServing());
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
    }

    [TestMethod]
    public void TestAddresses()
    {
        // Create server
        var s = new TestServer<int, string>(0, 1, 1);
        Assert.IsFalse(s.IsServing());
        s.Start();
        Assert.IsTrue(s.IsServing());
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);

        // Create client
        var c = new TestClient<string, int>(0, 0);
        Assert.IsFalse(c.IsConnected());
        c.Connect(serverHost, serverPort);
        Assert.IsTrue(c.IsConnected());
        Thread.Sleep(WaitTime);

        // Check addresses match
        Assert.AreEqual(s.GetHost(), c.GetServerHost());
        Assert.AreEqual(s.GetPort(), c.GetServerPort());
        Assert.AreEqual(c.GetHost(), s.GetClientHost(0));
        Assert.AreEqual(c.GetPort(), s.GetClientPort(0));

        // Disconnect client
        c.Disconnect();
        Assert.IsFalse(c.IsConnected());
        Thread.Sleep(WaitTime);

        // Stop server
        s.Stop();
        Assert.IsFalse(s.IsServing());
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received, new ArrayList());
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong>());
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong> { 0 });
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        CollectionAssert.AreEqual(c.Received, new ArrayList());
    }

    [TestMethod]
    public void TestSendReceive()
    {
        // Create server
        var s = new TestServer<string, string>(1, 1, 1);
        s.Start();
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client
        var c = new TestClient<string, string>(1, 0);
        c.Connect(serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Send messages
        const string serverMessage = "Hello, server!";
        const string clientMessage = "Hello, client #0!";
        c.Send(serverMessage);
        s.Send(0, clientMessage);
        Thread.Sleep(WaitTime);

        // Disconnect client
        c.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s.Stop();
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received, new List<string> { serverMessage });
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong> { 0 });
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        CollectionAssert.AreEqual(c.Received, new List<string> { clientMessage });
    }

    [TestMethod]
    public void TestSendLargeMessages()
    {
        // Create server
        var s = new TestServer<byte[], byte[]>(1, 1, 1);
        s.Start();
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client
        var c = new TestClient<byte[], byte[]>(1, 0);
        c.Connect(serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Send messages
        var largeServerMessage = new byte[_random.Next(32768, 65536)];
        _random.NextBytes(largeServerMessage);
        var largeClientMessage = new byte[_random.Next(16384, 32768)];
        _random.NextBytes(largeClientMessage);
        c.Send(largeServerMessage);
        s.Send(0, largeClientMessage);
        Thread.Sleep(WaitTime);

        // Disconnect client
        c.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s.Stop();
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        Assert.AreEqual(s.Received.Count, 1);
        CollectionAssert.AreEqual(s.Received[0], largeServerMessage);
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong> { 0 });
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        Assert.AreEqual(c.Received.Count, 1);
        CollectionAssert.AreEqual(c.Received[0], largeClientMessage);
    }

    [TestMethod]
    public void TestSendingNumerousMessages()
    {
        // Messages
        var serverMessages = new int[_random.Next(64, 128)];
        var clientMessages = new int[_random.Next(128, 256)];
        for (var i = 0; i < serverMessages.Length; i++) serverMessages[i] = _random.Next();
        for (var i = 0; i < clientMessages.Length; i++) clientMessages[i] = _random.Next();

        // Create server
        var s = new TestServer<int, int>(serverMessages.Length, 1, 1);
        s.Start();
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client
        var c = new TestClient<int, int>(clientMessages.Length, 0);
        c.Connect(serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Send messages
        foreach (var serverMessage in serverMessages)
        {
            c.Send(serverMessage);
            Thread.Sleep(10);
        }

        foreach (var clientMessage in clientMessages)
        {
            s.SendAll(clientMessage);
            Thread.Sleep(10);
        }

        Thread.Sleep(WaitTime);

        // Disconnect client
        c.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s.Stop();
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received.ToArray(), serverMessages);
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong> { 0 });
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        CollectionAssert.AreEqual(c.Received.ToArray(), clientMessages);
    }

    [TestMethod]
    public void TestMultipleClients()
    {
        // Messages
        var messageFromClient1 = "Hello from client #1!";
        var messageFromClient2 = "Goodbye from client #2!";
        var messageFromServer = 29275;

        // Create server
        var s = new TestServer<int, string>(2, 2, 2)
        {
            ReplyWithStringLength = true
        };
        s.Start();
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client 1
        var c1 = new TestClient<string, int>(2, 0);
        c1.Connect(serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Check client 1 address info
        Assert.AreEqual(c1.GetHost(), s.GetClientHost(0));
        Assert.AreEqual(c1.GetPort(), s.GetClientPort(0));
        Assert.AreEqual(c1.GetServerHost(), s.GetHost());
        Assert.AreEqual(c1.GetServerPort(), s.GetPort());

        // Create client 2
        var c2 = new TestClient<string, int>(2, 0);
        c2.Connect(serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Check client 2 address info
        Assert.AreEqual(c2.GetHost(), s.GetClientHost(1));
        Assert.AreEqual(c2.GetPort(), s.GetClientPort(1));
        Assert.AreEqual(c2.GetServerHost(), s.GetHost());
        Assert.AreEqual(c2.GetServerPort(), s.GetPort());

        // Send message from client 1
        c1.Send(messageFromClient1);
        Thread.Sleep(WaitTime);

        // Send message from client 2
        c2.Send(messageFromClient2);
        Thread.Sleep(WaitTime);

        // Send message to all clients
        s.SendAll(messageFromServer);
        Thread.Sleep(WaitTime);

        // Disconnect client 1
        c1.Disconnect();
        Thread.Sleep(WaitTime);

        // Disconnect client 2
        c2.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s.Stop();
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received, new List<string> { messageFromClient1, messageFromClient2 });
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong> { 0, 1 });
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0, 1 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong> { 0, 1 });
        Assert.AreEqual(c1.ReceiveCount, 0);
        Assert.AreEqual(c1.DisconnectedCount, 0);
        Assert.IsTrue(c1.EventsDone());
        CollectionAssert.AreEqual(c1.Received, new List<int> { messageFromClient1.Length, messageFromServer });
        Assert.AreEqual(c2.ReceiveCount, 0);
        Assert.AreEqual(c2.DisconnectedCount, 0);
        Assert.IsTrue(c2.EventsDone());
        CollectionAssert.AreEqual(c2.Received, new List<int> { messageFromClient2.Length, messageFromServer });
    }

    [TestMethod]
    public void TestClientDisconnected()
    {
        // Create server
        var s = new TestServer<int, string>(0, 1, 0);
        Assert.IsFalse(s.IsServing());
        s.Start();
        Assert.IsTrue(s.IsServing());
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client
        var c = new TestClient<string, int>(0, 1);
        Assert.IsFalse(c.IsConnected());
        c.Connect(serverHost, serverPort);
        Assert.IsTrue(c.IsConnected());
        Thread.Sleep(WaitTime);

        // Stop server
        Assert.IsTrue(s.IsServing());
        Assert.IsTrue(c.IsConnected());
        s.Stop();
        Assert.IsFalse(s.IsServing());
        Thread.Sleep(WaitTime);
        Assert.IsFalse(c.IsConnected());

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received, new List<string>());
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong>());
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong>());
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        CollectionAssert.AreEqual(c.Received, new List<string>());
    }

    [TestMethod]
    public void TestRemoveClient()
    {
        // Create server
        var s = new TestServer<int, string>(0, 1, 0);
        Assert.IsFalse(s.IsServing());
        s.Start();
        Assert.IsTrue(s.IsServing());
        var serverHost = s.GetHost();
        var serverPort = s.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost, serverPort);
        Thread.Sleep(WaitTime);

        // Create client
        var c = new TestClient<string, int>(0, 1);
        Assert.IsFalse(c.IsConnected());
        c.Connect(serverHost, serverPort);
        Assert.IsTrue(c.IsConnected());
        Thread.Sleep(WaitTime);

        // Disconnect the client
        Assert.IsTrue(c.IsConnected());
        s.RemoveClient(0);
        Assert.IsFalse(c.IsConnected());
        Thread.Sleep(WaitTime);

        // Stop server
        Assert.IsTrue(s.IsServing());
        s.Stop();
        Assert.IsFalse(s.IsServing());
        Thread.Sleep(WaitTime);

        // Check event counts
        Assert.AreEqual(s.ReceiveCount, 0);
        Assert.AreEqual(s.ConnectCount, 0);
        Assert.AreEqual(s.DisconnectCount, 0);
        Assert.IsTrue(s.EventsDone());
        CollectionAssert.AreEqual(s.Received, new List<string>());
        CollectionAssert.AreEqual(s.ReceivedClientIDs, new List<ulong>());
        CollectionAssert.AreEqual(s.ConnectClientIDs, new List<ulong> { 0 });
        CollectionAssert.AreEqual(s.DisconnectClientIDs, new List<ulong>());
        Assert.AreEqual(c.ReceiveCount, 0);
        Assert.AreEqual(c.DisconnectedCount, 0);
        Assert.IsTrue(c.EventsDone());
        CollectionAssert.AreEqual(c.Received, new List<string>());
    }

    [TestMethod]
    public void TestServerClientAddressDefaults()
    {
        // Create server
        var s1 = new TestServer<int, string>(0, 1, 1);
        s1.Start();
        var serverHost1 = s1.GetHost();
        var serverPort1 = s1.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost1, serverPort1);

        // Create client
        var c1 = new TestClient<string, int>(0, 0);
        c1.Connect();
        Thread.Sleep(WaitTime);

        // Disconnect client
        c1.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s1.Stop();
        Thread.Sleep(WaitTime);

        // Create server with host
        var s2 = new TestServer<int, string>(0, 1, 1);
        s2.Start("127.0.0.1");
        var serverHost2 = s2.GetHost();
        var serverPort2 = s2.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost2, serverPort2);

        // Create client with host
        var c2 = new TestClient<string, int>(0, 0);
        c2.Connect(serverHost2);
        Thread.Sleep(WaitTime);

        // Disconnect client
        c2.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s2.Stop();
        Thread.Sleep(WaitTime);

        // Create server with port
        var s3 = new TestServer<int, string>(0, 1, 1);
        s3.Start(35792);
        var serverHost3 = s3.GetHost();
        var serverPort3 = s3.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost3, serverPort3);

        // Create client with port
        var c3 = new TestClient<string, int>(0, 0);
        c3.Connect(serverPort3);
        Thread.Sleep(WaitTime);

        // Disconnect client
        c3.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s3.Stop();
        Thread.Sleep(WaitTime);

        // Create server with host and port
        var s4 = new TestServer<int, string>(0, 1, 1);
        s4.Start("127.0.0.1", 35792);
        var serverHost4 = s4.GetHost();
        var serverPort4 = s4.GetPort();
        Console.WriteLine("Server address: {0}:{1}", serverHost4, serverPort4);

        // Create client
        var c4 = new TestClient<string, int>(0, 0);
        c4.Connect(serverHost4, serverPort4);
        Thread.Sleep(WaitTime);

        // Disconnect client
        c4.Disconnect();
        Thread.Sleep(WaitTime);

        // Stop server
        s4.Stop();
        Thread.Sleep(WaitTime);
    }
}