using Microsoft.EntityFrameworkCore;

public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(string Username);
    Task AddToCartAsync(string username, int productId, int Quantity);
}

public class CartService : ICartService
{
    private readonly AppDbContext _context;

    public CartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> GetOrCreateCartAsync(string username)
    {
        // 💡 關鍵：使用 Include() 來主動載入一對多關聯的 Items，並 Include 裡面的 Product 資訊 (Eager Loading)
        var cart = await _context
            .Carts.Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Username == username);

        if (cart == null)
        {
            cart = new Cart { Username = username };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task AddToCartAsync(string username, int productId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(username);

        // 檢查商品是否存在與庫存
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            throw new Exception("商品不存在");
        if (product.Stock < quantity)
            throw new Exception("庫存不足");

        // 檢查購物車內是否已有該商品
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            // 有的話，數量累加
            existingItem.Quantity += quantity;
        }
        else
        {
            // 沒有的話，新增明細
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
        }
        await _context.SaveChangesAsync();
    }
}
