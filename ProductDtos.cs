using System.ComponentModel.DataAnnotations;

// 💡 專門用來接收前端「新增商品」請求的 DTO (不需要 Id)
public class ProductCreateDto
{
    [Required(ErrorMessage = "商品名稱不能為空")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "商品名稱長度必須介於2~100之間")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "商品價格是必填欄位")]
    [Range(1, 100000, ErrorMessage = "商品價格必須在1元到100,000之間")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "商品庫存是必填")]
    [Range(0, int.MaxValue, ErrorMessage = "庫存不能為負數")]
    public int Stock { get; set; }
}

// 💡 專門用來回傳給前端展示的 DTO (保護資料庫實體不被外洩)
public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// 💡 前端把商品加入購物車時發送的 Request
public class AddToCartDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

// 💡 回傳給前端看的購物車內容 (會自動計算總金額)
public class CartResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<CartItemResponseDto> Items { get; set; } = new();

    // 💡 唯讀屬性：自動加總所有項目的小計，算出購物車總金額
    public decimal TotalPrice => Items.Sum(item => item.SubTotal);
}

public class CartItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // 💡 每項商品的小計 (單價 * 數量)
    public decimal SubTotal => Price * Quantity;
}
