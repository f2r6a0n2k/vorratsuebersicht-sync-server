#!/usr/bin/env python3
"""
Vorratsuebersicht → SyncServer Migration Tool

Überträgt die Daten aus der Android-App (SQLite) in den SyncServer (REST API).

Verwendung:
  1. Datenbank vom Android-Gerät exportieren:
     - In der App: Menü → Datenbank sichern → Auf SD-Karte/Downloads
     - Oder via ADB: adb pull /storage/emulated/0/Vorratsuebersicht/Vorraete.db3
     - Die Datei auf den Linux-Server kopieren

  2. Dieses Skript ausführen:
     python3 tools/migrate-android-to-syncserver.py \
       --db /pfad/zur/Vorraete.db3 \
       --server http://192.168.178.109:5191

  Optional: --clear löscht vorher alle Daten im SyncServer
"""

import sqlite3
import json
import sys
import os
import argparse
from urllib.request import Request, urlopen
from urllib.error import URLError, HTTPError

API_ARTICLES = "/api/articles"
API_STORAGE = "/api/storage-items"
API_SHOPPING = "/api/shopping-items"

def post_json(url, data):
    body = json.dumps(data).encode("utf-8")
    req = Request(url, data=body, headers={
        "Content-Type": "application/json",
    }, method="POST")
    with urlopen(req) as resp:
        return json.loads(resp.read())

def put_json(url, data):
    body = json.dumps(data).encode("utf-8")
    req = Request(url, data=body, headers={
        "Content-Type": "application/json",
    }, method="PUT")
    with urlopen(req) as resp:
        return resp.read()

def delete_json(url):
    req = Request(url, method="DELETE")
    with urlopen(req) as resp:
        return resp.read()

def ping(server):
    try:
        with urlopen(f"{server}/api/discovery/ping", timeout=5) as resp:
            return resp.status == 200
    except:
        return False

def clear_server(server):
    print("  Lösche vorhandene Daten...")
    try:
        with urlopen(f"{server}{API_ARTICLES}") as resp:
            articles = json.loads(resp.read())
        for a in articles:
            delete_json(f"{server}{API_ARTICLES}/{a['articleId']}")
            print(f"    Gelöscht: Artikel #{a['articleId']} - {a['name']}")
    except:
        pass

def migrate_articles(server, db_path):
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()

    print("\n  Lese Artikel aus der Android-Datenbank...")
    rows = cursor.execute("""
        SELECT ArticleId, Name, Manufacturer, Category, SubCategory,
               DurableInfinity, WarnInDays, Size, Unit, Calorie,
               Notes, EANCode, StorageName, MinQuantity, PrefQuantity,
               Supermarket, Price
        FROM Article
        ORDER BY ArticleId
    """).fetchall()

    print(f"  Gefunden: {len(rows)} Artikel")
    id_map = {}  # alter ArticleId → neuer ArticleId

    for row in rows:
        article = {
            "name": row["Name"] or "",
            "manufacturer": row["Manufacturer"],
            "category": row["Category"],
            "subCategory": row["SubCategory"],
            "durableInfinity": bool(row["DurableInfinity"]),
            "warnInDays": row["WarnInDays"],
            "size": float(row["Size"]) if row["Size"] else None,
            "unit": row["Unit"],
            "calorie": row["Calorie"],
            "notes": row["Notes"],
            "eanCode": row["EANCode"],
            "storageName": row["StorageName"],
            "minQuantity": row["MinQuantity"],
            "prefQuantity": row["PrefQuantity"],
            "supermarket": row["Supermarket"],
            "price": float(row["Price"]) if row["Price"] else None,
        }

        try:
            result = post_json(f"{server}{API_ARTICLES}", article)
            new_id = result.get("articleId", 0)
            id_map[row["ArticleId"]] = new_id
            print(f"    ✓ {row['ArticleId']} → {new_id}: {row['Name']}")
        except HTTPError as e:
            print(f"    ✗ Fehler bei Artikel {row['ArticleId']}: {e.read().decode()}")
        except Exception as e:
            print(f"    ✗ Fehler bei Artikel {row['ArticleId']}: {e}")

    conn.close()
    return id_map

def migrate_storage(server, db_path, id_map):
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()

    print("\n  Lese Lagerbestand aus der Android-Datenbank...")
    rows = cursor.execute("""
        SELECT StorageItemId, ArticleId, Quantity, BestBefore, StorageName
        FROM StorageItem
        ORDER BY StorageItemId
    """).fetchall()

    print(f"  Gefunden: {len(rows)} Lagerpositionen")
    count = 0

    for row in rows:
        new_article_id = id_map.get(row["ArticleId"])
        if new_article_id is None:
            print(f"    - Übersprungen (kein Artikel): StorageItem #{row['StorageItemId']}")
            continue

        item = {
            "articleId": new_article_id,
            "quantity": row["Quantity"] or 0,
            "bestBeforeDate": row["BestBefore"],
            "storageName": row["StorageName"],
        }

        try:
            post_json(f"{server}{API_STORAGE}", item)
            count += 1
            print(f"    ✓ StorageItem #{row['StorageItemId']} → Article #{new_article_id}")
        except HTTPError as e:
            print(f"    ✗ Fehler: {e.read().decode()}")
        except Exception as e:
            print(f"    ✗ Fehler: {e}")

    conn.close()
    return count

def migrate_shopping(server, db_path, id_map):
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cursor = conn.cursor()

    print("\n  Lese Einkaufsliste aus der Android-Datenbank...")
    rows = cursor.execute("""
        SELECT ShoppingListId, ArticleId, Quantity, Bought
        FROM ShoppingList
        ORDER BY ShoppingListId
    """).fetchall()

    print(f"  Gefunden: {len(rows)} Einkaufszettel-Einträge")
    count = 0

    for row in rows:
        new_article_id = id_map.get(row["ArticleId"])
        if new_article_id is None:
            print(f"    - Übersprungen (kein Artikel): ShoppingList #{row['ShoppingListId']}")
            continue

        # Hole Artikelname für die Anzeige
        try:
            with urlopen(f"{server}{API_ARTICLES}/{new_article_id}") as resp:
                article = json.loads(resp.read())
                article_name = article.get("name", "")
        except:
            article_name = ""

        item = {
            "articleId": new_article_id,
            "articleName": article_name,
            "quantity": row["Quantity"] or 1,
            "isChecked": bool(row["Bought"]),
        }

        try:
            post_json(f"{server}{API_SHOPPING}", item)
            count += 1
            print(f"    ✓ ShoppingList #{row['ShoppingListId']} → Article #{new_article_id}")
        except HTTPError as e:
            print(f"    ✗ Fehler: {e.read().decode()}")
        except Exception as e:
            print(f"    ✗ Fehler: {e}")

    conn.close()
    return count

def main():
    parser = argparse.ArgumentParser(
        description="Migriere Daten von der Vorratsuebersicht Android-App in den SyncServer")
    parser.add_argument("--db", required=True,
                        help="Pfad zur Android SQLite-Datenbank (Vorraete.db3)")
    parser.add_argument("--server", default="http://localhost:5191",
                        help="SyncServer-URL (z.B. http://192.168.178.109:5191)")
    parser.add_argument("--clear", action="store_true",
                        help="Vorhandene Daten im SyncServer löschen vor dem Import")
    args = parser.parse_args()

    if not os.path.isfile(args.db):
        print(f"Fehler: Datenbank nicht gefunden: {args.db}")
        sys.exit(1)

    print("Vorratsuebersicht → SyncServer Migration")
    print("========================================")
    print(f"  Datenbank: {args.db}")
    print(f"  Server:    {args.server}")
    print()

    if not ping(args.server):
        print(f"Fehler: SyncServer unter {args.server} nicht erreichbar.")
        print("Stelle sicher, dass der Server läuft:")
        print(f"  /opt/vorratsync/Vorratsuebersicht.SyncServer")
        sys.exit(1)

    print("✓ Server verbunden")
    print()

    if args.clear:
        clear_server(args.server)

    print("\n--- Artikel migrieren ---")
    id_map = migrate_articles(args.server, args.db)

    print(f"\n→ {len(id_map)} Artikel migriert")

    print("\n--- Lagerbestand migrieren ---")
    storage_count = migrate_storage(args.server, args.db, id_map)

    print(f"\n→ {storage_count} Lagerpositionen migriert")

    print("\n--- Einkaufsliste migrieren ---")
    shopping_count = migrate_shopping(args.server, args.db, id_map)

    print(f"\n→ {shopping_count} Einkaufszettel-Einträge migriert")

    print()
    print("========================================")
    print("Migration abgeschlossen!")
    print(f"  Artikel:      {len(id_map)}")
    print(f"  Lager:        {storage_count}")
    print(f"  Einkauf:      {shopping_count}")
    print()
    print("Jetzt im Browser öffnen:")
    print(f"  {args.server}/")
    print()
    print("Oder mit dem Desktop-Client verbinden:")
    print(f"  Server-URL: {args.server}")

if __name__ == "__main__":
    main()
