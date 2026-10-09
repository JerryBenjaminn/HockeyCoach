# Päätösloki

Lukittuja päätöksiä muuttaa vain Jerry. Agentit kirjaavat muutosehdotukset tilalla **ehdotettu** ja perustelevat ne raportin luvuilla.

Tilat: **lukittu** · **ehdotettu** · **hyväksytty** · **hylätty**

## Lukitut päätökset

| ID | Päivä | Päätös | Valinta | Tila |
| --- | --- | --- | --- | --- |
| D-001 | 2026-10-09 | Alusta | Mobiili, kaikki ohjaus sormella | lukittu |
| D-002 | 2026-10-09 | Ydinmuoto | Asynkroninen PvP, PvE harjoitukseksi | lukittu |
| D-003 | 2026-10-09 | Joukkue | Kausidraft, ei pysyvää kokoelmaa | lukittu |
| D-004 | 2026-10-09 | Pelaajat | Kuvitteellisia, satunnaisesti generoituja | lukittu |
| D-005 | 2026-10-09 | Kuviot | Valmis pelikirja + solmu- ja tahtipohjainen editori | lukittu |
| D-006 | 2026-10-09 | Simulaatio | Tapahtumapohjainen, kahden tilan malli (kuviotila ja järjestelmätila) | lukittu |
| D-007 | 2026-10-09 | Siirtymät | Puolustuksen järjestäytyneisyys, siirtymäohje (suorahyökkäys tai kokoaminen) | lukittu |
| D-008 | 2026-10-09 | Paikkatyypit | Suorahyökkäys, kiekonriisto, alueella pelaaminen, ylivoima, alivoima, aloitus | lukittu |
| D-009 | 2026-10-09 | Monetisointi | Vain kosmetiikka ja kausipassi | lukittu |
| D-010 | 2026-10-09 | Tekniikka | C#, Sim ja AI netstandard2.1 / C# 9, Harness ja testit net10.0 (muutettu 2026-10-09: koneella ei ole .NET 8 -ajonaikaa) | lukittu |
| D-011 | 2026-10-09 | Kieli | Koodi englanniksi, dokumentit suomeksi | lukittu |
| D-012 | 2026-10-09 | Determinismi | Determinismi samassa ajoympäristössä riittää, bittitarkkuutta .NETin ja Unityn välillä ei vaadita. Asynkronisessa PvP:ssä palvelin laskee ottelun ja lähettää tapahtumalokin, jota asiakas vain toistaa. Kaikki matematiikka `CheckMath`-luokan takana, jotta sen voi vaihtaa yhdessä paikassa | lukittu |
| D-013 | 2026-10-09 | Dataskeemat | `docs/data-schema.md` on datatiedostojen skeemojen ainoa totuus, designer omistaa ja ylläpitää | lukittu |
| D-014 | 2026-10-09 | Todennäköisyyden rajat | Tarkistuksen onnistumistodennäköisyys rajataan välille min–max. Arvot (0,02 / 0,98) ovat tasapainoarvoja `tuning.json`:ssa | lukittu |
| D-015 | 2026-10-09 | Riippuvuudet | Simissä ja AI:ssa ei ajonaikaisia riippuvuuksia. Käännösaikaiset analysaattorit (BannedApiAnalyzers) sallittuja | lukittu |
| D-016 | 2026-10-09 | Tavoitehaarukat | `data/targets.json` on tavoitehaarukoiden ainoa totuus. `stats-and-checks.md` viittaa siihen eikä toista lukuja | lukittu |
| D-017 | 2026-10-09 | Datasopimukset | Tarkistuksen painot summautuvat 1:een kummallakin puolella (hyökkääjä, puolustaja). Solmun id = x × leveys + y (9 × 5 -verkossa x * 5 + y). Data kirjoitetaan oman joukkueen näkökulmasta: oma maali x = 0, vastustajan näkökulma kierrolla 180° | lukittu |
| D-018 | 2026-10-09 | Toteutus | Klassinen `.sln`, nimiavaruus `Sim.Config`. Virstanpylväässä 1 pelaajat luodaan käsin, `roles.json` myöhemmin | lukittu |

## Muutosehdotukset

Kirjaa uusin ylimmäksi.

<!--
### E-001 · YYYY-MM-DD · ehdotettu
**Ehdotus:** mitä muutetaan.
**Perustelu:** raportin luvut (mittari, tulos, tavoite).
**Vaikutus:** mihin dokumentteihin, dataan ja koodiin muutos koskee.
**Päätös:** (Jerry täyttää) hyväksytty / hylätty + lyhyt syy.
-->

### E-002 · 2026-10-09 · ehdotettu
**Ehdotus:** pidennetään kaukalon solmuverkko 9 × 5:stä 11 × 5:een niin, että kummankin maalin taakse tulee oma solmurivi.
**Perustelu:** nykyisessä verkossa maaliviivat ovat päätyrivit (x = 0 ja x = 8), joten maalin takana ei ole yhtään solmua. Oikeassa kiekossa iso osa alueella pelaamisesta tapahtuu maalin takana ja päätylaidoilla: kierrätys (cycle), wraparound, maalin takaa syöttö slottiin ja pakkien kiekonhaku päätyyn ammutun kiekon jälkeen. Analyysissa ottelun 2 laukauksista suurin osa syntyi päätypelistä (joukkue A 26 / 47 ja joukkue B 8 / 20 syntytavan mukaan luokitellusta laukauksesta, `docs/stats-and-checks.md`). Ilman maalin takaisia solmuja näitä kuvioita ei voi kirjoittaa, eikä "kiekko päätyyn" (E-001) voi päätyä oikeaan paikkaan. Ratkaisu kannattaa tehdä ennen ensimmäisiä kuvioita (virstanpylväs 2), koska kuviot ja järjestelmät kirjoitetaan solmukoordinaateilla.
**Ehdotettu verkko 11 × 5 (55 solmua):**

| x | Sisältö | Alue | xgZone (hyökkäyssuunta) |
| --- | --- | --- | --- |
| 0 | Oman maalin takana (päätylaita) | defensive | longRange |
| 1 | Oma maaliviiva, maali (1, 2) | defensive | longRange |
| 2 | Omat aloituspisteet (2, 1), (2, 3) | defensive | longRange |
| 3 | Oman pään yläosa, siniviiva x ≈ 3,9 | defensive | longRange |
| 4 | Keskialue, aloituspisteet (4, 1), (4, 3) | neutral | longRange |
| 5 | Keskiviiva, `center` (5, 2) | neutral | longRange |
| 6 | Keskialue, aloituspisteet (6, 1), (6, 3) | neutral | longRange |
| 7 | Ympyröiden yläreuna, siniviiva x ≈ 6,1 | offensive | boards / point / highSlot / point / boards |
| 8 | Hyökkäyspään aloituspisteet (8, 1), (8, 3) | offensive | boards / circle / slot / circle / boards |
| 9 | Vastustajan maaliviiva, maali (9, 2) | offensive | boards / lowAngle / crease / lowAngle / boards |
| 10 | Vastustajan maalin takana | offensive | uusi `behindNet` (y = 1–3), kulmat `boards` |

Alueet: `defensive` 0–3, `neutral` 4–6, `offensive` 7–10 (symmetrinen kierrossa). Solmuväli maaliviivojen välillä on n. 22 jalkaa kuten nyt (maalin takainen alue on oikeasti vain n. 11 jalkaa syvä, mutta yksi rivi riittää kuvaamaan sen), joten nykyiset slotin ja vyöhykkeiden rajat säilyvät, ne vain siirtyvät yhdellä. Uusi laukaisuvyöhyke `behindNet` tarkoittaa, että suoraa laukausta ei ole (tai vain wraparound hyvin pienellä xG:llä), mikä on tasapainoarvo `checks.shot.baseXg.behindNet`. Leveys 5 säilyy, koska kuvioiden kaistat (laidat, aloituspisteet, keskikaista) mahtuvat siihen.
**Vaikutus:** `data/rink.json` (uusi verkko, id = x × 5 + y, ids 0–54), `tuning.json` (`baseXg` ja `attackerByXgZone` uudelle vyöhykkeelle), `docs/data-schema.md` (koko ja taulukot), tech-spec.md ("9 × 5" → "11 × 5"), D-017:n esimerkki (9 × 5 → 11 × 5, kaava ei muutu). Q-002 ratkeaa tällä pituuden osalta. Programmer: verkon koko on datavetoinen, joten koodimuutoksia ei pitäisi tarvita. Testit, joissa on kovakoodattu 9 × 5 tai id 44, päivitetään. Mahdollinen lisäsääntö: maalin solmu ja sen takana oleva solmu eivät ole luistelureitillä läpi kuljettavia (maali on esteenä), mikä on uusi mekaniikka ja vaatii erillisen päätöksen.
**Päätös:** (Jerry täyttää)

### E-001 · 2026-10-09 · ehdotettu
**Ehdotus:** lisätään kuvioihin viides kiekkotoiminto `dump` ("kiekko päätyyn"): kiekollinen ampuu kiekon kohdesolmuun (kulma tai maalin taakse), ja siitä seuraa irtokiekkokamppailu.
**Perustelu:** "Kiekko päätyyn" on oma tapahtumansa tapahtumaskeemassa ja tarkistustaulukossa (ei tarkistusta, johtaa irtokiekkoon, maalivahdin kiekonkäsittely vaikuttaa), mutta kuvion neljällä toiminnolla (luistele, syötä, laukaise, aja maalille) sitä ei voi kirjoittaa. Oikeassa kiekossa dump and chase on peruskeino tulla alueelle järjestäytynyttä puolustusta vastaan (esim. trapia vastaan, vision.md: "suora rynnistys ei toimi"). Analyysin alueelle tulojen onnistuminen tavoittain (`docs/stats-and-checks.md`) vaatii, että tavat ovat erillisiä. Vaihtoehto: `dump` vain järjestelmätilan ja siirtymäohjeen toimintona eikä kuvioissa, mutta silloin pelaaja ei voi rakentaa dump and chase -kuviota.
**Vaikutus:** kuvion skeema (`docs/data-schema.md`: `{"type": "dump", "by": .., "to": [x, y]}`), vision.md ja tech-spec.md ("neljä toimintoa" → viisi), `tuning.json` (`time.secondsPerAction.dumpIn` ja `checks.dumpIn` ovat jo olemassa), simulaatio (`Sim.Tactics`: uusi toiminto, joka laukaisee `loosePuck`-tarkistuksen kohdesolmussa). Parhaiten toimii yhdessä E-002:n kanssa, jotta kiekon voi ampua maalin taakse.
**Päätös:** (Jerry täyttää)

## Tasapainomuutokset

Designerin `tuning.json`-säädöt tavoitehaarukoiden sisällä. Ei vaadi erillistä hyväksyntää, mutta jokainen kirjataan.

| Päivä | Arvo | Ennen | Jälkeen | Syy | Vaikutus raporttiin |
| --- | --- | --- | --- | --- | --- |
