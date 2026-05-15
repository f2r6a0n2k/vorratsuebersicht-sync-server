using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class ArticleEndpoints
{
    public static WebApplication MapArticleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/articles");

        group.MapGet("/", (SyncDbContext db, string? search) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            if (!string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText = "SELECT * FROM Articles WHERE Name LIKE $search OR EANCode LIKE $search ORDER BY Name";
                cmd.Parameters.AddWithValue("$search", $"%{search}%");
            }
            else
            {
                cmd.CommandText = "SELECT * FROM Articles ORDER BY Name";
            }

            var articles = new List<Article>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                articles.Add(MapArticle(reader));
            }
            return Results.Ok(articles);
        })
        .WithName("GetArticles");

        group.MapGet("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Articles WHERE ArticleId = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Results.NotFound();

            return Results.Ok(MapArticle(reader));
        })
        .WithName("GetArticle")
        ;

        group.MapPost("/", (SyncDbContext db, Article article) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

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
            AddArticleParams(cmd, article, now);
            var newId = Convert.ToInt32(cmd.ExecuteScalar());

            db.LogChange(connection, "Article", newId, "create");

            article.ArticleId = newId;
            article.CreatedAt = now;
            article.UpdatedAt = now;
            return Results.Created($"/api/articles/{newId}", article);
        })
        .WithName("CreateArticle")
        ;

        group.MapPut("/{id:int}", (SyncDbContext db, int id, Article article) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();
            var now = DateTime.UtcNow.ToString("O");

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
            cmd.Parameters.AddWithValue("$id", id);
            AddArticleParams(cmd, article, now);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "Article", id, "update");
            return Results.NoContent();
        })
        .WithName("UpdateArticle")
        ;

        group.MapDelete("/{id:int}", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Articles WHERE ArticleId = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();

            db.LogChange(connection, "Article", id, "delete");
            return Results.NoContent();
        })
        .WithName("DeleteArticle")
        ;

        group.MapGet("/{id:int}/image", (SyncDbContext db, int id) =>
        {
            using var connection = db.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ImageData, ImageMimeType FROM Articles WHERE ArticleId = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return Results.NotFound();

            if (reader.IsDBNull(0))
                return Results.NotFound();

            var imageData = (byte[])reader[0];
            var mimeType = reader.IsDBNull(1) ? "image/png" : reader.GetString(1);
            return Results.File(imageData, mimeType);
        })
        .WithName("GetArticleImage")
        ;

        return app;
    }

    private static Article MapArticle(SqliteDataReader reader)
    {
        return new Article
        {
            ArticleId = reader.GetInt32(reader.GetOrdinal("ArticleId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Manufacturer = GetStringOrNull(reader, "Manufacturer"),
            Category = GetStringOrNull(reader, "Category"),
            SubCategory = GetStringOrNull(reader, "SubCategory"),
            DurableInfinity = reader.GetBoolean(reader.GetOrdinal("DurableInfinity")),
            WarnInDays = GetIntOrNull(reader, "WarnInDays"),
            Size = GetDecimalOrNull(reader, "Size"),
            Unit = GetStringOrNull(reader, "Unit"),
            Calorie = GetIntOrNull(reader, "Calorie"),
            Notes = GetStringOrNull(reader, "Notes"),
            EANCode = GetStringOrNull(reader, "EANCode"),
            StorageName = GetStringOrNull(reader, "StorageName"),
            MinQuantity = GetIntOrNull(reader, "MinQuantity"),
            PrefQuantity = GetIntOrNull(reader, "PrefQuantity"),
            Supermarket = GetStringOrNull(reader, "Supermarket"),
            Price = GetDecimalOrNull(reader, "Price"),
            CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetString(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static void AddArticleParams(SqliteCommand cmd, Article article, string now)
    {
        cmd.Parameters.AddWithValue("$name", article.Name);
        cmd.Parameters.AddWithValue("$manufacturer", (object?)article.Manufacturer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$category", (object?)article.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$subCategory", (object?)article.SubCategory ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$durableInfinity", article.DurableInfinity ? 1 : 0);
        cmd.Parameters.AddWithValue("$warnInDays", (object?)article.WarnInDays ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$size", (object?)article.Size ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$unit", (object?)article.Unit ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$calorie", (object?)article.Calorie ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$notes", (object?)article.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$eanCode", (object?)article.EANCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$storageName", (object?)article.StorageName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$minQuantity", (object?)article.MinQuantity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prefQuantity", (object?)article.PrefQuantity ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$supermarket", (object?)article.Supermarket ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$price", (object?)article.Price ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$now", now);
    }

    private static string? GetStringOrNull(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetIntOrNull(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static decimal? GetDecimalOrNull(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }
}
