using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class SettingsRepository
    {
        private readonly string _dbPath;
        public SettingsRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task<ApplicationSetting?> GetAsync(string key)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Key, Value, Description, UpdatedAt FROM ApplicationSettings WHERE Key = $key LIMIT 1";
            cmd.Parameters.AddWithValue("$key", key);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ApplicationSetting
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Key = reader.GetString(1),
                    Value = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    UpdatedAt = reader.IsDBNull(4) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(4))
                };
            }
            return null;
        }

        public async Task SetAsync(ApplicationSetting setting)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO ApplicationSettings (Id, Key, Value, Description, UpdatedAt)
VALUES ($id, $key, $value, $desc, $updated)
ON CONFLICT(Key) DO UPDATE SET Value = $value, Description = $desc, UpdatedAt = $updated;";
            cmd.Parameters.AddWithValue("$id", setting.Id.ToString());
            cmd.Parameters.AddWithValue("$key", setting.Key);
            cmd.Parameters.AddWithValue("$value", setting.Value ?? string.Empty);
            cmd.Parameters.AddWithValue("$desc", setting.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("$updated", setting.UpdatedAt.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<ApplicationSetting>> GetAllAsync()
        {
            var list = new List<ApplicationSetting>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Key, Value, Description, UpdatedAt FROM ApplicationSettings";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ApplicationSetting
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Key = reader.GetString(1),
                    Value = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    UpdatedAt = reader.IsDBNull(4) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(4))
                });
            }
            return list;
        }
    }
}
