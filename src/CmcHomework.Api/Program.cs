using System.Text.Json;
using CmcHomework.Api.Handlers;
using CmcHomework.Api.Scanners;
using CmcHomework.Api.Services;
using CmcHomework.Api.Storage;
using Microsoft.AspNetCore.Http.Json;

// Program.cs là điểm bắt đầu của ứng dụng ASP.NET Core.
// Khi chạy lệnh `dotnet run`, .NET sẽ đọc file này để:
// 1. tạo web server,
// 2. đăng ký các service cần dùng,
// 3. khai báo các endpoint HTTP,
// 4. bắt đầu lắng nghe request từ client.
var builder = WebApplication.CreateBuilder(args);

// Cấu hình cách ASP.NET Core đọc/ghi JSON.
// Mặc định C# hay dùng PascalCase như `AssetCount`, còn API trong đề bài cần snake_case
// như `asset_count`. Hai dòng policy bên dưới giúp tự động đổi tên property khi trả JSON.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Khi body JSON của request bị sai định dạng, ASP.NET Core có thể ném lỗi rất dài trong môi trường
// Development. Tắt ThrowOnBadRequest giúp API trả về HTTP 400 Bad Request gọn hơn cho người dùng.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var allowedOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .GetChildren()
    .Select(section => section.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .ToArray();

// CORS cho frontend chạy riêng ở localhost:5173 hoặc localhost:3000.
// Nếu không có CORS, browser sẽ chặn frontend gọi backend dù curl/Postman vẫn chạy.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins!)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Đăng ký dependency injection.
// Hiểu đơn giản: thay vì tự `new AssetService(...)` ở nhiều nơi, ta đăng ký ở đây một lần.
// Khi handler cần IAssetService hoặc IAssetStorage, ASP.NET Core sẽ tự đưa đúng object vào.
//
// AddSingleton nghĩa là trong suốt thời gian app chạy chỉ có 1 object dùng chung.
builder.Services.AddSingleton<AppLifetime>();
builder.Services.AddSingleton<SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IAssetStorage, SqliteAssetStorage>();
builder.Services.AddSingleton<IScanStorage, SqliteScanStorage>();
builder.Services.AddSingleton<IAssetService, AssetService>();
builder.Services.AddSingleton<IScanService, ScanService>();
builder.Services.AddSingleton<IScanner, DnsScanner>();
builder.Services.AddSingleton<IScanner, WhoisScanner>();
builder.Services.AddSingleton<IScanner, SubdomainScanner>();
builder.Services.AddSingleton<IScanner, CertificateTransparencyScanner>();
builder.Services.AddSingleton<IScanner, AsnScanner>();
builder.Services.AddSingleton<IScanner, IpScanner>();
builder.Services.AddSingleton<IScanner, PortScanner>();
builder.Services.AddSingleton<IScanner, SslScanner>();
builder.Services.AddSingleton<IScanner, TechScanner>();

// Sau khi cấu hình xong builder, Build() tạo ra object app thật sự để khai báo route và chạy server.
var app = builder.Build();

// Tạo bảng SQLite nếu chưa có. Đây là migration tự động cho bài tập.
app.Services.GetRequiredService<DatabaseInitializer>().Initialize();

app.UseCors();

// Endpoint kiểm tra nhanh ở trang gốc.
// Khi truy cập GET /, API chỉ trả một message đơn giản để biết server đang chạy.
app.MapGet("/", () => Results.Ok(new { message = "CMC Homework API is running" }));

// Hai extension method này nằm trong thư mục Handlers.
// Chúng gom các route liên quan đến asset và health vào file riêng để Program.cs không bị quá dài.
app.MapAssetEndpoints();
app.MapScanEndpoints();
app.MapHealthEndpoints();

// Bắt đầu chạy web server. Từ dòng này trở đi app sẽ chờ request HTTP gửi tới.
app.Run();

// Dòng này để hỗ trợ test/integration test nếu sau này cần tạo WebApplicationFactory<Program>.
// Nó không ảnh hưởng tới cách API chạy bình thường.
public partial class Program;
