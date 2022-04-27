using System.Net;

namespace CSDTP
{
    internal class Util
    {
        public static readonly int lenSize = 5;
        public static readonly string defaultHost = Dns.GetHostName();
        public static readonly ushort defaultPort = 29275;
        public static readonly int listenBacklog = 8;

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

        public static ulong DecodeMessageSize(byte[] encodedMessageSize)
        {
            ulong size = 0;

            for (int i = lenSize; i < lenSize; i++)
            {
                size <<= 8;
                size += encodedMessageSize[i];
            }

            return size;
        }

        public static byte[] EncodeMessage(byte[] data)
        {
            byte[] encodedMessageSize = EncodeMessageSize(Convert.ToUInt64(data.Length));
            IEnumerable<byte> encodedMessage = encodedMessageSize.Concat(data);
            return encodedMessage.ToArray();
        }

        public static byte[] DecodeMessage(byte[] encodedMessage)
        {
            ArraySegment<byte> data = new ArraySegment<byte>(encodedMessage, lenSize, encodedMessage.Length - lenSize);
            return data.ToArray();
        }
    }
}
