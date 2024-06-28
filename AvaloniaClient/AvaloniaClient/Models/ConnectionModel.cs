using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Avalonia.Threading;
using System;
using System.IO;
using System.Threading;

namespace AvaloniaClient.Models
{
    public class ConnectionModel
    {
        private const string Hostname = "127.0.0.1";
        private const int Port = 8888;
        private const int MaxRetryAttempts = 5;

        private TcpClient? _client;
        private int _connectionAttempt;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<string>? ConnectionStatusChanged;
        public event EventHandler<bool>? IsAbleToConnectChanged;

        public ConnectionModel()
        {
            _connectionAttempt = 0;
        }

        public async Task ConnectAsync()
        {
            // если мы уже подключаемся
            // вызываем из UI-потока
            Dispatcher.UIThread.Post(() => IsAbleToConnectChanged?.Invoke(this, false));
            // если уже подключен
            if (_client != null && _client.Connected)
                return;
            for (_connectionAttempt = 1; _connectionAttempt <= MaxRetryAttempts; _connectionAttempt++)
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(Hostname, Port);

                    // При успешном подклчении сбрасываем попытки переподключений
                    _connectionAttempt = 0;

                    Dispatcher.UIThread.Post(() => ConnectionStatusChanged?.Invoke(this, $"Подключен к {Hostname}:{Port}"));

                    _ = Task.Run(ReceiveDataAsync);
                    break;
                }
                catch (SocketException)
                {
                    // попытка переподкючения каждые 2 секунд
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    Dispatcher.UIThread.Post(() => ConnectionStatusChanged?.Invoke(this, $"Попытка подключения ({_connectionAttempt})..."));

                    if (_connectionAttempt == MaxRetryAttempts)
                    {
                        Dispatcher.UIThread.Post(() => ConnectionStatusChanged?.Invoke(this, "Ошибка подключения"));
                        // выход из функции Connect - значит можем подключаться в UI
                        Dispatcher.UIThread.Post(() => IsAbleToConnectChanged?.Invoke(this, true));
                    }
                }
            }
        }

        public async Task SendDataAsync(string message)
        {
            // может отправить, только если подключен
            if (_client!.Connected)
            {
                await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(message));
            }
        }

        private async void ReceiveDataAsync()
        {
            // если не подключен
            if (!_client!.Connected)
                return;
            try
            {
                using NetworkStream networkStream = _client.GetStream();

                byte[] buffer = new byte[1024];

                while (true)
                {
                    var messageLength = await networkStream.ReadAsync(buffer);

                    // в UI потоке поменяли ответ сервера
                    Dispatcher.UIThread.Post(() => MessageReceived?.Invoke(this, Encoding.UTF8.GetString(buffer, 0, messageLength)));
                }
            }
            // сервер разорвал соединение
            catch (IOException IOEx) when (IOEx.InnerException is SocketException)
            {
                Dispatcher.UIThread.Post(() => ConnectionStatusChanged?.Invoke(this, "Сервер разовал соединение."));

                // повторно пытаемся подключиться
                _ = ConnectAsync();
            }
        }
    }
}
