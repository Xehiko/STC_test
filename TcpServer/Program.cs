using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

namespace ServerTcp
{
    internal class Program
    {
        const string Hostname = "127.0.0.1";
        const int Port = 8888;

        static void Main(string[] args)
        {
            TcpServer tcpServer = new TcpServer(Hostname, Port);
            tcpServer.InitializeServer();
        }
    }
}
