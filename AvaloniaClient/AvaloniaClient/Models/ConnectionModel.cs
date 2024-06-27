using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Avalonia.Threading;
using System;
using System.IO;

namespace AvaloniaClient.Models
{
    public class ConnectionModel : ObservableObject
    {
        private TcpClient _client;
        private const string Hostname = "127.0.0.1";
        private const int Port = 8888;
        private string _connectionStatus;
        private string _userInput;
        private string _serverResponse;

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }
        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }
        public string ServerResponse
        {
            get => _serverResponse;
            set => SetProperty(ref _serverResponse, value);
        }

        public ConnectionModel()
        {
            _client = new TcpClient();
            _connectionStatus = "";
            _userInput = "";
            _serverResponse= "";
        }

        public async Task Connect()
        {
            try
            {
                await _client.ConnectAsync(Hostname, Port);
                Dispatcher.UIThread.Post(() => UpdateConnectionStatus($"Подключен к {Hostname}:{Port}"));
                _ = Task.Run(ReceiveDataAsync);
            }
            catch (IOException IOEx) when (IOEx.InnerException is SocketException)
            {
                Dispatcher.UIThread.Post(() => UpdateConnectionStatus("Ошибка подключения"));
            }
        }

        public async Task SendDataAsync()
        {
            if (_client.Connected)
            {
                await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(_userInput));
            }
        }

        private async void ReceiveDataAsync()
        {
            if (_client.Connected)
            {
                try
                {
                    using NetworkStream networkStream = _client.GetStream();

                    byte[] buffer = new byte[1024];

                    while (true)
                    {
                        var messageLength = await networkStream.ReadAsync(buffer);

                        // в UI потоке поменяли ответ сервера
                        Dispatcher.UIThread.Post(() => UpdateServerResponse(Encoding.UTF8.GetString(buffer, 0, messageLength)));
                    }
                }
                catch (IOException IOEx) when (IOEx.InnerException is SocketException)
                {
                    Dispatcher.UIThread.Post(() => UpdateConnectionStatus("Удаленный хост разорвал соединение"));
                }
            }
        }

        private void UpdateServerResponse(string newResponse) => ServerResponse = newResponse;

        private void UpdateConnectionStatus(string status) => ConnectionStatus = status;
    }
}
