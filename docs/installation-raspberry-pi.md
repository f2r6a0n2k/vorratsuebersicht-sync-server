# Installation auf dem Raspberry Pi

Der Raspberry Pi ist der ideale Server für den Off-Grid-Betrieb — wenig Stromverbrauch, leise, 24/7 lauffähig.

## Übersicht

| Modell | Architektur | Empfohlen |
|--------|-------------|-----------|
| RPi Zero/W | ARMv6 | ❌ Zu langsam |
| RPi 2 | ARMv7 | ⚠️ Läuft, aber knapp |
| RPi 3/3+ | ARMv8 (32-Bit) | ✅ `linux-arm` |
| RPi 4/400 | ARMv8 (64-Bit) | ✅ `linux-arm` (32-Bit-Betriebssystem) oder Docker |
| RPi 5 | ARMv8 (64-Bit) | ✅ Docker oder selbst bauen |

## Variante A: Binär (RPi 3/4/5 mit 32-Bit-OS)

```bash
# Herunterladen
curl -sL https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases/latest/download/vorratsuebersicht-sync-server-linux-arm.tar.gz \
  -o /tmp/syncserver.tar.gz

# Entpacken
sudo mkdir -p /opt/vorratsync
sudo tar xzf /tmp/syncserver.tar.gz -C /opt/vorratsync
sudo chmod +x /opt/vorratsync/Vorratsuebersicht.SyncServer

# Starten
/opt/vorratsync/Vorratsuebersicht.SyncServer
```

## Variante B: Docker (RPi 4/5, empfohlen)

```bash
# Docker installieren (falls nicht vorhanden)
curl -sSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# Danach aus- und wieder einloggen

# SyncServer starten
docker run -d \
  --name vorratsync \
  -p 5191:5191 \
  -v vorratsync-data:/data \
  --restart unless-stopped \
  ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```

## Variante C: systemd-Dienst (Autostart, RPi 3/4/5)

```bash
# Nach Variante A: Binary installieren

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

sudo systemctl daemon-reload
sudo systemctl enable --now vorratsync
sudo systemctl status vorratsync
```

## Variante D: Selbst bauen (64-Bit-OS, RPi 4/5)

```bash
# .NET SDK installieren
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH="$HOME/.dotnet:$PATH"

# Repository klonen und bauen
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
dotnet publish src/Vorratsuebersicht.SyncServer \
  -c Release \
  --self-contained true \
  -o /opt/vorratsync

# Starten
/opt/vorratsync/Vorratsuebersicht.SyncServer
```

## WiFi-Empfehlung für Off-Grid

Für den reinen LAN-Betrieb ohne Internet:

1. **RPi als Access Point** einrichten, sodass sich Clients direkt mit dem RPi verbinden
2. Der SyncServer läuft dann auf `http://192.168.4.1:5191/`
3. Kein Router, kein Internet nötig

Anleitung: https://www.raspberrypi.com/documentation/computers/configuration.html#setting-up-a-routed-wireless-access-point

## Stromverbrauch

| Modell | Verbrauch | Kosten pro Jahr (24/7) |
|--------|-----------|----------------------|
| RPi 3 | ~5 W | ~1 € |
| RPi 4 | ~7 W | ~2 € |
| RPi 5 | ~10 W | ~3 € |

Bei 35 ct/kWh.
