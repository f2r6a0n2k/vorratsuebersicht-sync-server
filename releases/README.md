# Releases

## Aktuelle Version: v1.0.0 (Stable)

📦 https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/tag/v1.0.0

### Server (SyncServer) – self-contained, ~40 MB

| Datei | Plattform | Architektur |
|-------|-----------|-------------|
| `vorratsuebersicht-sync-server-windows-x64.zip`  | Windows | x64 |
| `vorratsuebersicht-sync-server-linux-x64.tar.gz` | Linux | x64 |
| `vorratsuebersicht-sync-server-linux-arm.tar.gz` | Linux ARM | ARM (RPi) |
| `vorratsuebersicht-sync-server-macos-x64.tar.gz` | macOS | Intel |
| `vorratsuebersicht-sync-server-macos-arm64.tar.gz` | macOS | Apple Silicon |

### Desktop-Client – self-contained, ~38 MB

| Datei | Plattform | Architektur |
|-------|-----------|-------------|
| `vorratsuebersicht-desktop-windows-x64.zip`  | Windows | x64 |
| `vorratsuebersicht-desktop-linux-x64.tar.gz` | Linux | x64 |
| `vorratsuebersicht-desktop-macos-x64.tar.gz` | macOS | Intel |
| `vorratsuebersicht-desktop-macos-arm64.tar.gz` | macOS | Apple Silicon |

### Web-UI – 1 MB

| Datei | Beschreibung |
|-------|-------------|
| `web-ui.zip` | index.html + CSS + JS |

### Changelog v1.0.0

- **SyncServer**: Zentrale SQLite-Datenbank im LAN, REST-API für Artikel/Lager/Einkauf
- **Desktop-Client**: Avalonia-GUI für Win/Linux/macOS mit vollem CRUD
- **Web-UI**: Browser-Oberfläche (HTML/CSS/JS) – ohne Installation nutzbar
- **Sync-Protokoll**: ChangeLog-basierte Push/Pull-Synchronisation
- **Off-Grid**: Reiner LAN-Betrieb, kein Internet nötig
- **Cross-Plattform**: Self-contained-Binaries für 5 Plattformen
