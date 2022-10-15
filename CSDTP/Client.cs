using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CSDTP;

/// <summary>
///     A socket client.
/// </summary>
/// <typeparam name="S">The type of data that will be sent.</typeparam>
/// <typeparam name="R">The type of data that will be received.</typeparam>
public abstract class Client<S, R>
{
    /// <summary>
    ///     If the client is currently connected to a server.
    /// </summary>
    private bool _connected;

    /// <summary>
    ///     The thread from which the client will handle data received from the server.
    /// </summary>
    private Thread? _handleThread;

    /// <summary>
    ///     The client crypto key.
    /// </summary>
    private byte[]? _key;

    /// <summary>
    ///     The client socket.
    /// </summary>
    private Socket? _sock;

    /// <summary>
    ///     Connect to a server.
    /// </summary>
    /// <param name="host">the server host.</param>
    /// <param name="port">the server port.</param>
    /// <exception cref="CSDTPException">Thrown when the client is already connected to a server.</exception>
    public void Connect(string host, ushort port)
    {
        if (_connected) throw new CSDTPException("client is already connected to a server");

        var hostEntry = Dns.GetHostEntry(host);
        var address = hostEntry.AddressList[0];
        var ipe = new IPEndPoint(address, port);

        _sock = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _sock.Connect(ipe);

        _connected = true;

        ExchangeKeys();

        CallHandle();
    }

    /// <summary>
    ///     Connect to a server, using the default port.
    /// </summary>
    /// <param name="host">the server host.</param>
    public void Connect(string host)
    {
        Connect(host, Util.DefaultPort);
    }

    /// <summary>
    ///     Connect to a server, using the default host.
    /// </summary>
    /// <param name="port">the server port.</param>
    public void Connect(ushort port)
    {
        Connect(Util.DefaultHost, port);
    }

    /// <summary>
    ///     Connect to a server, using the default host and port.
    /// </summary>
    public void Connect()
    {
        Connect(Util.DefaultHost, Util.DefaultPort);
    }

    /// <summary>
    ///     Disconnect from the server.
    /// </summary>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public void Disconnect()
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        _connected = false;

        _sock?.Shutdown(SocketShutdown.Both);
        _sock?.Close();

        if (_handleThread != null && _handleThread != Thread.CurrentThread) _handleThread.Join();
    }

    /// <summary>
    ///     Send data to the server.
    /// </summary>
    /// <param name="data">the data to send.</param>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public void Send(S data)
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        var serializedData = Util.Serialize(data);
        var encryptedData = Crypto.AesEncrypt(_key, serializedData);
        var encodedData = Util.EncodeMessage(encryptedData);
        _sock?.Send(encodedData);
    }

    /// <summary>
    ///     Check if the client is connected to a server.
    /// </summary>
    /// <returns>Whether the client is connected to a server.</returns>
    public bool IsConnected()
    {
        return _connected;
    }

    /// <summary>
    ///     Get the host address of the client.
    /// </summary>
    /// <returns>The host address of the client.</returns>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public string GetHost()
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        var ipe = _sock?.LocalEndPoint as IPEndPoint;
        var host = ipe?.Address.ToString();

        if (host != null)
            return host;

        throw new CSDTPException("could not get client host");
    }

    /// <summary>
    ///     Get the port of the client.
    /// </summary>
    /// <returns>The port of the client.</returns>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public ushort GetPort()
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        if (_sock?.LocalEndPoint is IPEndPoint ipe)
            return Convert.ToUInt16(ipe.Port);

        throw new CSDTPException("could not get client port");
    }

    /// <summary>
    ///     Get the host address of the server.
    /// </summary>
    /// <returns>The host address of the server.</returns>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public string GetServerHost()
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        var ipe = _sock?.RemoteEndPoint as IPEndPoint;
        var host = ipe?.Address.ToString();

        if (host != null)
            return host;

        throw new CSDTPException("could not get server host");
    }

    /// <summary>
    ///     Get the port of the server.
    /// </summary>
    /// <returns>The port of the server.</returns>
    /// <exception cref="CSDTPException">Thrown when the client is not connected to a server.</exception>
    public ushort GetServerPort()
    {
        if (!_connected) throw new CSDTPException("client is not connected to a server");

        if (_sock?.RemoteEndPoint is IPEndPoint ipe)
            return Convert.ToUInt16(ipe.Port);

        throw new CSDTPException("could not get server port");
    }

    /// <summary>
    ///     Call the handle method.
    /// </summary>
    private void CallHandle()
    {
        _handleThread = new Thread(() => Handle());
        _handleThread.Start();
    }

    /// <summary>
    ///     Handle data received from the server.
    /// </summary>
    private void Handle()
    {
        var sizeBuffer = new byte[Util.LenSize];

        while (_connected)
        {
            Debug.Assert(_sock != null);

            try
            {
                var bytesReceived = _sock.Receive(sizeBuffer, Util.LenSize, SocketFlags.None);

                if (bytesReceived == 0) break;
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
            {
                break;
            }

            var messageSize = Util.DecodeMessageSize(sizeBuffer);
            var messageBuffer = new byte[messageSize];

            try
            {
                var bytesReceived = _sock.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

                if (bytesReceived == 0) break;
            }
            catch (Exception ex) when (ex is ObjectDisposedException or SocketException)
            {
                break;
            }

            CallReceive(messageBuffer);
        }

        if (_connected)
        {
            _connected = false;
            _sock?.Close();

            CallDisconnected();
        }
    }

    /// <summary>
    ///     Exchange crypto keys with the server.
    /// </summary>
    private void ExchangeKeys()
    {
        var sizeBuffer = new byte[Util.LenSize];
        var bytesReceived = _sock?.Receive(sizeBuffer, Util.LenSize, SocketFlags.None);

        if (bytesReceived != Util.LenSize) throw new CSDTPException("invalid number of bytes received");

        var messageSize = Util.DecodeMessageSize(sizeBuffer);
        var messageBuffer = new byte[messageSize];
        bytesReceived = _sock?.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

        if (bytesReceived != Convert.ToInt32(messageSize)) throw new CSDTPException("invalid number of bytes received");

        var key = Crypto.NewAesKey();
        var keyEncrypted = Crypto.RsaEncrypt(messageBuffer, key);
        var keyEncoded = Util.EncodeMessage(keyEncrypted);
        _sock?.Send(keyEncoded);

        _key = key;
    }

    /// <summary>
    ///     Call the receive event method.
    /// </summary>
    /// <param name="data">the data received from the server.</param>
    private void CallReceive(byte[] data)
    {
        var decryptedData = Crypto.AesDecrypt(_key, data);
        var deserializedData = Util.Deserialize<R>(decryptedData);

        if (deserializedData != null)
            new Thread(() => Receive(deserializedData)).Start();
    }

    /// <summary>
    ///     Call the disconnected event method.
    /// </summary>
    private void CallDisconnected()
    {
        new Thread(() => Disconnected()).Start();
    }

    /// <summary>
    ///     An event method, called when data is received from the server.
    /// </summary>
    /// <param name="data">the data received from the server.</param>
    protected abstract void Receive(R data);

    /// <summary>
    ///     An event method, called when the server has disconnected the client.
    /// </summary>
    protected abstract void Disconnected();
}