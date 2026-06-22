using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Services;

namespace CmcHomework.Api.Handlers;

// Handler là lớp đứng sát HTTP nhất.
// Nhiệm vụ chính của file này:
// - khai báo URL nào sẽ gọi hàm nào,
// - nhận dữ liệu từ route/query/body,
// - gọi service để xử lý nghiệp vụ,
// - đổi kết quả thành HTTP response như 200, 201, 400, 404.
//
// Handler không trực tiếp validate chi tiết hay lưu dữ liệu; việc đó nằm ở AssetService/Storage.
public static class AssetHandlers
{
    // Extension method cho phép viết `app.MapAssetEndpoints()` trong Program.cs.
    // IEndpointRouteBuilder là kiểu chung mà ASP.NET Core dùng để đăng ký endpoint.
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        // Tất cả endpoint trong group này đều bắt đầu bằng /assets.
        // Ví dụ MapGet("/stats", ...) bên dưới sẽ thành GET /assets/stats.
        var group = app.MapGroup("/assets");

        // POST /assets
        // Tạo một asset từ JSON body.
        group.MapPost("", CreateAsset);

        // POST /assets/batch
        // Tạo nhiều asset trong một request.
        group.MapPost("/batch", BatchCreateAssets);

        // DELETE /assets/batch?ids=id1,id2
        // Xóa nhiều asset dựa trên query string `ids`.
        group.MapDelete("/batch", BatchDeleteAssets);

        // GET /assets?page=1&limit=20&type=domain&status=active
        // Lấy danh sách asset, có hỗ trợ phân trang và filter.
        group.MapGet("", ListAssets);

        // GET /assets/stats
        // Trả thống kê tổng số asset và số lượng theo type/status.
        group.MapGet("/stats", GetStats);

        // GET /assets/count?type=domain&status=active
        // Chỉ trả về số lượng asset khớp filter, không trả toàn bộ danh sách.
        group.MapGet("/count", CountAssets);

        // GET /assets/search?q=abc
        // Tìm asset theo tên.
        group.MapGet("/search", SearchAssets);

        // GET /assets/{id}
        // Lấy chi tiết một asset theo id. Route này đặt cuối để không "nuốt" các route chữ
        // như /stats, /count, /search.
        group.MapGet("/{id}", GetAssetById);

        return app;
    }

    // `request` được ASP.NET Core tự đọc từ JSON body.
    // `service` được lấy từ dependency injection đã đăng ký trong Program.cs.
    private static IResult CreateAsset(CreateAssetRequest request, IAssetService service)
    {
        try
        {
            var asset = service.Create(request);

            // HTTP 201 Created nghĩa là tạo mới thành công.
            // Header Location sẽ trỏ tới URL có thể dùng để xem asset vừa tạo.
            return Results.Created($"/assets/{asset.Id}", asset);
        }
        catch (ValidationException exception)
        {
            // ValidationException là lỗi nhập liệu sai, ví dụ thiếu name hoặc type không hợp lệ.
            // Với lỗi kiểu này API trả 400 để client biết cần sửa request.
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // Tạo nhiều asset cùng lúc.
    // Service sẽ đảm bảo kiểu all-or-nothing: nếu một item sai thì không item nào được lưu.
    private static IResult BatchCreateAssets(BatchCreateAssetsRequest request, IAssetService service)
    {
        try
        {
            var response = service.BatchCreate(request);
            return Results.Created("/assets/batch", response);
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // `ids` là query parameter, ví dụ:
    // DELETE /assets/batch?ids=abc,def,ghi
    // ASP.NET Core tự gán query parameter `ids` vào biến string? ids.
    private static IResult BatchDeleteAssets(string? ids, IAssetService service)
    {
        try
        {
            return Results.Ok(service.BatchDelete(ids));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // Các tham số page/limit/type/status cũng đến từ query string.
    // Dùng int? và string? vì client có thể không truyền, khi đó service sẽ tự dùng giá trị mặc định.
    private static IResult ListAssets(int? page, int? limit, string? type, string? status, IAssetService service)
    {
        try
        {
            return Results.Ok(service.List(page, limit, type, status));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // Endpoint thống kê không cần input từ client.
    // Handler chỉ gọi service rồi bọc kết quả bằng HTTP 200 OK.
    private static IResult GetStats(IAssetService service)
    {
        return Results.Ok(service.GetStats());
    }

    // Đếm asset theo filter. Nếu filter không truyền thì đếm tất cả.
    private static IResult CountAssets(string? type, string? status, IAssetService service)
    {
        try
        {
            return Results.Ok(service.Count(type, status));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // Tìm kiếm asset theo query parameter `q`.
    // Ví dụ: GET /assets/search?q=example
    private static IResult SearchAssets(string? q, IAssetService service)
    {
        try
        {
            return Results.Ok(service.Search(q));
        }
        catch (ValidationException exception)
        {
            return Results.BadRequest(new ApiError(exception.Message));
        }
    }

    // `id` lấy từ route /assets/{id}.
    // Nếu service trả null nghĩa là không có asset với id đó, API trả 404 Not Found.
    private static IResult GetAssetById(string id, IAssetService service)
    {
        var asset = service.GetById(id);
        return asset is null ? Results.NotFound(new ApiError("Không tìm thấy asset.")) : Results.Ok(asset);
    }
}
