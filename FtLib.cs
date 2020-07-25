using System;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace FtLib
{
    #region DataStructures
    /// <summary> 
    /// Base 255 class.
    /// </summary>
    public class Base255
    {
        public BigInteger Number { get; }
        public byte[] byteArr { get; }  //Inverted buffer

        public Base255(BigInteger number)
        {
            int size = (int)Math.Ceiling(BigInteger.Log(number, 255));
            this.byteArr = new byte[size];
            Number = number;
            var tempNumber = number;

            for (int i = 0; i < size; i++)
            {
                byteArr[i] = (byte)(tempNumber % 255);
                tempNumber /= 255;
            }
        }
        public Base255(byte[] buffer)
        {
            this.byteArr = new byte[buffer.Length];
            buffer.CopyTo(byteArr, 0);
            Number = 0;

            for (int i = 0; i < this.byteArr.Length; i++)
            {
                Number += this.byteArr[i] * BigInteger.Pow(255, i);
            }
        }

        public override string ToString()
        {
            return Number.ToString();
        }
    }
    /// <summary> 
    /// Struct-like class for holding meta information. Immutable.
    /// </summary>
    public class Meta
    {
        public string Name { get; }
        public BigInteger Size { get; }
        public Meta(string name, BigInteger size)
        {
            this.Name = name;
            this.Size = size;
        }
    }
    public class Logger
    {
        public enum State
        {
            Silent,
            Progress,
            Debug
        }
        public State CurrentState { get; set; }
        public TextWriter WriteTo;
        public Logger(State state)
        {
            CurrentState = state;
            WriteTo = Console.Out;
        }
        public void Log(string message, State state)
        {
            if (state != CurrentState || state == State.Silent)
            {
                return;
            }
            WriteTo.Write(message);
        }
    }
    #endregion
    /// <summary> 
    /// A bunch of helper functions for receiving files and such. 
    /// Hardcoded values in netconf.json in the library's folder.
    /// </summary>
    public static class Net
    {
        const int FileBufferSize = 1024;
        const int MetaBufferSize = 64;
        const int LengthBufferSize = 8;
        const int NameBufferSize = MetaBufferSize - LengthBufferSize;
        static Logger logger = new Logger(Logger.State.Silent);
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
        #region SendAndReceive
        /// <summary> 
        /// Gets file and stores it into the selected folder.
        /// </summary>
        public static Meta GetFile(Socket client, string folder = "./")
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException();
            }
            if (!folder.EndsWith('/'))
            {
                folder += '/';
            }

            Meta meta = new Meta(string.Empty, 0);
            string filename = "FtMobReceive-" + DateTime.Now.ToString("hh_mm_ss");

            FileStream fs = new FileStream(folder + filename, FileMode.Create);
            try
            {
                meta = Get(client, fs);
            }
            catch (Exception e)
            {
                Console.WriteLine("Get file caught: " + e); // Maybe add logger warn state
                fs.Close();
                File.Delete(folder + filename);
            }
            fs.Close();

            File.Move(folder + filename, folder + meta.Name);
            return meta;
        }
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
                logger.Log($"Got {received} / {meta.Size} || {bytes}", Logger.State.Debug);

                bytes = client.Receive(buffer, 0, bytes, SocketFlags.None);
                logger.Log("Not stuck here atleast.", Logger.State.Debug);
                received += bytes;
                toWrite.Write(buffer, 0, bytes);
            }
            return meta;
        }
        /// <summary> 
        /// Sends file via client.
        /// </summary>
        public static void SendFile(Socket client, string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }
            FileStream fs = new FileStream(fileName, FileMode.Open);

            string filename = string.Empty;
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                filename = fs.Name.Substring(fs.Name.LastIndexOf("\\") + 1);
            }
            else
            {
                filename = fs.Name.Substring(fs.Name.LastIndexOf("/") + 1);
            }
            Meta meta = new Meta(filename, fs.Length);

            try
            {
                Send(client, meta, fs);
            }
            catch (Exception e)
            {
                Console.WriteLine("Send file caught: " + e);
            }
            fs.Close();
        }
        ///<summary>
        /// Sends meta and stream via client socket.
        ///</summary>
        public static void Send(Socket client, Meta meta, Stream data)
        {
            logger.Log($"Sending meta: {meta.Name} - {meta.Size}", Logger.State.Debug);
            SendMeta(client, meta);

            byte[] buffer = new byte[FileBufferSize];
            for (BigInteger count = 0; count != meta.Size;)
            {
                int bytes = data.Read(buffer, 0, buffer.Length);
                logger.Log($"Read {bytes}", Logger.State.Debug);
                bytes = client.Send(buffer, bytes, SocketFlags.None);
                logger.Log($", Sent {bytes}", Logger.State.Debug);
                count += bytes;
            }
        }
        #endregion
        public static void UseLogger(Logger.State state)
        {
            logger.CurrentState = state;
        }
        #region HelperMethod
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