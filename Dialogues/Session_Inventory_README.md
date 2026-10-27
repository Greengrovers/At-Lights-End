# Spielername, Gedanken und Nachtinventar

## Start und Gedanken

Starte `Assets/At Lights End/Scenes/Shool Day.unity`. Vor dem ersten Raumdialog erscheint die Namenseingabe. Name, besuchte Räume und Inventar gelten nur bis zum Beenden des Spieldurchlaufs; es werden keine Spielstände geschrieben.

Im Dialog-Asset aktiviert **Player Thoughts** die blaue Gedankenblase (Speech Bubble, Stil `cloud`, Form `Think`). Dabei gibt es keine Porträts oder Nahansichten. Der Sprechername wird automatisch durch den eingegebenen Namen ersetzt. In Dialogtexten setzt `{playerName}` den Namen ein, etwa: `Du bist also {playerName}.` Bestehende Texte bleiben unverändert.

`Mein Character.asset` ist als **Is Player Character** markiert. Normale Gespräche behalten das Papierfeld und ihre Porträts. Die Raumdialoge werden pro Raum und Szene nur einmal je Spieldurchlauf gestartet.

## Tag und Nacht testen

Der Test-Button oben rechts wechselt zwischen `Shool Day` und `Prototype Nacht`. Während eines Dialogs oder bei geöffnetem Inventar ist er ausgeblendet. Beide Szenen sind in den Build-Szenen eingetragen.

Nachts öffnet **Ghost Hunting Equipment** unten links das Inventar. Tagsüber ist der Button ausgeblendet und auch ein direkter Öffnungsversuch gesperrt. Ein Wechsel löscht weder Inhalt noch eingesammelte Objekte. Escape oder Schließen beendet die Inventaransicht.

## Gegenstände hinzufügen

1. Im Project-Fenster: **Create → At Lights End → Inventory → Item**. Name, Beschreibung und ein Objektbild als **Icon** (Sprite) eintragen. Das Inventar zeigt große anklickbare Bildkacheln, den Namen darunter und bei mehreren Exemplaren die Menge. Für die vorhandenen Gegenstände wurden Bilder ihrer 3D-Modelle erstellt. Die Item ID identifiziert den Gegenstand; verschiedene Gegenstände brauchen verschiedene IDs.
2. Ein sichtbares Objekt unter die passende Raum-Gruppe stellen und einen 3D-Collider hinzufügen (kein Trigger).
3. **InspectableObject** und **InventoryPickup** hinzufügen und das Item-Asset sowie die Menge zuweisen. Beim Item unter **Inspection Prefab** ein Prefab mit Modell und InspectableObject zuweisen, ohne InventoryPickup. Dieses Modell wird für die Untersuchung aus dem Inventar verwendet.
4. Jede platzierte Kopie braucht eine eigene Pickup ID. Nach Duplizieren oder Platzieren einer Prefab-Kopie im Komponentenmenü **Generate New Pickup ID** ausführen. Mehrere Kopien dürfen dasselbe Item-Asset verwenden; ihre Mengen werden zusammengezählt.

Die Pflanzentöpfe beider Nachträume und das **Geister-Notizbuch** sind eingerichtet. Items und Untersuchungs-Prefabs liegen unter `Assets/At Lights End/Inventory`. Das Inventar beginnt leer. Ein Klick auf ein Objekt im Raum öffnet die Untersuchung; Ziehen mit der linken Maustaste dreht es. Erst **Escape** sammelt es ein und entfernt es aus dem Raum. Ein Klick auf seinen Inventareintrag öffnet die Untersuchung erneut. **Escape** legt es zurück und öffnet das Inventar wieder, ohne die Menge zu verändern. Ein automatischer Abbruch, etwa beim Raumwechsel, sammelt ein erstmals betrachtetes Objekt nicht ein. Benutzen, Kombinieren oder Ablegen von Ausrüstung ist noch nicht implementiert.

Die Szenen verwenden jeweils **Game Session** mit `GameSceneContext` und `SessionUI`. Neue Spielszenen brauchen diese Komponenten und die passenden Verweise; die Sammlung selbst liegt szenenunabhängig in `PlayerSession`.

## Geprüft

- Leere Namenseingabe abgewiesen; eingegebener Name in Gedankenblase angezeigt.
- Gedanken ohne Porträts; normale Gespräche weiterhin mit Papierfeld und Porträts.
- Nachtinventar zunächst leer; Notizbuch über den Kamera-Klickpfad eingesammelt, doppelte Aufnahme gesperrt.
- Nacht → Tag → Nacht: Name und Menge erhalten, tagsüber kein Inventarzugriff, eingesammeltes Buch bleibt verschwunden.
- Neustart im Editor: Name zurückgesetzt, Inventar leer.
