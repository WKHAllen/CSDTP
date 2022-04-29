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

        public void Send(byte[] data)
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            byte[] encodedData = Util.EncodeMessage(data);
            sock.Send(encodedData);
        }

        public bool IsConnected()
        {
            return connected;
        }

        public string GetHost()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        public ushort GetPort()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.LocalEndPoint as IPEndPoint;
            return Convert.ToUInt16(ipe.Port);
        }

        public string GetServerHost()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
            }

            IPEndPoint ipe = sock.RemoteEndPoint as IPEndPoint;
            return ipe.Address.ToString();
        }

        public ushort GetServerPort()
        {
            if (!connected)
            {
                throw new CSDTPException("client is not connected to a server");
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
            byte[] sizeBuffer = new byte[Util.lenSize];

            while (connected)
            {
                try
                {
                    sock.Receive(sizeBuffer, Util.lenSize, SocketFlags.None);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                ulong messageSize = Util.DecodeMessageSize(sizeBuffer);
                byte[] messageBuffer = new byte[messageSize];

                try
                {
                    sock.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);
                }
                catch (ObjectDisposedException)
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
