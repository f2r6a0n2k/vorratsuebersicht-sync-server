using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class ShoppingItemEndpoints
{
    public static WebApplication MapShoppingItemEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/shopping-items");

        group.MapGet("/", (SyncDbContext db) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT s.*, a.Name AS ArticleName FROM ShoppingItems s LEFT JOIN Articles a ON s.ArticleId = a.ArticleId ORDER BY s.IsChecked, a.Name";

            var items = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new
                {
                    ShoppingItemId = reader.GetInt32(0),
                    ArticleId = reader.GetInt32(1),
                    ArticleName = reader.IsDBNull(7) ? (reader.IsDBNull(2) ? null : reader.GetString(2)) : reader.GetString(7),
                    Quantity = reader.GetInt32(3),
                    IsChecked = reader.GetBoolean(4),
                    CreatedAt = reader.GetString(5),
                    UpdatedAt = reader.GetString(6)
                });
            }
            return Results.Ok(items);
        })
        .WithName("GetShoppingItems")
        ;

        group.MapGet("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ShoppingItems WHERE ShoppingItemId = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Results.NotFound();

            return Results.Ok(MapShoppingItem(reader));
        })
        .WithName("GetShoppingItem")
        ;

        group.MapPost("/", (SyncDbContext db, ShoppingItem item) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ShoppingItems (ArticleId, ArticleName, Quantity, IsChecked, CreatedAt, UpdatedAt)
                VALUES ($articleId, $articleName, $quantity, $isChecked, $now, $now);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$articleId", item.ArticleId);
            cmd.Parameters.AddWithValue("$articleName", (object?)item.ArticleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$quantity", item.Quantity);
            cmd.Parameters.AddWithValue("$isChecked", item.IsChecked ? 1 : 0);
            cmd.Parameters.AddWithValue("$now", now);

            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            db.LogChange(connection, "ShoppingItem", newId, "create");

            item.ShoppingItemId = newId;
            item.CreatedAt = now;
            item.UpdatedAt = now;
            return Results.Created($"/api/shopping-items/{newId}", item);
        })
        .WithName("CreateShoppingItem")
        ;

        group.MapPut("/{id:int}", (SyncDbContext db, int id, ShoppingItem item) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                UPDATE ShoppingItems SET
                    ArticleId = $articleId, ArticleName = $articleName,
                    Quantity = $quantity, IsChecked = $isChecked,
                    UpdatedAt = $now
                WHERE ShoppingItemId = $id
                """;
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$articleId", item.ArticleId);
            cmd.Parameters.AddWithValue("$articleName", (object?)item.ArticleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$quantity", item.Quantity);
            cmd.Parameters.AddWithValue("$isChecked", item.IsChecked ? 1 : 0);
            cmd.Parameters.AddWithValue("$now", now);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "ShoppingItem", id, "update");
            return Results.NoContent();
        })
        .WithName("UpdateShoppingItem")
        ;

        group.MapDelete("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ShoppingItems WHERE ShoppingItemId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "ShoppingItem", id, "delete");
            return Results.NoContent();
        })
        .WithName("DeleteShoppingItem")
        ;

        return app;
    }

    private static ShoppingItem MapShoppingItem(SqliteDataReader reader)
    {
        return new ShoppingItem
        {
            ShoppingItemId = reader.GetInt32(reader.GetOrdinal("ShoppingItemId")),
            ArticleId = reader.GetInt32(reader.GetOrdinal("ArticleId")),
            ArticleName = reader.IsDBNull(reader.GetOrdinal("ArticleName")) ? null : reader.GetString(reader.GetOrdinal("ArticleName")),
            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
            IsChecked = reader.GetBoolean(reader.GetOrdinal("IsChecked")),
            CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetString(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
