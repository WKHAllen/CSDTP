using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace CSDTP;

/// <summary>
///     Crypto utilities.
/// </summary>
public static class Crypto
{
    /// <summary>
    ///     The RSA key size.
    /// </summary>
    private const int RsaKeySize = 2048;

    /// <summary>
    ///     The AES key size.
    /// </summary>
    private const int AesKeySize = 32;

    /// <summary>
    ///     Generate a pair of RSA keys.
    /// </summary>
    /// <returns>The generated key pair.</returns>
    public static (byte[], byte[]) NewRsaKeys()
    {
        var cipher = new RSACryptoServiceProvider(RsaKeySize);

        var publicKey = cipher.ExportParameters(false);
        var privateKey = cipher.ExportParameters(true);

        var publicKeyStream = new MemoryStream();
        var publicKeySerializer = new XmlSerializer(typeof(RSAParameters));
        publicKeySerializer.Serialize(publicKeyStream, publicKey);
        var serializedPublicKey = publicKeyStream.ToArray();

        var privateKeyStream = new MemoryStream();
        var privateKeySerializer = new XmlSerializer(typeof(RSAParameters));
        privateKeySerializer.Serialize(privateKeyStream, privateKey);
        var serializedPrivateKey = privateKeyStream.ToArray();

        return (serializedPublicKey, serializedPrivateKey);
    }

    /// <summary>
    ///     Encrypt data with RSA.
    /// </summary>
    /// <param name="publicKey">The RSA public key.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns></returns>
    public static byte[] RsaEncrypt(byte[] publicKey, byte[] plaintext)
    {
        var publicKeyStream = new MemoryStream(publicKey);
        var publicKeySerializer = new XmlSerializer(typeof(RSAParameters));
        var deserializedPublicKeyObj = publicKeySerializer.Deserialize(publicKeyStream);

        if (deserializedPublicKeyObj == null) throw new CSDTPException("invalid public key");

        var deserializedPublicKey = (RSAParameters)deserializedPublicKeyObj;

        var cipher = new RSACryptoServiceProvider();
        cipher.ImportParameters(deserializedPublicKey);

        return cipher.Encrypt(plaintext, false);
    }

    /// <summary>
    ///     Decrypt data with RSA.
    /// </summary>
    /// <param name="privateKey">The RSA private key.</param>
    /// <param name="ciphertext">The data to decrypt.</param>
    /// <returns></returns>
    public static byte[] RsaDecrypt(byte[] privateKey, byte[] ciphertext)
    {
        var privateKeyStream = new MemoryStream(privateKey);
        var privateKeySerializer = new XmlSerializer(typeof(RSAParameters));
        var deserializedPrivateKeyObj = privateKeySerializer.Deserialize(privateKeyStream);

        if (deserializedPrivateKeyObj == null) throw new CSDTPException("invalid private key");

        var deserializedPrivateKey = (RSAParameters)deserializedPrivateKeyObj;

        var cipher = new RSACryptoServiceProvider();
        cipher.ImportParameters(deserializedPrivateKey);

        return cipher.Decrypt(ciphertext, false);
    }

    /// <summary>
    ///     Generate a new AES key.
    /// </summary>
    /// <returns>The generated AES key.</returns>
    public static byte[] NewAesKey()
    {
        var key = new byte[AesKeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    ///     Encrypt data with AES.
    /// </summary>
    /// <param name="key">The AES key.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns></returns>
    public static byte[] AesEncrypt(byte[] key, byte[] plaintext)
    {
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;
        var cipherSize = plaintext.Length;

        var encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;
        var encryptedData = new byte[encryptedDataLength].AsSpan();

        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        RandomNumberGenerator.Fill(nonce);

        var cipher = new AesGcm(key);
        cipher.Encrypt(nonce, plaintext.AsSpan(), cipherBytes, tag);

        return encryptedData.ToArray();
    }

    /// <summary>
    ///     Decrypt data with AES.
    /// </summary>
    /// <param name="key">The AES key.</param>
    /// <param name="ciphertext">The data to decrypt.</param>
    /// <returns></returns>
    public static byte[] AesDecrypt(byte[] key, byte[] ciphertext)
    {
        var encryptedData = ciphertext.AsSpan();

        var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(0, 4));
        var tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
        var cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

        var nonce = encryptedData.Slice(4, nonceSize);
        var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
        var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

        var plainBytes = new byte[cipherSize].AsSpan();
        var cipher = new AesGcm(key);
        cipher.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return plainBytes.ToArray();
    }
}