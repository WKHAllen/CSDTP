using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CSDTP;

/// <summary>
///     A socket server.
/// </summary>
/// <typeparam name="S">The type of data that will be sent.</typeparam>
/// <typeparam name="R">The type of data that will be received.</typeparam>
public abstract class Server<S, R>
{
    /// <summary>
    ///     A collection of the client sockets.
    /// </summary>
    private readonly Dictionary<ulong, Socket> _clients = new();

    /// <summary>
    ///     A collection of the client crypto keys.
    /// </summary>
    private readonly Dictionary<ulong, byte[]> _keys = new();

    /// <summary>
    ///     The next available client ID.
    /// </summary>
    private ulong _nextClientId;

    /// <summary>
    ///     The thread from which the server will serve clients.
    /// </summary>
    private Thread? _serveThread;

    /// <summary>
    ///     If the server is currently serving.
    /// </summary>
    private bool _serving;

    /// <summary>
    ///     The server socket.
    /// </summary>
    private Socket? _sock;

    /// <summary>
    ///     Start the socket server.
    /// </summary>
    /// <param name="host">the address to host the server on.</param>
    /// <param name="port">the port to host the server on.</param>
    /// <exception cref="CSDTPException">Thrown when the server is already serving.</exception>
    public void Start(string host, ushort port)
    {
        if (_serving) throw new CSDTPException("server is already serving");

        var hostEntry = Dns.GetHostEntry(host);
        var address = hostEntry.AddressList[0];
        var ipe = new IPEndPoint(address, port);

        _sock = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _sock.Bind(ipe);
        _sock.Listen(Util.ListenBacklog);

        _serving = true;
        CallServe();
    }

    /// <summary>
    ///     Start the socket server, using the default port.
    /// </summary>
    /// <param name="host">the address to host the server on.</param>
    public void Start(string host)
    {
        Start(host, Util.DefaultPort);
    }

    /// <summary>
    ///     Start the socket server, using the default host.
    /// </summary>
    /// <param name="port">the port to host the server on.</param>
    public void Start(ushort port)
    {
        Start(Util.DefaultHost, port);
    }

    /// <summary>
    ///     Start the socket server, using the default host and port.
    /// </summary>
    public void Start()
    {
        Start(Util.DefaultHost, Util.DefaultPort);
    }

    /// <summary>
    ///     Stop the server.
    /// </summary>
    /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
    public void Stop()
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        _serving = false;

        foreach (var client in _clients)
        {
            _clients.Remove(client.Key);
            client.Value.Shutdown(SocketShutdown.Both);
            client.Value.Close();
            _keys.Remove(client.Key);
        }

        _sock?.Close();

        if (_serveThread != null && _serveThread != Thread.CurrentThread) _serveThread.Join();
    }

    /// <summary>
    ///     Send data to a client.
    /// </summary>
    /// <param name="clientId">the ID of the client to send the data to.</param>
    /// <param name="data">the data to send.</param>
    /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
    public void Send(ulong clientId, S data)
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        var client = _clients.GetValueOrDefault(clientId);
        var key = _keys[clientId];

        if (client != null)
        {
            var serializedData = Util.Serialize(data);
            var encryptedData = Crypto.AesEncrypt(key, serializedData);
            var encodedData = Util.EncodeMessage(encryptedData);
            client.Send(encodedData);
        }
        else
        {
            throw new CSDTPException("client does not exist");
        }
    }

    /// <summary>
    ///     Send data to all clients.
    /// </summary>
    /// <param name="data">the data to send</param>
    /// <exception cref="CSDTPException">Thrown when the server is not serving.</exception>
    public void SendAll(S data)
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        foreach (var client in _clients) Send(client.Key, data);
    }

    /// <summary>
    ///     Disconnect a client from the server.
    /// </summary>
    /// <param name="clientId">the ID of the client to disconnect.</param>
    /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
    public void RemoveClient(ulong clientId)
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        var client = _clients.GetValueOrDefault(clientId);

        if (client != null)
        {
            _clients.Remove(clientId);
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            _keys.Remove(clientId);
        }
        else
        {
            throw new CSDTPException("client does not exist");
        }
    }

    /// <summary>
    ///     Check if the server is serving.
    /// </summary>
    /// <returns>Whether the server is serving.</returns>
    public bool IsServing()
    {
        return _serving;
    }

    /// <summary>
    ///     Get the host address of the server.
    /// </summary>
    /// <returns>The host address of the server.</returns>
    /// <exception cref="CSDTPException">
    ///     Thrown when the server is not serving, or when the server's host could not be
    ///     acquired.
    /// </exception>
    public string GetHost()
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        var ipe = _sock?.LocalEndPoint as IPEndPoint;
        var host = ipe?.Address.ToString();

        if (host != null)
            return host;

        throw new CSDTPException("could not get server host");
    }

    /// <summary>
    ///     Get the port of the server.
    /// </summary>
    /// <returns>The port of the server.</returns>
    /// <exception cref="CSDTPException">
    ///     Thrown when the server is not serving, or when the server's port could not be
    ///     acquired.
    /// </exception>
    public ushort GetPort()
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        if (_sock?.LocalEndPoint is IPEndPoint ipe)
            return Convert.ToUInt16(ipe.Port);

        throw new CSDTPException("could not get server port");
    }

    /// <summary>
    ///     Get the host address of a client.
    /// </summary>
    /// <param name="clientId">the ID of the client.</param>
    /// <returns>The host address of the client.</returns>
    /// <exception cref="CSDTPException">
    ///     Thrown when the server is not serving, if the specified client does not exist, or if
    ///     the client's host could not be acquired.
    /// </exception>
    public string GetClientHost(ulong clientId)
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        var client = _clients.GetValueOrDefault(clientId);

        if (client == null) throw new CSDTPException("client does not exist");

        var ipe = client.RemoteEndPoint as IPEndPoint;
        var host = ipe?.Address.ToString();

        if (host != null)
            return host;

        throw new CSDTPException("could not get client host");
    }

    /// <summary>
    ///     Get the port of a client.
    /// </summary>
    /// <param name="clientId">the ID of the client.</param>
    /// <returns>The port of the client.</returns>
    /// <exception cref="CSDTPException">Thrown when the server is not serving, or the specified client does not exist.</exception>
    public ushort GetClientPort(ulong clientId)
    {
        if (!_serving) throw new CSDTPException("server is not serving");

        var client = _clients.GetValueOrDefault(clientId);

        if (client == null) throw new CSDTPException("client does not exist");

        if (client.RemoteEndPoint is IPEndPoint ipe)
            return Convert.ToUInt16(ipe.Port);

        throw new CSDTPException("could not get client port");
    }

    /// <summary>
    ///     Get the next available client ID.
    /// </summary>
    /// <returns>The next available client ID.</returns>
    private ulong NewClientId()
    {
        return _nextClientId++;
    }

    /// <summary>
    ///     Call the serve method.
    /// </summary>
    private void CallServe()
    {
        _serveThread = new Thread(() => Serve());
        _serveThread.Start();
    }

    /// <summary>
    ///     Serve clients.
    /// </summary>
    private void Serve()
    {
        while (_serving)
        {
            Debug.Assert(_sock != null);

            var readSocks = new List<Socket>(_clients.Values.ToArray());
            readSocks.Add(_sock);

            var errorSocks = new List<Socket>(_clients.Values.ToArray());
            errorSocks.Add(_sock);

            try
            {
                Socket.Select(readSocks, null, errorSocks, -1);
            }
            catch (SocketException)
            {
                if (!_serving) return;
            }

            if (!_serving) return;

            foreach (var readSock in readSocks)
                if (readSock == _sock)
                {
                    var newClient = _sock.Accept();
                    var newClientId = NewClientId();

                    ExchangeKeys(newClientId, newClient);

                    _clients.Add(newClientId, newClient);

                    CallConnect(newClientId);
                }
                else
                {
                    var clientId = _clients.FirstOrDefault(client => client.Value == readSock).Key;
                    var sizeBuffer = new byte[Util.LenSize];

                    try
                    {
                        var bytesReceived = readSock.Receive(sizeBuffer, Util.LenSize, SocketFlags.None);

                        if (bytesReceived == 0)
                        {
                            var client = _clients.GetValueOrDefault(clientId);

                            if (client != null)
                            {
                                _clients.Remove(clientId);
                                client.Close();
                                _keys.Remove(clientId);

                                CallDisconnect(clientId);
                            }

                            continue;
                        }
                    }
                    catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                    {
                        var client = _clients.GetValueOrDefault(clientId);

                        if (client != null)
                        {
                            _clients.Remove(clientId);
                            client.Close();
                            _keys.Remove(clientId);

                            CallDisconnect(clientId);
                        }

                        continue;
                    }

                    var messageSize = Util.DecodeMessageSize(sizeBuffer);
                    var messageBuffer = new byte[messageSize];

                    try
                    {
                        var bytesReceived =
                            readSock.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

                        if (bytesReceived == 0)
                        {
                            var client = _clients.GetValueOrDefault(clientId);

                            if (client != null)
                            {
                                _clients.Remove(clientId);
                                client.Close();
                                _keys.Remove(clientId);

                                CallDisconnect(clientId);
                            }

                            continue;
                        }
                    }
                    catch (Exception ex) when (ex is ObjectDisposedException || ex is SocketException)
                    {
                        var client = _clients.GetValueOrDefault(clientId);

                        if (client != null)
                        {
                            _clients.Remove(clientId);
                            client.Close();
                            _keys.Remove(clientId);

                            CallDisconnect(clientId);
                        }

                        continue;
                    }

                    CallReceive(clientId, messageBuffer);
                }

            foreach (var errorSock in errorSocks)
                if (errorSock == _sock)
                {
                    if (_serving) Stop();

                    return;
                }
                else
                {
                    var clientId = _clients.FirstOrDefault(client => client.Value == errorSock).Key;

                    if (_clients.ContainsKey(clientId))
                    {
                        _clients[clientId].Close();
                        _clients.Remove(clientId);
                        _keys.Remove(clientId);

                        CallDisconnect(clientId);
                    }
                }
        }
    }

    /// <summary>
    ///     Exchange crypto keys with a client.
    /// </summary>
    /// <param name="clientId">The ID of the new client.</param>
    /// <param name="client">The client socket.</param>
    private void ExchangeKeys(ulong clientId, Socket client)
    {
        var (publicKey, privateKey) = Crypto.NewRsaKeys();
        var publicKeyEncoded = Util.EncodeMessage(publicKey);
        client.Send(publicKeyEncoded);

        var sizeBuffer = new byte[Util.LenSize];
        var bytesReceived = client.Receive(sizeBuffer, Util.LenSize, SocketFlags.None);

        if (bytesReceived != Util.LenSize) throw new CSDTPException("invalid number of bytes received");

        var messageSize = Util.DecodeMessageSize(sizeBuffer);
        var messageBuffer = new byte[messageSize];
        bytesReceived = client.Receive(messageBuffer, Convert.ToInt32(messageSize), SocketFlags.None);

        if (bytesReceived != Convert.ToInt32(messageSize)) throw new CSDTPException("invalid number of bytes received");

        var key = Crypto.RsaDecrypt(privateKey, messageBuffer);
        _keys.Add(clientId, key);
    }

    /// <summary>
    ///     Call the receive event method.
    /// </summary>
    /// <param name="clientId">the ID of the client who sent the data.</param>
    /// <param name="data">the data received from the client.</param>
    private void CallReceive(ulong clientId, byte[] data)
    {
        var key = _keys[clientId];
        var decryptedData = Crypto.AesDecrypt(key, data);
        var deserializedData = Util.Deserialize<R>(decryptedData);

        if (deserializedData != null)
            new Thread(() => Receive(clientId, deserializedData)).Start();
    }

    /// <summary>
    ///     Call the connect event method.
    /// </summary>
    /// <param name="clientId">the ID of the client who connected.</param>
    private void CallConnect(ulong clientId)
    {
        new Thread(() => Connect(clientId)).Start();
    }

    /// <summary>
    ///     Call the disconnect event method.
    /// </summary>
    /// <param name="clientId">the ID of the client who disconnected.</param>
    private void CallDisconnect(ulong clientId)
    {
        new Thread(() => Disconnect(clientId)).Start();
    }

    /// <summary>
    ///     An event method, called when data is received from a client.
    /// </summary>
    /// <param name="clientId">the ID of the client who sent the data.</param>
    /// <param name="data">the data received from the client.</param>
    protected abstract void Receive(ulong clientId, R data);

    /// <summary>
    ///     An event method, called when a client connects.
    /// </summary>
    /// <param name="clientId">the ID of the client who connected.</param>
    protected abstract void Connect(ulong clientId);

    /// <summary>
    ///     An event method, called when a client disconnects.
    /// </summary>
    /// <param name="clientId">the ID of the client who disconnected.</param>
    protected abstract void Disconnect(ulong clientId);
}