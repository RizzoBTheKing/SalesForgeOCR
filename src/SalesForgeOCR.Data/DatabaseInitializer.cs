using Microsoft.Data.Sqlite;
using SalesForgeOCR.Shared.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data
{
    public class DatabaseInitializer
    {
        private readonly string _dbPath;

        public DatabaseInitializer(string? dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(Directory.GetCurrentDirectory(), Constants.DatabaseFileName);
        }

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_dbPath))!);
            var connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();

            using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();

            // Schema initialization is idempotent even for an empty or existing database.
            // Always execute CREATE IF NOT EXISTS; the file may already exist without tables.
            if (conn.State == System.Data.ConnectionState.Open)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS Documents (
    Id TEXT PRIMARY KEY,
    FileName TEXT NOT NULL,
    Size INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CurrentVersionId TEXT
);

CREATE TABLE IF NOT EXISTS DocumentVersions (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionNumber INTEGER NOT NULL,
    FileName TEXT NOT NULL,
    Size INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    CreatedBy TEXT
);

CREATE TABLE IF NOT EXISTS DocumentText (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionId TEXT NOT NULL,
    PageNumber INTEGER NOT NULL,
    Content TEXT NOT NULL,
    Confidence REAL
);

CREATE VIRTUAL TABLE IF NOT EXISTS DocumentTextFTS USING fts5(DocumentId, VersionId, PageNumber UNINDEXED, Content);

-- Unified SearchIndexFTS stores searchable fragments and maps back to documents
CREATE VIRTUAL TABLE IF NOT EXISTS SearchIndexFTS USING fts5(
    DocumentId,
    SourceRecordId,
    SourceType,
    SearchableText,
    SearchWeight UNINDEXED,
    CreatedDate UNINDEXED,
    LastModifiedDate UNINDEXED
);

CREATE TABLE IF NOT EXISTS DocumentImages (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionId TEXT NOT NULL,
    PageNumber INTEGER NOT NULL,
    FilePath TEXT NOT NULL,
    ThumbnailPath TEXT,
    ContentType TEXT,
    ExtractedAt TEXT,
    Hash TEXT
);

CREATE TABLE IF NOT EXISTS DocumentImageLocations (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    ImageId TEXT NOT NULL,
    PageNumber INTEGER NOT NULL,
    X REAL,
    Y REAL,
    Width REAL,
    Height REAL,
    ResolutionDpi INTEGER
);

CREATE TABLE IF NOT EXISTS DocumentEntities (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionId TEXT NOT NULL,
    Type TEXT NOT NULL,
    Value TEXT NOT NULL,
    Confidence REAL,
    PageNumber INTEGER
);

CREATE VIRTUAL TABLE IF NOT EXISTS EntityFTS USING fts5(Type, Value);

CREATE TABLE IF NOT EXISTS DocumentTables (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionId TEXT NOT NULL,
    PageNumber INTEGER NOT NULL,
    Html TEXT
);

CREATE TABLE IF NOT EXISTS DocumentMetadata (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    Key TEXT NOT NULL,
    Value TEXT
);

CREATE VIRTUAL TABLE IF NOT EXISTS MetadataFTS USING fts5(Key, Value);

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id TEXT PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    EventType TEXT NOT NULL,
    Actor TEXT,
    Details TEXT,
    Severity TEXT
);

CREATE TABLE IF NOT EXISTS ApplicationSettings (
    Id TEXT PRIMARY KEY,
    Key TEXT NOT NULL UNIQUE,
    Value TEXT,
    Description TEXT,
    UpdatedAt TEXT
);

CREATE TABLE IF NOT EXISTS DocumentTemplates (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    RulesJson TEXT,
    CreatedAt TEXT,
    CreatedBy TEXT
);

CREATE TABLE IF NOT EXISTS ProcessingJobs (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    VersionId TEXT NOT NULL,
    Status INTEGER NOT NULL,
    Attempt INTEGER,
    ErrorMessage TEXT,
    CreatedAt TEXT,
    StartedAt TEXT,
    CompletedAt TEXT,
    CreatedBy TEXT
);

CREATE TABLE IF NOT EXISTS UserSessions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    CreatedAt TEXT,
    ExpiresAt TEXT,
    Machine TEXT,
    IpAddress TEXT
);

CREATE INDEX IF NOT EXISTS IDX_Documents_CreatedAt ON Documents(CreatedAt);
CREATE INDEX IF NOT EXISTS IDX_DocumentText_Page ON DocumentText(DocumentId, PageNumber);
CREATE INDEX IF NOT EXISTS IDX_DocumentImages_Hash ON DocumentImages(Hash);
CREATE INDEX IF NOT EXISTS IDX_ProcessingJobs_Status ON ProcessingJobs(Status);

COMMIT;";
                await cmd.ExecuteNonQueryAsync();
            }
            using var columns = conn.CreateCommand();
            columns.CommandText = "SELECT COUNT(*) FROM pragma_table_info('DocumentVersions') WHERE name = 'StoragePath'";
            if (Convert.ToInt64(await columns.ExecuteScalarAsync()) == 0)
            {
                using var migrate = conn.CreateCommand();
                migrate.CommandText = "ALTER TABLE DocumentVersions ADD COLUMN StoragePath TEXT";
                await migrate.ExecuteNonQueryAsync();
            }
            using var imageColumns=conn.CreateCommand();
            imageColumns.CommandText="SELECT COUNT(*) FROM pragma_table_info('DocumentImages') WHERE name='Role'";
            if(Convert.ToInt64(await imageColumns.ExecuteScalarAsync())==0)
            {
                using var migrateImages=conn.CreateCommand();migrateImages.CommandText="ALTER TABLE DocumentImages ADD COLUMN Role TEXT NOT NULL DEFAULT 'Body'";
                await migrateImages.ExecuteNonQueryAsync();
            }
        }
    }
}
