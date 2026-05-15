using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Data;

public class SyncDbContext
{
    private readonly string _connectionString;

    public SyncDbContext(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private void Initialize()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Articles (
                ArticleId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Manufacturer TEXT,
                Category TEXT,
                SubCategory TEXT,
                DurableInfinity INTEGER DEFAULT 0,
                WarnInDays INTEGER,
                Size REAL,
                Unit TEXT,
                Calorie INTEGER,
                Notes TEXT,
                EANCode TEXT,
                StorageName TEXT,
                MinQuantity INTEGER,
                PrefQuantity INTEGER,
                Supermarket TEXT,
                Price REAL,
                ImageData BLOB,
                ImageMimeType TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS StorageItems (
                StorageItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                ArticleId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 0,
                BestBeforeDate TEXT,
                StorageName TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ArticleId) REFERENCES Articles(ArticleId)
            );

            CREATE TABLE IF NOT EXISTS ShoppingItems (
                ShoppingItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                ArticleId INTEGER NOT NULL,
                ArticleName TEXT,
                Quantity INTEGER NOT NULL DEFAULT 1,
                IsChecked INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ArticleId) REFERENCES Articles(ArticleId)
            );

            CREATE TABLE IF NOT EXISTS SyncChangeLog (
                SyncChangeLogId INTEGER PRIMARY KEY AUTOINCREMENT,
                EntityType TEXT NOT NULL,
                EntityId INTEGER NOT NULL,
                Operation TEXT NOT NULL,
                Timestamp TEXT NOT NULL,
                Data TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_SyncChangeLog_Timestamp 
                ON SyncChangeLog(Timestamp);
            CREATE INDEX IF NOT EXISTS IX_SyncChangeLog_Entity 
                ON SyncChangeLog(EntityType, EntityId);
            CREATE INDEX IF NOT EXISTS IX_Articles_EANCode 
                ON Articles(EANCode);
            CREATE INDEX IF NOT EXISTS IX_StorageItems_ArticleId 
                ON StorageItems(ArticleId);
            CREATE INDEX IF NOT EXISTS IX_ShoppingItems_ArticleId 
                ON ShoppingItems(ArticleId);
            """;
        cmd.ExecuteNonQuery();
    }

    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public void LogChange(SqliteConnection connection, string entityType, int entityId, string operation, string? data = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SyncChangeLog (EntityType, EntityId, Operation, Timestamp, Data)
            VALUES ($entityType, $entityId, $operation, $timestamp, $data)
            """;
        cmd.Parameters.AddWithValue("$entityType", entityType);
        cmd.Parameters.AddWithValue("$entityId", entityId);
        cmd.Parameters.AddWithValue("$operation", operation);
        cmd.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$data", data ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<SyncChangeLog> GetChangesSince(DateTime since, int maxResults = 500)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT SyncChangeLogId, EntityType, EntityId, Operation, Timestamp, Data
            FROM SyncChangeLog
            WHERE Timestamp > $since
            ORDER BY Timestamp ASC
            LIMIT $maxResults
            """;
        cmd.Parameters.AddWithValue("$since", since.ToString("O"));
        cmd.Parameters.AddWithValue("$maxResults", maxResults);

        var changes = new List<SyncChangeLog>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            changes.Add(new SyncChangeLog
            {
                SyncChangeLogId = reader.GetInt32(0),
                EntityType = reader.GetString(1),
                EntityId = reader.GetInt32(2),
                Operation = reader.GetString(3),
                Timestamp = reader.GetString(4),
                Data = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return changes;
    }

    public void CleanupOldChanges(TimeSpan olderThan)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM SyncChangeLog WHERE Timestamp < $before";
        cmd.Parameters.AddWithValue("$before", DateTime.UtcNow.Subtract(olderThan).ToString("O"));
        cmd.ExecuteNonQuery();
    }
}
