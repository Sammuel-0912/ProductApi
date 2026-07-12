using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========== 1. 註冊服務到 DI 容器中 ==========
// Singleton (單例模式)：在整個應用程式生命週期中，只會存在一個 ProductService 實例
// 這樣我們對 List 的新增操作才不會因為重新連線而消失
// builder.Services.AddSingleton<IProductService, ProductService>();
// 1. 註冊 EF Core DbContext，並指定使用 SQLite，資料庫檔案名為 app.db
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=app.db"));

// 2. 註冊服務（注意：與資料庫有關的服務要用 AddScoped）
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// 自動自動初始化資料庫（如果 app.db 檔案不存在，會自動建立並套用結構)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // 如果資料庫是空的，順手塞幾筆初始商品進去
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product(1, "高階人體工學椅", 8800, 10),
            new Product(2, "機械鍵盤 (青軸)", 2499, 5),
            new Product(3, "無線垂直滑鼠", 1250, 0)
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
        return Results.Ok(await productService.GetAllProductsAsync());
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

// POST: 新增商品 (體驗自動型別驗證)
app.MapPost(
        "/api/products",
        async (Product newProduct, IProductService productService) =>
        {
            await productService.AddProductAsync(newProduct);
            return Results.Created($"/api/products/{newProduct.Id}", newProduct);
        }
    )
    .AddEndpointFilter(
        async (context, next) =>
        {
            // 1. 從請求參數中抓取第一個參數 (也就是 Product newProduct)
            var product = context.GetArgument<Product>(0);
            // 2. 觸發 .NET 內建的驗證器
            var validationContext = new ValidationContext(product);
            var validationResults = new List<ValidationResult>();

            // 3. 檢查 Product 類別上的 [Required] 和 [Range] 標籤
            bool isValid = Validator.TryValidateObject(
                product,
                validationContext,
                validationResults,
                true
            );

            if (!isValid)
            {
                // 4. 如果驗證失敗，整理錯誤訊息並直接回傳 400 Bad Request
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

app.Run();
