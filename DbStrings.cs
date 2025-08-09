// DbStrings.cs
using System;
using System.Threading.Tasks;
using Npgsql;

namespace MonkeyTyper.Data
{
    public static class DbStrings
    {
        // Call this with your content
        public static async Task SetAsync(string content)
        {
            await using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            // Make sure the table exists (safe to run repeatedly)
            const string createSql = @"
                CREATE TABLE IF NOT EXISTS my_strings (
                    id INT PRIMARY KEY,
                    content TEXT NOT NULL
                );";
            await using (var create = new NpgsqlCommand(createSql, conn))
                await create.ExecuteNonQueryAsync();

            // Single-row upsert (id = 1)
            const string upsertSql = @"
                INSERT INTO my_strings (id, content)
                VALUES (1, @p)
                ON CONFLICT (id) DO UPDATE
                SET content = EXCLUDED.content;";
            await using var cmd = new NpgsqlCommand(upsertSql, conn);
            cmd.Parameters.AddWithValue("p", content ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }

        // Optional: read it back if you ever need to verify
        public static async Task<string?> GetAsync()
        {
            await using var conn = new NpgsqlConnection(BuildConnectionString());
            await conn.OpenAsync();

            const string sql = "SELECT content FROM my_strings WHERE id = 1;";
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }

        // Build a safe Npgsql connection string from Render's DATABASE_URL
        private static string BuildConnectionString()
        {
            // Prefer DATABASE_URL from Render dashboard
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrWhiteSpace(dbUrl))
            {
                // Handles postgres://user:pass@host:port/dbname
                var uri = new Uri(dbUrl);
                var userInfo = uri.UserInfo.Split(':', 2);
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Username = username,
                    Password = password,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    SslMode = SslMode.Require,
                    TrustServerCertificate = true
                };
                return builder.ToString();
            }

            // Fallback: use standard env vars if you prefer setting them individually
            var host = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
            var port = int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out var p) ? p : 5432;
            var user = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
            var pass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
            var db   = Environment.GetEnvironmentVariable("PGDATABASE") ?? "postgres";

            var fallback = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Username = user,
                Password = pass,
                Database = db,
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            return fallback.ToString();
        }
    }
}
