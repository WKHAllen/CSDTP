using System.Net;
using System.Net.Sockets;

namespace CSDTP
{
    public abstract class Server
    {
        private bool blocking;
        private bool eventBlocking;
        private bool serving = false;
        private Socket? sock;
        private Dictionary<ulong, Socket> clients = new Dictionary<ulong, Socket>();
        private ulong nextClientID = 0;

        public Server(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        public Server() : this(false, false) { }

        public void Start(string host, ushort port)
        {
            if (serving)
            {
                // TODO: throw exception
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

        public void Start(string host)
        {
            Start(host, Util.defaultPort);
        }

        public void Start(ushort port)
        {
            Start(Util.defaultHost, port);
        }

        public void Start()
        {
            Start(Util.defaultHost, Util.defaultPort);
        }

        public void Stop()
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            foreach (KeyValuePair<ulong, Socket> client in clients)
            {
                RemoveClient(client.Key);
            }

            sock?.Close();
            serving = false;
        }

        public void Send(ulong clientID, byte[] data)
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                byte[] encodedData = Util.EncodeMessage(data);
                client.Send(encodedData);
            }
            else
            {
                // TODO: throw exception
            }
        }

        public void SendAll(byte[] data)
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            byte[] encodedData = Util.EncodeMessage(data);

            foreach (KeyValuePair<ulong, Socket> client in clients)
            {
                client.Value.Send(encodedData);
            }
        }

        public void RemoveClient(ulong clientID)
        {
            if (!serving)
            {
                // TODO: throw exception
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
                // TODO: throw exception
            }
        }

        public bool IsServing()
        {
            return serving;
        }

        public string GetHost()
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        public ushort GetPort()
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        public string GetClientHost(ulong clientID)
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                IPEndPoint ipe = client.RemoteEndPoint as IPEndPoint;
                return ipe.Address.ToString();
            }
            else
            {
                // TODO: throw exception
                return "";
            }
        }

        public ushort GetClientPort(ulong clientID)
        {
            if (!serving)
            {
                // TODO: throw exception
            }

            Socket? client = clients.GetValueOrDefault(clientID);

            if (client != null)
            {
                IPEndPoint ipe = client.RemoteEndPoint as IPEndPoint;
                return Convert.ToUInt16(ipe.Port);
            }
            else
            {
                // TODO: throw exception
                return 0;
            }
        }

        private ulong NewClientID()
        {
            return nextClientID++;
        }

        private void CallServe()
        {
            // TODO: call serve method
        }

        private void Serve()
        {
            // TODO: serve clients
        }

        private void CallReceive(ulong clientID, byte[] data)
        {
            // TODO: call receive event method
        }

        private void CallConnect(ulong clientID)
        {
            // TODO: call connect event method
        }

        private void CallDisconnect(ulong clientID)
        {
            // TODO: call disconnect event method
        }

        protected abstract void Receive(ulong clientID, byte[] data);

        protected abstract void Connect(ulong clientID);

        protected abstract void Disconnect(ulong clientID);
    }
}
