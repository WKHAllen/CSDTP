using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace CSDTPTest
{
    internal class TestServer : CSDTP.Server
    {
        public bool receivingRandomMessage = false;
        public byte[]? randomMessage;

        public TestServer() : base(false, true) { }

        protected override void Receive(ulong clientID, byte[] data)
        {
            if (!receivingRandomMessage)
            {
                string message = System.Text.Encoding.UTF8.GetString(data);
                Console.WriteLine("[SERVER] Received data from client #{0}: {1} (size {2})", clientID, message, message.Length);
            }
            else
            {
                Console.WriteLine("[SERVER] Received large random message from client (size {0}, {1})", data.Length, randomMessage?.Length);
                Assert.AreEqual(data.Length, randomMessage?.Length);
                CollectionAssert.AreEqual(data, randomMessage);
            }
        }

        protected override void Connect(ulong clientID)
        {
            Console.WriteLine("[SERVER] Client #{0} connected", clientID);
        }

        protected override void Disconnect(ulong clientID)
        {
            Console.WriteLine("[SERVER] Client #{0} disconnected", clientID);
        }
    }

    internal class TestClient : CSDTP.Client
    {
        public bool receivingRandomMessage = false;
        public byte[]? randomMessage;

        public TestClient() : base(false, true) { }

        protected override void Receive(byte[] data)
        {
            if (!receivingRandomMessage)
            {
                string message = System.Text.Encoding.UTF8.GetString(data);
                Console.WriteLine("[CLIENT] Received data from server: {0} (size {1})", message, message.Length);
            }
            else
            {
                Console.WriteLine("[CLIENT] Received large random message from server (size {0}, {1})", data.Length, randomMessage?.Length);
                Assert.AreEqual(data.Length, randomMessage?.Length);
                CollectionAssert.AreEqual(data, randomMessage);
            }
        }

        protected override void Disconnected()
        {
            Console.WriteLine("[CLIENT] Unexpectedly disconnected from server");
        }
    }

    [TestClass]
    public class Test
    {
        [TestMethod]
        public void TestCSDTP()
        {
            int waitTime = 100;

            // Generate large random messages
            Random random = new Random();
            byte[] randomMessageToServer = new byte[random.Next(16384, 32767)];
            random.NextBytes(randomMessageToServer);
            byte[] randomMessageToClient = new byte[random.Next(32768, 65535)];
            random.NextBytes(randomMessageToClient);
            Console.WriteLine("Large random message sizes: {0}, {1}", randomMessageToServer.Length, randomMessageToClient.Length);

            // Begin testing
            Console.WriteLine("Running tests...");

            // Start server
            TestServer server = new TestServer();
            server.randomMessage = randomMessageToServer;
            server.Start();

            Thread.Sleep(waitTime);

            // Get server host and port
            string serverHost = server.GetHost();
            ushort serverPort = server.GetPort();
            Console.WriteLine("Server host: {0}", serverHost);
            Console.WriteLine("Server port: {0}", serverPort);

            // Test that the client does not exist
            try
            {
                server.RemoveClient(0);
                Console.WriteLine("Did not throw on removal of unknown client");
                Assert.Fail();
            }
            catch (CSDTP.CSDTPException ex)
            {
                Console.WriteLine("Throws on removal of unknown client: '{0}'", ex.Message);
            }

            Thread.Sleep(waitTime);

            // Start client
            TestClient client = new TestClient();
            client.randomMessage = randomMessageToClient;
            client.Connect();

            Thread.Sleep(waitTime);

            // Get client host and port
            string clientHost = client.GetHost();
            ushort clientPort = client.GetPort();
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
            string clientMessage = "Hello, server";
            client.Send(System.Text.Encoding.UTF8.GetBytes(clientMessage));

            Thread.Sleep(waitTime);

            // Server send
            string serverMessage = "Hello, client #0";
            server.Send(0, System.Text.Encoding.UTF8.GetBytes(serverMessage));

            Thread.Sleep(waitTime);

            server.receivingRandomMessage = true;
            client.receivingRandomMessage = true;

            Thread.Sleep(waitTime);

            // Client send large message
            client.Send(randomMessageToServer);

            Thread.Sleep(waitTime);

            // Server send large message
            server.SendAll(randomMessageToClient);

            Thread.Sleep(waitTime);

            server.receivingRandomMessage = false;
            client.receivingRandomMessage = false;

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
}
