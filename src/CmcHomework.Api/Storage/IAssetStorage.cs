using CmcHomework.Api.Models;

namespace CmcHomework.Api.Storage;

// Interface cho tầng lưu trữ dữ liệu.
// Service làm việc với interface này thay vì phụ thuộc trực tiếp vào SqliteAssetStorage.
// Nếu sau này đổi sang database thật, chỉ cần viết class storage mới cùng interface.
public interface IAssetStorage
{
    // Lưu một asset mới.
    Asset Create(Asset asset);

    // Lưu nhiều asset cùng lúc.
    IReadOnlyList<Asset> BatchCreate(IReadOnlyList<Asset> assets);

    // Xóa nhiều asset theo danh sách id.
    BatchDeleteAssetsResponse BatchDelete(IReadOnlyList<string> ids);

    // Lấy asset theo id. Có thể trả null nếu id không tồn tại.
    Asset? GetById(string id);

    // Lấy toàn bộ asset hiện đang lưu.
    IReadOnlyList<Asset> GetAll();

    // Đếm tổng số asset.
    int Count();
}
