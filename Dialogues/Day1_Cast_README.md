# Tag 1: Räume, Gruppen und Dialogdarstellung

Die Szene `Shool Day` enthält nun `Classroom Day 1` und `Cafeteria Day 1`. Die Cafeteria verwendet vorerst eine Kopie des Classroom-Hintergrunds. Die Figuren sind Platzhalter mit vorhandenen Sprites.

| Raum/Besuch | Gespräch | Dialog-Asset unter Assets/At Lights End/Dialogues |
| --- | --- | --- |
| Classroom | Danica am Telefon (vorerst nur ihre Seite) | Class/Day 1/Danica_Class_Day1_Phone |
| Classroom | Ema links, Amilia rechts | Class/Day 1/Ema_Amilia_Class_Day1 |
| Cafeteria, erster Besuch | Chris links, Sean mittig, Marcie rechts | Cafeteria/Day 1/Chris_Sean_Marcie_Cafeteria_Day1_Visit1 |
| Cafeteria, ab zweitem Besuch | Danica links, Steve mittig, Eve rechts | Cafeteria/Day 1/Danica_Steve_Eve_Cafeteria_Day1_Visit2 |
| Cafeteria, ab zweitem Besuch | Fred links, Albert rechts | Cafeteria/Day 1/Fred_Albert_Cafeteria_Day1_Visit2 |

Jede Person einer Gruppe startet denselben Gruppendialog. Die Texte der neuen Gespräche sind ausdrücklich als Platzhalter markiert. Bestehende Dialog-Assets bleiben erhalten. Frühere Demofiguren und der frühere zweite Raum sind deaktiviert als `Archived` aufbewahrt.

## Cafeteria-Besuche

`RoomVisitCast` am Cafeteria-Objekt schaltet zwischen `Visit 1 - Chris Sean Marcie` und `Visit 2 Plus` um. Beim ersten Betreten sind nur die ersten drei sichtbar und anklickbar. Nach Verlassen und Wiederbetreten sind nur die anderen fünf sichtbar. Ab dem dritten Besuch bleibt die zweite Besetzung. Dialogöffnen und -schließen erhöhen den Zähler nicht. Der Zähler bleibt bei Szenenwechseln im aktuellen Spieldurchlauf erhalten und wird beim neuen Spiel zurückgesetzt.

## Texte bearbeiten

Im Dialog-Asset gibt es **Left Character**, **Middle Character**, **Right Character**. Pro Zeile wählst du unter **Speaker** den Sprecher. Bei Dreiergesprächen bleibt der aktive Sprecher hell; beide Zuhörer werden abgedunkelt.

**Player Thoughts** am Dialog-Asset zeigt die neue blaue Notizbuchbox mit dem eigenen Symbol, lila Schrift und rundem Weiter-Button. Der Spielername wird automatisch verwendet; der Main Character hat kein Porträt.

**Narrator** am Dialog-Asset zeigt das beige Papierfeld mit dem Namen Narrator, ohne Porträts. Das Beispiel liegt unter `Narrator/Day 1/Narrator_Cafeteria_Day1.asset` und ist nicht automatisch einem Raum zugewiesen.

Für gemischte Dialoge pro Zeile **Kind** wählen:

- **Default**: folgt der Einstellung des Dialog-Assets.
- **Player Thought**: blaue Main-Character-Box.
- **Narrator**: beiges Narrator-Feld.

Der vorhandene Classroom-Introtext wurde am Absatz `You look around the room.` getrennt: Dieser Absatz ist nun eine Narrator-Zeile. Der Wortlaut bleibt erhalten. Andere vorhandene Texte wurden nicht ersetzt.

## Sprites austauschen

### Position im Dialog einstellen

- **Wer wo steht:** Im Gesprächs-Asset über Left Character, Middle Character und Right Character. Pro Zeile bestimmt Speaker, wer spricht.
- **Wo die Plätze auf dem Bildschirm liegen:** In der Szene `Dialogue System` auswählen, Komponente `Dialogue View`, Abschnitt `Dialogue Portrait Positions`. `Two Person Left/Right` gelten für zwei Plätze, `Three Person Left/Middle/Right` für drei. Die Rect-Werte X/Y/Width/Height sind Bildschirmanteile von 0 bis 1; Y wird von unten gemessen. Außerhalb des Play-Modus bearbeiten und Szene speichern.
- **Welcher Bildausschnitt gezeigt wird:** Im Charakterprofil `Auto Portrait Region` eingeschaltet lassen. Die sichtbare Fläche des zugewiesenen Sprites wird einmal je Dialogansicht automatisch erkannt; transparente Ränder werden ausgespart. `Close Up Bottom Crop` bestimmt, wie viel unten für die Nahansicht weggelassen wird (Standard 0,30). Für einen manuell gesetzten Ausschnitt Auto Portrait Region ausschalten und Portrait Region ändern.

Bei Bildern mit undurchsichtigem Hintergrund ist die automatische Erkennung auf das gesamte Sprite begrenzt; dafür den manuellen Ausschnitt verwenden. Die Position des Raumobjekts beeinflusst die Dialogposition nicht.

Die gemeinsamen Charakterprofile liegen unter `Dialogues/Characters`. Danica verwendet in beiden Räumen dasselbe Profil. Für ein neues Bild sowohl den **Sprite Renderer → Sprite** am Raumobjekt als auch **Portrait / Portrait Region** im Charakterprofil anpassen. Den BoxCollider passend zur sichtbaren Figur ausrichten. Bei Danica beide Raumobjekte beachten.

Die Tag-1-Besetzung ist eingerichtet; eine automatische Kalender-/Tag-2-Verwaltung ist damit noch nicht verbunden.
