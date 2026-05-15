using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class StorageItemEndpoints
{
    public static WebApplication MapStorageItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/storage-items");

        group.MapGet("/", (SyncDbContext db) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT s.*, a.Name AS ArticleName FROM StorageItems s LEFT JOIN Articles a ON s.ArticleId = a.ArticleId ORDER BY a.Name";

            var items = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new
                {
                    StorageItemId = reader.GetInt32(0),
                    ArticleId = reader.GetInt32(1),
                    Quantity = reader.GetInt32(2),
                    BestBeforeDate = reader.IsDBNull(3) ? null : reader.GetString(3),
                    StorageName = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CreatedAt = reader.GetString(5),
                    UpdatedAt = reader.GetString(6),
                    ArticleName = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }
            return Results.Ok(items);
        })
        .WithName("GetStorageItems")
        ;

        group.MapGet("/by-article/{articleId:int}", (SyncDbContext db, int articleId) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM StorageItems WHERE ArticleId = $articleId ORDER BY BestBeforeDate";
            cmd.Parameters.AddWithValue("$articleId", articleId);

            var items = new List<StorageItem>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(MapStorageItem(reader));
            }
            return Results.Ok(items);
        })
        .WithName("GetStorageItemsByArticle")
        ;

        group.MapGet("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM StorageItems WHERE StorageItemId = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Results.NotFound();

            return Results.Ok(MapStorageItem(reader));
        })
        .WithName("GetStorageItem")
        ;

        group.MapPost("/", (SyncDbContext db, StorageItem item) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO StorageItems (ArticleId, Quantity, BestBeforeDate, StorageName, CreatedAt, UpdatedAt)
                VALUES ($articleId, $quantity, $bestBeforeDate, $storageName, $now, $now);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$articleId", item.ArticleId);
            cmd.Parameters.AddWithValue("$quantity", item.Quantity);
            cmd.Parameters.AddWithValue("$bestBeforeDate", (object?)item.BestBeforeDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$storageName", (object?)item.StorageName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);

            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            db.LogChange(connection, "StorageItem", newId, "create");

            item.StorageItemId = newId;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            return Results.Created($"/api/storage-items/{newId}", item);
        })
        .WithName("CreateStorageItem")
        ;

        group.MapPut("/{id:int}", (SyncDbContext db, int id, StorageItem item) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE StorageItems SET
                    ArticleId = $articleId, Quantity = $quantity,
                    BestBeforeDate = $bestBeforeDate, StorageName = $storageName,
                    UpdatedAt = $now
                WHERE StorageItemId = $id
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$articleId", item.ArticleId);
            cmd.Parameters.AddWithValue("$quantity", item.Quantity);
            cmd.Parameters.AddWithValue("$bestBeforeDate", (object?)item.BestBeforeDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$storageName", (object?)item.StorageName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "StorageItem", id, "update");
            return Results.NoContent();
        })
        .WithName("UpdateStorageItem")
        ;

        group.MapDelete("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM StorageItems WHERE StorageItemId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "StorageItem", id, "delete");
            return Results.NoContent();
        })
        .WithName("DeleteStorageItem")
        ;

        return app;
    }

    private static StorageItem MapStorageItem(SqliteDataReader reader)
    {
        return new StorageItem
        {
            StorageItemId = reader.GetInt32(reader.GetOrdinal("StorageItemId")),
            ArticleId = reader.GetInt32(reader.GetOrdinal("ArticleId")),
            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
            BestBeforeDate = reader.IsDBNull(reader.GetOrdinal("BestBeforeDate")) ? null : reader.GetString(reader.GetOrdinal("BestBeforeDate")),
            StorageName = reader.IsDBNull(reader.GetOrdinal("StorageName")) ? null : reader.GetString(reader.GetOrdinal("StorageName")),
            CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetString(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
