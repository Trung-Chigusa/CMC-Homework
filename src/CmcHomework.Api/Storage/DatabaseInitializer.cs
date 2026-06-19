namespace CmcHomework.Api.Storage;

// DatabaseInitializer đóng vai trò "migration" đơn giản cho bài tập.
// Khi app khởi động, lớp này tạo các bảng còn thiếu để người chạy không cần cài thêm tool migrate.
public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS assets (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                type TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_assets_type_status
            ON assets(type, status);

            CREATE TABLE IF NOT EXISTS scan_jobs (
                id TEXT PRIMARY KEY,
                asset_id TEXT NOT NULL,
                scan_type TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at TEXT NOT NULL,
                ended_at TEXT NULL,
                error TEXT NOT NULL DEFAULT '',
                results INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                FOREIGN KEY(asset_id) REFERENCES assets(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_scan_jobs_asset_id
            ON scan_jobs(asset_id);

            CREATE TABLE IF NOT EXISTS scan_results (
                id TEXT PRIMARY KEY,
                job_id TEXT NOT NULL,
                asset_id TEXT NOT NULL,
                scan_type TEXT NOT NULL,
                data_json TEXT NOT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY(job_id) REFERENCES scan_jobs(id) ON DELETE CASCADE,
                FOREIGN KEY(asset_id) REFERENCES assets(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_scan_results_job_id
            ON scan_results(job_id);

            CREATE INDEX IF NOT EXISTS idx_scan_results_asset_id
            ON scan_results(asset_id);
            """;

        command.ExecuteNonQuery();
    }
}
