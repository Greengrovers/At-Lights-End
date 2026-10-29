# Videoereignis: Katze und Donner

In `Prototype Nacht` liegt das Objekt **Cat Shadow Video Event** mit der Komponente **RoomVideoEvent**.

- Aktuell startet es beim ersten Betreten von **Raum 2** (Room Index = 1), nach dem Raumübergang und nach der Namenseingabe.
- Video: `Assets/At Lights End/Cinematics/GAP_cat_glitchy_shadow.mov` (ca. 5 Sekunden).
- Sound: `Assets/At Lights End/Cinematics/thunder-clap.mp3` (ca. 7 Sekunden), Lautstärke 0,8.
- Der Donner beginnt mit dem ersten dekodierten Videobild. Nach Videoende bleibt das letzte Bild während des kurzen Donnerausklangs sichtbar.
- Währenddessen sind Dialogfortschritt und Raumeingaben gesperrt. Danach geht das Spiel automatisch weiter.
- Das Ereignis läuft einmal pro Spieldurchlauf; Raum- und Tag-/Nachtwechsel setzen es nicht zurück. Ein Neustart setzt es zurück.

## Später aus einem Dialog auslösen

1. Am **Cat Shadow Video Event** die Option **Play On First Entry** ausschalten.
2. Im gewünschten Dialog-Asset bei der passenden Zeile **Video Event Id** auf `cat-shadow` setzen.
3. Sobald diese Zeile angezeigt wird, spielt das Video über dem Dialog. Nach dem Video kann der Dialog fortgesetzt werden.

Das Ereignisobjekt muss in der Szene des Dialogs vorhanden und aktiv sein. Ein bereits in diesem Durchlauf abgespieltes Ereignis wird nicht wiederholt. Als alternative Verbindung steht die öffentliche Methode **RoomVideoEvent.PlayOnce()** für UnityEvents zur Verfügung. Über **On Finished** lassen sich weitere Aktionen nach erfolgreichem Ende verbinden.

Die Originaldateien im Downloads-Ordner werden nicht benötigt: Beide Medien wurden ins Projekt kopiert. Bei einem Ladefehler oder Zeitüberschreitung wird die Eingabe wieder freigegeben und eine Konsolenmeldung ausgegeben.
