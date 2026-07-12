using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========== 1. 註冊服務到 DI 容器中 ==========
// Singleton (單例模式)：在整個應用程式生命週期中，只會存在一個 ProductService 實例
// 這樣我們對 List 的新增操作才不會因為重新連線而消失
// builder.Services.AddSingleton<IProductService, ProductService>();

// 1. 註冊 EF Core DbContext，使用 SQLite
//    業界標準寫法：從 appsettings.json 讀取連線字串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddScoped<ICartService, CartService>(); // 💡 註冊 CartService

// 2. 註冊服務（注意：與資料庫有關的服務要用 AddScoped）
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// 模擬當前登入的使用者為 "sam60"
const string mockUser = "sam60";

// 自動自動初始化資料庫（如果 app.db 檔案不存在，會自動建立並套用結構)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // 💡 改用 Migrate()：自動套用所有尚未執行的 Migration（會依結構建立/更新資料表）
    db.Database.Migrate();

    // 如果資料庫是空的，順手塞幾筆初始商品進去
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product
            {
                Id = 1,
                Name = "高階人體工學椅",
                Price = 8800,
                Stock = 10,
            },
            new Product
            {
                Id = 2,
                Name = "機械鍵盤 (青軸)",
                Price = 2499,
                Stock = 5,
            },
            new Product
            {
                Id = 3,
                Name = "無線垂直滑鼠",
                Price = 1250,
                Stock = 0,
            }
        );
        db.SaveChanges();
    }
}

// ========== 2. 路由設定 (透過 DI 注入服務) ==========

// .NET 看到參數裡有 IProductService，會自動從容器中抓取實例注入進來
app.MapGet(
    "/api/products",
    async (IProductService productService) =>
    {
        var products = await productService.GetAllProductsAsync();
        var response = products.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Stock = p.Stock,
        });

        return Results.Ok(response);
    }
);

// GET: 依 ID 取得單一商品
app.MapGet(
    "/api/products/{id:int}",
    async (int id, IProductService productService) =>
    {
        var product = await productService.GetProductByIdAsync(id);
        return product is not null
            ? Results.Ok(product)
            : Results.NotFound($"找不到 ID 為 {id} 的商品");
    }
);

// 2. POST 路由：改接收 ProductCreateDto 參數
app.MapPost(
        "/api/products",
        async (ProductCreateDto dto, IProductService productService) =>
        {
            // 將 DTO 轉換為 真正的資料庫實體 Entity
            var newProduct = new Product(dto.Name, dto.Price, dto.Stock);

            await productService.AddProductAsync(newProduct);

            var responseDto = new ProductResponseDto
            {
                Id = newProduct.Id, // 這時資料庫已經自動生成出新 Id 了
                Name = newProduct.Name,
                Price = newProduct.Price,
                Stock = newProduct.Stock,
            };
            return Results.Created($"/api/products/{responseDto.Id}", responseDto);
        }
    )
    .AddEndpointFilter(
        async (context, next) =>
        {
            // 💡 這裡非常關鍵！安檢閘門現在改為檢查 ProductCreateDto (參數索引從 0 開始)
            var dto = context.GetArgument<ProductCreateDto>(0);
            var validationContext = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
            {
                var errors = validationResults.ToDictionary(
                    r => r.MemberNames.FirstOrDefault() ?? "Error",
                    r => new[] { r.ErrorMessage ?? "欄位驗證錯誤" }
                );
                return Results.BadRequest(
                    new
                    {
                        title = "One or more validation errors occurred.",
                        status = 400,
                        errors,
                    }
                );
            }
            // 5. 驗證通過，繼續往下執行原本的 API 邏輯
            return await next(context);
        }
    );

// 1. GET 購物車 API：轉換成結構精美的 DTO
app.MapGet(
    "/api/cart",
    async (ICartService cartService) =>
    {
        var cart = await cartService.GetOrCreateCartAsync(mockUser);

        var response = new CartResponseDto
        {
            Id = cart.Id,
            Username = cart.Username,
            Items = cart
                .Items.Select(ci => new CartItemResponseDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity,
                })
                .ToList(),
        };
        return Results.Ok(response);
    }
);

// 2. POST 加入購物車 API
app.MapPost(
    "/api/cart/items",
    async (AddToCartDto dto, ICartService cartService) =>
    {
        try
        {
            await cartService.AddToCartAsync(mockUser, dto.ProductId, dto.Quantity);
            return Results.Ok(new { message = "成功加入購物車" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
);

app.Run();
