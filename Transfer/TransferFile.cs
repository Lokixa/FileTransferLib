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
        /// <summary> 
        /// Sends file via client.
        /// </summary>
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
            catch (Exception e)
            {
                Console.WriteLine("Send file caught: " + e);
            }
            fs.Close();
        }
    }
}