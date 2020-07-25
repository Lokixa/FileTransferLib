using System;
using System.IO;
using System.Net.Sockets;

namespace FtLib
{
    public static partial class Net
    {
        /// <summary> 
        /// Gets file and stores it into the selected folder.
        /// </summary>
        /// <exception cref="System.IO.DirectoryNotFoundException">Throws if the folder given isn't found.</exception>
        /// See <see cref="Net.Get(Socket, Stream)"/> for receiving to a stream. 
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
            catch (SocketException se)
            {
                var errCode = (SocketError)se.ErrorCode;
                if (errCode == SocketError.NotConnected)
                {
                    fs.Close();
                    File.Delete(folder + filename);
                    throw se;
                }
                Console.WriteLine("Get file caught Socket Exception: " + se);
            }
            fs.Close();

            File.Move(folder + filename, folder + meta.Name);
            return meta;
        }
        /// <summary> 
        /// Sends file via client.
        /// </summary> 
        /// <exception cref="System.IO.FileNotFoundException">Throws if the filePath is wrong.</exception>
        /// See <see cref="Net.Send(Socket, Meta, Stream)"/> for sending a stream. 
        public static void SendFile(Socket client, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }
            FileStream fs = new FileStream(filePath, FileMode.Open);
            string filename = string.Empty;

            var isWindows = System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

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
            finally
            {
                fs.Close();
            }
        }
    }
}