using System;
using System.Net.Sockets;
using System.Text;

namespace FtLib
{
    public static partial class Net
    {
        #region Meta
        /// <summary> 
        /// Gets meta file structure from client.
        /// Abstain from using this method, unless you know how it works.
        /// <para/>
        /// See <see cref="Net.Send(Socket, Meta, System.IO.Stream)"/>,
        /// or see <see cref="Net.SendFile(Socket, string)"/>
        /// </summary>
        /// <exception cref="System.Exception">
        /// Thrown when the promised bytes and received bytes don't match.
        /// </exception>
        /// <exception cref="System.Net.Sockets.SocketException">
        /// Thrown when a receive operation has failed, the client has disconnected.
        /// </exception>

        public static Meta GetMeta(Socket client)
        {
            // Get 
            byte[] metaBuffer = new byte[MetaBufferSize];
            int bytes = client.Receive(metaBuffer);

            // Probably disconnected.
            if (bytes == 0)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            // Lengths don't match.
            if (bytes != metaBuffer.Length)
            {
                throw new Exception("Not enough metadata");
            }
            logger.Log($"Got meta buffer - [{string.Join(",", metaBuffer)}]", Logger.State.Debug);

            // Split 
            byte[] nameBuffer = subArray(metaBuffer, 0, NameBufferSize);
            byte[] sizeBuffer = subArray(metaBuffer, NameBufferSize, metaBuffer.Length);

            // Clean nameBuffer, first 'non-blank' element.
            int nonZeroIndex = 0;
            for (int i = 0; i < nameBuffer.Length; i++)
            {
                if (nameBuffer[i] < 33)  // If its not a letter (ASCII)
                {
                    nonZeroIndex = i;
                    break;
                }
            }
            Array.Resize(ref nameBuffer, nonZeroIndex);

            // Parse
            logger.Log($"Got size buffer [{string.Join(",", sizeBuffer)}]", Logger.State.Debug);
            Base255 size = new Base255(sizeBuffer);
            logger.Log("Parsed as " + size.Number, Logger.State.Debug);
            string name = System.Text.Encoding.UTF8.GetString(nameBuffer);

            return new Meta(name, size.Number);
        }
        /// <summary> 
        /// Sends meta file structure to host via client. Check netconf.json for meta structure.
        /// </summary>
        /// <exception cref="System.Net.Sockets.SocketException">
        /// Thrown when a send operation has failed, the host has disconnected.
        /// </exception>
        public static void SendMeta(Socket client, Meta meta)
        {
            byte[] metaBuffer = new byte[MetaBufferSize];

            // Cap filename to NameBufferSize
            string filename = meta.Name;
            if (filename.Length > NameBufferSize)
            {
                filename = filename.Substring(0, NameBufferSize);
            }

            // Copy to the meta buffer.
            Encoding.UTF8.GetBytes(filename).CopyTo(metaBuffer, 0);

            // Copy the converted size to meta buffer.
            Base255 size = new Base255(meta.Size);
            logger.Log($"Compressing {meta.Size} into [{string.Join(",", size.byteArr)}]", Logger.State.Debug);
            for (int i = 0; i < size.byteArr.Length; i++)
            {
                metaBuffer[(NameBufferSize) + i] = size.byteArr[i];
            }
            logger.Log($"Sending meta buffer - [{string.Join(",", metaBuffer)}]", Logger.State.Debug);

            // Send to client
            int bytes = client.Send(metaBuffer);

            // If no bytes received, probably disconnected.
            if (bytes == 0)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
        }
        #endregion
    }
}