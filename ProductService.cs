using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class Product
{
    // Id 為主鍵，由資料庫自動遞增產生，新增時不需前端提供
    public int Id { get; set; }

    [Required(ErrorMessage = "商品名稱不能為空")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "商品名稱長度必須介於2~100之間")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "商品價格是必填欄位")]
    [Range(1, 100000, ErrorMessage = "商品價格必須在1元到100,000之間")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "商品庫存是必填")]
    [Range(0, int.MaxValue, ErrorMessage = "庫存不能為負數")]
    public int Stock { get; set; }

    //為了方便建立初始資料，加上一個建構子

    public Product() { }

    public Product(int id, string name, decimal price, int stock)
    {
        Id = id;
        Name = name;
        Price = price;
        Stock = stock;
    }
}

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task AddProductAsync(Product product);
}

// 2. 實作類別：真正撰寫商務邏輯的地方
public class ProductService : IProductService
{
    // 把原本在 Program.cs 的 List 移到這裡當作私有欄位
    private readonly AppDbContext _context;

    // {
    //     new Product(1, "高階人體工學椅", 8800, 10),
    //     new Product(2, "機械鍵盤 (青軸)", 2499, 5),
    //     new Product(3, "無線垂直滑鼠", 1250, 0),
    // };

    // 💡 透過相依性注入 (DI) 把資料庫內容 (DbContext) 傳進來
    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    // 使用 Async (非同步) 的方式讀取資料庫，這是高效能後端的標準做法
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task AddProductAsync(Product product)
    {
        // 💡 新增時把 Id 歸零，交給資料庫自動遞增產生，
        //    避免前端傳入的 Id 撞到既有主鍵而拋出 UNIQUE constraint 例外 (500)
        product.Id = 0;
        _context.Products.Add(product);
        await _context.SaveChangesAsync(); // 💡 這行才會真正把資料寫入 SQLite 檔案中！
    }

    // public List<Product> GetAllProducts()
    // {
    //     return _products;
    // }

    // public Product? GetProductById(int id)
    // {
    //     return _products.FirstOrDefault(p => p.Id == id);
    // }

    // public void AddProduct(Product product)
    // {
    //     _products.Add(product);
    // }
}

// // 資料模型依然保留
// public record Product(int Id, string Name, decimal Price, int Stock);
