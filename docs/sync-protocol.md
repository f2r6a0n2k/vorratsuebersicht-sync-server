# Sync-Protokoll — Eigene Clients bauen

Der SyncServer verwendet ein einfaches REST/JSON-Protokoll.  
Jede Plattform (iOS, Windows-App, Web-App, IoT) kann einen eigenen Client implementieren.

## Basis-URL

```
http://<server-ip>:5191/
```

## Übersicht

| Endpunkt | Methode | Beschreibung |
|----------|---------|-------------|
| `/api/discovery` | GET | Server-Info abrufen |
| `/api/discovery/ping` | GET | Gesundheitscheck |
| `/api/articles` | GET, POST | Artikel CRUD |
| `/api/articles/{id}` | GET, PUT, DELETE | Einzelner Artikel |
| `/api/storage-items` | GET, POST | Lagerbestand CRUD |
| `/api/storage-items/{id}` | GET, PUT, DELETE | Einzelner Eintrag |
| `/api/shopping-items` | GET, POST | Einkaufszettel CRUD |
| `/api/shopping-items/{id}` | GET, PUT, DELETE | Einzelner Eintrag |
| `/api/sync/changes?since=` | GET | Änderungen abholen |
| `/api/sync/push` | POST | Änderungen hochladen |

## Datenmodelle

### Article

```json
{
  "articleId": 1,
  "name": "Milch",
  "manufacturer": "Molkerei",
  "category": "Getränke",
  "subCategory": "Milchprodukte",
  "durableInfinity": false,
  "warnInDays": 3,
  "size": 1.0,
  "unit": "L",
  "calorie": 65,
  "notes": "Vollmilch 3.5%",
  "eanCode": "4001234567890",
  "storageName": "Kühlschrank",
  "minQuantity": 2,
  "prefQuantity": 4,
  "supermarket": "Supermarkt",
  "price": 1.49,
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-15T10:30:00Z"
}
```

### StorageItem

```json
{
  "storageItemId": 1,
  "articleId": 1,
  "quantity": 3,
  "bestBeforeDate": "2026-02-15",
  "storageName": "Kühlschrank",
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-15T10:30:00Z"
}
```

### ShoppingItem

```json
{
  "shoppingItemId": 1,
  "articleId": 1,
  "articleName": "Milch",
  "quantity": 2,
  "isChecked": false,
  "createdAt": "2026-01-15T10:30:00Z",
  "updatedAt": "2026-01-15T10:30:00Z"
}
```

## Sync-Protokoll

### Pull: Änderungen abholen

```http
GET /api/sync/changes?since=2026-01-01T00:00:00Z
```

**Antwort:**
```json
[
  {
    "syncChangeLogId": 42,
    "entityType": "Article",
    "entityId": 5,
    "operation": "update",
    "timestamp": "2026-01-15T10:30:00Z",
    "data": {
      "articleId": 5,
      "name": "Milch",
      "quantity": 2,
      "...": "..."
    }
  },
  {
    "syncChangeLogId": 43,
    "entityType": "StorageItem",
    "entityId": 12,
    "operation": "create",
    "timestamp": "2026-01-15T11:00:00Z",
    "data": {
      "storageItemId": 12,
      "articleId": 5,
      "quantity": 3,
      "...": "..."
    }
  }
]
```

| Feld | Bedeutung |
|------|-----------|
| `entityType` | `Article`, `StorageItem` oder `ShoppingItem` |
| `operation` | `create`, `update` oder `delete` |
| `data` | Aktueller Zustand der Entität (bei `delete` nur die ID) |

### Push: Änderungen hochladen

```http
POST /api/sync/push
Content-Type: application/json

[
  {
    "clientChangeId": "a1b2c3d4-...",
    "entityType": "StorageItem",
    "entityId": null,
    "operation": "create",
    "data": {
      "articleId": 5,
      "quantity": 2,
      "bestBeforeDate": "2026-03-01"
    },
    "clientTimestamp": "2026-01-15T12:00:00Z"
  }
]
```

**Antwort:**
```json
[
  {
    "clientChangeId": "a1b2c3d4-...",
    "accepted": true,
    "entityId": 15,
    "conflictMessage": null
  }
]
```

| Feld | Bedeutung |
|------|-----------|
| `clientChangeId` | Eindeutige ID der Änderung (vom Client) |
| `accepted` | `true` = übernommen, `false` = abgelehnt |
| `entityId` | Vom Server vergebene ID (bei `create`) |
| `conflictMessage` | Fehlermeldung bei Konflikt |

## Client-Implementierungen (Beispiele)

### JavaScript (Browser)

```javascript
class SyncClient {
  constructor(serverUrl) {
    this.base = serverUrl.replace(/\/+$/, '');
  }

  async getArticles() {
    const res = await fetch(`${this.base}/api/articles`);
    return res.json();
  }

  async createArticle(article) {
    const res = await fetch(`${this.base}/api/articles`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(article)
    });
    return res.json();
  }

  async pullChanges(since) {
    const res = await fetch(`${this.base}/api/sync/changes?since=${encodeURIComponent(since)}`);
    return res.json();
  }

  async pushChanges(changes) {
    const res = await fetch(`${this.base}/api/sync/push`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(changes)
    });
    return res.json();
  }
}

// Verwendung
const client = new SyncClient('http://192.168.1.42:5191');
const articles = await client.getArticles();
```

### Python

```python
import requests
import json

class SyncClient:
    def __init__(self, server_url):
        self.base = server_url.rstrip('/')

    def get_articles(self):
        r = requests.get(f'{self.base}/api/articles')
        return r.json()

    def create_article(self, article):
        r = requests.post(
            f'{self.base}/api/articles',
            json=article,
            headers={'Content-Type': 'application/json'}
        )
        return r.json()

    def pull_changes(self, since):
        r = requests.get(f'{self.base}/api/sync/changes', params={'since': since})
        return r.json()

    def push_changes(self, changes):
        r = requests.post(
            f'{self.base}/api/sync/push',
            json=changes,
            headers={'Content-Type': 'application/json'}
        )
        return r.json()

# Verwendung
client = SyncClient('http://192.168.1.42:5191')
articles = client.get_articles()
```

### Swift (iOS)

```swift
import Foundation

class SyncClient {
    let base: URL

    init(serverUrl: String) {
        var url = serverUrl
        if url.hasSuffix("/") { url = String(url.dropLast()) }
        self.base = URL(string: url)!
    }

    func getArticles() async throws -> [Article] {
        let url = base.appendingPathComponent("/api/articles")
        let (data, _) = try await URLSession.shared.data(from: url)
        return try JSONDecoder().decode([Article].self, from: data)
    }

    func createArticle(_ article: Article) async throws -> Article {
        let url = base.appendingPathComponent("/api/articles")
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(article)
        let (data, _) = try await URLSession.shared.data(for: request)
        return try JSONDecoder().decode(Article.self, from: data)
    }
}
```

### C# (.NET MAUI / WPF / WinForms)

```csharp
using System.Net.Http.Json;

public class SyncClient
{
    private readonly HttpClient _http = new();
    private readonly string _base;

    public SyncClient(string serverUrl)
    {
        _base = serverUrl.TrimEnd('/');
    }

    public async Task<List<Article>> GetArticles()
    {
        return await _http.GetFromJsonAsync<List<Article>>(
            $"{_base}/api/articles") ?? new();
    }

    public async Task<Article> CreateArticle(Article article)
    {
        var response = await _http.PostAsJsonAsync(
            $"{_base}/api/articles", article);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Article>();
    }
}
```

## Fehlerbehandlung

| Statuscode | Bedeutung |
|-----------|-----------|
| `200 OK` | Erfolg |
| `201 Created` | Erfolg bei `POST` (neue Ressource) |
| `204 No Content` | Erfolg bei `PUT`/`DELETE` |
| `404 Not Found` | Ressource nicht gefunden |
| `500 Internal Server Error` | Serverfehler |

Alle Endpunkte sind zustandslos — der Server speichert keine Client-Sitzungen.
