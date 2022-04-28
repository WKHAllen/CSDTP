using System.Net;
using System.Net.Sockets;

namespace CSDTP
{
    public abstract class Client
    {
        private readonly bool blocking;
        private readonly bool eventBlocking;
        private bool connected = false;
        private Socket? sock;
        private Thread? handleThread;

        public Client(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        public Client() : this(false, false) { }

        public void Connect(string host, ushort port)
        {
            if (connected)
            {
                // TODO: throw exception
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            IPAddress address = hostEntry.AddressList[0];
            IPEndPoint ipe = new IPEndPoint(address, port);

            sock = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(ipe);

            connected = true;
            CallHandle();
        }

        public void Connect(string host)
        {
            Connect(host, Util.defaultPort);
        }

        public void Connect(ushort port)
        {
            Connect(Util.defaultHost, port);
        }

        public void Connect()
        {
            Connect(Util.defaultHost, Util.defaultPort);
        }

        public void Disconnect()
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            sock?.Shutdown(SocketShutdown.Both);
            sock?.Close();

            connected = false;
        }

        public void Send(byte[] data)
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            byte[] encodedData = Util.EncodeMessage(data);
            sock?.Send(encodedData);
        }

        public bool IsConnected()
        {
            return connected;
        }

        public string GetHost()
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        public ushort GetPort()
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        public string GetServerHost()
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.RemoteEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        public ushort GetServerPort()
        {
            if (!connected)
            {
                // TODO: throw exception
            }

            IPEndPoint ipe = sock.RemoteEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

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

        private void Handle()
        {
            // TODO: handle messages from server
        }

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

        protected abstract void Receive(byte[] data);

        protected abstract void Disconnected();
    }
}
