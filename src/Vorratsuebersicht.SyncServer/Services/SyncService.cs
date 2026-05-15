using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Services;

public class SyncService
{
    private readonly SyncDbContext _db;
    private readonly ILogger<SyncService> _logger;

    public SyncService(SyncDbContext db, ILogger<SyncService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<SyncChangeDto> PullChanges(DateTime? since = null)
    {
        since ??= DateTime.UtcNow.AddDays(-30);
        var changes = _db.GetChangesSince(since.Value);

        var result = new List<SyncChangeDto>();
        using var connection = _db.CreateConnection();
        connection.Open();

        foreach (var change in changes)
        {
            var data = GetEntityData(connection, change.EntityType, change.EntityId);
            result.Add(new SyncChangeDto
            {
                SyncChangeLogId = change.SyncChangeLogId,
                EntityType = change.EntityType,
                EntityId = change.EntityId,
                Operation = change.Operation,
                Timestamp = change.Timestamp,
                Data = data
            });
        }

        return result;
    }

    public List<PushResult> PushChanges(List<ClientChange> clientChanges)
    {
        var results = new List<PushResult>();

        using var connection = _db.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var change in clientChanges)
            {
                var result = ApplyChange(connection, change);
                results.Add(result);
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to push changes");
            throw;
        }

        return results;
    }

    private PushResult ApplyChange(SqliteConnection connection, ClientChange change)
    {
        try
        {
            switch (change.EntityType)
            {
                case "Article":
                    return ApplyArticleChange(connection, change);
                case "StorageItem":
                    return ApplyStorageItemChange(connection, change);
                case "ShoppingItem":
                    return ApplyShoppingItemChange(connection, change);
                default:
                    return new PushResult
                    {
                        ClientChangeId = change.ClientChangeId,
                        Accepted = false,
                        ConflictMessage = $"Unknown entity type: {change.EntityType}"
                    };
            }
        }
        catch (Exception ex)
        {
            return new PushResult
            {
                ClientChangeId = change.ClientChangeId,
                Accepted = false,
                ConflictMessage = ex.Message
            };
        }
    }

    private PushResult ApplyArticleChange(SqliteConnection connection, ClientChange change)
    {
        var now = DateTime.UtcNow.ToString("O");

        switch (change.Operation)
        {
            case "create":
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO Articles (Name, Manufacturer, Category, SubCategory, DurableInfinity,
                        WarnInDays, Size, Unit, Calorie, Notes, EANCode, StorageName,
                        MinQuantity, PrefQuantity, Supermarket, Price, CreatedAt, UpdatedAt)
                    VALUES ($name, $manufacturer, $category, $subCategory, $durableInfinity,
                        $warnInDays, $size, $unit, $calorie, $notes, $eanCode, $storageName,
                        $minQuantity, $prefQuantity, $supermarket, $price, $now, $now);
                    SELECT last_insert_rowid();
                    """;
                AddParameters(cmd, change.Data, now);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                _db.LogChange(connection, "Article", newId, "create", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = newId };
            }

            case "update":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    UPDATE Articles SET
                        Name = $name, Manufacturer = $manufacturer, Category = $category,
                        SubCategory = $subCategory, DurableInfinity = $durableInfinity,
                        WarnInDays = $warnInDays, Size = $size, Unit = $unit,
                        Calorie = $calorie, Notes = $notes, EANCode = $eanCode,
                        StorageName = $storageName, MinQuantity = $minQuantity,
                        PrefQuantity = $prefQuantity, Supermarket = $supermarket,
                        Price = $price, UpdatedAt = $now
                    WHERE ArticleId = $id
                    """;
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                AddParameters(cmd, change.Data, now);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "Article", change.EntityId.Value, "update", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            case "delete":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Articles WHERE ArticleId = $id";
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "Article", change.EntityId.Value, "delete", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            default:
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = $"Unknown operation: {change.Operation}" };
        }
    }

    private PushResult ApplyStorageItemChange(SqliteConnection connection, ClientChange change)
    {
        var now = DateTime.UtcNow.ToString("O");

        switch (change.Operation)
        {
            case "create":
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO StorageItems (ArticleId, Quantity, BestBeforeDate, StorageName, CreatedAt, UpdatedAt)
                    VALUES ($articleId, $quantity, $bestBeforeDate, $storageName, $now, $now);
                    SELECT last_insert_rowid();
                    """;
                AddStorageItemParameters(cmd, change.Data, now);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                _db.LogChange(connection, "StorageItem", newId, "create", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = newId };
            }

            case "update":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    UPDATE StorageItems SET
                        ArticleId = $articleId, Quantity = $quantity,
                        BestBeforeDate = $bestBeforeDate, StorageName = $storageName,
                        UpdatedAt = $now
                    WHERE StorageItemId = $id
                    """;
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                AddStorageItemParameters(cmd, change.Data, now);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "StorageItem", change.EntityId.Value, "update", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            case "delete":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM StorageItems WHERE StorageItemId = $id";
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "StorageItem", change.EntityId.Value, "delete", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            default:
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = $"Unknown operation: {change.Operation}" };
        }
    }

    private PushResult ApplyShoppingItemChange(SqliteConnection connection, ClientChange change)
    {
        var now = DateTime.UtcNow.ToString("O");

        switch (change.Operation)
        {
            case "create":
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO ShoppingItems (ArticleId, ArticleName, Quantity, IsChecked, CreatedAt, UpdatedAt)
                    VALUES ($articleId, $articleName, $quantity, $isChecked, $now, $now);
                    SELECT last_insert_rowid();
                    """;
                AddShoppingItemParameters(cmd, change.Data, now);
                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                _db.LogChange(connection, "ShoppingItem", newId, "create", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = newId };
            }

            case "update":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = """
                    UPDATE ShoppingItems SET
                        ArticleId = $articleId, ArticleName = $articleName,
                        Quantity = $quantity, IsChecked = $isChecked,
                        UpdatedAt = $now
                    WHERE ShoppingItemId = $id
                    """;
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                AddShoppingItemParameters(cmd, change.Data, now);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "ShoppingItem", change.EntityId.Value, "update", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            case "delete":
            {
                if (change.EntityId == null)
                    return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = "Missing EntityId" };

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM ShoppingItems WHERE ShoppingItemId = $id";
                cmd.Parameters.AddWithValue("$id", change.EntityId.Value);
                cmd.ExecuteNonQuery();
                _db.LogChange(connection, "ShoppingItem", change.EntityId.Value, "delete", change.ClientChangeId);
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = true, EntityId = change.EntityId };
            }

            default:
                return new PushResult { ClientChangeId = change.ClientChangeId, Accepted = false, ConflictMessage = $"Unknown operation: {change.Operation}" };
        }
    }

    private Dictionary<string, object?>? GetEntityData(SqliteConnection connection, string entityType, int entityId)
    {
        string tableName = entityType switch
        {
            "Article" => "Articles",
            "StorageItem" => "StorageItems",
            "ShoppingItem" => "ShoppingItems",
            _ => null
        };

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {tableName} WHERE {entityType}Id = $id";
        cmd.Parameters.AddWithValue("$id", entityId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var data = new Dictionary<string, object?>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            if (value is byte[] && reader.GetName(i) == "ImageData")
                value = "[binary data]";
            data[reader.GetName(i)] = value;
        }
        return data;
    }

    private static void AddParameters(SqliteCommand cmd, Dictionary<string, object?>? data, string now)
    {
        cmd.Parameters.AddWithValue("$name", GetValue(data, "Name", ""));
        cmd.Parameters.AddWithValue("$manufacturer", GetValue(data, "Manufacturer"));
        cmd.Parameters.AddWithValue("$category", GetValue(data, "Category"));
        cmd.Parameters.AddWithValue("$subCategory", GetValue(data, "SubCategory"));
        cmd.Parameters.AddWithValue("$durableInfinity", GetValue(data, "DurableInfinity", false));
        cmd.Parameters.AddWithValue("$warnInDays", GetValue(data, "WarnInDays"));
        cmd.Parameters.AddWithValue("$size", GetValue(data, "Size"));
        cmd.Parameters.AddWithValue("$unit", GetValue(data, "Unit"));
        cmd.Parameters.AddWithValue("$calorie", GetValue(data, "Calorie"));
        cmd.Parameters.AddWithValue("$notes", GetValue(data, "Notes"));
        cmd.Parameters.AddWithValue("$eanCode", GetValue(data, "EANCode"));
        cmd.Parameters.AddWithValue("$storageName", GetValue(data, "StorageName"));
        cmd.Parameters.AddWithValue("$minQuantity", GetValue(data, "MinQuantity"));
        cmd.Parameters.AddWithValue("$prefQuantity", GetValue(data, "PrefQuantity"));
        cmd.Parameters.AddWithValue("$supermarket", GetValue(data, "Supermarket"));
        cmd.Parameters.AddWithValue("$price", GetValue(data, "Price"));
        cmd.Parameters.AddWithValue("$now", now);
    }

    private static void AddStorageItemParameters(SqliteCommand cmd, Dictionary<string, object?>? data, string now)
    {
        cmd.Parameters.AddWithValue("$articleId", GetValue(data, "ArticleId", 0));
        cmd.Parameters.AddWithValue("$quantity", GetValue(data, "Quantity", 0));
        cmd.Parameters.AddWithValue("$bestBeforeDate", GetValue(data, "BestBeforeDate"));
        cmd.Parameters.AddWithValue("$storageName", GetValue(data, "StorageName"));
        cmd.Parameters.AddWithValue("$now", now);
    }

    private static void AddShoppingItemParameters(SqliteCommand cmd, Dictionary<string, object?>? data, string now)
    {
        cmd.Parameters.AddWithValue("$articleId", GetValue(data, "ArticleId", 0));
        cmd.Parameters.AddWithValue("$articleName", GetValue(data, "ArticleName"));
        cmd.Parameters.AddWithValue("$quantity", GetValue(data, "Quantity", 1));
        cmd.Parameters.AddWithValue("$isChecked", GetValue(data, "IsChecked", false));
        cmd.Parameters.AddWithValue("$now", now);
    }

    private static object GetValue(Dictionary<string, object?>? data, string key, object? defaultValue = null)
    {
        if (data == null || !data.TryGetValue(key, out var value))
            return defaultValue ?? DBNull.Value;
        return value ?? DBNull.Value;
    }
}
