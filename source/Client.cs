using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace RemoteCMD
{
    class Client
    {
        private const int port = 1024, bufferSize = 4096;
        private static string location = "DISCONNECTED", hostName = "";
        private const string TITLE = "Remote CMD Client by Aminadav";
        private static TcpClient client = new TcpClient();
        private static NetworkStream ns;
        static void Main()
        {
            Console.Title = TITLE;
            Boolean active = true;
            while (active)
            {
                Console.Write(Location() + ">");
                string line = Console.ReadLine();

                string[] brokenLine = line.Split(" ");
                switch (brokenLine[0].ToLower())
                {
                    case "connect":
                        Connect(brokenLine[1]);
                        break;
                    case "shutdown":
                        Console.WriteLine(Command(line));
                        Disconnect();
                        break;
                    case "disconnect":
                        Disconnect();
                        break;
                    case "cd":
                        Cd(brokenLine);
                        break;
                    case "drive":
                        Drive(brokenLine[1]);
                        break;
                    case "exit":
                        Disconnect();
                        active = false;
                        break;
                    default:
                        Console.WriteLine(Command(line));
                        break;
                }
            }
        }

        private static void Connect(string host)
        {
            try
            {
                client = new TcpClient(host, port);
                ns = client.GetStream();
                //sets the location
                byte[] bytes = new byte[bufferSize];
                int bytesRead = ns.Read(bytes, 0, bytes.Length);
                location = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                hostName = host + ":" + port + "\\\\";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Title = TITLE + " - Connected to " + host + " at port: " + port;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        static string Command(string command)
        {
            if (!client.Connected)
            {
                Console.Error.WriteLine("Usage: Connect <hostName>");
                return null;
            }
            string result = "";
            try
            {
                ns.Write(Encoding.ASCII.GetBytes(command));
                //if (ns.DataAvailable)
                //{
                byte[] bytes = new byte[bufferSize];
                int bytesRead = ns.Read(bytes, 0, bytes.Length);
                result = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }
        static void Disconnect()
        {
            try
            {
                ns.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            hostName = "";
            location = "DISCONNECTED";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = TITLE;
        }

        private static string Location()
        {
            return hostName + location;
        }

        private static void Drive(string newDrive)
        {
            string result = Command("drive " + (newDrive.EndsWith("\\") ? newDrive : $"{newDrive}\\"));
            if (result.Equals("true"))
            {
                location = newDrive;
            }
            else
            {
                Console.Error.WriteLine("Server could not find {0}", newDrive);
            }
        }

        private static void Cd(string[] brokenLine)
        {
            if (brokenLine.Length <= 1)
            {
                Console.WriteLine(Location());
                return;
            }
            else if (brokenLine[1].StartsWith(".."))
            {
                string dir = Directory.GetParent(location).FullName;
                if (brokenLine[1].Length > 2)
                {
                    string newDir = brokenLine[1].Substring(2);
                    location = Path.Combine(dir, newDir);
                }
                else
                {
                    location = dir;
                }
                Command("setPath " + location);
            }
            else
            {
                var result = Command("cd " + brokenLine[1]);
                if (result.Equals("exist"))
                {
                    location += brokenLine[1];
                }
                else
                {
                    Console.Error.WriteLine("Server could not find {0}", brokenLine[1]);
                }
            }
        }
    }
}