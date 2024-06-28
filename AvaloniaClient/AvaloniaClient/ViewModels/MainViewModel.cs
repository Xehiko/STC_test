using AvaloniaClient.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace AvaloniaClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ConnectionModel _connection;
    [ObservableProperty]
    private string _connectionStatus = "";
    [ObservableProperty]
    private string _userInput = "";
    [ObservableProperty]
    private string _serverResponse = "";
    [ObservableProperty]
    private bool _isAbleToConnect = true;

    public IAsyncRelayCommand InitiateConnectionCommand { get; }
    public IAsyncRelayCommand SendDataCommand { get; }

    public ConnectionModel Connection
    {
        get => _connection;
    }

    public MainViewModel()
    {
        _connection = new ConnectionModel();
        _connection.MessageReceived += OnMessageReceived;
        _connection.ConnectionStatusChanged += OnConnectionStatusChanged;
        _connection.IsAbleToConnectChanged += OnIsAbleToConnectChanged;

        InitiateConnectionCommand = new AsyncRelayCommand(InitiateConnection, canExecute: () => IsAbleToConnect);
        SendDataCommand = new AsyncRelayCommand(SendData, canExecute: () => !string.IsNullOrEmpty(UserInput));
    }

    private async Task InitiateConnection()
    {
        await _connection.ConnectAsync();
    }

    private async Task SendData()
    {
        await _connection.SendDataAsync(UserInput);
    }

    partial void OnUserInputChanged(string value)
    {
        SendDataCommand.NotifyCanExecuteChanged();
    }
    private void OnIsAbleToConnectChanged(object? sender, bool isAbleToConnect)
    {
        IsAbleToConnect = isAbleToConnect;
        InitiateConnectionCommand.NotifyCanExecuteChanged();
    }

    private void OnMessageReceived(object? sender, string message)
    {
        ServerResponse = message;
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        ConnectionStatus = status;
    }
}
