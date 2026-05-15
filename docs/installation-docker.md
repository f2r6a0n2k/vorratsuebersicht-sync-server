# Installation mit Docker

Docker-Images sind ideal für NAS-Systeme (Synology, QNAP), Heimserver oder fortgeschrittene Nutzer.

## Image starten

```bash
docker run -d \
  --name vorratsync \
  -p 5191:5191 \
  -v vorratsync-data:/data \
  --restart unless-stopped \
  ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```

| Option | Bedeutung |
|--------|-----------|
| `-d` | Läuft im Hintergrund |
| `-p 5191:5191` | Port 5191 im LAN freigeben |
| `-v vorratsync-data:/data` | Datenbank dauerhaft speichern |
| `--restart unless-stopped` | Automatischer Neustart |

Danach erreichbar unter: `http://<server-ip>:5191/`

## Mit docker-compose (empfohlen)

```bash
# docker-compose.yml herunterladen
curl -sLO https://raw.githubusercontent.com/f2r6a0n2k/vorratsuebersicht-sync-server/main/docker-compose.yml

# Starten
docker compose up -d

# Logs anzeigen
docker compose logs -f
```

## Image selbst bauen

```bash
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
docker build -t vorratsuebersicht-sync-server .
docker run -d -p 5191:5191 vorratsuebersicht-sync-server
```

## Datenbank-Pfad ändern

```bash
docker run -d \
  --name vorratsync \
  -p 5191:5191 \
  -v /pfad/zu/meinem/ordner:/data \
  -e Server__DatabasePath=/data/vorratsuebersicht.db \
  ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```

## Auf Synology NAS

1. **Docker** aus dem Paket-Center installieren
2. **Registrierung** → `ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server` suchen und pullen
3. **Container erstellen**:
   - Port: `5191` → `5191`
   - Volume: `docker/vorratsync:/data` einbinden
   - Automatischer Neustart aktivieren
4. Erreichbar unter: `http://<synology-ip>:5191/`

## Update

```bash
# Neues Image pullen
docker pull ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server

# Alten Container ersetzen
docker stop vorratsync
docker rm vorratsync
docker run -d --name vorratsync -p 5191:5191 -v vorratsync-data:/data ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```

## Deinstallation

```bash
docker stop vorratsync
docker rm vorratsync
docker volume rm vorratsync-data
docker rmi ghcr.io/f2r6a0n2k/vorratsuebersicht-sync-server
```
