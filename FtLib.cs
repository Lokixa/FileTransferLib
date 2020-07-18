using System;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace FtLib
{
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
    /// <summary> 
    /// A bunch of helper functions for receiving files and such. 
    /// Hardcoded values in netconf.json in the library's folder.
    /// </summary>
    public static class Net
    {
        /// <summary> 
        /// Gets meta file structure from client.
        /// </summary>
        public static Meta GetMeta(Socket client)
        {
            // Get 
            byte[] metaBuffer = new byte[40];
            int bytes = client.Receive(metaBuffer);

            if (bytes != metaBuffer.Length)
            {
                throw new Exception("Not enough metadata");
            }

            // Split 
            byte[] nameBuffer = metaBuffer[0..32];
            byte[] sizeBuffer = metaBuffer[32..metaBuffer.Length];

            // Clean i.e. only non-zero values
            cleanBuffer(ref nameBuffer);
            cleanBuffer(ref sizeBuffer);

            // Parse
            Base255 size = new Base255(sizeBuffer);
            string name = System.Text.Encoding.UTF8.GetString(nameBuffer);

            return new Meta(name, size.Number);
        }
        /// <summary> 
        /// Sends meta file structure to host via client. Check netconf.json for meta structure.
        /// </summary>
        public static void SendMeta(Socket client, Meta meta)
        {
            byte[] metaBuffer = new byte[40];

            Encoding.UTF8.GetBytes(meta.Name).CopyTo(metaBuffer, 0);

            Base255 size = new Base255(meta.Size);
            for (int i = 0; i < size.byteArr.Length; i++)
            {
                metaBuffer[32 + i] = size.byteArr[i];
            }

            client.Send(metaBuffer);
        }
        /// <summary> 
        /// Gets file and stores it into the selected folder.
        /// </summary>
        public static Meta GetFile(Socket client, string folder = "./")
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException();
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
                Console.WriteLine("Get file caught: " + e);
            }
            fs.Close();
            File.Move(filename, meta.Name);
            return meta;
        }
        public static Meta Get(Socket client, Stream toWrite)
        {
            Meta meta = GetMeta(client);
            byte[] buffer = new byte[1024];
            BigInteger received = 0;
            while (received != meta.Size)
            {
                int bytes = client.Receive(buffer);
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

            string filename = fs.Name.Substring(fs.Name.LastIndexOf("\\") + 1);
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
            SendMeta(client, meta);
            byte[] buffer = new byte[1024];
            int bytes = 1;
            while (bytes != 0)
            {
                bytes = data.Read(buffer, 0, buffer.Length);
                client.Send(buffer, bytes, SocketFlags.None);
            }
        }
        // Resizes the array to the first element before 0.
        private static void cleanBuffer(ref byte[] buffer)
        {
            int index = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    index = i;
                    break;
                }
            }
            Array.Resize(ref buffer, index);
        }
    }
}