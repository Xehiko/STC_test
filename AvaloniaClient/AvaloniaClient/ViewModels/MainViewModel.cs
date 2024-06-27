using AvaloniaClient.Models;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AvaloniaClient.ViewModels;

public partial class MainViewModel
{
    public ConnectionModel _connection;
    public IAsyncRelayCommand InitiateConnectionCommand { get; }
    public IAsyncRelayCommand SendDataCommand { get; }

    public ConnectionModel Connection
    {
        get => _connection;
    }

    public MainViewModel()
    {
        _connection = new ConnectionModel();
        InitiateConnectionCommand = new AsyncRelayCommand(InitiateConnection, canExecute: () => string.IsNullOrEmpty(_connection.ConnectionStatus)); // попробовать просто проверять Connected у TcpClient
        SendDataCommand = new AsyncRelayCommand(SendData);
    }

    private async Task InitiateConnection()
    {
        await _connection.Connect();
    }

    private async Task SendData()
    {
        await _connection.SendDataAsync();
    }
}
