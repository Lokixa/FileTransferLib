using System;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FtLib
{

    /// <summary> 
    /// A bunch of helper functions for receiving files and such. 
    /// Hardcoded values in netconf.json in the library's folder.
    /// </summary>
    public static partial class Net
    {
        ///<value>The size of the default file reading buffer.</value>
        const int FileBufferSize = 1024;
        const int MetaBufferSize = 64;
        const int LengthBufferSize = 8;
        const int NameBufferSize = MetaBufferSize - LengthBufferSize;
        static Logger logger = new Logger(Logger.State.Silent);

        ///<summary>
        /// Gets meta and stream via client socket.
        ///</summary>
        ///<exception cref="System.Net.Sockets.SocketException">Thrown if the client is disconnected</exception>
        public static Meta Get(Socket client, Stream toWrite)
        {
            Meta meta = GetMeta(client);
            logger.Log($"Got meta: {meta.Name} - {meta.Size}", Logger.State.Debug);

            byte[] buffer = new byte[FileBufferSize];

            for (BigInteger received = 0; received != meta.Size;)
            {
                int bytes;

                BigInteger bytesLeft = meta.Size - received;
                if (bytesLeft - FileBufferSize < 0)
                    bytes = (int)bytesLeft;
                else
                    bytes = FileBufferSize;
                logger.Log($"{meta.Name} - {received} / {meta.Size}", Logger.State.Progress);

                bytes = client.Receive(buffer, 0, bytes, SocketFlags.None);
                received += bytes;
                toWrite.Write(buffer, 0, bytes);
            }
            return meta;
        }
        ///<summary>
        /// Sends meta and stream via client socket.
        ///</summary>
        ///<exception cref="System.Net.Sockets.SocketException">Thrown if the client is disconnected</exception>
        public static void Send(Socket client, Meta meta, Stream data)
        {
            logger.Log($"Sending meta: {meta.Name} - {meta.Size}", Logger.State.Debug);
            SendMeta(client, meta);

            byte[] buffer = new byte[FileBufferSize];


            for (BigInteger count = 0; count != meta.Size;)
            {
                int bytes = data.Read(buffer, 0, buffer.Length);
                bytes = client.Send(buffer, bytes, SocketFlags.None);
                count += bytes;
                logger.Log($"{meta.Name} - {count} / {meta.Size}", Logger.State.Progress);
            }
        }

        #region HelperMethod
        ///<summary>
        /// Sets logger state.
        ///</summary>
        public static void UseLogger(Logger.State state)
        {
            logger.CurrentState = state;
        }
        // Returns a new sub-array.
        private static byte[] subArray(byte[] arr, int from, int to)
        {
            byte[] sub = new byte[to - from];
            for (int i = 0; from < to; i++, from++)
            {
                sub[i] = arr[from];
            }
            return sub;
        }
        #endregion
    }
}