using System.Net;
using System.Net.Sockets;

namespace CSDTP
{
    /// <summary>
    /// A socket server.
    /// </summary>
    public abstract class Server
    {
        /// <summary>
        /// If the server will block while serving clients.
        /// </summary>
        private readonly bool blocking;

        /// <summary>
        /// If the server will block while calling event methods.
        /// </summary>
        private readonly bool eventBlocking;

        /// <summary>
        /// If the server is currently serving.
        /// </summary>
        private bool serving = false;

        /// <summary>
        /// The server socket.
        /// </summary>
        private Socket? sock;

        /// <summary>
        /// The thread from which the server will serve clients.
        /// </summary>
        private Thread? serveThread;

        /// <summary>
        /// A collection of the client sockets.
        /// </summary>
        private Dictionary<ulong, Socket> clients = new Dictionary<ulong, Socket>();

        /// <summary>
        /// The next available client ID.
        /// </summary>
        private ulong nextClientID = 0;

        /// <summary>
        /// Instantiate a socket server.
        /// </summary>
        /// <param name="blocking_">if the server should block while serving clients.</param>
        /// <param name="eventBlocking_">if the server should block while calling event methods.</param>
        public Server(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        /// <summary>
        /// Instantiate a socket server.
        /// </summary>
        public Server() : this(false, false) { }

        /// <summary>
        /// Start the socket server.
        /// </summary>
        /// <param name="host">the address to host the server on.</param>
        /// <param name="port">the port to host the server on.</param>
        /// <exception cref="CSDTPException">Thrown when the server is already serving.</exception>
        public void Start(string host, ushort port)
        {
            if (serving)
            {
                throw new CSDTPException("server is already serving");
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            IPAddress address = hostEntry.AddressList[0];
            IPEndPoint ipe = new IPEndPoint(address, port);

            sock = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Bind(ipe);
            sock.Listen(Util.listenBacklog);

            serving = true;
            CallServe();
        }

        /// <summary>
        /// Start the socket server, using the default port.
        /// </summary>
        /// <param name="host">the address to host the server on.</param>
        public void Start(string host)
        {
            Start(host, Util.defaultPort);
        }

        /// <summary>
        /// Start the socket server, using the default host.
        /// </summary>
        /// <param name="port">the port to host the server on.</param>
        public void Start(ushort port)
        {
            Start(Util.defaultHost, port);
        }

        /// <summary>
        /// Start the socket server, using the default host and port.
        /// </summary>
        public void Start()
        {
            Start(Util.defaultHost, Util.defaultPort);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
        public void Stop()
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            serving = false;

            foreach (KeyValuePair<ulong, Socket> client in clients)
            {
                RemoveClient(client.Key);
            }

            sock.Close();

            if (serveThread != null && serveThread != Thread.CurrentThread)
            {
                serveThread.Join();
            }
        }

        /// <summary>
        /// Send data to a client.
        /// </summary>
        /// <param name="clientID">the ID of the client to send the data to.</param>
        /// <param name="data">the data to send.</param>
        /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
        public void Send(ulong clientID, byte[] data)
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                byte[] encodedData = Util.EncodeMessage(data);
                client.Send(encodedData);
            }
            else
            {
                throw new CSDTPException("client does not exist");
            }
        }

        /// <summary>
        /// Send data to all clients.
        /// </summary>
        /// <param name="data">the data to send</param>
        /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
        public void SendAll(byte[] data)
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            byte[] encodedData = Util.EncodeMessage(data);

            foreach (KeyValuePair<ulong, Socket> client in clients)
            {
                client.Value.Send(encodedData);
            }
        }

        /// <summary>
        /// Disconnect a client from the server.
        /// </summary>
        /// <param name="clientID">the ID of the client to disconnect.</param>
        /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
        public void RemoveClient(ulong clientID)
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                clients.Remove(clientID);
            }
            else
            {
                throw new CSDTPException("client does not exist");
            }
        }

        /// <summary>
        /// Check if the server is serving.
        /// </summary>
        /// <returns>Whether the server is serving.</returns>
        public bool IsServing()
        {
            return serving;
        }

        /// <summary>
        /// Get the host address of the server.
        /// </summary>
        /// <returns>The host address of the server.</returns>
        /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
        public string GetHost()
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        /// <summary>
        /// Get the port of the server.
        /// </summary>
        /// <returns>The port of the server.</returns>
        /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
        public ushort GetPort()
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        /// <summary>
        /// Get the host of a client.
        /// </summary>
        /// <param name="clientID">the ID of the client.</param>
        /// <returns>The host of the client.</returns>
        /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
        public string GetClientHost(ulong clientID)
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                IPEndPoint ipe = client.RemoteEndPoint as IPEndPoint;
                return ipe.Address.ToString();
            }
            else
            {
                throw new CSDTPException("client does not exist");
            }
        }

        /// <summary>
        /// Get the port of a client.
        /// </summary>
        /// <param name="clientID">the ID of the client.</param>
        /// <returns>The port of the client.</returns>
        /// <exception cref="CSDTPException"></exception>
        public ushort GetClientPort(ulong clientID)
        {
            if (!serving)
            {
                throw new CSDTPException("server is not serving");
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                IPEndPoint ipe = client.RemoteEndPoint as IPEndPoint;
                return Convert.ToUInt16(ipe.Port);
            }
            else
            {
                throw new CSDTPException("client does not exist");
            }
        }

        /// <summary>
        /// Get the next available client ID.
        /// </summary>
        /// <returns>The next available client ID.</returns>
        private ulong NewClientID()
        {
            return nextClientID++;
        }

        /// <summary>
        /// Call the serve method.
        /// </summary>
        private void CallServe()
        {
            if (blocking)
            {
                Serve();
            }
            else
            {
                serveThread = new Thread(() => Serve());
                serveThread.Start();
            }
        }

        /// <summary>
        /// Serve clients.
        /// </summary>
        private void Serve()
        {
            while (serving)
            {
                List<Socket> readSocks = new List<Socket>(clients.Values.ToArray());
                readSocks.Add(sock);

                List<Socket> errorSocks = new List<Socket>(clients.Values.ToArray());
                errorSocks.Add(sock);

                try
                {
                    Socket.Select(readSocks, null, errorSocks, -1);
                }
                catch (SocketException)
                {
                    if (!serving)
                    {
                        return;
                    }
                }

                if (!serving)
                {
                    return;
                }

                foreach (Socket readSock in readSocks)
                {
                    if (readSock == sock)
                    {
                        Socket newClient = sock.Accept();
                        ulong newClientID = NewClientID();

                        clients.Add(newClientID, newClient);

                        CallConnect(newClientID);
                    }
                    else
                    {
                        ulong clientID = clients.FirstOrDefault(client => client.Value == readSock).Key;
                        byte[] sizeBuffer = new byte[Util.lenSize];

                        try
                        {
                            int bytesReceived = readSock.Receive(sizeBuffer, Util.lenSize, SocketFlags.None);

                            if (bytesReceived == 0)
                            {
                                if (clients.ContainsKey(clientID))
                                {
                                    clients[clientID].Close();
                                    clients.Remove(clientID);

                                    CallDisconnect(clientID);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                        {
                            if (clients.ContainsKey(clientID))
                            {
                                clients[clientID].Close();
                                clients.Remove(clientID);

                                CallDisconnect(clientID);
                                continue;
                            }
                        }

                        ulong messageSize = Util.DecodeMessageSize(sizeBuffer);
                        byte[] messageBuffer = new byte[messageSize];

                        try
                        {
                            int bytesReceived = readSock.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

                            if (bytesReceived == 0)
                            {
                                if (clients.ContainsKey(clientID))
                                {
                                    clients[clientID].Close();
                                    clients.Remove(clientID);

                                    CallDisconnect(clientID);
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                        {
                            if (clients.ContainsKey(clientID))
                            {
                                clients[clientID].Close();
                                clients.Remove(clientID);

                                CallDisconnect(clientID);
                                continue;
                            }
                        }

                        CallReceive(clientID, messageBuffer);
                    }
                }

                foreach (Socket errorSock in errorSocks)
                {
                    if (errorSock == sock)
                    {
                        if (serving)
                        {
                            Stop();
                        }

                        return;
                    }
                    else
                    {
                        ulong clientID = clients.FirstOrDefault(client => client.Value == errorSock).Key;

                        if (clients.ContainsKey(clientID))
                        {
                            clients[clientID].Close();
                            clients.Remove(clientID);

                            CallDisconnect(clientID);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call the receive event method.
        /// </summary>
        /// <param name="clientID">the ID of the client who sent the data.</param>
        /// <param name="data">the data received from the client.</param>
        private void CallReceive(ulong clientID, byte[] data)
        {
            if (eventBlocking)
            {
                Receive(clientID, data);
            }
            else
            {
                new Thread(() => Receive(clientID, data));
            }
        }

        /// <summary>
        /// Call the connect event method.
        /// </summary>
        /// <param name="clientID">the ID of the client who connected.</param>
        private void CallConnect(ulong clientID)
        {
            if (eventBlocking)
            {
                Connect(clientID);
            }
            else
            {
                new Thread(() => Connect(clientID));
            }
        }

        /// <summary>
        /// Call the disconnect event method.
        /// </summary>
        /// <param name="clientID">the ID of the client who disconnected.</param>
        private void CallDisconnect(ulong clientID)
        {
            if (eventBlocking)
            {
                Disconnect(clientID);
            }
            else
            {
                new Thread(() => Disconnect(clientID));
            }
        }

        /// <summary>
        /// An event method, called when data is received from a client.
        /// </summary>
        /// <param name="clientID">the ID of the client who sent the data.</param>
        /// <param name="data">the data received from the client.</param>
        protected abstract void Receive(ulong clientID, byte[] data);

        /// <summary>
        /// An event method, called when a client connects.
        /// </summary>
        /// <param name="clientID">the ID of the client who connected.</param>
        protected abstract void Connect(ulong clientID);

        /// <summary>
        /// An event method, called when a client disconnects.
        /// </summary>
        /// <param name="clientID">the ID of the client who disconnected.</param>
        protected abstract void Disconnect(ulong clientID);
    }
}
