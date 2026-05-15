using Vorratsuebersicht.Desktop.Models;
using Vorratsuebersicht.Desktop.Services;

namespace Vorratsuebersicht.Desktop.ViewModels;

public partial class ShoppingListViewModel : ObservableObject
{
    private readonly SyncClient _client;

    [ObservableProperty] private ObservableCollection<ShoppingItem> _items = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showAddForm;
    [ObservableProperty] private ShoppingItem _newItem = new();
    [ObservableProperty] private ObservableCollection<Article> _articles = new();

    public ShoppingListViewModel(SyncClient client)
    {
        _client = client;
    }

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        try
        {
            var list = await _client.GetShoppingItemsAsync();
            Items.Clear();
            foreach (var s in list.OrderBy(x => x.IsChecked).ThenBy(x => x.ArticleName))
                Items.Add(s);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task ShowForm()
    {
        NewItem = new ShoppingItem();
        var arts = await _client.GetArticlesAsync();
        Articles.Clear();
        foreach (var a in arts) Articles.Add(a);
        ShowAddForm = true;
    }

    [RelayCommand]
    public async Task SaveItem()
    {
        try
        {
            await _client.CreateShoppingItemAsync(NewItem);
            ShowAddForm = false;
            await Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CancelForm() => ShowAddForm = false;

    [RelayCommand]
    public async Task ToggleItem(ShoppingItem s)
    {
        s.IsChecked = !s.IsChecked;
        await _client.UpdateShoppingItemAsync(s.ShoppingItemId, s);
        await Load();
    }

    [RelayCommand]
    public async Task DeleteItem(ShoppingItem s)
    {
        await _client.DeleteShoppingItemAsync(s.ShoppingItemId);
        await Load();
    }
}
