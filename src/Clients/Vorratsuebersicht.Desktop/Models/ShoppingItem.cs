namespace Vorratsuebersicht.Desktop.Models;

public class ShoppingItem
{
    public int ShoppingItemId { get; set; }
    public int ArticleId { get; set; }
    public string? ArticleName { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsChecked { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
