using Vorratsuebersicht.Desktop.Models;

namespace Vorratsuebersicht.Desktop.Services;

public class SyncClient
{
    private readonly HttpClient _http;
    private string _baseUrl = "";

    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value.TrimEnd('/');
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);

    public SyncClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    private string Url(string path) => $"{_baseUrl}{path}";

    public async Task<bool> PingAsync()
    {
        try
        {
            var res = await _http.GetAsync(Url("/api/discovery/ping"));
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<Article>> GetArticlesAsync(string? search = null)
    {
        var path = "/api/articles";
        if (!string.IsNullOrWhiteSpace(search))
            path += $"?search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<List<Article>>(Url(path)) ?? new();
    }

    public async Task<Article> CreateArticleAsync(Article a)
    {
        var res = await _http.PostAsJsonAsync(Url("/api/articles"), a);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<Article>())!;
    }

    public async Task UpdateArticleAsync(int id, Article a)
    {
        var res = await _http.PutAsJsonAsync(Url($"/api/articles/{id}"), a);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteArticleAsync(int id)
    {
        await _http.DeleteAsync(Url($"/api/articles/{id}"));
    }

    public async Task<List<StorageItem>> GetStorageItemsAsync()
    {
        return await _http.GetFromJsonAsync<List<StorageItem>>(Url("/api/storage-items")) ?? new();
    }

    public async Task<StorageItem> CreateStorageItemAsync(StorageItem s)
    {
        var res = await _http.PostAsJsonAsync(Url("/api/storage-items"), s);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<StorageItem>())!;
    }

    public async Task DeleteStorageItemAsync(int id)
    {
        await _http.DeleteAsync(Url($"/api/storage-items/{id}"));
    }

    public async Task<List<ShoppingItem>> GetShoppingItemsAsync()
    {
        return await _http.GetFromJsonAsync<List<ShoppingItem>>(Url("/api/shopping-items")) ?? new();
    }

    public async Task<ShoppingItem> CreateShoppingItemAsync(ShoppingItem s)
    {
        var res = await _http.PostAsJsonAsync(Url("/api/shopping-items"), s);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ShoppingItem>())!;
    }

    public async Task UpdateShoppingItemAsync(int id, ShoppingItem s)
    {
        var res = await _http.PutAsJsonAsync(Url($"/api/shopping-items/{id}"), s);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteShoppingItemAsync(int id)
    {
        await _http.DeleteAsync(Url($"/api/shopping-items/{id}"));
    }
}
