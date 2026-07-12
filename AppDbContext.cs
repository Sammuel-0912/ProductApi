using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    // 建構子：接收外部傳入的資料庫設定
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // 💡 這行代表資料庫裡會有一張叫做 Products 的資料表
    // EF Core 會自動根據 Product 類別的欄位去建立資料表欄位！
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;

    // 💡 透過 Fluent API 來定義更嚴謹的資料庫關聯 (業界標準作法)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 設定 Cart 與 CartItem 的一對多關係
        modelBuilder
            .Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade); // 💡 串聯刪除：如果購物車被刪了，裡面的明細自動清除
    }
}
