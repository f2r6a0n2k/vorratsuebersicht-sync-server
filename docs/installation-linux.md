# Installation unter Linux

## Variante A: Binär (einfach)

Je nach Architektur das passende Paket herunterladen:

| Architektur | Paket |
|-------------|-------|
| x64 (PC) | `vorratsuebersicht-sync-server-linux-x64.tar.gz` |
| ARM (RPi 3/4/5) | `vorratsuebersicht-sync-server-linux-arm.tar.gz` |
| ARM64 (RPi 5, Oracle ARM) | `vorratsuebersicht-sync-server-linux-arm64.tar.gz` *(in Vorbereitung)* |

```bash
# Herunterladen des neuesten Releases
curl -sL https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/latest/download/vorratsuebersicht-sync-server-linux-x64.tar.gz \
  -o /tmp/syncserver.tar.gz

# Entpacken
sudo mkdir -p /opt/vorratsync
sudo tar xzf /tmp/syncserver.tar.gz -C /opt/vorratsync
sudo chmod +x /opt/vorratsync/Vorratsuebersicht.SyncServer

# Starten
/opt/vorratsync/Vorratsuebersicht.SyncServer
```

Der Server startet und zeigt seine LAN-IP-Adressen an.  
Im Browser erreichbar unter: `http://localhost:5191/`

## Variante B: Installationsskript (empfohlen)

Einzeiler für Linux x64:

```bash
curl -sL https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/install.sh | bash
cd ~/vorratsuebersicht-sync-server
./Vorratsuebersicht.SyncServer
```

## Variante C: systemd-Dienst (Autostart)

Damit der Server automatisch im Hintergrund läuft:

```bash
# Herunterladen und Entpacken nach /opt
curl -sL https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/latest/download/vorratsuebersicht-sync-server-linux-x64.tar.gz \
  -o /tmp/syncserver.tar.gz
sudo mkdir -p /opt/vorratsync
sudo tar xzf /tmp/syncserver.tar.gz -C /opt/vorratsync
sudo chmod +x /opt/vorratsync/Vorratsuebersicht.SyncServer

# systemd-Dienst einrichten
sudo tee /etc/systemd/system/vorratsync.service > /dev/null << 'EOF'
[Unit]
Description=Vorratsuebersicht SyncServer
After=network.target

[Service]
Type=simple
ExecStart=/opt/vorratsync/Vorratsuebersicht.SyncServer --urls http://0.0.0.0:5191
WorkingDirectory=/opt/vorratsync
Restart=on-failure
RestartSec=5
User=nobody
Group=nogroup

[Install]
WantedBy=multi-user.target
EOF

# Dienst aktivieren und starten
sudo systemctl daemon-reload
sudo systemctl enable --now vorratsync

# Status prüfen
sudo systemctl status vorratsync
```

## Variante D: Docker

Siehe [Docker-Installation](installation-docker.md).

## Variante E: Selbst bauen (mit .NET SDK)

```bash
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
dotnet run --project src/Vorratsuebersicht.SyncServer
```

---

## Firewall (ufw)

Falls `ufw` aktiv ist, Port freigeben:

```bash
sudo ufw allow 5191/tcp comment 'Vorratsuebersicht SyncServer'
```

## Datenbank-Sicherung

Die Datenbank liegt standardmäßig neben der ausführbaren Datei:

```bash
# Backup erstellen
cp /opt/vorratsync/vorratsuebersicht.db /opt/vorratsync/vorratsuebersicht.db.bak
```

## Deinstallation

```bash
# systemd-Dienst entfernen
sudo systemctl stop vorratsync
sudo systemctl disable vorratsync
sudo rm /etc/systemd/system/vorratsync.service
sudo systemctl daemon-reload

# Programm entfernen
sudo rm -rf /opt/vorratsync
```
