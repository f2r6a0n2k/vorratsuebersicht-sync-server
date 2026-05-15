using Microsoft.Data.Sqlite;
using Vorratsuebersicht.SyncServer.Data;
using Vorratsuebersicht.SyncServer.Models;

namespace Vorratsuebersicht.SyncServer.Endpoints;

public static class MigrateEndpoints
{
    public static WebApplication MapMigrateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapPost("/import-sqlite", async (SyncDbContext db, HttpRequest request) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Erwarte Multipart-Form mit Datei im Feld 'file'.");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file == null || file.Length == 0)
                return Results.BadRequest("Keine Datei hochgeladen.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"import_{Guid.NewGuid()}.db3");
            try
            {
                await using (var stream = File.Create(tempPath))
                    await file.CopyToAsync(stream);

                var result = ImportFromSqlite(db, tempPath);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Import fehlgeschlagen: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        })
        .WithName("ImportSqlite");

        return app;
    }

    private static ImportResult ImportFromSqlite(SyncDbContext db, string sqlitePath)
    {
        var result = new ImportResult();
        using var src = new SqliteConnection($"Data Source={sqlitePath}");
        src.Open();
        using var dst = db.CreateConnection();
        dst.Open();

        using var transaction = dst.BeginTransaction();

        try
        {
            // 1. Articles
            using var cmd = src.CreateCommand();
            cmd.CommandText = "SELECT * FROM Article ORDER BY ArticleId";
            using var reader = cmd.ExecuteReader();

            var idMap = new Dictionary<int, int>();
            while (reader.Read())
            {
                var oldId = reader.GetInt32(0);
                using var insert = dst.CreateCommand();
                insert.CommandText = """
                    INSERT INTO Articles (Name, Manufacturer, Category, SubCategory, DurableInfinity,
                        WarnInDays, Size, Unit, Calorie, Notes, EANCode, StorageName,
                        MinQuantity, PrefQuantity, Supermarket, Price, CreatedAt, UpdatedAt)
                    VALUES ($n, $m, $c, $s, $d, $w, $z, $u, $k, $nn, $e, $sn, $mn, $pn, $sm, $p, $now, $now);
                    SELECT last_insert_rowid()
                    """;
                insert.Parameters.AddWithValue("$n", reader.IsDBNull(1) ? "" : reader.GetString(1));
                insert.Parameters.AddWithValue("$m", GetDbValue(reader, 2));
                insert.Parameters.AddWithValue("$c", GetDbValue(reader, 3));
                insert.Parameters.AddWithValue("$s", GetDbValue(reader, 4));
                insert.Parameters.AddWithValue("$d", reader.GetBoolean(5) ? 1 : 0);
                insert.Parameters.AddWithValue("$w", GetDbValue(reader, 6));
                insert.Parameters.AddWithValue("$z", GetDbValue(reader, 7));
                insert.Parameters.AddWithValue("$u", GetDbValue(reader, 8));
                insert.Parameters.AddWithValue("$k", GetDbValue(reader, 9));
                insert.Parameters.AddWithValue("$nn", GetDbValue(reader, 10));
                insert.Parameters.AddWithValue("$e", GetDbValue(reader, 11));
                insert.Parameters.AddWithValue("$sn", GetDbValue(reader, 12));
                insert.Parameters.AddWithValue("$mn", GetDbValue(reader, 13));
                insert.Parameters.AddWithValue("$pn", GetDbValue(reader, 14));
                insert.Parameters.AddWithValue("$sm", GetDbValue(reader, 15));
                insert.Parameters.AddWithValue("$p", GetDbValue(reader, 16));
                insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));

                var newId = Convert.ToInt32(insert.ExecuteScalar());
                idMap[oldId] = newId;
                result.Articles++;
            }

            // 2. StorageItems
            using var cmd2 = src.CreateCommand();
            cmd2.CommandText = "SELECT * FROM StorageItem ORDER BY StorageItemId";
            using var reader2 = cmd2.ExecuteReader();

            while (reader2.Read())
            {
                var oldArticleId = reader2.GetInt32(1);
                if (!idMap.TryGetValue(oldArticleId, out var newArticleId))
                    continue;

                using var insert = dst.CreateCommand();
                insert.CommandText = """
                    INSERT INTO StorageItems (ArticleId, Quantity, BestBeforeDate, StorageName, CreatedAt, UpdatedAt)
                    VALUES ($a, $q, $b, $s, $now, $now)
                    """;
                insert.Parameters.AddWithValue("$a", newArticleId);
                insert.Parameters.AddWithValue("$q", reader2.GetInt32(2));
                insert.Parameters.AddWithValue("$b", GetDbValue(reader2, 3));
                insert.Parameters.AddWithValue("$s", GetDbValue(reader2, 4));
                insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
                insert.ExecuteNonQuery();
                result.StorageItems++;
            }

            // 3. ShoppingList
            using var cmd3 = src.CreateCommand();
            cmd3.CommandText = "SELECT * FROM ShoppingList ORDER BY ShoppingListId";
            using var reader3 = cmd3.ExecuteReader();

            while (reader3.Read())
            {
                var oldArticleId = reader3.GetInt32(1);
                if (!idMap.TryGetValue(oldArticleId, out var newArticleId))
                    continue;

                using var insert = dst.CreateCommand();
                insert.CommandText = """
                    INSERT INTO ShoppingItems (ArticleId, Quantity, IsChecked, CreatedAt, UpdatedAt)
                    VALUES ($a, $q, $b, $now, $now)
                    """;
                insert.Parameters.AddWithValue("$a", newArticleId);
                insert.Parameters.AddWithValue("$q", reader3.GetInt32(2));
                insert.Parameters.AddWithValue("$b", reader3.IsDBNull(3) ? 0 : (reader3.GetInt32(3) > 0 ? 1 : 0));
                insert.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
                insert.ExecuteNonQuery();
                result.ShoppingItems++;
            }

            transaction.Commit();

            // Log all changes
            foreach (var kv in idMap)
                db.LogChange(dst, "Article", kv.Value, "create", "migrate");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return result;
    }

    private static object GetDbValue(SqliteDataReader reader, int index)
    {
        return reader.IsDBNull(index) ? DBNull.Value : reader.GetValue(index);
    }

    public class ImportResult
    {
        public int Articles { get; set; }
        public int StorageItems { get; set; }
        public int ShoppingItems { get; set; }
    }
}
