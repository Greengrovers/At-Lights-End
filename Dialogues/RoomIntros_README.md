# Dialog beim ersten Betreten

Am Szenenobjekt `Room Navigation (1)` in der Komponente `Room Navigation Controller` unter `Rooms`:

- `First Visit Dialogue`: Dialog-Asset fuer diesen Raum. Leer lassen, wenn kein automatischer Dialog gewuenscht ist.
- Aktuell verwenden beide Raeume das vorhandene Asset `Main Character Text`; dessen Texte wurden nicht veraendert.
- Fuer verschiedene Texte das Asset im Project-Fenster duplizieren, die `Lines` bearbeiten und die Kopie dem anderen Raum zuweisen.

Der Start-Raum spielt seinen Dialog beim Spielstart. Weitere Raeume erst nach dem abgeschlossenen Kamerauebergang. Zurueckgehen loest denselben Raumdialog nicht nochmals aus. Auch Schliessen/Escape zaehlt als bereits gestartet.

Das Gedaechtnis gilt fuer die aktuelle geladene Szene. Ein neuer Play-Durchlauf oder erneutes Laden der Szene setzt es zurueck. Es gibt noch keine Speicherung ueber Spielstarts hinweg.

Monologe sind erlaubt: nur `Left Character` setzen und alle Zeilen auf `Left` stellen, `Right Character` leer lassen (oder umgekehrt). Der unbesetzte Portraitplatz bleibt leer.

Der Anzeigename des Main Characters kommt aus `Mein Character > Display Name` (aktuell Zeke).
