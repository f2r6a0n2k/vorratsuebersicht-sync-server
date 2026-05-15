namespace Vorratsuebersicht.Desktop.Models;

public class StorageItem
{
    public int StorageItemId { get; set; }
    public int ArticleId { get; set; }
    public string? ArticleName { get; set; }
    public int Quantity { get; set; }
    public string? BestBeforeDate { get; set; }
    public string? StorageName { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
