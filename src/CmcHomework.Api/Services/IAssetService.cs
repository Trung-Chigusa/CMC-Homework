using CmcHomework.Api.Models;

namespace CmcHomework.Api.Services;

// Interface mô tả những việc tầng nghiệp vụ asset có thể làm.
// Handler chỉ cần biết "service có các hàm này", không cần biết service lưu dữ liệu bằng cách nào.
// Cách viết này giúp sau này có thể đổi AssetService khác mà ít ảnh hưởng tới Handler.
public interface IAssetService
{
    // Tạo một asset mới từ dữ liệu client gửi lên.
    Asset Create(CreateAssetRequest request);

    // Tạo nhiều asset trong một lần gọi.
    BatchCreateAssetsResponse BatchCreate(BatchCreateAssetsRequest request);

    // Xóa nhiều asset theo chuỗi ids lấy từ query string, ví dụ "id1,id2,id3".
    BatchDeleteAssetsResponse BatchDelete(string? idsParam);

    // Lấy một asset theo id. Dấu ? nghĩa là có thể không tìm thấy và trả về null.
    Asset? GetById(string id);

    // Lấy danh sách asset, có phân trang và lọc optional theo type/status.
    PagedAssetsResponse List(int? page, int? limit, string? type, string? status);

    // Trả thống kê tổng số asset và phân bố theo type/status.
    AssetsStatsResponse GetStats();

    // Đếm số asset khớp filter.
    CountAssetsResponse Count(string? type, string? status);

    // Tìm asset theo tên.
    IReadOnlyList<Asset> Search(string? query);

    // Đếm toàn bộ asset, dùng cho health check.
    int CountAll();
}
