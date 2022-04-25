namespace CSDTP
{
    public abstract class Server
    {
        private bool blocking;
        private bool eventBlocking;
        private bool serving = false;
        // TODO: add server listener socket
        // TODO: add client socket map
        private ulong nextClientID = 0;

        public Server(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        public Server() : this(false, false) { }

        public void Start(string host, ushort port)
        {
            // TODO: start the server
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
            // TODO: stop the server
        }

        public void Send(ulong clientID, byte[] data)
        {
            // TODO: send data
        }

        public void SendAll(byte[] data)
        {
            // TODO: send data to all clients
        }

        public void RemoveClient(ulong clientID)
        {
            // TODO: remove the client
        }

        public bool IsServing()
        {
            return serving;
        }

        public string GetHost()
        {
            // TODO: return server listener host
            return "";
        }

        public ushort GetPort()
        {
            // TODO: return server listener port
            return 0;
        }

        private void Serve()
        {
            // TODO: serve clients
        }

        private ulong NewClientID()
        {
            return nextClientID++;
        }

        protected abstract void Receive(ulong clientID, byte[] data);

        protected abstract void Connect(ulong clientID);

        protected abstract void Disconnect(ulong clientID);
    }
}
