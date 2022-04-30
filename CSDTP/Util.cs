using System.Net;

namespace CSDTP
{
    /// <summary>
    /// CSDTP utilities.
    /// </summary>
    internal class Util
    {
        /// <summary>
        /// The length of the size portion of each message.
        /// </summary>
        public static readonly int lenSize = 5;

        /// <summary>
        /// The default host.
        /// </summary>
        public static readonly string defaultHost = Dns.GetHostName();

        /// <summary>
        /// The default port.
        /// </summary>
        public static readonly ushort defaultPort = 29275;

        /// <summary>
        /// The server listen backlog.
        /// </summary>
        public static readonly int listenBacklog = 8;

        /// <summary>
        /// Encode the size portion of a message to bytes.
        /// </summary>
        /// <param name="size">the message size.</param>
        /// <returns>The encoded message size.</returns>
        public static byte[] EncodeMessageSize(ulong size)
        {
            byte[] encodedMessageSize = new byte[lenSize];

            for (int i = lenSize - 1; i >= 0; i--)
            {
                encodedMessageSize[i] = Convert.ToByte(size % 256);
                size >>= 8;
            }

            return encodedMessageSize;
        }

        /// <summary>
        /// Decode the size portion of a message.
        /// </summary>
        /// <param name="encodedMessageSize">the encoded message size.</param>
        /// <returns>The actual message size.</returns>
        public static ulong DecodeMessageSize(byte[] encodedMessageSize)
        {
            ulong size = 0;

            for (int i = 0; i < lenSize; i++)
            {
                size <<= 8;
                size += Convert.ToUInt64(encodedMessageSize[i]);
            }

            return size;
        }

        /// <summary>
        /// Encode a message.
        /// </summary>
        /// <param name="data">the message data.</param>
        /// <returns>The encoded message.</returns>
        public static byte[] EncodeMessage(byte[] data)
        {
            byte[] encodedMessageSize = EncodeMessageSize(Convert.ToUInt64(data.Length));
            IEnumerable<byte> encodedMessage = encodedMessageSize.Concat(data);
            return encodedMessage.ToArray();
        }

        /// <summary>
        /// Decode a message.
        /// </summary>
        /// <param name="encodedMessage">the encoded message.</param>
        /// <returns>The decoded message data.</returns>
        public static byte[] DecodeMessage(byte[] encodedMessage)
        {
            ArraySegment<byte> data = new ArraySegment<byte>(encodedMessage, lenSize, encodedMessage.Length - lenSize);
            return data.ToArray();
        }
    }
}
