#!/usr/bin/env bash
set -e

REPO="f2r6a0n2k/vorratsuebersicht-sync-server"
VERSION="${1:-latest}"

detect_platform() {
  local arch
  arch=$(uname -m)

  case "$(uname -s)" in
    Linux)
      case "$arch" in
        x86_64)  echo "linux-x64" ;;
        aarch64|arm64) echo "linux-arm64" ;;
        armv7l|armv6l) echo "linux-arm" ;;
        *) echo "Unsupported Linux architecture: $arch"; exit 1 ;;
      esac
      ;;
    Darwin)
      case "$arch" in
        x86_64)  echo "osx-x64" ;;
        arm64)   echo "osx-arm64" ;;
        *) echo "Unsupported macOS architecture: $arch"; exit 1 ;;
      esac
      ;;
    MINGW*|MSYS*|CYGWIN*)
      echo "win-x64"
      ;;
    *)
      echo "Unsupported OS: $(uname -s)"; exit 1 ;;
  esac
}

download_release() {
  local platform="$1"
  local ext url archive

  if [ "$platform" = "win-x64" ]; then
    ext=".zip"
  else
    ext=".tar.gz"
  fi

  if [ "$VERSION" = "latest" ]; then
    url="https://github.com/$REPO/releases/latest/download/vorratsuebersicht-sync-server-$platform$ext"
  else
    url="https://github.com/$REPO/releases/download/$VERSION/vorratsuebersicht-sync-server-$platform$ext"
  fi

  archive="/tmp/vorratsuebersicht-sync-server-$platform$ext"
  echo "Downloading $url ..."
  curl -sL "$url" -o "$archive"

  mkdir -p "$HOME/vorratsuebersicht-sync-server"
  if [ "$platform" = "win-x64" ]; then
    unzip -o "$archive" -d "$HOME/vorratsuebersicht-sync-server"
  else
    tar xzf "$archive" -C "$HOME/vorratsuebersicht-sync-server"
  fi
  rm "$archive"

  echo ""
  echo "Installiert nach: $HOME/vorratsuebersicht-sync-server/"
  echo ""
  echo "Zum Starten:"
  echo "  $HOME/vorratsuebersicht-sync-server/Vorratsuebersicht.SyncServer"
  echo ""
  echo "Im Browser öffnen: http://localhost:5191/"
  echo ""
  echo "Dann im Browser öffnen: http://localhost:5191/"
  echo ""
  echo "Hinweis: Der Server ist ab Start im gesamten LAN erreichbar."
  echo "Sollte der Port 5191 belegt sein, anderen Port verwenden:"
  echo "  ASPNETCORE_URLS=http://0.0.0.0:5192 $HOME/vorratsuebersicht-sync-server/Vorratsuebersicht.SyncServer"
}

main() {
  echo "Vorratsuebersicht SyncServer - Installer"
  echo "========================================="
  local platform
  platform=$(detect_platform)
  echo "Erkannte Plattform: $platform"
  download_release "$platform"
}

main
