using Vorratsuebersicht.Desktop.Models;
using Vorratsuebersicht.Desktop.Services;

namespace Vorratsuebersicht.Desktop.ViewModels;

public partial class StorageListViewModel : ObservableObject
{
    private readonly SyncClient _client;

    [ObservableProperty] private ObservableCollection<StorageItem> _items = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showEntryForm;
    [ObservableProperty] private StorageItem _newItem = new();
    [ObservableProperty] private ObservableCollection<Article> _articles = new();
    [ObservableProperty] private Article? _selectedArticle;

    public StorageListViewModel(SyncClient client)
    {
        _client = client;
    }

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        try
        {
            var list = await _client.GetStorageItemsAsync();
            Items.Clear();
            foreach (var s in list.OrderBy(x =>
            {
                if (DateTime.TryParse(x.BestBeforeDate, out var d)) return d;
                return DateTime.MaxValue;
            })) Items.Add(s);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task ShowForm()
    {
        NewItem = new StorageItem();
        var arts = await _client.GetArticlesAsync();
        Articles.Clear();
        foreach (var a in arts) Articles.Add(a);
        ShowEntryForm = true;
    }

    [RelayCommand]
    public async Task SaveItem()
    {
        try
        {
            await _client.CreateStorageItemAsync(NewItem);
            ShowEntryForm = false;
            await Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CancelForm() => ShowEntryForm = false;

    [RelayCommand]
    public async Task DeleteItem(StorageItem s)
    {
        try
        {
            await _client.DeleteStorageItemAsync(s.StorageItemId);
            await Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }
    }
}
