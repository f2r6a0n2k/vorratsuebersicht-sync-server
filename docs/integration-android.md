# Integration mit der Android-App

Diese Anleitung beschreibt, wie die bestehende [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) Android-App an den SyncServer angebunden werden kann.

## Voraussetzung

- Die [Vorratsübersicht Android-App](https://github.com/Stryi/Vorratsuebersicht) (Xamarin.Android, C#)
- Visual Studio 2022 mit Xamarin-Workload
- Ein laufender SyncServer im LAN

## Architektur

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Android App    │     │  SyncServer     │     │  Android App    │
│  (lokale SQLite)│◄───►│  (Master-DB)    │◄───►│  (Gerät 2)      │
│  + SyncClient   │     │  REST + Sync    │     │  + SyncClient   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Notwendige Änderungen an der Android-App

### 1. NuGet-Paket hinzufügen

Füge `Newtonsoft.Json` (bereits vorhanden) oder `System.Text.Json` zur `Vorratsübersicht.csproj` hinzu:

```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

Für HTTP-Anfragen wird der bereits in Xamarin.Android enthaltene `HttpClient` verwendet.

### 2. Neue Klasse: `SyncClient.cs`

Erstelle eine neue Datei `Service/SyncClient.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VorratsUebersicht
{
    public class SyncClient
    {
        private readonly HttpClient _http;
        private string _serverUrl;
        private DateTime _lastSync;

        public SyncClient()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _lastSync = DateTime.UtcNow.AddDays(-30);
        }

        public void Configure(string serverUrl)
        {
            _serverUrl = serverUrl.TrimEnd('/');
        }

        public async Task<List<Article>> PullArticles()
        {
            var json = await _http.GetStringAsync($"{_serverUrl}/api/articles");
            return JsonSerializer.Deserialize<List<Article>>(json);
        }

        public async Task PushArticle(Article article)
        {
            var json = JsonSerializer.Serialize(article);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (article.ArticleId == 0)
            {
                var response = await _http.PostAsync($"{_serverUrl}/api/articles", content);
                response.EnsureSuccessStatusCode();
            }
            else
            {
                var response = await _http.PutAsync(
                    $"{_serverUrl}/api/articles/{article.ArticleId}", content);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<List<SyncChange>> PullChanges(DateTime since)
        {
            var url = $"{_serverUrl}/api/sync/changes?since={since:O}";
            var json = await _http.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<SyncChange>>(json);
        }

        public async Task<List<PushResult>> PushChanges(List<ClientChange> changes)
        {
            var json = JsonSerializer.Serialize(changes);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"{_serverUrl}/api/sync/push", content);
            response.EnsureSuccessStatusCode();
            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PushResult>>(resultJson);
        }
    }

    public class SyncChange
    {
        public int SyncChangeLogId { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string Operation { get; set; }
        public string Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class ClientChange
    {
        public string ClientChangeId { get; set; } = Guid.NewGuid().ToString();
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Operation { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public string ClientTimestamp { get; set; } = DateTime.UtcNow.ToString("O");
    }

    public class PushResult
    {
        public string ClientChangeId { get; set; }
        public bool Accepted { get; set; }
        public int? EntityId { get; set; }
        public string ConflictMessage { get; set; }
    }
}
```

### 3. Bestehende Database.cs erweitern

Füge Änderungsverfolgung zu den wichtigsten Methoden hinzu:

```csharp
public class Android_Database
{
    // Bestehende Methoden bleiben unverändert

    public string GetLastSyncTimestamp()
    {
        return Settings.GetString("LastSyncTimestamp", DateTime.UtcNow.AddDays(-30).ToString("O"));
    }

    public void SetLastSyncTimestamp(string timestamp)
    {
        Settings.SetString("LastSyncTimestamp", timestamp);
    }

    public List<ClientChange> GetPendingChanges()
    {
        // Lese alle Datensätze, die seit dem letzten Sync geändert wurden
        var lastSync = GetLastSyncTimestamp();
        var changes = new List<ClientChange>();

        using var conn = new SQLiteConnection(GetDatabasePath());
        var articles = conn.Query<Article>(
            "SELECT * FROM Article WHERE UpdatedAt > ?", lastSync);
        foreach (var a in articles)
        {
            changes.Add(new ClientChange
            {
                EntityType = "Article",
                EntityId = a.ArticleId,
                Operation = "update",
                Data = new Dictionary<string, object>
                {
                    ["Name"] = a.Name,
                    ["Manufacturer"] = a.Manufacturer,
                    // ... weitere Felder
                }
            });
        }
        return changes;
    }
}
```

### 4. Sync in den Einstellungen aktivieren

In `SettingsActivity.cs` ein neues Preference-Feld hinzufügen:

- Checkbox: "SyncServer aktivieren"
- Textfeld: "Server-URL (z.B. http://192.168.1.42:5191)"
- Button: "Jetzt synchronisieren"

### 5. SyncService im Hintergrund

In `Service/PeriodicBackgroundService.cs` einen periodischen Sync einbauen:

```csharp
protected override async void OnHandleIntent(Intent intent)
{
    if (!Settings.GetBool("SyncEnabled", false))
        return;

    var client = new SyncClient();
    client.Configure(Settings.GetString("SyncServerUrl", ""));

    try
    {
        // Push lokale Änderungen
        var localChanges = database.GetPendingChanges();
        if (localChanges.Count > 0)
        {
            var results = await client.PushChanges(localChanges);
            // Ergebnisse loggen
        }

        // Pull Änderungen vom Server
        var lastSync = database.GetLastSyncTimestamp();
        var remoteChanges = await client.PullChanges(DateTime.Parse(lastSync));

        // Änderungen in lokale DB einspielen
        foreach (var change in remoteChanges)
        {
            ApplyRemoteChange(change);
        }

        database.SetLastSyncTimestamp(DateTime.UtcNow.ToString("O"));
    }
    catch (Exception ex)
    {
        Logging.Error("Sync fehlgeschlagen", ex);
    }
}
```

## Server-URL ermitteln

Am einfachsten per QR-Code:

1. Auf einem Gerät den SyncServer starten
2. Auf der Web-Oberfläche (`http://<ip>:5191/`) wird die Server-URL angezeigt
3. QR-Code generieren (z.B. `http://192.168.1.42:5191`)
4. In der App per QR-Scanner (ZXing) die URL übernehmen

Oder manuell: IP im LAN ermitteln (`ip addr`, `ifconfig`) und in den App-Einstellungen eintragen.

## Test

```bash
# Server starten
dotnet run --project src/Vorratsuebersicht.SyncServer

# API testen
curl http://localhost:5191/api/discovery
```
