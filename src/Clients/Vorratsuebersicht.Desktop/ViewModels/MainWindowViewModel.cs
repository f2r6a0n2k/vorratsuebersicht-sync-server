using Vorratsuebersicht.Desktop.Services;

namespace Vorratsuebersicht.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SyncClient _client = new();

    [ObservableProperty] private string _title = "Vorrats\u00fcbersicht Desktop";
    [ObservableProperty] private int _selectedTab;
    [ObservableProperty] private string _serverStatus = "Nicht verbunden";
    [ObservableProperty] private bool _isConnected;

    public SyncClient Client => _client;
    public ArticleListViewModel Articles { get; }
    public StorageListViewModel Storage { get; }
    public ShoppingListViewModel Shopping { get; }
    public SettingsViewModel Settings { get; }

    public MainWindowViewModel()
    {
        Articles = new ArticleListViewModel(_client);
        Storage = new StorageListViewModel(_client);
        Shopping = new ShoppingListViewModel(_client);
        Settings = new SettingsViewModel(_client);
        Settings.OnServerChanged += OnServerChanged;
    }

    private async void OnServerChanged(object? sender, EventArgs e)
    {
        var ok = await _client.PingAsync();
        IsConnected = ok;
        ServerStatus = ok ? "Verbunden" : "Nicht verbunden";
        if (ok)
        {
            await Articles.Load();
            await Storage.Load();
            await Shopping.Load();
        }
    }
}
