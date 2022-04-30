using System.Net;
using System.Net.Sockets;

namespace CSDTP
{
    /// <summary>
    /// A socket client.
    /// </summary>
    public abstract class Client
    {
        /// <summary>
        /// If the client will block while connected to a server.
        /// </summary>
        private readonly bool blocking;

        /// <summary>
        /// If the client will block while calling event methods.
        /// </summary>
        private readonly bool eventBlocking;

        /// <summary>
        /// If the client is currently connected to a server.
        /// </summary>
        private bool connected = false;

        /// <summary>
        /// The client socket.
        /// </summary>
        private Socket? sock;

        /// <summary>
        /// The thread from which the client will handle data received from the server.
        /// </summary>
        private Thread? handleThread;

        /// <summary>
        /// Instantiate a socket client.
        /// </summary>
        /// <param name="blocking_">if the client should block while connected to a server.</param>
        /// <param name="eventBlocking_">if the client should block while calling event methods.</param>
        public Client(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        /// <summary>
        /// Instantiate a socket client.
        /// </summary>
        public Client() : this(false, false) { }

        /// <summary>
        /// Connect to a server.
        /// </summary>
        /// <param name="host">the server host.</param>
        /// <param name="port">the server port.</param>
        /// <exception cref="CSDTPException">Thrown when the client is already connected to a server.</exception>
        public void Connect(string host, ushort port)
        {
            if (connected)
            {
                throw new CSDTPException("client is already connected to a server");
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            IPAddress address = hostEntry.AddressList[0];
            IPEndPoint ipe = new IPEndPoint(address, port);

            sock = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(ipe);

            connected = true;
            CallHandle();
        }

        /// <summary>
        /// Connect to a server, using the default port.
        /// </summary>
        /// <param name="host">the server host.</param>
        public void Connect(string host)
        {
            Connect(host, Util.defaultPort);
        }

        /// <summary>
        /// Connect to a server, using the default host.
        /// </summary>
        /// <param name="port">the server port.</param>
        public void Connect(ushort port)
        {
            Connect(Util.defaultHost, port);
        }

        /// <summary>
        /// Connect to a server, using the default host and port.
        /// </summary>
        public void Connect()
        {
            Connect(Util.defaultHost, Util.defaultPort);
        }

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public void Disconnect()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            connected = false;

            sock.Shutdown(SocketShutdown.Both);
            sock.Close();

            if (handleThread != null && handleThread != Thread.CurrentThread)
            {
                handleThread.Join();
            }
        }

        /// <summary>
        /// Send data to the server.
        /// </summary>
        /// <param name="data">the data to send.</param>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public void Send(byte[] data)
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            byte[] encodedData = Util.EncodeMessage(data);
            sock.Send(encodedData);
        }

        /// <summary>
        /// Check if the client is connected to a server.
        /// </summary>
        /// <returns>Whether the client is connected to a server.</returns>
        public bool IsConnected()
        {
            return connected;
        }

        /// <summary>
        /// Get the host address of the client.
        /// </summary>
        /// <returns>The host address of the client.</returns>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public string GetHost()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        /// <summary>
        /// Get the port of the client.
        /// </summary>
        /// <returns>The port of the client.</returns>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public ushort GetPort()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        /// <summary>
        /// Get the host address of the server.
        /// </summary>
        /// <returns>The host address of the server.</returns>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public string GetServerHost()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.RemoteEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        /// <summary>
        /// Get the port of the server.
        /// </summary>
        /// <returns>The port of the server.</returns>
        /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
        public ushort GetServerPort()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.RemoteEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        /// <summary>
        /// Call the handle method.
        /// </summary>
        private void CallHandle()
        {
            if (blocking)
            {
                Handle();
            }
            else
            {
                handleThread = new Thread(() => Handle());
                handleThread.Start();
            }
        }

        /// <summary>
        /// Handle data received from the server.
        /// </summary>
        private void Handle()
        {
            byte[] sizeBuffer = new byte[Util.lenSize];

            while (connected)
            {
                try
                {
                    int bytesReceived = sock.Receive(sizeBuffer, Util.lenSize, SocketFlags.None);

                    if (bytesReceived == 0)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                {
                    break;
                }

                ulong messageSize = Util.DecodeMessageSize(sizeBuffer);
                byte[] messageBuffer = new byte[messageSize];

                try
                {
                    int bytesReceived = sock.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

                    if (bytesReceived == 0)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                {
                    break;
                }

                CallReceive(messageBuffer);
            }

            if (connected)
            {
                connected = false;
                sock.Close();

                CallDisconnected();
            }
        }

        /// <summary>
        /// Call the receive event method.
        /// </summary>
        /// <param name="data">the data received from the server.</param>
        private void CallReceive(byte[] data)
        {
            if (eventBlocking)
            {
                Receive(data);
            }
            else
            {
                new Thread(() => Receive(data)).Start();
            }
        }

        /// <summary>
        /// Call the disconnected event method.
        /// </summary>
        private void CallDisconnected()
        {
            if (eventBlocking)
            {
                Disconnected();
            }
            else
            {
                new Thread(() => Disconnected()).Start();
            }
        }

        /// <summary>
        /// An event method, called when data is received from the server.
        /// </summary>
        /// <param name="data">the data received from the server.</param>
        protected abstract void Receive(byte[] data);

        /// <summary>
        /// An event method, called when the server has disconnected the client.
        /// </summary>
        protected abstract void Disconnected();
    }
}
