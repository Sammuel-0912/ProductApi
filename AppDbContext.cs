using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    // 建構子：接收外部傳入的資料庫設定
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // 💡 這行代表資料庫裡會有一張叫做 Products 的資料表
    // EF Core 會自動根據 Product 類別的欄位去建立資料表欄位！
    public DbSet<Product> Products { get; set; } = null!;
}
