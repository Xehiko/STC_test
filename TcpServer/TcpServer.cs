using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerTcp
{
    public class TcpServer
    {
        private string _hostname;
        private int _port;
        private TcpListener _listener;

        // все клиенты
        List<TcpClient> _clients;
        // очередь обработки полученных от клиентов строк
        ConcurrentQueue<(TcpClient, string)> _recievedDataQueue;

        public TcpServer(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            _listener = new TcpListener(IPAddress.Parse(_hostname), _port);
            _clients = [];
            _recievedDataQueue = [];
        }

        public void InitializeServer()
        {
            try
            {
                _listener.Start();
                Console.WriteLine("Север запущен.\nОжидание подключений...\n");

                // поток для обработки получаемых данных
                Thread processDataThread = new Thread(new ThreadStart(ProcessData));
                processDataThread.Start();

                // Запускаем отправку времени каждые 10 секунд
                Timer timer = new Timer(SendTimeToClients, null, 0, 10000);

                while (true)
                {
                    var newClient = _listener.AcceptTcpClient();

                    lock (_clients)
                    {
                        _clients.Add(newClient);
                    }
                    Console.WriteLine($"Подключился клиент {newClient.Client.RemoteEndPoint}\n");

                    // новый поток для получения сообщений от клиента
                    Thread recieverThread = new Thread(new ParameterizedThreadStart(RecieveData));
                    recieverThread.Start(newClient);
                }
            }
            finally
            {
                _listener.Stop();
            }
        }

        public void RecieveData(Object? obj)
        {
            TcpClient? tcpClient = obj as TcpClient;
            // если не получилось преобразовать в Client
            if (tcpClient == null)
                return;

            byte[] bytes = new byte[1024];

            try
            {
                NetworkStream stream = tcpClient.GetStream();
                while(true)
                {
                    var messageLength = stream.Read(bytes);
                    var message = Encoding.UTF8.GetString(bytes, 0, messageLength);

                    Console.WriteLine($"Клиент {tcpClient.Client.RemoteEndPoint} прислал строку: {message}\n");

                    // добавили полученные данные в очередь для последующей обработки
                    _recievedDataQueue.Enqueue((tcpClient, message));
                }
            }
            // Клиент отключился
            catch(IOException IOEx) when (IOEx.InnerException is SocketException)
            {
                Console.WriteLine($"Клиент {tcpClient.Client.RemoteEndPoint} отключился.\n");
                lock(_clients)
                {
                    _clients.Remove(tcpClient);
                }
                tcpClient.Close();
            }
        }

        public void ProcessData()
        {
            while (true)
            {
                if (_recievedDataQueue.TryDequeue(out var message))
                {
                    NetworkStream stream = message.Item1.GetStream();

                    stream.Write(Encoding.UTF8.GetBytes(message.Item2));
                }
            }
        }

        private void SendTimeToClients(object? o)
        {
            lock(_clients)
            {
                foreach (var client in _clients)
                {
                    client.GetStream().Write(Encoding.UTF8.GetBytes(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")));
                }
            }
        }
    }
}
