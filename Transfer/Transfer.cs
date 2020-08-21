using System.IO;
using System.Net.Sockets;
using System.Numerics;

namespace FtLib
{
    /// <summary> 
    /// A bunch of helper functions for receiving files and such. 
    /// Hardcoded values in netconf.json in the library's folder.
    /// </summary>
    public static partial class Transfer
    {
        ///<value>The size of the default file reading buffer.</value>
        const int FileBufferSize = 1024;
        ///<value>The size of the default file reading buffer.</value>
        const int MetaBufferSize = 64;
        ///<value>The size of the default file reading buffer.</value>
        const int LengthBufferSize = 8;
        ///<value>The size of the default file reading buffer.</value>
        const int NameBufferSize = MetaBufferSize - LengthBufferSize;
        static Logger logger = new Logger(LoggerState.Silent);
        ///<summary>
        /// Gets meta and stream via client socket.
        ///</summary>
        ///<exception cref="System.Net.Sockets.SocketException">Thrown if the client is disconnected</exception>
        public static Meta Get(Socket client, Stream toWrite)
        {
            Meta meta = GetMeta(client);
            logger.Log(
                $"Got meta: {meta.Name} - {meta.Size}",
                LoggerState.Debug | LoggerState.Simple);

            byte[] buffer = new byte[FileBufferSize];

            for (BigInteger received = 0; received != meta.Size;)
            {
                logger.Log($"\r{meta.Name} - {received} / {meta.Size}",
                           LoggerState.Progress);

                int bytes = client.Receive(buffer, FileBufferSize, SocketFlags.None);
                received += bytes;
                toWrite.Write(buffer, 0, bytes);
            }
            logger.Log($"\r{meta.Name} - {meta.Size} / {meta.Size}",
                       LoggerState.Progress);
            return meta;
        }
        ///<summary>
        /// Sends meta and stream via client socket.
        ///</summary>
        ///<exception cref="System.Net.Sockets.SocketException">Thrown if the client is disconnected</exception>
        public static void Send(Socket client, Meta meta, Stream data)
        {
            logger.Log(
                $"Sending meta: {meta.Name} - {meta.Size}",
                LoggerState.Debug | LoggerState.Simple);

            SendMeta(client, meta);

            byte[] buffer = new byte[FileBufferSize];


            for (BigInteger count = 0; count != meta.Size;)
            {
                int bytes = data.Read(buffer, 0, buffer.Length);
                bytes = client.Send(buffer, bytes, SocketFlags.None);
                count += bytes;

                logger.Log($"\r{meta.Name} - {count} / {meta.Size}",
                           LoggerState.Progress);
            }
            logger.Log($"\r{meta.Name} - {meta.Size} / {meta.Size}",
                       LoggerState.Progress);
        }
        #region HelperMethods
        ///<summary>
        /// Sets logger state.
        ///</summary>
        public static void UseLogger(LoggerState state)
        {
            logger.CurrentState = state;
        }

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