# Installation unter Windows

## Variante A: Binär (einfach)

1. Lade die Datei `vorratsuebersicht-sync-server-win-x64.zip` von der [Release-Seite](https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server/releases) herunter.
2. Entpacke das ZIP in einen Ordner, z.B. `C:\Programme\Vorratsuebersicht-SyncServer\`.
3. Starte `Vorratsuebersicht.SyncServer.exe` durch Doppelklick.

Der Server startet und zeigt seine LAN-IP-Adressen an.  
Im Browser erreichbar unter: `http://localhost:5191/`

> **Hinweis:** Der Server bindet automatisch an `http://0.0.0.0:5191` (alle Netzwerkschnittstellen).
> Falls der Port belegt ist, einen anderen verwenden:
> ```bash
> ASPNETCORE_URLS="http://0.0.0.0:5192" /opt/vorratsync/Vorratsuebersicht.SyncServer
> ```

## Variante B: Als Windows-Dienst (Autostart)

Damit der Server automatisch im Hintergrund läuft:

1. `Vorratsuebersicht.SyncServer.exe` nach `C:\Programme\Vorratsuebersicht-SyncServer\` entpacken.
2. **PowerShell als Administrator** öffnen und folgenden Befehl ausführen:

```powershell
New-Service -Name "Vorratsync" `
  -BinaryPathName "C:\Programme\Vorratsuebersicht-SyncServer\Vorratsuebersicht.SyncServer.exe --urls http://0.0.0.0:5191" `
  -Description "Vorratsuebersicht LAN-SyncServer" `
  -StartupType Automatic

Start-Service -Name "Vorratsync"
```

3. Dienst prüfen:

```powershell
Get-Service -Name "Vorratsync"
```

4. Der Server läuft nun im Hintergrund und startet automatisch mit Windows.

## Variante C: Docker unter Windows

Siehe [Docker-Installation](installation-docker.md).

## Variante D: Mit .NET SDK (für Entwickler)

```powershell
git clone https://github.com/f2r6a0n2k/vorratsuebersicht-sync-server.git
cd vorratsuebersicht-sync-server
dotnet run --project src/Vorratsuebersicht.SyncServer
```

---

## Firewall-Hinweis

Stelle sicher, dass Port `5191` in der Windows-Firewall freigegeben ist:

```powershell
New-NetFirewallRule -DisplayName "Vorratsuebersicht SyncServer" `
  -Direction Inbound -Protocol TCP -LocalPort 5191 -Action Allow
```

## Deinstallation

- **Variante A/B**: Einfach den Ordner löschen. Bei Dienst-Variante vorher:

```powershell
Stop-Service -Name "Vorratsync"
sc.exe delete "Vorratsync"
```

- Die Datenbank (`vorratsuebersicht.db`) bleibt erhalten, falls du sie sichern möchtest.
