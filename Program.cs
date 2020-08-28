using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace fftc
{
    /// <summary>
    /// Represents the main class of fftc.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The current version of fftc in the following format: <Major>.<Month>-<Minor (Reset every month)>.Year
        /// </summary>
        private const string VERSION = "v1.8-1.20";

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">The command line parameters.</param>
        private static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Fast File Transfer Client " + VERSION);
            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "-l":
                        Receive(args);
                        break;

                    case "-c":
                    case "-s":
                        Send(args);
                        break;

                    default:
                        Usage();
                        break;
                }
            }
            else
            {
                Usage();
            }
        }

        /// <summary>
        /// Sends the specified file to the specified destination.
        /// </summary>
        /// <param name="args">
        /// The array of command line arguments where:
        ///     <list type="bullet">
        ///         <item><c><paramref name="args"/>[0]</c> doesn't matter and can be <c>null</c>.</item>
        ///         <item><c><paramref name="args"/>[1]</c> is the IPv4 address of the destination.</item>
        ///         <item><c><paramref name="args"/>[2]</c> is the destination port.</item>
        ///         <item><c><paramref name="args"/>[3]</c> currently must be <c>-f</c> to specify that a file is being transmitted (may add support for directory structures in the future).</item>
        ///         <item><c><paramref name="args"/>[4]</c> is path of the file to be transmitted.</item>
        ///     </list>
        /// </param>
        private static void Send(string[] args)
        {
            if (args.Length != 5)
            {
                Usage();
                return;
            }
            if (!string.Equals(args[3], "-f", StringComparison.OrdinalIgnoreCase))
            {
                Usage();
                return;
            }
            string file = args[4];
            string ip = args[1];
            int port = Convert.ToInt32(args[2]);
            IPAddress ipAddress = null;
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ip);
                IPAddress[] ipAddresses = entry.AddressList;
                for (int i = 0; i < ipAddresses.Length; i++)
                {
                    if (ipAddresses[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = ipAddresses[i];
                        break;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error: no route to host.");
                return;
            }
            if (ipAddress == null)
            {
                Console.WriteLine("Error: no route to host.");
                return;
            }
            IPEndPoint connection = new IPEndPoint(ipAddress, port);
            Console.WriteLine("Connecting to " + connection.Address);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(connection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("Error: could not connect to peer.");
                return;
            }
            string filePath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + file;
            Console.WriteLine("Sending " + ((ulong)new FileInfo(filePath).Length).ToHumanReadableFileSize(1) + " of data. This may take a while ...");
            socket.SendFile(filePath);
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(true);
            socket.Close();
            Console.WriteLine("Done!");
        }

        /// <summary>
        /// Listens for a data at the specified port and writes it to the specified file.
        /// </summary>
        /// <param name="args">
        /// The array of command line arguments where:
        ///     <list type="bullet">
        ///         <item><c><paramref name="args"/>[0]</c> doesn't matter and can be <c>null</c>.</item>
        ///         <item><c><paramref name="args"/>[1]</c> is the local port to listen on.</item>
        ///         <item><c><paramref name="args"/>[2]</c> currently must be <c>-f</c> to specify that a file is being received (may add support for directory structures in the future).</item>
        ///         <item><c><paramref name="args"/>[3]</c> is the file the data will be written to.</item>
        ///     </list>
        /// </param>
        private static void Receive(string[] args)
        {
            if (args.Length != 4)
            {
                Usage();
                return;
            }
            if (!args[2].Equals("-f"))
            {
                Usage();
                return;
            }
            string file = args[3];
            int port = Convert.ToInt32(args[1]);
            IPAddress ipAddress = GetLocalIPAddress();
            IPEndPoint local = new IPEndPoint(ipAddress, port);
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(local);
            socket.Listen(16);
            Console.WriteLine("Listening on port " + local.Port.ToString());
            using Socket connection = socket.Accept();
            IPEndPoint remoteEndPoint = (IPEndPoint)connection.RemoteEndPoint;
            Console.WriteLine("Connection from " + remoteEndPoint.Address + ":" + remoteEndPoint.Port);
            byte[] buffer = new byte[65536];
            ulong rec = 0;

            byte counter = 0;
            using FileStream fileStream = File.OpenWrite(Directory.GetCurrentDirectory() + "\\" + file);
            while (true)
            {
                int sizeReceived = connection.Receive(buffer);
                if (sizeReceived == 0)
                {
                    break;
                }
                rec += (ulong)sizeReceived;
                fileStream.Write(buffer, 0, sizeReceived);
                counter++;
                if (counter >= 16)
                {
                    counter = 0;
                    Console.Write("\rReceived: " + rec.ToHumanReadableFileSize(1) + " ...");
                    fileStream.Flush();
                }
            }
            Console.Write("\rReceived: " + rec.ToHumanReadableFileSize(1) + " ...\n");
            fileStream.Flush();
            fileStream.Close();
            try
            {
                connection.Shutdown(SocketShutdown.Both);
                connection.Disconnect(true);
                connection.Close();
            }
            catch (SocketException) { }
            try
            {
                socket.Close();
            }
            catch (SocketException) { }
            Console.WriteLine(rec.ToString() + " bytes written to " + file);
            Console.WriteLine("Closed connection to " + remoteEndPoint.Address);
        }

        /// <summary>
        /// Displays usage information
        /// </summary>
        private static void Usage()
        {
            Console.WriteLine("Fast File Transfer Client " + VERSION);
            Console.WriteLine("\n" +
                "Usage:\n" +
                "\n" +
                "Send a file to a specified ip and port:\n" +
                "           fftc [-c|-s] <ip> <port> -f <filename>\n" +
                "Listen for a file at a specified port:\n" +
                "           fftc -l <port> -f <filename>\n" +
                "\n" +
                "Examples:\n" +
                "\n" +
                "Send a file called \"myfile.zip\" to 10.1.1.12, port 12344:\n" +
                "fftc -s 10.1.1.12 12344 -f myfile.zip\n" +
                "\n" +
                "Listen for a file called \"myfile.zip\" at port 12344:\n" +
                "fftc -l 12344 -f myfile.zip\n");
        }

        /// <summary>
        /// Gets the <see cref="IPAddress"/> of the local network interface to listen on.
        /// </summary>
        private static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && (ip.ToString().StartsWith("192.168") || ip.ToString().StartsWith("10.") || ip.ToString().Equals("127.0.0.1")))
                {
                    return ip;
                }
            }
            throw new Exception("No IPv4 network adapters found!");
        }
    }
}