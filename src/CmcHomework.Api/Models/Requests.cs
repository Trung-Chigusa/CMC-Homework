namespace CmcHomework.Api.Models;

// DTO là viết tắt của Data Transfer Object: object dùng để chuyển dữ liệu qua lại với API.
// Các request record dưới đây mô tả JSON body mà client gửi lên server.

// Body của POST /assets.
// Dùng string? vì client có thể không gửi field hoặc gửi null, service sẽ validate sau.
public sealed record CreateAssetRequest(string? Name, string? Type, string? Status);

// Body của POST /assets/batch.
// JSON mong đợi có dạng:
// {
//   "assets": [
//     { "name": "example.com", "type": "domain" },
//     { "name": "nginx", "type": "service", "status": "active" }
//   ]
// }
public sealed record BatchCreateAssetsRequest(IReadOnlyList<CreateAssetRequest>? Assets);
