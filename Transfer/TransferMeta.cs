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
        /// </summary>
        public static Meta GetMeta(Socket client)
        {
            // Get 
            byte[] metaBuffer = new byte[MetaBufferSize];
            int bytes = client.Receive(metaBuffer);

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
                if (nameBuffer[i] < 33)  // If its blank (ASCII)
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
        public static void SendMeta(Socket client, Meta meta)
        {
            byte[] metaBuffer = new byte[MetaBufferSize];

            string filename = meta.Name;
            if (filename.Length > NameBufferSize)
            {
                filename = filename.Substring(0, NameBufferSize);
            }

            Encoding.UTF8.GetBytes(filename).CopyTo(metaBuffer, 0);

            Base255 size = new Base255(meta.Size);
            logger.Log($"Compressing {meta.Size} into [{string.Join(",", size.byteArr)}]", Logger.State.Debug);
            for (int i = 0; i < size.byteArr.Length; i++)
            {
                metaBuffer[(NameBufferSize) + i] = size.byteArr[i];
            }
            logger.Log($"Sending meta buffer - [{string.Join(",", metaBuffer)}]", Logger.State.Debug);

            client.Send(metaBuffer);
        }
        #endregion
    }
}