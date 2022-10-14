using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("CSDTPTest")]

namespace CSDTP;

/// <summary>
///     CSDTP utilities.
/// </summary>
internal static class Util
{
    /// <summary>
    ///     The length of the size portion of each message.
    /// </summary>
    public const int LenSize = 5;

    /// <summary>
    ///     The default host.
    /// </summary>
    public static readonly string DefaultHost = Dns.GetHostName();

    /// <summary>
    ///     The default port.
    /// </summary>
    public static readonly ushort DefaultPort = 29275;

    /// <summary>
    ///     The server listen backlog.
    /// </summary>
    public static readonly int ListenBacklog = 8;

    /// <summary>
    ///     Serialize an object to bytes.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <typeparam name="T">The type of object being serialized.</typeparam>
    /// <returns>The resulting bytes.</returns>
    public static byte[] Serialize<T>(T obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj);
    }

    /// <summary>
    ///     Deserialize an object from bytes.
    /// </summary>
    /// <param name="bytes">The bytes to deserialize.</param>
    /// <typeparam name="T">The type of the object being deserialized.</typeparam>
    /// <returns>The resulting object.</returns>
    public static T? Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes);
    }

    /// <summary>
    ///     Encode the size portion of a message to bytes.
    /// </summary>
    /// <param name="size">the message size.</param>
    /// <returns>The encoded message size.</returns>
    public static byte[] EncodeMessageSize(ulong size)
    {
        var encodedMessageSize = new byte[LenSize];

        for (var i = LenSize - 1; i >= 0; i--)
        {
            encodedMessageSize[i] = Convert.ToByte(size % 256);
            size >>= 8;
        }

        return encodedMessageSize;
    }

    /// <summary>
    ///     Decode the size portion of a message.
    /// </summary>
    /// <param name="encodedMessageSize">the encoded message size.</param>
    /// <returns>The actual message size.</returns>
    public static ulong DecodeMessageSize(byte[] encodedMessageSize)
    {
        ulong size = 0;

        for (var i = 0; i < LenSize; i++)
        {
            size <<= 8;
            size += Convert.ToUInt64(encodedMessageSize[i]);
        }

        return size;
    }

    /// <summary>
    ///     Encode a message.
    /// </summary>
    /// <param name="data">the message data.</param>
    /// <returns>The encoded message.</returns>
    public static byte[] EncodeMessage(byte[] data)
    {
        var encodedMessageSize = EncodeMessageSize(Convert.ToUInt64(data.Length));
        var encodedMessage = encodedMessageSize.Concat(data);
        return encodedMessage.ToArray();
    }

    /// <summary>
    ///     Decode a message.
    /// </summary>
    /// <param name="encodedMessage">the encoded message.</param>
    /// <returns>The decoded message data.</returns>
    public static byte[] DecodeMessage(byte[] encodedMessage)
    {
        var data = new ArraySegment<byte>(encodedMessage, LenSize, encodedMessage.Length - LenSize);
        return data.ToArray();
    }
}