using System.ComponentModel.DataAnnotations;
using CmcHomework.Api.Models;
using CmcHomework.Api.Storage;

namespace CmcHomework.Api.Services;

// AssetService là tầng nghiệp vụ chính của project.
// Handler nhận HTTP request, còn Service quyết định request đó có hợp lệ không và phải xử lý ra sao.
//
// Những việc quan trọng trong file này:
// - kiểm tra dữ liệu đầu vào,
// - tạo Id và thời gian tạo asset,
// - xử lý batch create/delete,
// - lọc dữ liệu, phân trang, thống kê, tìm kiếm,
// - gọi Storage để đọc/ghi dữ liệu.
public sealed class AssetService : IAssetService
{
    // Các hằng số giúp rule được đặt tên rõ ràng thay vì dùng số "ảo" rải rác trong code.
    // Nếu đề bài đổi giới hạn, chỉ cần sửa ở đây.
    private const int MaxBatchSize = 100;
    private const int DefaultPage = 1;
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MaxSearchResults = 100;

    // Service không lưu Dictionary trực tiếp.
    // Nó giao việc lưu trữ cho IAssetStorage để code nghiệp vụ tách khỏi chi tiết lưu RAM/database.
    private readonly IAssetStorage _storage;

    public AssetService(IAssetStorage storage)
    {
        _storage = storage;
    }

    // Tạo một asset.
    // Luồng xử lý:
    // 1. validate request,
    // 2. chuẩn hóa dữ liệu và tạo Asset object,
    // 3. gửi asset sang storage để lưu.
    public Asset Create(CreateAssetRequest request)
    {
        var asset = BuildAssetAfterValidation(request);
        return _storage.Create(asset);
    }

    // Tạo nhiều asset trong một request.
    // Điểm quan trọng: hàm này làm theo nguyên tắc all-or-nothing.
    // Nghĩa là nếu danh sách có 10 item nhưng 1 item sai, API sẽ không lưu cả 10 item.
    public BatchCreateAssetsResponse BatchCreate(BatchCreateAssetsRequest request)
    {
        // `request.Assets is null` nghĩa là client không gửi field assets.
        // `Count == 0` nghĩa là có gửi nhưng danh sách rỗng.
        if (request.Assets is null || request.Assets.Count == 0)
        {
            throw new ValidationException("Danh sách assets không được để trống.");
        }

        // Giới hạn batch để tránh một request quá lớn làm app tốn RAM/thời gian xử lý.
        if (request.Assets.Count > MaxBatchSize)
        {
            throw new ValidationException($"Mỗi request chỉ được tạo tối đa {MaxBatchSize} assets.");
        }

        // All-or-nothing bước 1:
        // validate toàn bộ danh sách trước, chưa ghi gì vào storage.
        // Nếu một input sai, ValidateCreateInput sẽ throw ValidationException và vòng lặp dừng.
        foreach (var input in request.Assets)
        {
            ValidateCreateInput(input);
        }

        // All-or-nothing bước 2:
        // chỉ khi toàn bộ input hợp lệ mới build Asset object.
        // BuildAssetFromValidInput không validate lại để tránh làm trùng việc ở bước trên.
        var assets = request.Assets
            .Select(BuildAssetFromValidInput)
            .ToList();

        // Storage chịu trách nhiệm lưu cả danh sách vào Dictionary.
        var created = _storage.BatchCreate(assets);

        // Response chỉ trả số lượng tạo thành công và danh sách id,
        // đúng kiểu thường dùng cho batch endpoint.
        return new BatchCreateAssetsResponse(created.Count, created.Select(asset => asset.Id).ToList());
    }

    // Xóa nhiều asset theo query string `ids`.
    // Ví dụ client gọi: DELETE /assets/batch?ids=id1,id2,id3
    public BatchDeleteAssetsResponse BatchDelete(string? idsParam)
    {
        if (string.IsNullOrWhiteSpace(idsParam))
        {
            throw new ValidationException("Query parameter 'ids' là bắt buộc.");
        }

        // Tách chuỗi "id1,id2,id3" thành danh sách ["id1", "id2", "id3"].
        //
        // RemoveEmptyEntries: bỏ phần rỗng, ví dụ "id1,,id2".
        // TrimEntries: bỏ khoảng trắng quanh từng id.
        // Distinct: nếu client gửi trùng id thì chỉ xử lý một lần.
        var ids = idsParam
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ids.Count == 0)
        {
            throw new ValidationException("Cần truyền ít nhất 1 id hợp lệ trong query parameter 'ids'.");
        }

        return _storage.BatchDelete(ids);
    }

    // Lấy asset theo id. Service chỉ chuyển tiếp xuống storage vì ở đây không có rule validate phức tạp.
    public Asset? GetById(string id)
    {
        return _storage.GetById(id);
    }

    // Lấy danh sách asset có phân trang và filter.
    // `page`, `limit`, `type`, `status` đều optional nên dùng kiểu nullable.
    public PagedAssetsResponse List(int? page, int? limit, string? type, string? status)
    {
        // Nếu client không truyền page/limit thì dùng mặc định.
        // Toán tử ?? nghĩa là "nếu bên trái null thì lấy bên phải".
        var pageValue = page ?? DefaultPage;
        var limitValue = limit ?? DefaultLimit;

        // Trang bắt đầu từ 1 để dễ hiểu với người dùng API.
        if (pageValue < 1)
        {
            throw new ValidationException("page phải lớn hơn hoặc bằng 1.");
        }

        // limit quá nhỏ hoặc quá lớn đều không hợp lệ.
        // Giới hạn MaxLimit giúp một request không lấy quá nhiều dữ liệu cùng lúc.
        if (limitValue < 1 || limitValue > MaxLimit)
        {
            throw new ValidationException($"limit phải nằm trong khoảng 1 đến {MaxLimit}.");
        }

        // Nếu client có truyền type/status thì phải là giá trị hợp lệ.
        // Nếu không truyền thì bỏ qua filter đó.
        ValidateOptionalFilters(type, status);

        // Lấy toàn bộ asset từ storage rồi lọc theo type/status.
        // ToList() giúp materialize kết quả để các bước Count/Skip/Take dùng chung một snapshot.
        var filtered = FilterAssets(_storage.GetAll(), type, status).ToList();
        var total = filtered.Count;

        // Math.Ceiling làm tròn lên.
        // Ví dụ total=21, limit=20 thì cần 2 trang.
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limitValue);

        // Skip bỏ qua dữ liệu của các trang trước.
        // Take lấy số item của trang hiện tại.
        // Ví dụ page=2, limit=20 thì bỏ qua 20 item đầu và lấy 20 item tiếp theo.
        var data = filtered
            .Skip((pageValue - 1) * limitValue)
            .Take(limitValue)
            .ToList();

        // Response gồm data thật và thông tin pagination để client biết tổng/trang hiện tại.
        return new PagedAssetsResponse(
            data,
            new PaginationInfo(pageValue, limitValue, total, totalPages));
    }

    // Thống kê số lượng asset theo type và status.
    public AssetsStatsResponse GetStats()
    {
        var assets = _storage.GetAll();

        // Tạo dictionary ban đầu có đủ key với giá trị 0.
        // Làm vậy giúp response luôn có đủ domain/ip/service và active/inactive,
        // kể cả khi hiện tại chưa có asset nào thuộc nhóm đó.
        var byType = AssetRules.AllowedTypes.ToDictionary(type => type, _ => 0);
        var byStatus = AssetRules.AllowedStatuses.ToDictionary(status => status, _ => 0);

        // Duyệt từng asset và tăng bộ đếm tương ứng.
        foreach (var asset in assets)
        {
            byType[asset.Type]++;
            byStatus[asset.Status]++;
        }

        return new AssetsStatsResponse(assets.Count, byType, byStatus);
    }

    // Đếm số asset khớp filter.
    // Khác List ở chỗ hàm này chỉ trả count, không trả danh sách asset.
    public CountAssetsResponse Count(string? type, string? status)
    {
        ValidateOptionalFilters(type, status);

        var count = FilterAssets(_storage.GetAll(), type, status).Count();

        // Trả lại filter sau khi normalize để client biết server đã dùng điều kiện nào.
        // Nếu client không truyền filter thì field tương ứng là null.
        var filters = new CountFilters(NormalizeOrNull(type), NormalizeOrNull(status));

        return new CountAssetsResponse(count, filters);
    }

    // Tìm asset theo tên.
    // Hàm này tìm kiểu "contains", nghĩa là chỉ cần name chứa query là khớp.
    public IReadOnlyList<Asset> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ValidationException("Query parameter 'q' là bắt buộc.");
        }

        // Trim để query "  abc  " được hiểu là "abc".
        var trimmedQuery = query.Trim();

        return _storage.GetAll()
            // StringComparison.OrdinalIgnoreCase giúp tìm không phân biệt chữ hoa/thường.
            // Ví dụ "Example.com" vẫn khớp với query "example".
            .Where(asset => asset.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            // Giới hạn kết quả để response không quá lớn nếu dữ liệu nhiều.
            .Take(MaxSearchResults)
            .ToList();
    }

    // Đếm toàn bộ asset. Health endpoint dùng hàm này để báo hiện có bao nhiêu asset trong storage.
    public int CountAll()
    {
        return _storage.Count();
    }

    // Hàm lọc dùng chung cho List và Count.
    // Vì type/status là optional nên logic lọc phải hiểu:
    // - nếu filter null: không lọc theo field đó,
    // - nếu filter có giá trị: asset phải khớp chính xác giá trị đã chuẩn hóa.
    private static IEnumerable<Asset> FilterAssets(IEnumerable<Asset> assets, string? type, string? status)
    {
        var normalizedType = NormalizeOrNull(type);
        var normalizedStatus = NormalizeOrNull(status);

        return assets.Where(asset =>
            (normalizedType is null || asset.Type == normalizedType) &&
            (normalizedStatus is null || asset.Status == normalizedStatus));
    }

    // Tạo Asset từ request nhưng luôn validate trước.
    // Hàm này dùng cho create đơn lẻ để tránh quên bước validate.
    private static Asset BuildAssetAfterValidation(CreateAssetRequest request)
    {
        ValidateCreateInput(request);
        return BuildAssetFromValidInput(request);
    }

    // Biến input đã hợp lệ thành entity Asset dùng trong hệ thống.
    // Hàm này giả định request đã qua ValidateCreateInput.
    private static Asset BuildAssetFromValidInput(CreateAssetRequest request)
    {
        return new Asset(
            // GUID là chuỗi gần như không trùng, phù hợp để làm id cho asset demo.
            Guid.NewGuid().ToString(),

            // Name được trim để tránh lưu thừa khoảng trắng đầu/cuối.
            request.Name!.Trim(),

            // Type được đưa về chữ thường để dữ liệu trong storage thống nhất.
            request.Type!.Trim().ToLowerInvariant(),

            // Nếu client không truyền status thì dùng default active.
            NormalizeOrNull(request.Status) ?? AssetRules.DefaultStatus,

            // Lưu thời điểm tạo theo UTC để không phụ thuộc timezone máy chạy server.
            DateTimeOffset.UtcNow);
    }

    // Validate dữ liệu khi tạo asset.
    // Dấu `?` trong request cho biết field có thể null, nên phải tự kiểm tra trước khi dùng.
    private static void ValidateCreateInput(CreateAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("name là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new ValidationException("type là bắt buộc.");
        }

        if (!AssetRules.IsValidType(request.Type))
        {
            throw new ValidationException($"type chỉ nhận các giá trị: {AssetRules.TypeText}.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && !AssetRules.IsValidStatus(request.Status))
        {
            throw new ValidationException($"status chỉ nhận các giá trị: {AssetRules.StatusText}.");
        }
    }

    // Validate filter optional cho các endpoint list/count.
    // Nếu client không truyền filter thì hợp lệ.
    // Nếu truyền thì phải nằm trong danh sách cho phép.
    private static void ValidateOptionalFilters(string? type, string? status)
    {
        if (!string.IsNullOrWhiteSpace(type) && !AssetRules.IsValidType(type))
        {
            throw new ValidationException($"type filter chỉ nhận các giá trị: {AssetRules.TypeText}.");
        }

        if (!string.IsNullOrWhiteSpace(status) && !AssetRules.IsValidStatus(status))
        {
            throw new ValidationException($"status filter chỉ nhận các giá trị: {AssetRules.StatusText}.");
        }
    }

    // Chuẩn hóa chuỗi nhập vào:
    // - null/rỗng/toàn khoảng trắng => null,
    // - có giá trị => trim và chuyển về chữ thường.
    //
    // Hàm này giúp code so sánh type/status đơn giản và nhất quán.
    private static string? NormalizeOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }
}
