using Vorratsuebersicht.Desktop.Services;

namespace Vorratsuebersicht.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SyncClient _client;

    [ObservableProperty] private string _serverUrl = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isChecking;

    public event EventHandler? OnServerChanged;

    public SettingsViewModel(SyncClient client)
    {
        _client = client;
    }

    [RelayCommand]
    public async Task ConnectAsync()
    {
        IsChecking = true;
        StatusText = "Verbinde...";
        _client.BaseUrl = ServerUrl;

        try
        {
            var ok = await _client.PingAsync();
            if (ok)
            {
                StatusText = "Verbunden! Server ist erreichbar.";
                OnServerChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusText = "Fehler: Server nicht erreichbar.";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
        }
    }
}
