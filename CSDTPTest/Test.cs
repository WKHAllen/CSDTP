using System;
using System.Text;
using System.Threading;
using CSDTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CSDTPTest;

[TestClass]
public class Test
{
    [TestMethod]
    public void TestAll()
    {
        const int waitTime = 100;

        // Generate large random messages
        var random = new Random();
        var randomMessageToServer = new byte[random.Next(16384, 32767)];
        random.NextBytes(randomMessageToServer);
        var randomMessageToClient = new byte[random.Next(32768, 65535)];
        random.NextBytes(randomMessageToClient);
        Console.WriteLine("Large random message sizes: {0}, {1}", randomMessageToServer.Length,
            randomMessageToClient.Length);

        // Begin testing
        Console.WriteLine("Running tests...");

        // Start server
        var server = new TestServer();
        server.RandomMessage = randomMessageToServer;
        server.Start();

        Thread.Sleep(waitTime);

        // Get server host and port
        var serverHost = server.GetHost();
        var serverPort = server.GetPort();
        Console.WriteLine("Server host: {0}", serverHost);
        Console.WriteLine("Server port: {0}", serverPort);

        // Test that the client does not exist
        try
        {
            server.RemoveClient(0);
            Console.WriteLine("Did not throw on removal of unknown client");
            Assert.Fail();
        }
        catch (CSDTPException ex)
        {
            Console.WriteLine("Throws on removal of unknown client: '{0}'", ex.Message);
        }

        Thread.Sleep(waitTime);

        // Start client
        var client = new TestClient();
        client.RandomMessage = randomMessageToClient;
        client.Connect();

        Thread.Sleep(waitTime);

        // Get client host and port
        var clientHost = client.GetHost();
        var clientPort = client.GetPort();
        Console.WriteLine("Client host: {0}", clientHost);
        Console.WriteLine("Client port: {0}", clientPort);

        // Check server and client host and port line up
        Assert.AreEqual(server.GetHost(), client.GetServerHost());
        Assert.AreEqual(server.GetPort(), client.GetServerPort());
        Assert.AreEqual(server.GetClientHost(0), client.GetHost());
        Assert.AreEqual(server.GetClientPort(0), client.GetPort());
        Console.WriteLine("Server and client host and port line up");

        Thread.Sleep(waitTime);

        // Client send
        var clientMessage = "Hello, server";
        client.Send(Encoding.UTF8.GetBytes(clientMessage));

        Thread.Sleep(waitTime);

        // Server send
        var serverMessage = "Hello, client #0";
        server.Send(0, Encoding.UTF8.GetBytes(serverMessage));

        Thread.Sleep(waitTime);

        server.ReceivingRandomMessage = true;
        client.ReceivingRandomMessage = true;

        Thread.Sleep(waitTime);

        // Client send large message
        client.Send(randomMessageToServer);

        Thread.Sleep(waitTime);

        // Server send large message
        server.SendAll(randomMessageToClient);

        Thread.Sleep(waitTime);

        server.ReceivingRandomMessage = false;
        client.ReceivingRandomMessage = false;

        Thread.Sleep(waitTime);

        // Client disconnect
        client.Disconnect();

        Thread.Sleep(waitTime);

        // Server stop
        server.Stop();

        // Done
        Console.WriteLine("Successfully passed all tests");
    }
}