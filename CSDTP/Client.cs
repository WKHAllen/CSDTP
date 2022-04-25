namespace CSDTP
{
    public abstract class Client
    {
        private bool blocking;
        private bool eventBlocking;
        private bool connected = false;
        // TODO: add client socket

        public Client(bool blocking_, bool eventBlocking_)
        {
            blocking = blocking_;
            eventBlocking = eventBlocking_;
        }

        public Client() : this(false, false) { }

        public void Connect(string host, ushort port)
        {
            // TODO: connect to server
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
            // TODO: disconnect from server
        }

        public void Send(byte[] data)
        {
            // TODO: send data to server
        }

        public bool IsConnected()
        {
            return connected;
        }

        public string GetHost()
        {
            // TODO: return server host
            return "";
        }

        public ushort GetPort()
        {
            // TODO: return server port
            return 0;
        }

        private void Handle()
        {
            // TODO: handle messages from server
        }

        protected abstract void Receive(byte[] data);

        protected abstract void Disconnected();
    }
}
