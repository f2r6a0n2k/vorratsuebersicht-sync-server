using Vorratsuebersicht.Desktop.Models;
using Vorratsuebersicht.Desktop.Services;

namespace Vorratsuebersicht.Desktop.ViewModels;

public partial class ArticleListViewModel : ObservableObject
{
    private readonly SyncClient _client;

    [ObservableProperty]
    private ObservableCollection<Article> _articles = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private Article? _selectedArticle;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showEditor;

    [ObservableProperty]
    private Article _editArticle = new();

    public ArticleListViewModel(SyncClient client)
    {
        _client = client;
    }

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        try
        {
            var list = await _client.GetArticlesAsync(SearchText);
            Articles.Clear();
            foreach (var a in list) Articles.Add(a);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public void NewArticle()
    {
        EditArticle = new Article();
        ShowEditor = true;
    }

    [RelayCommand]
    public void Edit(Article a)
    {
        EditArticle = new Article
        {
            ArticleId = a.ArticleId,
            Name = a.Name,
            Manufacturer = a.Manufacturer,
            Category = a.Category,
            SubCategory = a.SubCategory,
            DurableInfinity = a.DurableInfinity,
            WarnInDays = a.WarnInDays,
            Size = a.Size,
            Unit = a.Unit,
            Calorie = a.Calorie,
            Notes = a.Notes,
            EANCode = a.EANCode,
            StorageName = a.StorageName,
            MinQuantity = a.MinQuantity,
            PrefQuantity = a.PrefQuantity,
            Supermarket = a.Supermarket,
            Price = a.Price
        };
        ShowEditor = true;
    }

    [RelayCommand]
    public async Task Save()
    {
        try
        {
            if (EditArticle.ArticleId == 0)
                await _client.CreateArticleAsync(EditArticle);
            else
                await _client.UpdateArticleAsync(EditArticle.ArticleId, EditArticle);
            ShowEditor = false;
            await Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }
    }

    [RelayCommand]
    public void Cancel() => ShowEditor = false;

    [RelayCommand]
    public async Task Delete(Article a)
    {
        try
        {
            await _client.DeleteArticleAsync(a.ArticleId);
            await Load();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler: {ex.Message}");
        }
    }

    public async void OnSearchChanged() => await Load();
}
