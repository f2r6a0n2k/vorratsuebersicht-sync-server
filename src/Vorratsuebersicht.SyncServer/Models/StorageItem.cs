namespace Vorratsuebersicht.SyncServer.Models;

public class StorageItem
{
    public int StorageItemId { get; set; }
    public int ArticleId { get; set; }
    public int Quantity { get; set; }
    public string? BestBeforeDate { get; set; }
    public string? StorageName { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}
