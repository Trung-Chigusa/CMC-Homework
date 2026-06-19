namespace CmcHomework.Api.Models;

// Asset là "entity" chính của bài này, hiểu đơn giản là một bản ghi tài sản.
//
// `record` trong C# phù hợp cho dữ liệu vì nó tự có constructor, property get-only,
// so sánh theo giá trị và in ra thông tin dễ đọc hơn class thông thường.
//
// Các field này sẽ được trả ra JSON khi API response chứa Asset.
public sealed record Asset(
    // Id duy nhất của asset. Project đang tạo bằng Guid.NewGuid().
    string Id,

    // Tên asset, ví dụ "example.com", "192.168.1.1" hoặc "nginx".
    string Name,

    // Loại asset. Giá trị hợp lệ được khai báo trong AssetRules.AllowedTypes.
    string Type,

    // Trạng thái asset. Giá trị hợp lệ được khai báo trong AssetRules.AllowedStatuses.
    string Status,

    // Thời điểm asset được tạo. Dùng DateTimeOffset để giữ thông tin mốc thời gian rõ ràng hơn DateTime.
    DateTimeOffset CreatedAt);
