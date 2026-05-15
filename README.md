# Vorratsübersicht SyncServer

**LAN-Synchronisationsserver für die [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) Android-App**

[![Release](https://img.shields.io/github/v/release/f2r6a0n2k/vorratsuebersicht-sync-server?label=v1.0.0&color=success)](https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases)
[![Build](https://img.shields.io/github/actions/workflow/status/f2r6a0n2k/vorratsuebersicht-sync-server/build.yml?branch=main)](https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/actions)
[![License](https://img.shields.io/github/license/f2r6a0n2k/vorratsuebersicht-sync-server)](LICENSE)

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

## Installationsanleitungen (plattformspezifisch)

| Plattform | Anleitung | Autostart | Paket |
|-----------|-----------|-----------|-------|
| 🪟 **Windows** | [Anleitung](docs/installation-windows.md) | Windows-Dienst | `.zip` (EXE) |
| 🐧 **Linux** | [Anleitung](docs/installation-linux.md) | systemd | `.tar.gz` (binary) |
| 🍎 **macOS** | [Anleitung](docs/installation-macos.md) | launchd | `.tar.gz` (binary) |
| 🥧 **Raspberry Pi** | [Anleitung](docs/installation-raspberry-pi.md) | systemd | `.tar.gz` (binary) |
| 🐳 **Docker** | [Anleitung](docs/installation-docker.md) | Container | ghcr.io |
| 📱 **Android-App** | [Integrationsanleitung](docs/integration-android.md) | — | Quelltext |
| 🌐 **Sync-Protokoll** | [Protokoll-Doku](docs/sync-protocol.md) | — | — |

**Kurzbefehle für den Schnellstart:**

```bash
# Linux/macOS — Einzeiler-Installation
curl -sL https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/install.sh | bash

# Docker
docker run -d --name vorratsync -p 5191:5191 ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server

# Selbst bauen
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
dotnet run --project src/Vorratsuebersicht.SyncServer
```

→ Binäre Downloads (self-contained, ohne .NET SDK): [Releases](https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases)

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

## Client-Integration

| Plattform | Anleitung |
|-----------|-----------|
| 📱 Android (Xamarin) | [Integrationsanleitung](docs/integration-android.md) — bestehende App anbinden |
| 🌐 Alle Plattformen | [Sync-Protokoll](docs/sync-protocol.md) — eigenen Client bauen (JS, Python, Swift, C#) |

## Lizenz

Apache License 2.0 — siehe [LICENSE](LICENSE).

## Verwandte Projekte

- [Vorratsübersicht](https://github.com/Stryi/Vorratsuebersicht) — Originale Android-App von Stryi
