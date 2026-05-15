# Vorratsübersicht SyncServer

**LAN-Synchronisationsserver für die [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) Android-App**

Ein leichtgewichtiger ASP.NET-Core-Server, der als zentrale Datenbank im lokalen Netzwerk dient. Ermöglicht die gemeinsame Nutzung von Artikelstamm, Lagerbestand und Einkaufszettel über mehrere Geräte hinweg — **ohne Internet**, nur im LAN.

## Architektur

```
┌──────────────────────────────────────────────┐
│  SyncServer (Raspberry Pi / alter Laptop)    │
│  ┌────────────────────────────────────────┐  │
│  │ ASP.NET Core Minimal API + SQLite      │  │
│  │ REST: CRUD für Artikel/Lager/Einkauf   │  │
│  │ Sync: Änderungsprotokoll + Push/Pull   │  │
│  │ Web UI: Browser-Oberfläche             │  │
│  └────────────────────────────────────────┘  │
└──────────────────┬───────────────────────────┘
                    │  LAN (kein Internet)
    ┌───────────────┼───────────────┐
    ▼               ▼               ▼
┌──────────┐  ┌──────────┐  ┌──────────────┐
│ Android  │  │ Windows  │  │ Browser      │
│ (besteh. │  │ / Linux  │  │ HTML/JS      │
│ App)     │  │ (MAUI)   │  │ (Web UI)     │
└──────────┘  └──────────┘  └──────────────┘
```

### Warum diese Architektur?

Das ursprüngliche Konzept in `ReadMe - Verteilte Datenbanken.txt` sah eine direkte Master-Slave-Replikation per WifiManager vor. Das scheitert jedoch an:

1. **WifiManager** ist Android-only → kein iOS/Windows/Linux
2. **Fehlende Zeitstempel** → Konflikte nicht auflösbar
3. **Keine Browser-Unterstützung** → keine universelle Zugänglichkeit
4. **Bilder-Sync** nicht adressiert

Der SyncServer löst dies durch einen plattformunabhängigen REST-API-Ansatz. Jeder Client kommuniziert per HTTP — egal ob Android, iOS, Windows, Linux oder Browser.

## Features

- **REST-API** für Artikel, Lagerbestand und Einkaufszettel
- **Änderungsprotokoll** (ChangeLog) für Synchronisation
- **Sync-Push/Pull** — Clients können Änderungen abholen und senden
- **Integrierte Web-Oberfläche** — Nutzung direkt im Browser
- **SQLite-Datenbank** — kein separates DBMS nötig
- **Off-Grid** — reiner LAN-Betrieb, kein Internet erforderlich
- **Cross-Plattform** — Server läuft auf Windows, Linux, macOS, Raspberry Pi

## Installation

### Installationsskript (Linux/macOS/WSL)

```bash
curl -sL https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/install.sh | bash
cd ~/vorratsuebersicht-sync-server
./Vorratsuebersicht.SyncServer
```

### Oder: Binär von GitHub Releases (alle Plattformen)

Lade das passende Paket von [Releases](https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases) herunter:

| Plattform | Architektur | Datei |
|-----------|-------------|-------|
| Windows | x64 | `vorratsuebersicht-sync-server-win-x64.zip` |
| Linux | x64 | `vorratsuebersicht-sync-server-linux-x64.tar.gz` |
| Linux | ARM (RPi) | `vorratsuebersicht-sync-server-linux-arm.tar.gz` |
| macOS | Intel | `vorratsuebersicht-sync-server-osx-x64.tar.gz` |
| macOS | Apple Silicon | `vorratsuebersicht-sync-server-osx-arm64.tar.gz` |

Einfach entpacken und starten — **kein .NET SDK nötig** (self-contained).

### Docker

```bash
docker run -d --name vorratsync -p 5191:5191 ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```

Oder mit docker-compose:

```bash
curl -sLO https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/docker-compose.yml
docker compose up -d
```

### Selbst bauen (mit .NET SDK)

```bash
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
dotnet run --project src/Vorratsuebersicht.SyncServer
```

Nach dem Start erscheint eine Übersicht mit allen LAN-IP-Adressen und Endpunkten. Der Server ist dann im gesamten lokalen Netzwerk erreichbar.

## API-Endpunkte

### Discovery

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/api/discovery` | Server-Informationen (Name, IPs, Version) |
| GET | `/api/discovery/ping` | Gesundheitscheck |

### Artikel (Article)

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/api/articles` | Alle Artikel (optional `?search=...`) |
| GET | `/api/articles/{id}` | Einzelnen Artikel abrufen |
| POST | `/api/articles` | Neuen Artikel anlegen |
| PUT | `/api/articles/{id}` | Artikel aktualisieren |
| DELETE | `/api/articles/{id}` | Artikel löschen |
| GET | `/api/articles/{id}/image` | Artikelbild abrufen |

### Lagerbestand (StorageItem)

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/api/storage-items` | Alle Lagerbestände |
| GET | `/api/storage-items/{id}` | Einzelnen Eintrag |
| GET | `/api/storage-items/by-article/{articleId}` | Einträge eines Artikels |
| POST | `/api/storage-items` | Neuen Lagerzugang |
| PUT | `/api/storage-items/{id}` | Bestand aktualisieren |
| DELETE | `/api/storage-items/{id}` | Eintrag löschen |

### Einkaufszettel (ShoppingItem)

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/api/shopping-items` | Alle Einkaufszettel-Einträge |
| GET | `/api/shopping-items/{id}` | Einzelnen Eintrag |
| POST | `/api/shopping-items` | Neuen Eintrag |
| PUT | `/api/shopping-items/{id}` | Eintrag aktualisieren |
| DELETE | `/api/shopping-items/{id}` | Eintrag löschen |

### Sync

| Methode | Pfad | Beschreibung |
|---------|------|-------------|
| GET | `/api/sync/changes?since={ISO8601}` | Änderungen seit Zeitpunkt abrufen |
| POST | `/api/sync/push` | Eigene Änderungen hochladen |

## Sync-Protokoll

### Pull (Änderungen abholen)

```
GET /api/sync/changes?since=2026-01-01T00:00:00Z

Response:
[
  {
    "syncChangeLogId": 42,
    "entityType": "Article",
    "entityId": 5,
    "operation": "update",
    "timestamp": "2026-01-15T10:30:00Z",
    "data": { "articleId": 5, "name": "Milch", ... }
  }
]
```

### Push (Änderungen hochladen)

```
POST /api/sync/push

Body:
[
  {
    "clientChangeId": "uuid-123",
    "entityType": "Article",
    "entityId": null,
    "operation": "create",
    "data": { "name": "Milch", "category": "Getränke" },
    "clientTimestamp": "2026-01-15T10:30:00Z"
  }
]

Response:
[
  {
    "clientChangeId": "uuid-123",
    "accepted": true,
    "entityId": 42,
    "conflictMessage": null
  }
]
```

## In die Android-App integrieren

Der SyncServer kann als Backend für die bestehende [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) Android-App dienen. Dazu muss die App einen SyncService erhalten, der:

1. Bei jeder Änderung die Daten lokal speichert **und** ans REST-API sendet
2. Periodisch `/api/sync/changes?since=...` abruft
3. Die empfangenen Änderungen in die lokale SQLite-Datenbank einspielt

Siehe [`SyncService.cs`](src/Vorratsuebersicht.SyncServer/Services/SyncService.cs) für die serverseitige Referenzimplementierung.

## Eigenen Sync-Client bauen

Der Server verwendet ein einfaches HTTP-Protokoll. Jede Plattform kann einen Client implementieren:

```csharp
// Beispiel: Änderungen abrufen (C#)
var json = await httpClient.GetStringAsync(
    "http://192.168.1.42:5191/api/sync/changes?since=2026-01-01T00:00:00Z");
var changes = JsonSerializer.Deserialize<List<SyncChangeDto>>(json);
```

```javascript
// Beispiel: Artikel abrufen (JavaScript)
fetch('http://192.168.1.42:5191/api/articles')
  .then(r => r.json())
  .then(articles => console.log(articles));
```

## Lizenz

Apache License 2.0 — siehe [LICENSE](LICENSE).

## Verwandte Projekte

- [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) — Originale Android-App von Stryi
