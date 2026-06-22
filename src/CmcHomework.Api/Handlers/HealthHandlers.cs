using CmcHomework.Api.Models;
using CmcHomework.Api.Services;

namespace CmcHomework.Api.Handlers;

// Health endpoint dùng để kiểm tra nhanh ứng dụng còn sống hay không.
// Trong thực tế, endpoint kiểu này thường được dùng bởi load balancer, monitor hoặc người chấm bài.
public static class HealthHandlers
{
    // Đăng ký route GET /health vào ứng dụng.
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealth);
        return app;
    }

    // ASP.NET Core tự inject IAssetService và AppLifetime từ DI container.
    // Nhờ đó hàm này biết được hiện có bao nhiêu asset và app đã chạy bao lâu.
    private static IResult GetHealth(IAssetService service, AppLifetime lifetime)
    {
        // Dùng UTC để thời gian không phụ thuộc múi giờ máy chạy server.
        var now = DateTimeOffset.UtcNow;

        // Tạo object response rõ ràng rồi trả về JSON.
        // Do Program.cs đã cấu hình snake_case, các field như UptimeSeconds sẽ thành uptime_seconds.
        var response = new HealthResponse(
            "ok",
            new HealthStorageInfo("sqlite", service.CountAll()),
            lifetime.GetUptimeSeconds(now),
            now);

        return Results.Ok(response);
    }
}
