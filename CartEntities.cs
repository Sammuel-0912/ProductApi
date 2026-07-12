public class Cart
{
    public int Id { get; set; }

    // 💡 業界標準：區分是哪個會員的購物車（目前先寫死一個 Username 模擬
    public string Username { get; set; } = string.Empty;

    // 💡 一對多關聯：一個購物車包含多個明細項目 (導覽屬性 Navigation Property)
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public int Id { get; set; }

    // 外鍵 (Foreign Key)：指向它屬於哪一台購物車
    public int CartId { get; set; }

    // 外鍵 (Foreign Key)：指向它買了哪一個商品
    public int ProductId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(
        1,
        99,
        ErrorMessage = "購買數量必須在 1 到 99 之間"
    )]
    public int Quantity { get; set; }

    // 💡 導覽屬性：讓 EF Core 能夠直接載入商品詳細資訊 (例如品名、價格)
    public Product Product { get; set; } = null!;
}
