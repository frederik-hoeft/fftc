using fftc;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;

const string VERSION = "v8.0.1";

Console.WriteLine($"Welcome to Fast File Transfer Client {VERSION}");
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

static void Usage() => Console.WriteLine(
    $"""
    Fast File Transfer Client {VERSION}
    Usage:

        Send a file to a specified ip and port:
               fftc [-c|-s] <ip> <port> -f <filename>
        Listen for a file at a specified port:
               fftc -l <port> -f <filename>
    
    Examples:

        Send a file called \"myfile.zip\" to 10.1.1.12, port 12344:
        fftc -s 10.1.1.12 12344 -f myfile.zip

        Listen for a file called \"myfile.zip\" at port 12344:
        fftc -l 12344 -f myfile.zip
    """);

static void Send(string[] args)
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
    if (!IPAddress.TryParse(ip, out IPAddress? ipAddress))
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
    if (ipAddress == null)
    {
        Console.WriteLine($"Error: could not host {ip}");
        return;
    }
    IPEndPoint connection = new(ipAddress, port);
    Console.WriteLine("Connecting to " + connection.Address);
    using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
    using NetworkStream stream = new(socket, FileAccess.Write);
    using Stream fileStream = File.OpenRead(filePath);
    CopyTo(fileStream, stream, 65536);
    Console.WriteLine("Done!");
}

static void CopyTo(Stream source, Stream destination, int bufferSize)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    try
    {
        ulong totalBytesSent = 0;
        int bytesRead;
        int counter = 0;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) != 0)
        {
            destination.Write(buffer, 0, bytesRead);
            totalBytesSent += (ulong)bytesRead;
            if (counter % 16 == 0)
            {
                counter = 0;
                Console.Write("\rSent: " + totalBytesSent.ToHumanReadableFileSize(1) + " ...");
            }
        }
        Console.WriteLine("\rReceived: " + totalBytesSent.ToHumanReadableFileSize(1) + " ...");
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

static void Receive(string[] args)
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
    IPAddress ipAddress = IPAddress.Any;
    IPEndPoint local = new(ipAddress, port);
    using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    socket.Bind(local);
    socket.Listen(16);
    Console.WriteLine("Listening on port " + local.Port.ToString());
    using Socket connection = socket.Accept();
    string remoteAddress = connection.RemoteEndPoint is not IPEndPoint remoteEndPoint ? "unknown" : $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
    Console.WriteLine($"Connection from {remoteAddress}");
    byte[] buffer = new byte[65536];
    ulong rec = 0;
    byte counter = 0;
    using FileStream fileStream = File.OpenWrite(Directory.GetCurrentDirectory() + "\\" + file);
    while (true)
    {
        int sizeReceived;
        try
        {
            sizeReceived = connection.Receive(buffer);
        }
        catch (SocketException)
        {
            break;
        }
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
            Console.Write($"\rReceived: {rec.ToHumanReadableFileSize(1)} ...");
            fileStream.Flush();
        }
    }
    Console.WriteLine($"\rReceived: {rec.ToHumanReadableFileSize(1)} ...");
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
    Console.WriteLine($"{rec} bytes written to {file}");
    Console.WriteLine($"Closed connection to {remoteAddress}");
}