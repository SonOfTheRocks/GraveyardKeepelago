# Prüfung der Technologie- und Rezeptfreischaltungen

Stand: 25.10.2026. Bestandsaufnahme und Empfehlung für den nächsten Umbau.

Nachtrag 26.10.2026: Der hier beschriebene Altstand wurde inzwischen durch die direkte
Technologie-/Check-Zuordnung und die getrennte Rezeptverwaltung ersetzt. Details und Abnahme
stehen in `technology-implementation.md`; die folgenden Befunde dokumentieren die Ausgangslage.

## Ergebnis

Der Technologiebaum und sein Forschungsfortschritt werden weiterhin benötigt. Die Umsetzung über
künstliche Rezepte ist dafür nicht erforderlich. Sie sollte durch eine direkte, datenbasierte
Zuordnung von Technologien zu AP-Locations ersetzt werden. Die Verwaltung empfangener Rezepte muss
unabhängig davon erfolgen.

Die drei Aufgaben sind:

1. Forschung: Forschungskosten, Elterntechnologien und den gekauften Zustand im Spiel erhalten.
2. Checks: Beim Erforschen die für diese Technologie definierten AP-Locations melden.
3. Itemempfang: Ausschließlich die im empfangenen AP-Item enthaltenen Rezepte, Arbeiten, Perks
   oder anderen Belohnungen anwenden und anschließend die Empfangsanzeige einreihen.

Ein Technologie-Check verleiht damit kein originales Rezept. Ein Rezeptempfang erforscht nicht
automatisch die ursprünglich dazugehörige Technologie.

## Was zurzeit tatsächlich läuft

- `Plugin.InitializeAfterConnection()` ruft nach dem Laden `LocationPatcher.ReplaceAllLocationsRewardsWithChecks()` auf.
- `LocationPatcher.ReplaceTechUnlocksWithChecks()` bearbeitet sämtliche `GameBalance.me.techs_data`.
- `TechsModifier.ReplaceTechDefinitionUnlocksWithChecks()` leert `crafts`, `works`, `phrases` und `perks`.
  Es ersetzt also die Belohnungslisten bestehender Technologien, nicht die Technologien oder den Baum selbst.
- Für 184 bekannte Technologien werden jeweils drei `ap_… - Unlock N`-Einträge in `crafts` eingesetzt.
- Die drei Crone-Endtechnologien werden nach dem Leeren ausgenommen; sie erhalten keine künstlichen Einträge.
- `GameSave.CopyLists` und `GameSave.UnlockCraft` fangen diese Einträge ab und behandeln sie als Checks.
- `TechUnlock.GetData` liefert dafür generische AP-Texte und Icons.
- `flowscript` sowie die Verbindungen, Preise und weiteren Technologie-Eigenschaften bleiben bestehen.
- Ein zweiter, älterer Versuch liegt unter `UnsortedPatches` und in `Logic.LocationHandler`.
  Die untersuchte Startverdrahtung registriert dessen Technologie-Umbau nicht. Harmony-Attribute allein
  aktivieren ihn nicht; `Plugin.Awake()` ruft kein `PatchAll()` auf.

## Bestätigte Probleme

### Rezeptbesitz wird nicht durchgehend erzwungen

Die Originaldaten der beiden Fleisch-Extraktionsrezepte enthalten `needs_unlock = false`.
`AutopsyGUI.GetExtractCraftDefinition()` prüft über `GameSave.IsCraftVisible()` und
`CraftDefinition.IsLocked()`. Ohne zusätzliche AP-Sperre ist Fleisch daher auch ohne Rezeptempfang verfügbar.

Im lokalen Vanilla-Export wurden 505 verschiedene kanonische Rezept-IDs aus den direkten und
progressiven AP-Grants verglichen. Acht dort gefundene Rezepte benötigen von Haus aus keinen Unlock:

- `ex:mf_preparation_1:flesh`
- `ex:mf_preparation_2:flesh`
- `fake_global_craft`
- `garden_grapes_growing`
- `garden_hop_growing`
- `honey_production`
- `ingot_metal`
- `peat_from_waste`

15 weitere IDs wurden in den beiden exportierten Rezeptlisten nicht gefunden. Das ist zunächst ein
Datenabgleich-Befund, kein Beweis, dass jede dieser IDs ungültig ist. Dynamische Definitionen,
Pseudo-Rezepte und Schreibfehler müssen dabei unterschieden werden; insbesondere ist
`paper_bad, paper_from_herb` als einzelne ID prüfbedürftig.

### Der eigentliche Item-Unlock fehlt

`PlayerActions.ApplyUnlocks()` erstellt eine temporäre Technologie, trägt sie kurz in die internen
GameBalance-Listen ein und entfernt sie anschließend. Es fehlt die Anwendung auf den Spielstand.
Die derzeitigen Handler-Tests prüfen überwiegend Aufrufe einer Fake-Implementierung von
`IPlayerActions`, nicht die Wirkung dieser echten Methode auf den Freischaltungszustand.

### Künstliche Check-Namen stimmen nicht mit den APWorld-Labels überein

Beispiel: Der Client bildet aus `ap_FIRST_SLICE - Unlock 1` den Namen `FIRST_SLICE - Unlock 1`.
Die APWorld erzeugt aus dem Regionslabel dagegen `First Slice - Unlock 1`.
Für Spiritualism existiert zusätzlich eine besondere Label-Erzeugung in `data.py`.
Diese parallel gepflegten Ableitungen sollten durch eine gemeinsame explizite Zuordnung ersetzt werden.
Bestehende AP-Namen und IDs müssen dabei erhalten bleiben.

### Technologie-Skripte bleiben als zusätzliche Belohnungspfade bestehen

Das Leeren der Listen entfernt keine Technologie-Skripte. Die Originaldaten enthalten 19 Technologien
mit `flowscript`. Beispielsweise schaltet `tech_first_slice` einen Bauplan im Seelenraum frei.
Die Skripte müssen nach Wirkung geprüft werden: notwendiger Welt-/Questfortschritt, AP-kontrollierter
Unlock oder eine Kombination daraus. Pauschales Ausführen kann Belohnungen vorzeitig vergeben;
pauschales Entfernen kann Fortschritt blockieren.

### Forschungsgruppen haben bereits eine eigene Bedeutung

`Unlock: Anatomy` ist laut gemeinsamem Grant eine `technology_unlock` für `First slice`.
Diese Bedeutung darf beim Umbau nicht stillschweigend zu `Extract: Flesh` werden.
Forschungsgruppen und Rezeptbesitz müssen getrennt bleiben. Die Meldung der zugehörigen Forschungschecks
bei automatisch erforschten Wurzeltechnologien muss ausdrücklich festgelegt und getestet werden.

### Wiederverbinden und Laden benötigen eigenen Zustand

`APItemManager` und `GKLocationChecker` werden mit leeren Listen bereits verarbeiteter Items bzw. Checks
initialisiert; die vorgesehenen gespeicherten Zustände sind auskommentiert. `SlotData.Seed` wird noch
nicht eingelesen. `Plugin.EnterGame()` initialisiert wegen `IsInGame` zudem nicht erneut beim späteren
Spielstandwechsel im selben Prozess. Änderungen an den globalen Technologiedaten werden nicht zurückgesetzt.

Ein Serverreset beseitigt serverseitigen Testfortschritt. Er ersetzt weder die Zuordnung eines Spielstands
zu Seed/Slot noch die clientseitige Nachverfolgung bereits angewandter Items.

## Empfohlene Umsetzung

1. Eine gemeinsame Zuordnung von echten Technologie-IDs zu den vorhandenen AP-Locations bereitstellen,
   einschließlich DLC-Zuordnung und der gesonderten Quest-Technologie-Rewards.
2. Forschung direkt abfangen und ihren Zustand erhalten: Kosten genau einmal bezahlen,
   Technologie als erforscht merken, Elternabhängigkeiten und Sichtbarkeit erhalten, Checks melden.
   Das vollständige Überspringen von `UnlockTech()` ohne Ersatz für diese Zustandsänderungen reicht nicht.
3. Native Technologie-Ansichten mit AP-Check-Informationen versorgen. Dafür sind keine fingierten
   Craft-IDs nötig. Der AP-Empfangsdialog verwendet weiterhin die tatsächlichen Item-Grants.
4. Die Menge AP-kontrollierter Rezepte aus den vorhandenen Grants aufbauen: direkte Rezepte,
   progressive Stufen und in Perks enthaltene Rezepte. `@` beim ID-Vergleich entfernen; es ist
   Präsentationsmetadatum, kein Teil der gespeicherten Rezept-ID.
5. AP-kontrollierte Rezepte unabhängig vom Vanilla-Standard bis zum passenden Itemempfang sperren.
   Nicht kontrollierte Rezepte und deaktivierte DLC-Inhalte gemäß ihrer eigenen Regeln behandeln.
   Vorhandene Vanilla-Freischaltungen dürfen nicht als Nachweis eines AP-Itemempfangs gelten.
6. `ApplyUnlocks` über die tatsächlichen Spielstand-APIs umsetzen. Rezepte, Arbeiten, Phrasen und
   Perks typgerecht anwenden; doppelte Perks dürfen ihre Parameterboni nicht erneut addieren.
7. Empfangsindex, progressive Stufen, kontrollierte Freischaltungen und ausstehende Checks an
   Spielstand, Seed und Slot binden. Beim erneuten Laden aus diesem Zustand bzw. der synchronisierten
   Itemhistorie rekonstruieren, ohne Gegenstände, Boni oder Benachrichtigungen doppelt zu vergeben.
8. Technologie-Skripte einzeln prüfen und ihre AP-kontrollierten Belohnungen von erforderlichen
   Welt-/Questaktionen trennen. Erst danach die alte Dummy-/CopyLists-Umleitung entfernen.

## Erforderliche Abnahmefälle

- Neuer Spielstand: First-Slice-Questcheck vergibt Tough Mood; Fleisch bleibt gesperrt.
- Empfang von `Extract: Flesh`: beide Autopsietisch-Varianten werden freigeschaltet.
- Andere bereits frei verfügbare AP-Rezepte sind ebenfalls vor Empfang gesperrt.
- Technologie erforschen: richtige bestehende Checks, keine ursprünglichen Rezepte/Perks,
  Kosten genau einmal und Folgetechnologien korrekt zugänglich.
- `Unlock: Anatomy`: Forschungsgruppen-Semantik gemäß Vertrag, kein Fleischrezept als Nebenwirkung.
- Progressive Items, versteckte `@`-Varianten und zusammengesetzte Perks.
- Offline-Check, Wiederverbindung, wiederholte Pakete, Speichern/Laden und Spielstandwechsel.
- Alle DLC-Kombinationen; kein Verweis auf ausgeschlossene Definitionen oder Locations.
- Technologie-Skripte erhalten erforderlichen Fortschritt und vergeben keine AP-Unlocks vorzeitig.
- Vertragstests für Technologie-IDs, Location-Labels und unveränderte numerische IDs.

## Empfehlung zur Reihenfolge

Zuerst die eindeutige Rezeptbesitz-/Empfangsschicht einschließlich Speicherung herstellen.
Anschließend die künstlichen Technologie-Rewards auf direkte, vertraglich geprüfte Checks umstellen.
Den Baum selbst, Forschungskosten und bestehende AP-Locations dabei beibehalten.

Diese historische Bestandsaufnahme dokumentiert den Zustand vor der Umsetzung.
