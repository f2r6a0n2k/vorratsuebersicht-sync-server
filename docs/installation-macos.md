# Installation unter macOS

## Variante A: Binär

Je nach Prozessor das passende Paket herunterladen:

| Prozessor | Paket |
|-----------|-------|
| Intel (2019 und älter) | `vorratsuebersicht-sync-server-osx-x64.tar.gz` |
| Apple Silicon (M1/M2/M3/M4) | `vorratsuebersicht-sync-server-osx-arm64.tar.gz` |

```bash
# Falsches Paket heruntergeladen? Mit diesem Befehl das richtige ermitteln:
# uname -m  → x86_64 = Intel, arm64 = Apple Silicon

curl -sL https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/latest/download/vorratsuebersicht-sync-server-osx-arm64.tar.gz \
  -o /tmp/syncserver.tar.gz

# Entpacken nach /Applications
sudo mkdir -p /Applications/Vorratsync
sudo tar xzf /tmp/syncserver.tar.gz -C /Applications/Vorratsync
sudo chmod +x /Applications/Vorratsync/Vorratsuebersicht.SyncServer

# Starten (im Terminal)
/Applications/Vorratsync/Vorratsuebersicht.SyncServer
```

Der Server startet und zeigt seine LAN-IP-Adressen an.  
Im Browser erreichbar unter: `http://localhost:5191/`

## Variante B: Installationsskript

```bash
curl -sL https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/install.sh | bash
cd ~/vorratsuebersicht-sync-server
./Vorratsuebersicht.SyncServer
```

## Variante C: launchd-Dienst (Autostart)

Damit der Server automatisch im Hintergrund läuft:

```bash
# Herunterladen und entpacken
curl -sL https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/latest/download/vorratsuebersicht-sync-server-osx-arm64.tar.gz \
  -o /tmp/syncserver.tar.gz
sudo mkdir -p /Applications/Vorratsync
sudo tar xzf /tmp/syncserver.tar.gz -C /Applications/Vorratsync
sudo chmod +x /Applications/Vorratsync/Vorratsuebersicht.SyncServer

# launchd-Plist erstellen
cat > ~/Library/LaunchAgents/de.stryi.vorratsync.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>de.stryi.vorratsync</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Applications/Vorratsync/Vorratsuebersicht.SyncServer</string>
        <string>--urls</string>
        <string>http://0.0.0.0:5191</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>/Applications/Vorratsync</string>
    <key>StandardOutPath</key>
    <string>/Applications/Vorratsync/vorratsync.log</string>
    <key>StandardErrorPath</key>
    <string>/Applications/Vorratsync/vorratsync.log</string>
</dict>
</plist>
EOF

# Dienst laden und starten
launchctl load ~/Library/LaunchAgents/de.stryi.vorratsync.plist

# Status prüfen
launchctl list | grep vorratsync
```

## Variante D: Docker

Siehe [Docker-Installation](installation-docker.md).

## Sicherheitshinweis

macOS kann die Ausführung blockieren, da es sich um ein nicht signiertes Programm handelt:

1. Gehe zu **Systemeinstellungen → Datenschutz & Sicherheit**
2. Klicke bei "Öffnen von `Vorratsuebersicht.SyncServer` wurde blockiert" auf **Trotzdem öffnen**
3. Oder entferne die Quarantäne:

```bash
sudo xattr -rd com.apple.quarantine /Applications/Vorratsync/
```

## Deinstallation

```bash
# launchd-Dienst entfernen
launchctl unload ~/Library/LaunchAgents/de.stryi.vorratsync.plist
rm ~/Library/LaunchAgents/de.stryi.vorratsync.plist

# Programm entfernen
sudo rm -rf /Applications/Vorratsync
```
