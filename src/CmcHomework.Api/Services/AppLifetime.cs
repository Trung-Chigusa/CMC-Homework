namespace CmcHomework.Api.Services;

// Lớp này ghi nhớ thời điểm ứng dụng bắt đầu chạy.
// Health endpoint dùng nó để tính uptime, tức là server đã chạy được bao nhiêu giây.
public sealed class AppLifetime
{
    public AppLifetime()
    {
        // Lưu thời điểm start theo UTC để không bị lệch do timezone.
        StartedAt = DateTimeOffset.UtcNow;
    }

    // get-only property: sau khi set trong constructor thì không đổi nữa.
    public DateTimeOffset StartedAt { get; }

    // Nhận `now` từ bên ngoài để dễ test hơn.
    // Nếu tự gọi DateTimeOffset.UtcNow trong hàm này, test sẽ khó kiểm soát thời gian.
    public long GetUptimeSeconds(DateTimeOffset now)
    {
        var seconds = (long)(now - StartedAt).TotalSeconds;

        // Math.Max bảo vệ trường hợp đồng hồ hệ thống bị chỉnh lùi làm kết quả âm.
        return Math.Max(0, seconds);
    }
}
