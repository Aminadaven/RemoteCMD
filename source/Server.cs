using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteCMD
{
    public class Server
    {
        private static NetworkStream ns;
        private static bool connected;
        private const int port = 1024, bufferSize = 4096;
        private static string drive = "", path = "";
        private const string TITLE = "Remote CMD Server by Aminadav";
        private const string commands = "shutdown\ncd <dir>\ndrive <drivename>:\ndir\nmkdir <dir>\n" +
            "rndir <dir>\ncopydir <dir> <destination>\nmovedir <dir> <destination>\nmake <file>\n" +
            "del <file>\nmove <file> <destination>\ncopy <file <destination>\nrename <file> <newname>\n" +
            "read <file>\nwrite <file> - enter \"<EOF>\" to end writing.";
        static void Main()
        {
            bool on = true;
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.Title = TITLE + " at: " + listener.LocalEndpoint.ToString();
            while (on)
            {
                Console.Write("Waiting for connection... ");
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Connection accepted.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Title = TITLE + " - Connected to: " + client.Client.RemoteEndPoint.ToString() + ", my endPoint: " + client.Client.LocalEndPoint.ToString();
                ns = client.GetStream();
                SetPath();
                SendResult(Path());
                connected = true;
                while (connected)
                {
                    string[] bm = GetCommand().Split(" ");
                    if (!connected) break;
                    switch (bm[0])
                    {
                        case "shutdown":
                            connected = false;
                            on = false;
                            SendResult("Server is turning off.");
                            break;
                        case "cd":
                            Cd(bm[1]);
                            break;
                        case "dir":
                            Dir();
                            break;
                        case "drive":
                            Drive(bm[1]);
                            break;
                        case "setPath":
                            path = bm[1].Substring(3);
                            break;
                        case "mkdir":
                            MkDir(bm[1]);
                            break;
                        case "rmdir":
                            RmDir(bm[1]);
                            break;
                        case "movedir":
                            MoveDir(bm[1], bm[2]);
                            break;
                        case "copydir":
                            CopyDir(bm[1], bm[2]);
                            break;
                        case "move":
                            Move(bm[1], bm[2]);
                            break;
                        case "copy":
                            Copy(bm[1], bm[2]);
                            break;
                        case "del":
                            Del(bm[1]);
                            break;
                        case "make":
                            Make(bm[1]);
                            break;
                        case "rename":
                            Rename(bm[1], bm[2]);
                            break;
                        case "read":
                            Read(bm[1]);
                            break;
                        //case "write":
                        //    Write(bm[1]);
                        //    break;
                        default:
                            Help();
                            break;
                    }
                }
                try
                {
                    ns.Close();
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            listener.Stop();
        }

        private static void Help()
        {
            SendResult(commands);
        }

        private static void Read(string file)
        {
            string data = "The file: " + file + " not found, or too large (>" + bufferSize + " bytes) to read properly.";
            if (File.Exists(Path() + "\\" + file) && new FileInfo(Path() + "\\" + file).Length > bufferSize)
            {
                data = File.ReadAllText(Path() + "\\" + file);
            }
            SendResult(data);
        }

        private static void Write(string fileName)
        {
            string data = "The file: " + fileName + " not found, or is read-only.";
            FileInfo file = new FileInfo(Path() + "\\" + fileName);
            if (file.Exists && !file.IsReadOnly)
            {
                var stream = file.AppendText();
                string line = GetCommand();
                while (!line.EndsWith("<EOF>"))
                {
                    stream.WriteLine(line);
                    line = GetCommand();
                }
                line = line.Replace("<EOF>", "");
                stream.Write(line);
                stream.Flush();
                stream.Close();
                stream.Dispose();
                data = $"The file {fileName} has been writed.";
            }
            SendResult(data);
        }

        private static void Rename(string oldName, string newName)
        {
            if (!File.Exists(Path() + "\\" + oldName) || File.Exists(Path() + "\\" + newName))
            {
                File.Move(Path() + "\\" + newName, Path() + "\\" + newName);
            }
            Success();
        }

        private static void Make(string newFile)
        {
            if (!File.Exists(Path() + "\\" + newFile))
            {
                bool s = true;
                try
                {
                    File.Create(Path() + "\\" + newFile).Dispose();
                }
                catch (Exception e)
                {
                    SendResult(e.Message);
                    s = false;
                }
                if (s)
                    Success();
            }
        }

        private static void Drive(string newDrive)
        {
            DriveInfo di = new DriveInfo(newDrive);
            if (di.IsReady)
            {
                drive = newDrive;
                path = "";
                SendResult("true");
                return;
            }
            SendResult("false");
        }

        private static void MkDir(string newDir)
        {
            if (!Directory.Exists(Path() + "\\" + newDir))
            {
                Directory.CreateDirectory(Path() + "\\" + newDir);
            }
            Success();
        }

        private static void RmDir(string dir)
        {
            if (Directory.Exists(Path() + "\\" + dir))
            {
                Directory.Delete(Path() + "\\" + dir);
            }
            Success();
        }

        private static void Del(string file)
        {
            if (!File.Exists(Path() + "\\" + file))
            {
                Fail();
                return;
            }
            File.Delete(Path() + "\\" + file);
            Success();
        }

        private static void Move(string file, string dest)
        {
            if (!File.Exists(Path() + "\\" + file) || Directory.Exists(dest + "\\" + file))
            {
                Fail();
                return;
            }
            File.Move(Path() + "\\" + file, dest + "\\" + file);
            Success();
        }

        private static void Copy(string file, string dest)
        {
            if (!File.Exists(Path() + "\\" + file) || Directory.Exists(dest + "\\" + file))
            {
                Fail();
                return;
            }
            File.Copy(Path() + "\\" + file, dest + "\\" + file);
            Success();
        }

        private static void MoveDir(string dir, string dest)
        {
            if (!Directory.Exists(Path() + "\\" + dir) || Directory.Exists(dest + "\\" + dir))
            {
                Fail();
                return;
            }
            Directory.Move(Path() + "\\" + dir, dest + "\\" + dir);
            Success();
        }

        private static void CopyDir(string dir, string dest)
        {
            if (!Directory.Exists(Path() + "\\" + dir) || Directory.Exists(dest + "\\" + dir))
            {
                Fail();
                return;
            }
            CPD(Path() + "\\" + dir, dest + "\\" + dir);
            File.Copy(Path() + "\\" + dir, dest + "\\" + dir);
            Success();
        }

        private static void CPD(string dir, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(dir))
            {
                File.Copy(dir + "\\" + file, dest + "\\" + file);
            }
            foreach (string subDir in Directory.GetDirectories(Path() + "\\" + dir))
            {
                CPD(dir + "\\" + subDir, dest + "\\" + subDir);
            }
        }

        private static void Dir()
        {
            SendResult(SearchDirectoryTree(new DirectoryInfo(Path())));
        }

        static string SearchDirectoryTree(DirectoryInfo root)
        {
            FileInfo[] files;
            DirectoryInfo[] subDirs;
            string result = "";
            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();
            }
            catch (Exception e)
            {
                return e.Message;
            }
            if (subDirs != null)
            {
                result += ("Dirs: \n");
                foreach (DirectoryInfo dir in subDirs)
                {
                    string dirDetails = dir.LastAccessTime.ToShortDateString() + "  " + dir.LastAccessTime.ToShortTimeString() + "\t" + dir.Name;
                    result += (dirDetails + "\n");
                }
            }
            if (files != null)
            {
                result += ("Files: " + "\n");
                foreach (FileInfo fi in files)
                {
                    string fileDetails = fi.LastAccessTime.ToShortDateString() + "  " + fi.LastAccessTime.ToShortTimeString() + "\t" + fi.Name;
                    result += (fileDetails + "\n");
                }
            }
            return result;
        }

        private static void Cd(string newDir)
        {

            string newPath = path + "\\" + newDir;
            if (!Directory.Exists(newPath))
            {
                SendResult("Not Exist");
                return;
            }
            path = newPath;
            SendResult("exist");
        }

        static string Path()
        {
            return drive + path;
        }

        static void SetPath()
        {
            string[] drives = Environment.GetLogicalDrives();
            foreach (string dr in drives)
            {
                DriveInfo di = new DriveInfo(dr);
                if (di.IsReady)
                {
                    drive = dr;
                    break;
                }
            }
        }

        private static void SendResult(string result)
        {
            byte[] byteTime = Encoding.ASCII.GetBytes(result);
            ns.Write(byteTime, 0, byteTime.Length);
        }

        private static string GetCommand()
        {
            try
            {
                byte[] bytes = new byte[bufferSize];
                int bytesRead = ns.Read(bytes, 0, bytes.Length);
                return Encoding.ASCII.GetString(bytes, 0, bytesRead);
            }
            catch (IOException e)
            {
                Console.Beep();
                Console.Error.WriteLine(e.Message);
                connected = false;
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Title = TITLE;

                return "";
            }
        }

        private static void Fail()
        {
            SendResult("Command Failed.");
        }

        private static void Success()
        {
            SendResult("Command Success.");
        }
    }
}