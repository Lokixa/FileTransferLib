using System;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;

namespace FtLib
{
    public static partial class Transfer
    {
        const int TagBufferSize = 16;
        const int NonceBufferSize = 12;
        public static Meta GetSecure(Socket client, Stream toWriteTo, byte[] encryptionKey)
        {
            Meta meta = GetMeta(client);

            AesGcm gcm = new AesGcm(encryptionKey);
            byte[] buffer = new byte[FileBufferSize + TagBufferSize];
            int clientGetBuffer = client.SendBufferSize;
            client.ReceiveBufferSize = FileBufferSize + TagBufferSize;

            BigInteger count = 0;
            for (BigInteger i = 0; meta.Size != count; i++)
            {
                int bytes = client.Receive(buffer, buffer.Length, SocketFlags.None);
                count += bytes - TagBufferSize;

                byte[] cypher = subArray(buffer, 0, bytes - TagBufferSize);
                byte[] tag = subArray(buffer, bytes - TagBufferSize, bytes);

                byte[] nonce = Base255.ToByteArr(i, NonceBufferSize);

                byte[] plainText = new byte[cypher.Length];
                gcm.Decrypt(nonce, cypher, tag, plainText);

                toWriteTo.Write(plainText, 0, plainText.Length);
            }
            client.ReceiveBufferSize = clientGetBuffer;
            return meta;
        }
        public static void SendSecure(Socket client, Meta meta, Stream data, byte[] encryptionKey)
        {
            SendMeta(client, meta);

            AesGcm gcm = new AesGcm(encryptionKey);
            byte[] buffer = new byte[FileBufferSize];
            int clientSendBuffer = client.SendBufferSize;
            client.SendBufferSize = FileBufferSize + TagBufferSize;

            BigInteger count = 0;
            for (BigInteger i = 0; count != meta.Size; i++)
            {
                int bytes = data.Read(buffer, 0, buffer.Length);
                count += bytes;
                logger.Log($"[{i}] Count: {count}", LoggerState.Debug);
                if (bytes != buffer.Length)
                {
                    Array.Resize(ref buffer, bytes);
                }

                byte[] tag = new byte[16];
                byte[] cyphertext = new byte[buffer.Length];

                byte[] nonce = Base255.ToByteArr(i, NonceBufferSize);

                gcm.Encrypt(nonce, buffer, cyphertext, tag);
                logger.Log($"Cypher: {string.Join(',', cyphertext)}", LoggerState.Debug);
                logger.Log($"Tag: {string.Join(',', tag)}", LoggerState.Debug);
                logger.Log($"Nonce: {string.Join(',', nonce)}", LoggerState.Debug);

                byte[] unifiedEnc = new byte[cyphertext.Length + tag.Length];
                cyphertext.CopyTo(unifiedEnc, 0);
                tag.CopyTo(unifiedEnc, cyphertext.Length);

                client.Send(unifiedEnc, unifiedEnc.Length, SocketFlags.None);
            }
            client.SendBufferSize = clientSendBuffer;
        }
    }
}