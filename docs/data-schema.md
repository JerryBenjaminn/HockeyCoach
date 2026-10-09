# Kiekkovalmentaja – dataskeemat

Oct 9, 2026 · game-designer

Tämä dokumentti on datatiedostojen skeemojen ainoa totuus (D-013). Designer omistaa ja ylläpitää sitä. Jos data, koodi ja tämä dokumentti ovat ristiriidassa, tämä dokumentti voittaa, ellei lukittu päätös sano muuta.

| Tiedosto | Tila | Kuka lukee |
| --- | --- | --- |
| `data/rink.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/tuning.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/targets.json` | käytössä | Harness (raportin tavoitevertailu) |
| `data/plays/*.json` | skeema hyväksytty (D-034), tiedostoja ei vielä | Sim (`Sim.Tactics`) |
| `data/systems/*.json` | skeema hyväksytty (D-034), tiedostoja ei vielä | Sim (`Sim.Tactics`) |
| `data/roles.json` | myöhemmin (D-018) | – |

## Yleiset käytännöt

**Muoto.** Puhdas JSON (UTF-8, ei kommentteja). Kenttien nimet englanniksi camelCase-muodossa. Jokaisella tiedostolla on kokonaisluku `schemaVersion`. Kun skeema muuttuu yhteensopimattomasti, versio kasvaa ja lataaja hylkää tuntemattoman version.

**Meta-avaimet.** Avaimet, jotka alkavat alaviivalla (`_notes`, `_placeholders`), ovat ihmisille. Lataaja ohittaa ne kaikilla tasoilla.

**Paikanpitäjät.** `tuning.json`:n `_placeholders` on lista JSON-polkuja (pisteillä erotettu, esim. `checks.shot.baseXg.slot`). Listatut arvot eivät tule dokumenteista vaan ovat alkuarvauksia, jotka kalibroidaan simulaatiolla. Kun arvo vahvistetaan (dokumentti, analyysi tai Jerry), se poistetaan listasta ja kirjataan päätöslokin tasapainomuutoksiin. Taulukkoarvo (esim. `laneDefenderDistance`) listataan yhtenä polkuna.

**Yksiköt.**

| Suure | Yksikkö | Esimerkki |
| --- | --- | --- |
| Stat | 1–20 (kokonaisluku pelaajalla, painotettu summa liukuluku) | `"speed": 14` |
| Todennäköisyys, osuus | 0–1 (ei prosentteja) | `"p0": 0.85` |
| Aika | pelisekuntia | `"setupSeconds": 6` |
| Energia, paine, järjestäytyneisyys | 0–1 | `"recoveryPerEvent": 0.15` |
| Muokkaaja | logit-yksikköä, lisätään summaan M | `"crossIce": -0.4` |
| Etäisyys | solmuja (Chebyshev-etäisyys, ks. kaukalo) | `"maxNodesPerBeat": 2` |

**Muokkaajat logit-yksikköinä.** Jokainen muokkaaja lisätään tarkistuskaavan summaan M (`docs/stats-and-checks.md`, Tarkistukset). Positiivinen arvo auttaa hyökkääjäpuolta. Avaimen nimi kertoo, miten arvo skaalautuu:

| Nimen muoto | Merkitys |
| --- | --- |
| `pressure`, `organization`, `goalieEnergy` (tilaan sidottu) | Logit täydellä vaikutuksella, lineaarisesti välissä. Järjestäytyneisyys: arvo × (1 − puolustuksen järjestäytyneisyys). Paine: arvo × paine. Energia: arvo × (1 − energia) |
| `crossIce`, `royalRoad`, `screen`, `homeAdvantage` (ehto) | Logit, kun ehto on tosi, muuten 0 |
| `...PerPoint` | Logit per stat-piste suhteessa `checkFormula.referenceValue`-arvoon |
| `...PerNode` | Logit per solmu |
| Taulukko, esim. `laneDefenderDistance` | Indeksi on etäisyys solmuina, viimeinen arvo pätee kaikkiin suurempiin |

Avaimet, joiden nimi päättyy `Probability` tai `Share`, ovat todennäköisyyksiä eivätkä muokkaajia. Yksikkö logit on toistaiseksi oletus, ks. Q-008.

**Determinismi.** Objektien avainten järjestykseen ei saa luottaa: lataaja käy sanakirjat läpi järjestettynä (ordinaalinen merkkijonovertailu). Listojen järjestys on merkitsevä (esim. järjestelmän säännöt, kuvion tahdit).

### Koordinaatit ja näkökulma (D-017)

- Kaukalo on `length × width` -solmuverkko. Nykyinen koko on 11 × 5 (E-002 hyväksytty): maaliviivojen välissä 9 riviä ja kummankin maalin takana yksi päätyrivi. Koodi ei saa olettaa kokoa, vaan lukee sen `rink.json`:sta.
- `x` kulkee pituussuunnassa 0 … length − 1, `y` leveyssuunnassa 0 … width − 1.
- **Jokainen tiedosto kirjoitetaan oman joukkueen näkökulmasta:** oma maali on maaliviivalla x = 1 (`ownGoal`), vastustajan maali x = length − 2 (`opponentGoal`), ja joukkue hyökkää kohti kasvavaa x:ää. Rivit x = 0 ja x = length − 1 ovat maalien takana. y = 0 on vasen laita, kun katsotaan kohti vastustajan maalia. Koodi lukee maalien paikat `rink.json`:sta eikä johda niitä koosta.
- **Vastustajan näkökulma** saadaan kierrolla 180°: (x, y) → (length − 1 − x, width − 1 − y), 11 × 5 -verkossa (10 − x, 4 − y). Kierto vaihtaa alueet (defensive ↔ offensive) ja puolet (vasen ↔ oikea), ja oma maali (1, 2) kiertyy vastustajan maaliksi (9, 2). Kaukalon tila tallennetaan simulaatiossa yhdessä kiinteässä koordinaatistossa (kotijoukkueen näkökulma); vierasjoukkueen data kierretään latauksen jälkeen.
- **Solmun id** johdetaan, sitä ei tallenneta: `id = x * width + y` (11 × 5 -verkossa `x * 5 + y`, ids 0–54).
- **Solmun alue** johdetaan `zones`-osion x-väleistä, sitä ei tallenneta solmuun (Q-022).
- **Etäisyys** solmujen välillä on Chebyshev-etäisyys max(|dx|, |dy|), ellei toisin mainita. Syöttölinja on suora jana solmujen välillä.
- **Keskiviiva** on x = (length − 1) / 2 (11 × 5 -verkossa x = 5). **Keskikaista** on y = (width − 1) / 2 (y = 2): vasen puoli y < 2, oikea y > 2.
- **Maalisolmut** (D-033): `ownGoal` ja `opponentGoal` ovat vain laukauksen kohteita. Kenttäpelaaja ei koskaan seiso maalisolmussa, joten mikään kenttäpelaajan kohde (kuvion lähtösolmu, siirto, `skate`, `driveNet`, `dump`-kohde, järjestelmän kohde) ei saa olla maalisolmu, eikä laukaus lähde maalisolmusta. Irtokiekko ei jää maalisolmuun. Poikkeus: maalivahti, jonka paikka on maalisolmu.
- **Maalin edusta** (`netFront`) on solmu, joka on yhden askeleen maalisolmusta keskiviivaa kohti x-suunnassa: oman joukkueen näkökulmasta vastustajan maalin edusta (8, 2) ja oman maalin edusta (2, 2). Se johdetaan, sitä ei tallenneta. `driveNet` vie tänne, ja maalisolmuun osuva järjestelmän kohde siirretään tänne.
- **Lähin pelaaja, tasatilanne.** Kun sääntö valitsee lähimmän pelaajan (esim. `nearestDefender`, irtokiekon kamppailija), etäisyytenä on Chebyshev-etäisyys, sitten Manhattan-etäisyys |dx| + |dy|, ja jos sekin on tasan, pelipaikkajärjestys `C`, `LW`, `RW`, `LD`, `RD`. Sama sääntö kaikkialla, jotta tulos on deterministinen.
- **Peilaus** (y-suunnassa, D-034): (x, y) → (x, width − 1 − y), 11 × 5 -verkossa (x, 4 − y). Pelipaikat vaihtuvat `LW` ↔ `RW` ja `LD` ↔ `RD` (`C` pysyy), aloituspisteiden id:t `...Left` ↔ `...Right` (`center` pysyy). Maalisolmut, maalin edustat ja keskikaista kuvautuvat itselleen. Peilaus edellyttää, että kaukalo on symmetrinen y-suunnassa (ks. rink.json, Validointi).
- Koordinaatit kirjoitetaan kuvioissa ja järjestelmissä taulukkona `[x, y]`, kaukalotiedostossa objektina `{"x": .., "y": ..}`.

## rink.json

| Kenttä | Tyyppi | Kuvaus |
| --- | --- | --- |
| `schemaVersion` | int | 1 |
| `length`, `width` | int | Verkon koko. Nyt 11 ja 5 |
| `ownGoal`, `opponentGoal` | `{x, y}` | Maalien solmut. Nyt (1, 2) ja (9, 2), molemmat maaliviivalla keskikaistalla. Kierrossa toistensa kuvat. Vain laukauksen kohteita (D-033) |
| `zones` | lista `{id, xMin, xMax}` | Alueet x-väleinä (mukaan lukien): `defensive` 0–3, `neutral` 4–6, `offensive` 7–10. Jokainen x välillä 0 … length − 1 kuuluu tasan yhteen alueeseen. Solmun alue johdetaan tästä (Q-022) |
| `xgZones` | lista merkkijonoja | Laukaisuvyöhykkeiden nimet. Jokaisella on `tuning.json`:ssa `checks.shot.baseXg`- ja `checks.shot.attackerByXgZone`-arvo |
| `faceoffSpots` | lista `{id, x, y}` | Aloituspisteet, nimet oman joukkueen näkökulmasta |
| `nodes` | lista `{x, y, xgZone, isSlot}` | Kaikki `length × width` solmua id-järjestyksessä. **Ei `zone`-kenttää** |

**Solmun kentät.** Solmun alue johdetaan sen x-koordinaatista `zones`-osion väleillä, eikä sitä kirjoiteta solmuun (Q-022): yksi totuus, ei ristiriitamahdollisuutta. `xgZone` ja `isSlot` kuvaavat laukausta **vastustajan maalia kohti** solmun omasta näkökulmasta. Puolustava joukkue löytää suojattavan slottinsa kierrolla: hyökkääjän solmu (8, 2) on puolustajan näkökulmasta (10 − 8, 4 − 2) = (2, 2).

**Alueiden perustelu.** Maaliviivat ovat x = 1 ja x = 9, joten solmuväli on n. 22 jalkaa (178 ft / 8). Siniviiva on 64 jalan päässä maaliviivasta, eli x ≈ 3,9 ja x ≈ 6,1. Keskiviiva on x = 5. Päätyrivit x = 0 ja x = 10 kuvaavat maalin takaista aluetta (oikeasti n. 11 jalkaa syvä, mutta yksi rivi riittää): kierrätys, wraparound, maalin takaa syöttö ja kiekonhaku päätyyn ammutun kiekon jälkeen (E-002). Alueet ovat symmetriset kierrossa: defensive 0–3 ↔ offensive 7–10, neutral 4–6 ↔ 4–6.

**Laukaisuvyöhykkeet (hyökkäyspää, 11 × 5).** Rivit x = 7–10 (y 0 … 4):

| x | y = 0 | y = 1 | y = 2 | y = 3 | y = 4 |
| --- | --- | --- | --- | --- | --- |
| 10 (maalin takana) | boards | behindNet | behindNet | behindNet | boards |
| 9 (maaliviiva) | boards | lowAngle | maalisolmu (`crease`, ei käytössä) | lowAngle | boards |
| 8 (aloituspisteet) | boards | circle | slot | circle | boards |
| 7 (ympyröiden yläreuna) | boards | point | highSlot | point | boards |

x ≤ 6 on `longRange`. `behindNet` = laukaus maaliviivan takaa (wraparound), hyvin pieni xG (`checks.shot.baseXg.behindNet`). `isSlot` on tosi solmuissa (7, 2) ja (8, 2). Slotin rajaus ja `point`-vyöhykkeen sijainti x = 7:llä (siniviiva on x ≈ 6,1) ovat 5-leveän verkon kompromisseja, ks. Q-015.

**Maalisolmu ja `crease`** (D-033). (9, 2) on vastustajan maalisolmu: kukaan ei laukaise sieltä, joten se ei ole slottisolmu (`isSlot: false`), ja sen `xgZone`-arvo `crease` on vain nimilappu, jota laukaus ei koskaan käytä. Maalin edustan laukaukset lähtevät solmusta (8, 2) ja käyttävät `slot`-vyöhykettä. Solmu (8, 2) kattaa 22 jalan verkossa sekä maalin edustan että matalan slotin, eikä niitä voi erottaa sijainnilla. `checks.shot.baseXg.crease` ja `attackerByXgZone.crease` pysyvät datassa, mutta niitä ei käytetä, ennen kuin Jerry päättää, saavatko maalin edustan tilannelaukaukset (rebound, ohjaus maskista) `crease`-xG:n (ehdotus E-003).

**Aloituspisteet.** `center` (5, 2), `defensiveLeft` (2, 1), `defensiveRight` (2, 3), `neutralDefensiveLeft` (4, 1), `neutralDefensiveRight` (4, 3), `neutralOffensiveLeft` (6, 1), `neutralOffensiveRight` (6, 3), `offensiveLeft` (8, 1), `offensiveRight` (8, 3). Kierrossa `offensiveLeft` ↔ `defensiveRight`, `neutralOffensiveLeft` ↔ `neutralDefensiveRight` jne., `center` kiertyy itsekseen.

**Validointi.** Lataaja hylkää tiedoston, jos: solmuja ei ole tasan `length × width` tai ne eivät ole id-järjestyksessä; solmulla on tuntematon kenttä (myös vanha `zone`, Q-022); `zones`-välit eivät kata jokaista x:ää 0 … length − 1 tasan kerran tai alueen id on tuntematon (`defensive`, `neutral`, `offensive`); `xgZone` ei löydy `xgZones`-listasta; `ownGoal`, `opponentGoal` tai aloituspiste on verkon ulkopuolella; aloituspisteiden id:t eivät ole yksilöllisiä.

Uudet säännöt peilausta ja maalisolmuja varten (D-033, D-034; lataaja ei vielä tarkista, programmer lisää ennen kuvioiden lataajaa): maalisolmut ovat keskikaistalla (y = (width − 1) / 2) ja toistensa kuvat kierrossa; maalisolmun `isSlot` on epätosi; maalisolmu ei ole aloituspiste; kaukalo on symmetrinen y-suunnassa (solmuilla (x, y) ja (x, width − 1 − y) on sama `xgZone` ja `isSlot`); jokaisella `...Left`-aloituspisteellä on `...Right`-pari peilikuvasolmussa ja päinvastoin, ja keskikaistan aloituspisteen id ei pääty `Left` tai `Right`.

## tuning.json

Kaikki tasapainoarvot (CLAUDE.md, sääntö 2).

**Sallitut ylimmän tason avaimet.** Lataaja hylkää tiedoston, jos sen ylimmällä tasolla on avain, jota ei ole tässä listassa (`_`-alkuisia meta-avaimia lukuun ottamatta): `schemaVersion`, `stats`, `checkFormula`, `checks`, `positions`, `time`, `energy`, `organization`, `pressure`, `form`, `chemistry`, `familiarity`, `plays`, `chanceTypes`, `chanceClasses`. Lista vastaa lataajan sallittujen osioiden listaa. Osiot, joiden virstanpylväs ei ole vielä käynnissä, hyväksytään ja ohitetaan, kunnes koodi alkaa käyttää niitä.

**Uuden osion lisääminen.**

1. Designer kuvaa osion tähän dokumenttiin (alla oleva taulukko: nimi, virstanpylväs, sisältö, validointisäännöt) ja lisää sen `tuning.json`:iin, alkuarvaukset `_placeholders`-listaan.
2. Programmer lisää osion lataajan sallittujen osioiden listaan (ja tarvittaessa tyypitettyyn konfiguraatioon ja validointiin) **samassa muutoksessa**.
3. Data ja koodi commitoidaan yhdessä vasta, kun `dotnet test` menee läpi. Pelkkä datamuutos ilman lataajan päivitystä rikkoo latauksen.

Ylimmän tason osiot:

| Osio | Virstanpylväs | Sisältö |
| --- | --- | --- |
| `stats` | 1 | `min`, `max` (1, 20), `grades` (A–E-rajat, `[min, max]`) |
| `checkFormula` | 1 | `k` (0,15), `minProbability` (0,02), `maxProbability` (0,98) (D-014: koskee vain tarkistuksia, ei laukauksen maalintodennäköisyyttä eikä xG:tä), `referenceValue` (10,5: yksipuolisen tarkistuksen puuttuva puoli ja `...PerPoint`-muokkaajien nollakohta, D-019) |
| `checks` | 1 | Tarkistukset, ks. alla |
| `time` | 2 | `periods`, `periodSeconds`, `forwardShiftSeconds`, `defenceShiftSeconds` (kolmikot ja pakkiparit vaihtuvat erikseen, D-023), `secondsPerAction`, `setupSeconds`, `regroupSeconds` (Q-006) |
| `energy` | 3 | Kulutus, palautuminen penkillä, `checkModifierAtZero` (kaikkiin tarkistuksiin) |
| `positions` | 2 | `offSideCheckModifier`: väärän puolen miinus (D-024), ks. Pelaajat, pelipaikat ja ketjut |
| `organization` | 3 | Pudotus kiekonmenetyksessä alueittain, palautuminen, `organizedThreshold` |
| `pressure` | 3 | Kasvu, säilyminen katkolla, energian kulutus, henkisen kestävyyden vaimennus |
| `form`, `chemistry`, `familiarity` | 3+ | Vire (±`maxStatDelta`), ketjukemia, tuttuus |
| `plays` | 2 | `maxBeats` (4), `maxNodesPerBeat` |
| `chanceTypes` | 2 | Paikkatyyppien luokittelu (suorahyökkäys, kiekonriisto) |
| `chanceClasses` | 4 | Paikkaluokkien xG-rajat (Q-004) |

### Tarkistus (`checks.<nimi>`)

```json
"pass": {
  "kind": "twoSided",
  "p0": 0.85,
  "attacker": { "passer": { "passing": 0.6 }, "receiver": { "hands": 0.4 } },
  "defender": { "nearestDefender": { "awareness": 0.5, "positioning": 0.5 } },
  "modifiers": { "crossIce": -0.4, "pressure": -0.9 }
}
```

| Kenttä | Kuvaus |
| --- | --- |
| `kind` | Pakollinen. `twoSided` (kaksipuolinen, D-017), `oneSided` (yksipuolinen, D-019) tai `noCheck` (ei tarkistusta, vain arvoja muille tarkistuksille, nyt `dumpIn`) |
| `side` | Vain `oneSided`: läsnä oleva puoli, `attacker` tai `defender`. Toista puolta ei kirjoiteta |
| `p0` | Perustaso 0 < p0 < 1: onnistumisen todennäköisyys, kun H = D ja M = 0. Onnistuminen = hyökkääjäpuolen lopputulos (kerrottu `_notes`-kentässä), myös yksipuolisissa |
| `attacker`, `defender` | Puoli → osallistujarooli → stat → paino. **Painot summautuvat 1:een puolta kohden**: kaksipuolisessa molemmilla puolilla (D-017), yksipuolisessa läsnä olevalla puolella (D-019). Ei osallistujittain |
| `modifiers` | Nimi → logit-arvo (ks. Yleiset käytännöt). Tarkistuskohtaiset tilamuokkaajat (paine, järjestäytyneisyys) ovat täällä, kaikkia tarkistuksia koskevat (energia) omissa osioissaan |
| muut | Tarkistuskohtaiset lisäkentät, esim. `noWinnerShare`, `onTargetShare`, `baseXg` |

**Laskenta.** H = hyökkääjäpuolen Σ paino × stat, D = puolustajapuolen vastaava. P = clamp(logistic(logit(p0) + k (H − D) + M), `checkFormula.minProbability`, `checkFormula.maxProbability`).

- **Usean pelaajan rooli** (esim. `forecheckers`): jokaisen statin arvona käytetään roolin pelaajien statsien aritmeettista keskiarvoa, ja paino kerrotaan sillä (D-026). Keskiarvo lasketaan ennen painotusta, joten roolin osuus ei kasva pelaajamäärän mukana.
- **Yksipuolinen tarkistus** (`kind: "oneSided"`, D-019): puuttuva puoli korvataan arvolla `checkFormula.referenceValue` (10,5). Kaava ja etumerkki pysyvät samoina: kun `side` on `defender`, H = `referenceValue` ja D = puolustajapuolen painotettu arvo; kun `side` on `attacker`, D = `referenceValue`. Esimerkki: `block` onnistuu (laukaus menee läpi) todennäköisyydellä p0, kun blokkaajan Sijoittuminen on 10,5, ja parempi blokkaaja laskee todennäköisyyttä. `rebound` vastaavasti: parempi maalivahdin Rebound-kontrolli vähentää reboundeja slottiin.
- **Laukaus** (`shot`): maalintodennäköisyys ja xG rajataan arvoilla `checks.shot.minProbability` ja `checks.shot.maxProbability`, ei `checkFormula`-rajoilla (D-014, Q-009). Laukaukseen liittyvä `block` on tavallinen tarkistus ja käyttää `checkFormula`-rajoja.

**Osallistujaroolit:** `centre` (aloittava sentteri), `passer`, `receiver`, `carrier` (kiekollinen), `shooter`, `hitter`, `participant` (kamppailija), `nearestDefender`, `forecheckers` (karvaavat pelaajat, keskiarvo), `goalie`. Osallistujarooli ei ole pelipaikka.

**Statsien nimet.** Kenttäpelaajat: `speed`, `agility`, `endurance`, `hands`, `passing`, `shotAccuracy`, `shotPower`, `positioning`, `awareness`, `strength`, `discipline`, `faceoffs`. Maalivahdit: `reflexes`, `positioning`, `mobility`, `reboundControl`, `puckHandling`, `mentalToughness`.

**Tarkistukset nyt:** kaksipuoliset `faceoff`, `pass`, `zoneEntryCarry`, `deke`, `breakout`, `shot`, `loosePuck`, `hit`; yksipuoliset (`side: "defender"`) `block` ja `rebound`; `dumpIn` (`noCheck`, vain arvoja irtokiekkoon). Erikoisrakenteet:

- `dumpIn` (kuvion `dump`, D-031): `goaliePuckHandlingPerPoint` (logit per maalivahdin kiekonkäsittelypiste yli `referenceValue`:n, puolustavan joukkueen hyväksi) ja `goalieReachNodes` (kuinka lähellä maalisolmua, Chebyshev, kohteen pitää olla, jotta maalivahti ehtii kiekkoon). Ks. Kuviot, `dump`.
- `deke`: myös `skate`-toiminnon kuljetustarkistus, kun kuljetus ei ylitä alueen rajaa (D-032). `modifiers.defenderDistance` on taulukko lähimmän puolustajan etäisyydelle luistelureitistä (0, 1, 2+), sama janasääntö kuin `pass.modifiers.laneDefenderDistance`.

- `shot`: ei `p0`- eikä `attacker`-kenttää. Perustaso on `baseXg[xgZone]` ja hyökkääjän painot `attackerByXgZone[xgZone]` (laukaisijan solmu hänen näkökulmastaan). Järjestys: blokki → `onTargetShare` (maalia kohti vai ohi) → maalitarkistus (Q-010). Omat todennäköisyysrajat `minProbability` ja `maxProbability` (D-014, Q-009).
- `loosePuck`: kolme lopputulosta. Ensin `noWinnerShare`, sitten jäljelle jäävä osuus jaetaan voittoon ja häviöön logistisella tarkistuksella (Q-007).
- `rebound`: onnistuminen = rebound slottiin. Muuten maalivahti hallitsee kiekon: `controlledHoldShare` pitää (katko), loput kulmaan.

**Validointi.** Lataaja ohittaa `_`-avaimet ja hylkää tiedoston, jos: `kind` puuttuu tai on tuntematon; `twoSided`-tarkistukselta puuttuu jompikumpi puoli (`shot`: `attackerByXgZone` korvaa `attacker`-puolen) tai sillä on `side`; `oneSided`-tarkistukselta puuttuu `side`, `side`-puoli puuttuu tai toinen puoli on kirjoitettu; `noCheck`-tarkistuksella on `p0`, `attacker` tai `defender`; läsnä olevan puolen painojen summa poikkeaa 1:stä yli 1e-6; stat- tai roolinimi on tuntematon; p0 tai osuus on välin (0, 1) ulkopuolella; `checkFormula`:n tai `checks.shot`:n `minProbability` ≥ `maxProbability` tai jompikumpi on välin (0, 1) ulkopuolella; `referenceValue` on välin `stats.min`–`stats.max` ulkopuolella; `baseXg` tai `attackerByXgZone` ei kata täsmälleen `rink.json`:n `xgZones`-listaa; jokin `baseXg`-arvo on välin 0–1 ulkopuolella tai suurempi kuin `checks.shot.maxProbability` (Q-023: perustaso, jota laukauksen yläraja leikkaisi jo ennen statseja, on datavirhe); tai arvosanarajat eivät kata väliä `min`–`max` aukottomasti. Ylimmän tason tuntematon avain hylätään (ks. Sallitut ylimmän tason avaimet).

## Pelaajat, pelipaikat ja ketjut

Pelaajadatan skeema tulee myöhemmin (`roles.json`, D-018). Tämä osio kertoo sopimukset, joihin muu data jo nojaa.

**Pelipaikat** (D-024): `C`, `LW`, `RW`, `LD`, `RD`. Jokaisella kenttäpelaajalla on yksi ensisijainen pelipaikka. Jos pelaaja pelaa ensisijaisen paikkansa vastakkaisella puolella (`LW` ↔ `RW`, `LD` ↔ `RD`), jokaiseen tarkistukseen, johon hän osallistuu, lisätään `positions.offSideCheckModifier` (logit, negatiivinen) hänen oman puolensa vahingoksi: hyökkääjäpuolella se lisätään summaan M, puolustajapuolella vähennetään. Muokkaaja lasketaan kerran puolta kohden, vaikka puolella olisi useampi väärän puolen pelaaja. Sentterillä ei ole puolta. Muut paikkavaihdot (esim. sentteri laidassa, hyökkääjä pakkina) eivät ole vielä määriteltyjä.

**Ketjut** (D-023): hyökkäyskolmikot (`LW`, `C`, `RW`) ja pakkiparit (`LD`, `RD`) ovat erillisiä yksiköitä, jotka vaihtuvat eri tahtiin (`time.forwardShiftSeconds`, `time.defenceShiftSeconds`). Jäällä olevat viisi kenttäpelaajaa ovat aina yksi kolmikko ja yksi pari, ja mikä tahansa kolmikko voi pelata minkä tahansa parin kanssa. Kuvioiden paikat `LW`, `C`, `RW` viittaavat jäällä olevaan kolmikkoon ja `LD`, `RD` jäällä olevaan pariin. Järjestelmän roolit `F1`–`F3` jaetaan kolmikon ja `D1`–`D2` parin pelaajille. Ketjukemia (`chemistry`) kertyy yksikön sisällä; kolmikon ja parin välinen kemia on avoin virstanpylväälle 3.

## targets.json

Tavoitehaarukoiden ainoa totuus (D-016). Vain haarukoita, ei otteluiden raakadataa (CLAUDE.md, sääntö 5). Havainnot, joista haarukat johdetaan, ovat `docs/stats-and-checks.md`:n Tavoitearvot-taulukossa.

```json
"goalsPerMatch": { "unit": "count", "scope": "perMatch", "min": 4, "max": 7, "status": "approved", "source": "vision.md, Onnistumisen kriteerit" }
```

| Kenttä | Kuvaus |
| --- | --- |
| `metrics` | Mittarin nimi → määrittely. Raportti käyttää samaa nimeä |
| `unit` | `share` (0–1), `count`, `xg`, `seconds` |
| `scope` | `perMatch`, `perTeamPerMatch`, `perTeam`, `perShot`, `perMatchup` |
| `min`, `max` | Haarukka (mukaan lukien). `null`, kun haarukkaa ei vielä tiedetä |
| `status` | `approved` (Jerry asettanut) tai `placeholder` (ei haarukkaa, raportti näyttää arvon ilman OK/korkea/matala-tilaa) |
| `source` | Mistä mittari tulee |

Haarukoita muuttaa vain Jerry. Aloitusten voittoprosentti ja ykkösketjun jääaika eivät ole tavoitteita (ne kertovat hajonnasta ja väsymyksestä), joten ne eivät ole tiedostossa.

## Kuviot (`data/plays/*.json`) – hyväksytty (D-034)

Vastaa kysymykseen Q-001 (kuvion osalta). Yksi kuvio per tiedosto, tiedoston nimi = `id`. Jerryn ehdot (D-034): viisi toimintoa (D-031), kuviot viittaavat pelipaikkoihin, jokaisen kuvion voi pelata peilattuna kummallakin laidalla.

```json
{
  "schemaVersion": 1,
  "id": "pointShotScreen",
  "name": "Point shot with screen",
  "type": "offensiveZone",
  "start": {
    "puckCarrier": "LW",
    "positions": { "LW": [8, 0], "C": [8, 3], "RW": [9, 4], "LD": [7, 1], "RD": [7, 3] }
  },
  "beats": [
    {
      "moves": { "C": [9, 3] },
      "action": { "type": "pass", "from": "LW", "to": "LD" }
    },
    {
      "moves": { "LW": [9, 1] },
      "action": { "type": "driveNet", "by": "RW" }
    },
    {
      "moves": {},
      "action": { "type": "shoot", "by": "LD" }
    }
  ]
}
```

Idea: laituri syöttää laidasta siniviivalle, oikea laituri ajaa maalin eteen maskiin (8, 2), pakki laukoo maskin läpi.

| Kenttä | Kuvaus |
| --- | --- |
| `id` | camelCase, yksilöllinen, sama kuin tiedostonimi |
| `name` | Näyttönimi (lokalisointi myöhemmin) |
| `type` | `breakout` (avaus), `zoneEntry` (alueelle tulo), `offensiveZone` (alueella pelaaminen), `faceoff` (aloitus), `powerPlay` (ylivoima, myöhemmin) |
| `faceoffSpot` | Vain `faceoff`-tyypillä: `rink.json`:n aloituspisteen id |
| `start.positions` | Viiden kenttäpelaajan lähtösolmut paikoittain `LW`, `C`, `RW`, `LD`, `RD` |
| `start.puckCarrier` | Pelipaikka, jolla kiekko on kuvion alussa |
| `beats` | 1–`plays.maxBeats` (4) tahtia |
| `beats[].moves` | Pelipaikka → kohdesolmu. Puuttuva paikka pysyy paikallaan. Siirto enintään `plays.maxNodesPerBeat` solmua. Ei koskaan kiekollista (D-032) |
| `beats[].action` | Tasan yksi kiekkotoiminto, suoritetaan siirtojen jälkeen (D-032) |

Peilauslippua ei ole: aiempi `mirrorable`-kenttä on poistettu, ja lataaja hylkää sen tuntemattomana kenttänä. Ks. Peilaus.

**Pelipaikat, ei pelaajia** (D-034, ehto 2). Kaikki viittaukset (`start.positions`, `start.puckCarrier`, `moves`, toimintojen `by`, `from`, `to`) ovat pelipaikkoja `LW`, `C`, `RW`, `LD`, `RD`. Kuviossa ei ole pelaajien id:itä, nimiä eikä rooleja. Paikka tarkoittaa sitä jäällä olevaa pelaajaa, joka pelaa paikkaa (kolmikko ja pari, D-023), myös jos se ei ole hänen ensisijainen paikkansa (silloin väärän puolen miinus, D-024).

**Kiekkotoiminnot** (D-031):

| `type` | Kentät | Mitä tapahtuu | Tarkistus | Aika (`time.secondsPerAction.*`) |
| --- | --- | --- | --- | --- |
| `skate` | `by`, `to` (solmu) | Kiekollinen luistelee kiekon kanssa kohdesolmuun, enintään `plays.maxNodesPerBeat` solmua. Ainoa tapa liikuttaa kiekollista (D-032) | Aina tarkistus (D-032), yksi per toiminto: kohde hyökkäysalueella ja lähtö ei = `zoneEntryCarry`; lähtö omalla alueella ja kohde ei = `breakout`; muuten `deke` lähintä puolustajaa vastaan, `deke.modifiers.defenderDistance` reitin etäisyyden mukaan. Epäonnistuminen = kiekonmenetys | `skate` |
| `pass` | `from`, `to` (pelipaikka) | Kiekko siirtyy vastaanottajalle, jonka solmu on hänen sijaintinsa tahdin siirtojen jälkeen | `pass` | `pass` |
| `shoot` | `by` | Laukaus vastustajan maalisolmua kohti laukaisijan solmusta. Päättää kuvion | `block` → `onTargetShare` → `shot` (Q-010) | `shoot` |
| `driveNet` | `by` | Kiekoton pelaaja ajaa vastustajan maalin eteen (`netFront`, (8, 2)) maskiin ja reboundille (D-020, D-033). Kiekko ei liiku, kiekollinen pitää kiekon. Ainoa tapa päästä kuviossa maalin eteen | Ei tarkistusta | `driveNet` |
| `dump` | `by`, `to` (solmu) | Kiekollinen ampuu kiekon päätyyn kohdesolmuun, ja siellä seuraa irtokiekkokamppailu (E-001, D-031). Päättää kuvion | Ei omaa tarkistusta (`checks.dumpIn`, `noCheck`), sitten `loosePuck` kohdesolmussa | `dumpIn`, sitten `loosePuck` |

**`dump` tarkemmin.**

- Kiekko lentää suoraan kohdesolmuun. Matkalla ei ole katkoa eikä tarkistusta (`checks.dumpIn` on `noCheck`).
- Kohdesolmussa ratkaistaan `loosePuck`. Kamppailijat ja `extraPlayer`-muokkaaja määräytyvät irtokiekon yleisellä säännöllä (järjestelmätila, virstanpylväs 2). Jahtaaja asetetaan saman tahdin siirroilla, koska siirrot suoritetaan ennen toimintoa.
- **Maalivahti:** jos kohteen Chebyshev-etäisyys puolustavan joukkueen maalisolmuun (hyökkääjän näkökulmasta `opponentGoal`) on enintään `checks.dumpIn.goalieReachNodes` (1), kamppailun summaan M lisätään −`goaliePuckHandlingPerPoint` × (maalivahdin `puckHandling` − `checkFormula.referenceValue`). Hyvä kiekkoa pelaava maalivahti katkaisee maalin taakse ammutut kiekot, joten kulmaan ampuminen on eri valinta kuin maalin taakse ampuminen.
- Tapahtumaloki: "Kiekko päätyyn" (ampuja, kamppailun lopputulos).
- Kuvio päättyy, ja peli jatkuu järjestelmätilassa kamppailun lopputuloksen mukaan.

**Kulku.** Kun kuvio valitaan, pelaajat siirtyvät lähtösolmuihin (aika `time.setupSeconds`, Q-006). Tahdit suoritetaan järjestyksessä: ensin tahdin siirrot, sitten toiminto. Kiekollinen on tahdin alussa se, jolla kiekko on edellisen tahdin jälkeen (ensimmäisessä tahdissa `start.puckCarrier`). Epäonnistunut tarkistus päättää kuvion (kiekonmenetys tai irtokiekko), ja peli siirtyy järjestelmätilaan. `shoot` ja `dump` päättävät kuvion aina. Jos viimeinen tahti ei ole `shoot` tai `dump`, peli siirtyy järjestelmätilaan kiekko tallessa.

**Maski.** `checks.shot.modifiers.screen` pätee, kun laukaus menee maalia kohti ja joku laukaisijan joukkuetoveri on vastustajan maalin edustalla (`netFront`). Kuviossa sinne pääsee vain `driveNet`-toiminnolla.

**Peilaus** (D-034, ehto 3). Jokaisen kuvion voi pelata peilattuna, eikä kuvio voi kieltää sitä. Peilattu kuvio saadaan muunnoksella (ks. Koordinaatit, Peilaus): solmut y → width − 1 − y, pelipaikat `LW` ↔ `RW` ja `LD` ↔ `RD`, `faceoffSpot` `...Left` ↔ `...Right`. Pelipaikat vaihtuvat, jotta vasen laituri pelaa peilikuvassakin vasenta laitaa eikä saa väärän puolen miinusta. Koska kaukalo on symmetrinen y-suunnassa, validin kuvion peilikuva on aina validi, eikä sitä validoida erikseen. Simulaatio valitsee puolen deterministisesti:

- Kuvion **kirjoituspuoli** on kiekollisen lähtösolmun puoli (`start.positions[start.puckCarrier]`): vasen (y < 2), oikea (y > 2) tai keskikaista.
- `faceoff`-kuvio peilataan, kun aloitus on `faceoffSpot`-pisteen peilikuvapisteessä. Kuviota käytetään vain sen omassa ja peilikuvan pisteessä.
- Muu kuvio peilataan, kun kiekon solmu kuvion alkaessa on eri puolella kuin kirjoituspuoli. Jos kumpikaan on keskikaistalla, kuvio pelataan kirjoitetulla puolella.
- Valmentajan oma puolivalinta (esim. "aina oikealta") on myöhempi editorin ominaisuus, ei osa tätä skeemaa.

**Validointi.** Lataaja hylkää kuvion, jos:

- `schemaVersion` on tuntematon, `id` ei ole yksilöllinen tai ei vastaa tiedostonimeä, tai kuviossa on tuntematon kenttä (myös `mirrorable`);
- `type` on tuntematon, `faceoff`-tyypiltä puuttuu `faceoffSpot` tai se ei löydy kaukalosta, tai muulla tyypillä on `faceoffSpot`;
- pelipaikka on jokin muu kuin `LW`, `C`, `RW`, `LD`, `RD`, tai `start.positions` ei sisällä täsmälleen näitä viittä;
- tahteja on 0 tai yli `plays.maxBeats`;
- jokin solmu on verkon ulkopuolella;
- **kenttäpelaajan solmu on maalisolmu** (D-033): lähtösolmut, siirtojen kohteet, `skate.to` ja `dump.to`;
- kaksi pelaajaa on samassa solmussa lähdössä tai tahdin siirtojen ja toiminnon jälkeen;
- siirto tai `skate` on pidempi kuin `plays.maxNodesPerBeat`;
- **`moves` sisältää tahdin alun kiekollisen** (D-032) tai saman tahdin `driveNet`-toiminnon tekijän;
- siirto päättyy vastustajan maalin edustalle (`netFront`); sinne mennään `driveNet`-toiminnolla;
- tahdissa ei ole tasan yhtä `action`-kenttää tai toiminnon tyyppi on tuntematon;
- `skate.by`, `shoot.by`, `dump.by` tai `pass.from` ei ole tahdin alun kiekollinen; `pass.to` on sama kuin `pass.from`;
- `driveNet.by` on kiekollinen, on jo maalin edustalla tai on siitä yli `plays.maxNodesPerBeat` solmun päässä;
- `dump.to` ei ole hyökkäysalueella, tai `dump.by` on keskiviivan takana (x < keskiviiva: pitkä kiekko, jota ei kirjoiteta kuvioon);
- `shoot`- tai `dump`-tahdin jälkeen on tahteja.

## Puolustusjärjestelmät (`data/systems/*.json`) – hyväksytty (D-034)

Vastaa kysymykseen Q-001 (järjestelmän osalta). Säännöt ovat deterministisiä, jotta editori voi näyttää puolustajien haamut (tech-spec.md), ja viimeinen sääntö on aina varasääntö (D-034, ehto 4). Järjestelmä kirjoitetaan **puolustavan joukkueen omasta näkökulmasta** (oma maali (1, 2)).

```json
{
  "schemaVersion": 1,
  "id": "trap122",
  "name": "1-2-2 trap",
  "mirrorY": true,
  "rules": [
    {
      "when": { "puckZones": ["offensive"] },
      "targets": {
        "F1": { "puckOffset": [0, 0] },
        "F2": { "node": [6, 1] },
        "F3": { "node": [6, 3] },
        "D1": { "node": [4, 1] },
        "D2": { "node": [4, 3] }
      }
    },
    {
      "when": {},
      "targets": {
        "F1": { "puckOffset": [-1, 0] },
        "F2": { "node": [3, 1] },
        "F3": { "node": [3, 3] },
        "D1": { "node": [2, 1] },
        "D2": { "node": [2, 2] }
      }
    }
  ]
}
```

Esimerkin sijainnit ovat havainnollistus. Ensimmäiset järjestelmät (D-021) ovat selvästi erilaiset:

| `id` | Nimi | Idea |
| --- | --- | --- |
| `forecheck212` | 2-1-2 aggressiivinen karvaus | Kaksi hyökkääjää painostaa kiekollista hyökkäyspäässä, kolmas tukee ylempänä, pakit pitävät siniviivaa |
| `trap122` | 1-2-2 passiivinen / trap | Yksi karvaaja ohjaa kiekon laitaan, kaksi hyökkääjää ja pakit odottavat keskialueella ja oman siniviivan tuntumassa |

Järjestelmätiedostot kirjoitetaan virstanpylväässä 2. Alueella puolustamisen tarkemmat säännöt odottavat Q-003:a.

| Kenttä | Kuvaus |
| --- | --- |
| `id`, `name` | Kuten kuvioissa |
| `mirrorY` | Jos tosi, säännöt kirjoitetaan kanoniselle puolelle: kiekko vasemmalla tai keskikaistalla (y ≤ 2). Kun kiekko on oikealla (y > 2), kiekon solmu peilataan, sääntö valitaan peilatulla kiekolla ja valitun säännön kohteet peilataan takaisin (`node` [x, y] → [x, width − 1 − y], `puckOffset` [dx, dy] → [dx, −dy]). Kohteet saavat olla kummalla puolella tahansa. Jos epätosi, säännöt kattavat koko leveyden |
| `rules` | Järjestetty lista. Ensimmäinen sääntö, jonka `when` täsmää, ratkaisee |
| `when` | Ehdot, joiden kaikkien pitää täsmätä (JA). Tyhjä `{}` = aina tosi. Vain viimeisellä säännöllä on tyhjä `when` |
| `when.puckZones` | Lista alueita, joissa kiekko on (puolustajan näkökulmasta) |
| `when.puckX`, `when.puckY` | `[min, max]`-välit (mukaan lukien) kiekon koordinaateille |
| `when.puckState` | `controlled` (jollakin on kiekko) tai `loose` (irtokiekko) |
| `targets` | Rooli → kohde. Kaikille viidelle roolille `F1`, `F2`, `F3` (jäällä oleva hyökkäyskolmikko), `D1`, `D2` (jäällä oleva pakkipari) |
| kohde `node` | Kiinteä solmu `[x, y]` |
| kohde `puckOffset` | Kiekon solmu + `[dx, dy]`. `[0, 0]` = painostaa kiekollista |

**Arviointi.** Järjestelmä arvioidaan jokaisen tapahtuman jälkeen, kun puolustavalla joukkueella ei ole kiekkoa. Arviointi ei käytä satunnaisuutta, joten samasta tilanteesta tulee aina samat kohteet.

1. Kiekon solmu muunnetaan puolustajan näkökulmaan (kierto 180°, jos puolustaja on vierasjoukkue). Kiekko ei ole koskaan maalisolmussa (D-033).
2. `mirrorY`: jos kiekon y > 2, kiekko peilataan.
3. Valitaan ensimmäinen sääntö, jonka `when` täsmää. Viimeinen sääntö täsmää aina.
4. Roolit jaetaan (alla).
5. Kohteet lasketaan (alla) ja peilataan takaisin, jos kiekko peilattiin.

**Roolien jako** (Q-011). Hyökkääjät järjestetään etäisyyden mukaan kiekon solmuun: lähin on F1, sitten F2 ja F3. Pakeista lähin on D1. Etäisyys on Chebyshev, tasatilanteessa Manhattan |dx| + |dy|, ja jos sekin on tasan, kiinteä järjestys: hyökkääjistä `C`, sitten kiekon puolen laituri, sitten toinen laituri; pakeista ensin kiekon puolen pakki. Kiekon puoli: vasen tai keskikaista → `LW` ja `LD`, oikea → `RW` ja `RD` (puolustajan näkökulmasta). Näin järjestys on sama peilikuvassa. Rooli ei ole pelipaikka, joten sentteri voi olla F2.

**Kohteen laskenta.**

- `node`: solmu sellaisenaan.
- `puckOffset`: (kiekon x + dx, kiekon y + dy), jonka jälkeen x rajataan välille 0 … length − 1 ja y välille 0 … width − 1 erikseen. Jos tulos on maalisolmu (kumpi tahansa), se siirretään yhden askeleen keskiviivaa kohti x-suunnassa eli maalin edustalle (oma maali (1, 2) → (2, 2), vastustajan maali (9, 2) → (8, 2)).

**Liikkuminen.** Jokainen puolustaja liikkuu tapahtumaa kohden enintään `plays.maxNodesPerBeat` askelta kohti kohdettaan. Askel on (sign(dx), sign(dy)), eli vinottain, kunnes toinen koordinaatti täsmää. Jos askel osuisi maalisolmuun: kun sign(dy) ≠ 0, otetaan askel (sign(dx), 0); kun sign(dy) = 0, otetaan askel (sign(dx), s), jossa s = +1, jos kiekko on oikealla (y > 2), muuten −1. Kaksi puolustajaa saa päätyä samaan solmuun. Järjestäytyneisyys johdetaan siitä, kuinka moni puolustaja on kohteessaan tai kiekon takana (stats-and-checks.md), ja sen tarkka kaava kuuluu virstanpylvääseen 3.

**Validointi.** Lataaja hylkää järjestelmän, jos:

- `schemaVersion` on tuntematon, `id` ei ole yksilöllinen tai tiedostossa on tuntematon kenttä;
- `rules` on tyhjä, **viimeisen säännön `when` ei ole tyhjä `{}`**, tai jonkin muun säännön `when` on tyhjä (sitä seuraavat säännöt eivät koskaan täsmäisi);
- `when`-avain on tuntematon, `puckZones` on tyhjä tai sisältää tuntemattoman alueen, väli on `min > max` tai verkon ulkopuolella, tai `puckState` on tuntematon;
- `mirrorY`-järjestelmässä `when.puckY`-väli ulottuu oikealle puoliskolle (y > 2);
- säännöltä puuttuu jokin viidestä roolista tai siinä on tuntematon rooli, tai kohteella on muu kuin tasan yksi kentistä `node` ja `puckOffset`;
- `node` on verkon ulkopuolella tai **maalisolmu** (kumpi tahansa, D-033). Kohdesolmut saavat olla oikealla puolella.

## Määritelmät (D-048) – luonnos – odottaa Jerryn hyväksyntää

Tämä osio määrittelee D-048:n käsitteet niin tarkasti, että ne voi toteuttaa deterministisesti 11 × 5 -verkossa. Programmer ei toteuta näitä ennen Jerryn hyväksyntää. Kaikki koordinaatit ovat **hyökkäävän joukkueen näkökulmasta** (vastustajan maali (9, 2), `netFront` (8, 2), keskikaista y = 2), ellei toisin mainita. Sijainnit luetaan sillä hetkellä, kun toiminto ratkaistaan, eli tahdin hyökkääjien siirtojen ja puolustajien järjestelmäliikkeen jälkeen (D-041). "Puolustaja" tarkoittaa puolustavan joukkueen viittä kenttäpelaajaa; maalivahti ei ole näissä säännöissä koskaan puolustaja. Säännöt eivät käytä satunnaisuutta, ellei sitä erikseen mainita.

### M-1 Linjan solmut ja etäisyys linjasta

Sama sääntö koskee syöttölinjaa (syöttäjä → vastaanottaja), luistelureittiä (`skate`: lähtö → kohde) ja laukauslinjaa (laukaisija → maalisolmu).

**Linjan solmut** janalle A = (ax, ay) → B = (bx, by), pelkällä kokonaislukulaskennalla:

1. dx = bx − ax, dy = by − ay, n = max(|dx|, |dy|). Jos n = 0, linja on {A}.
2. Jokaiselle i = 0 … n ja kummallekin koordinaatille (a, d) ∈ {(ax, dx), (ay, dy)}: q = i · d, f = floor(q / n), r = q − f · n (0 ≤ r < n).
   - r = 0 → koordinaatti a + f.
   - 2r < n → a + f; 2r > n → a + f + 1.
   - 2r = n (tasan puolivälissä) → **molemmat**, a + f ja a + f + 1.
3. Linjan solmut ovat kaikkien näin saatujen solmujen joukko, päätepisteet mukaan lukien.

Pääakselilla (pidempi koordinaattiero) jako menee aina tasan, joten kullakin i:llä syntyy yksi tai kaksi solmua. Puolivälin kaksi solmua tekevät säännöstä symmetrisen: linja on sama molempiin suuntiin, peilattuna ja kierrettynä, joten peilatun kuvion riskit ovat alkuperäisen peilikuva (D-034). Esimerkkejä: (8, 1) → (8, 3) = {(8, 1), (8, 2), (8, 3)}; (7, 1) → (9, 2) = {(7, 1), (8, 1), (8, 2), (9, 2)}; (10, 1) → (8, 3) = {(10, 1), (9, 2), (8, 3)}.

**Etäisyys linjasta** puolustajalle = pienin Chebyshev-etäisyys puolustajan solmusta mihin tahansa linjan solmuun (0 = puolustaja on linjalla). **Linjan puolustaja** on se, jonka etäisyys on pienin; tasatilanteessa ratkaisee Chebyshev-etäisyys linjan viitesolmuun (syötössä vastaanottaja, luistelussa kohde, blokissa laukaisija, ks. M-5), sitten Manhattan-etäisyys samaan solmuun, sitten pelipaikkajärjestys `C`, `LW`, `RW`, `LD`, `RD`.

**Suositus: solmut linjalla, ei Chebyshev-etäisyys janaan.** Vaihtoehto "Chebyshev-etäisyys jatkuvaan janaan" tuottaa murtolukuja (esim. 0,5 vinossa syötössä), jotka pitäisi pyöristää taulukon indeksiksi, ja pyöristys rikkoisi joko symmetrian tai kokonaislukulaskennan. Solmusääntö on pelkkää kokonaislukulaskentaa, ja editori voi korostaa täsmälleen ne solmut, joita simulaatio käyttää.

**Käyttö.**

| Tarkistus | Linja | Taulukko | `nearestDefender` |
| --- | --- | --- | --- |
| `pass` | syöttäjän solmu → vastaanottajan solmu | `pass.modifiers.laneDefenderDistance[min(etäisyys, pituus − 1)]` | syöttölinjan puolustaja |
| `skate` (`deke`, `zoneEntryCarry`) | lähtö → kohde | `deke.modifiers.defenderDistance[...]` (vain `deke`) | reitin puolustaja |
| `block` | ks. M-5 | `block.modifiers.laneDistance[...]` | blokkaaja |

**Riskin väri** (editori, vision.md). Väri johdetaan syöttölinjan etäisyydestä `laneDefenderDistance`-taulukon indeksillä: indeksi 0 = punainen, viimeinen indeksi (ja sitä suuremmat etäisyydet) = vihreä, välissä keltainen. Nykyisellä kolmen arvon taulukolla: etäisyys 0 punainen, 1 keltainen, 2+ vihreä. Editori laskee värin saman tahdin puolustajahaamujen sijainneista. Erillisiä värirajoja ei ole, joten väri ja vaikutus eivät voi joutua ristiriitaan.

**`forecheckers`** (`breakout`): puolustavan joukkueen hyökkääjät (järjestelmäroolit F1–F3), jotka ovat hyökkäävän joukkueen omalla alueella (`defensive`). Jos sellaisia ei ole, rooliin otetaan F1 (lähin hyökkääjä kiekkoon).

### M-2 Poikittaissyöttö (`crossIce`)

Syöttö on poikittaissyöttö, kun syöttäjä ja vastaanottaja ovat keskikaistan **eri puolilla**: toisen y < (width − 1) / 2 ja toisen y > (width − 1) / 2, eli 11 × 5 -verkossa toinen y ≤ 1 ja toinen y ≥ 3. Syöttö keskikaistalta tai keskikaistalle ei ole poikittaissyöttö. Pätee kaikilla alueilla. `pass.modifiers.crossIce` lisätään, kun ehto on tosi.

### M-3 Royal Road (`royalRoad`)

Laukaus saa `shot.modifiers.royalRoad`-muokkaajan, kun **kaikki** pätevät:

1. Laukaus on onnistunutta syöttöä **seuraava kiekkotoiminto**, ja syötön vastaanottaja on laukaisija. Kuviossa: syöttö tahdissa n ja `shoot` tahdissa n + 1. Mikä tahansa välissä oleva toiminto katkaisee ehdon, myös `driveNet`, vaikka se ei näy tapahtumalokissa omana tapahtumanaan. Järjestelmätilassa sama: syötön ja laukauksen välissä ei ole muuta toimintoa eikä järjestelmätilan askelta.
2. Syöttö oli poikittaissyöttö (M-2).
3. Sekä syöttäjän että vastaanottajan x on hyökkäysalueella maaliviivan edessä tai sillä: hyökkäysalueen `xMin` ≤ x ≤ `opponentGoal.x`, 11 × 5 -verkossa 7 ≤ x ≤ 9. Päätyriviltä (x = 10) lähtevä tai sinne menevä syöttö ei ole Royal Road, koska se ei ylitä maalin ja siniviivan välistä keskilinjaa.

Kiekon kieli: poikittaissyöttö slotin yli hyökkäysalueella juuri ennen laukausta pakottaa maalivahdin sivuttaisliikkeeseen.

### M-4 Maski (`screen`)

Laukauksen maalitarkistukseen lisätään `shot.modifiers.screen`, kun kaikki pätevät:

1. joku laukaisijan joukkuetoveri on vastustajan maalin edustalla (`netFront`, (8, 2)) laukaushetkellä, riippumatta siitä, miten hän sinne tuli (kuviossa `driveNet`, järjestelmätilassa myöhemmin `netFrontAfterShot`);
2. laukaisija itse ei ole `netFront`-solmussa;
3. laukaisija on maaliviivan edessä (x < `opponentGoal.x`). Maalin takaa ei laukota maskin läpi.

Muokkaaja on ehto, ei määrä: kaksi pelaajaa maskissa ei tuplaa sitä. Maskissa oleva puolustaja ei poista maskia virstanpylväässä 2. Hyväksyttynä korvaa Kuviot-osion Maski-kappaleen (sisältö on sama, ehdot 2 ja 3 ovat uusia).

### M-5 Blokkaaja

1. **Laukauslinja** = M-1:n linjan solmut laukaisijan solmusta vastustajan maalisolmuun, **ilman maalisolmua**. Laukaisijan oma solmu kuuluu linjaan: samassa solmussa painostava puolustaja voi blokata.
2. **Ehdokkaat:** puolustajat, joiden etäisyys laukauslinjasta on enintään `checks.block.maxLaneDistance` (1).
3. Jos ehdokkaita ei ole, blokkitarkistusta ei tehdä ja laukaus jatkaa suoraan `onTargetShare`-vaiheeseen (Q-010).
4. **Blokkaaja** on ehdokas, jonka etäisyys linjasta on pienin. Tasatilanteessa ratkaisee pienempi Chebyshev-etäisyys laukaisijaan (lähempänä laukaisijaa oleva ehtii ensin eteen), sitten Manhattan-etäisyys laukaisijaan, sitten pelipaikkajärjestys.
5. `block`-tarkistuksen `nearestDefender` on blokkaaja. Muokkaajat: `distancePerNode` × Chebyshev(laukaisija, blokkaaja) ja `laneDistance[min(blokkaajan etäisyys linjasta, pituus − 1)]`, molemmat logit laukaisijan hyväksi (kaukaa ja linjan vierestä on vaikeampi blokata).

### M-6 Irtokiekon kamppailijat ja `extraPlayer`

Irtokiekko solmussa P (paikat: ks. Irtokiekon paikat alla).

1. **Hyökkääjäpuoli** on joukkue, jolla kiekko oli viimeksi: laukaisijan joukkue (rebound, blokattu, ohi), päätyyn ampunut joukkue (`dump`), syöttänyt joukkue (epäonnistunut syöttö). Toistuvassa kamppailussa (kohta 5) hyökkääjäpuoli pysyy samana.
2. **Kamppailijat:** kummaltakin joukkueelta yksi, sen kenttäpelaaja, joka on lähimpänä P:tä (yleinen sääntö: Chebyshev, Manhattan, pelipaikkajärjestys). Maalivahti ei ole kamppailija; `dump`-tilanteen maalivahtimuokkaaja on erikseen (`checks.dumpIn`).
3. **Matkaero:** M += `loosePuck.modifiers.distancePerNode` × (d_puolustaja − d_hyökkääjä), jossa d on kamppailijan Chebyshev-etäisyys P:hen. Lähempänä oleva ehtii kiekkoon ensin; Nopeus on jo kamppailun painoissa.
4. **`extraPlayer`:** lasketaan kummankin joukkueen kenttäpelaajat, joiden Chebyshev-etäisyys P:hen on enintään `checks.loosePuck.extraPlayerRadius` (1), kamppailijat mukaan lukien. Jos hyökkääjäpuolella on enemmän, M += `extraPlayer`; jos puolustajapuolella on enemmän, M −= `extraPlayer`; tasan 0. Lisätään kerran, ei pelaajaa kohden.
5. **Lopputulos:** voittaja saa kiekon solmussa P. "Ei voittajaa" → kiekko jää irtokiekoksi P:hen, kumpikin joukkue liikkuu yhden järjestelmätilan askeleen (`time.secondsPerAction.systemStep`), ja kamppailu ratkaistaan uudelleen uusilla kamppailijoilla.
6. Virstanpylväässä 2 järjestelmätilan ohje `looseChasers` on kiinteä: vain kohdan 2 lähin pelaaja kamppailee (D-046).

### M-7 Paineen alla (`underPressure`)

Pelaaja on **paineen alla**, kun ainakin yksi vastustajan kenttäpelaaja on enintään `pressure.underPressureNodes` (1) solmun päässä hänestä (Chebyshev) toiminnon hetkellä.

- **Syöttö:** tapahtuman kenttä "paineen alla" = syöttäjä on paineen alla. Kun se on tosi, syöttötarkistukseen lisätään ehtomuokkaaja `pass.modifiers.underPressure` (−0,9; analyysi 85 % → n. 70 %).
- **Nimimuutos hyväksynnän jälkeen:** nykyinen `pass.modifiers.pressure` nimetään `underPressure`:ksi (arvo ei muutu). Yleisten käytäntöjen mukaan `pressure`-niminen muokkaaja skaalautuu joukkueen painetilalla (hyökkäävän joukkueen momentum), eikä momentum saa heikentää hyökkäävän joukkueen omia syöttöjä. Analyysin "paineen alla" tarkoittaa fyysistä painetta syöttäjään. `underPressure` lisätään Yleisten käytäntöjen ehtomuokkaajien listaan.
- **Laukaus:** sama ehto laukaisijalle. Virstanpylväässä 2 se vain kirjataan raporttia varten (mittari "Laukaukset paineen alla (osuus)") eikä muuta todennäköisyyksiä. Laukaustapahtumassa ei vielä ole tätä kenttää, ks. Q-026. `shot.modifiers.pressure` pysyy joukkueen painetilaan sidottuna.

### M-8 Alueelle tulon voimasuhde "N vs M"

Hallittu alueelletuonti on joko `skate`, jonka kohde on hyökkäysalueella ja lähtö ei (`zoneEntryCarry`, tapa "kuljetus"), tai syöttö, jonka syöttäjä on hyökkäysalueen ulkopuolella ja vastaanottaja hyökkäysalueella (tapa "syöttö"). `dump` ei ole hallittu alueelletuonti, vaan oma tapahtumansa.

Lasketaan toiminnon hetkellä ennen tarkistusta:

- **N** = hyökkäävän joukkueen kenttäpelaajat, joiden x ≥ keskiviiva (x ≥ 5). Kiekollinen tai syöttäjä lasketaan aina mukaan.
- **M** = puolustavan joukkueen kenttäpelaajat, joiden x ≥ kiekon x (kiekollisen tai syöttäjän solmu), eli kiekon tasalla tai kiekon ja oman maalinsa välissä.
- Kirjataan muodossa "N vs M", esim. "3 vs 2". Kiekon kuljettaja = kiekollinen (kuljetus) tai vastaanottaja (syöttö). Lopputulos: hallinta säilytetty (tarkistus onnistui) tai menetetty.

### Irtokiekon paikat (D-044) ja epäonnistunut syöttö (D-040)

Paikkasäännöt kirjoitetaan `tuning.json`:n uuteen ylimmän tason osioon `loosePuckSpots` (D-044). Arvo on säännön nimi alla olevasta taulukosta. Osio vaatii lataajan tuen (D-030), joten se lisätään dataan samassa muutoksessa kuin lataajan tuki:

```json
"loosePuckSpots": {
  "reboundSlot": "netFront",
  "reboundCorner": "shooterSideCorner",
  "missedShot": "endRowShooterLane",
  "blockedShot": "blockerNode",
  "failedPass": "laneDefenderNode"
}
```

| Avain | Tilanne | Sääntö | Solmu (hyökkääjän näkökulma, 11 × 5) |
| --- | --- | --- | --- |
| `reboundSlot` | `rebound` onnistui (rebound slottiin) | `netFront` | (8, 2) |
| `reboundCorner` | `rebound` epäonnistui eikä maalivahti pitänyt kiekkoa (osuus 1 − `controlledHoldShare`) | `shooterSideCorner`: maaliviivan kulma laukaisijan puolella | (9, 0), jos laukaisijan y < 2; (9, 4), jos y > 2; keskikaistalta yksi `Sim.Random`-arvonta 50 / 50 |
| `missedShot` | laukaus ohi (`onTargetShare`) | `endRowShooterLane`: päätyrivi laukaisijan kaistalla | (10, laukaisijan y) |
| `blockedShot` | blokki onnistui puolustajalle | `blockerNode`: blokkaajan solmu | blokkaajan solmu (M-5) |
| `failedPass` | syöttö epäonnistui eikä tullut syötönkatkoa | `laneDefenderNode`: syöttölinjan puolustajan solmu | M-1:n syöttölinjan puolustaja |

Koodi johtaa solmut `rink.json`:sta: kulma = (`opponentGoal.x`, 0) tai (`opponentGoal.x`, width − 1), päätyrivi x = length − 1. Mikään sääntö ei anna maalisolmua (D-033): (10, 2) on päätyrivillä maalin takana. Kaikista paikoista seuraa M-6:n kamppailu. Lataaja hylkää tuntemattoman avaimen, puuttuvan avaimen ja tuntemattoman säännön nimen.

**Epäonnistunut syöttö (D-040).** Kun `pass` epäonnistuu, arvotaan yhdellä `Sim.Random`-arvolla `checks.pass.interceptionShare` (0,6):

- **syötönkatko** (todennäköisyys `interceptionShare`): syöttölinjan puolustaja saa kiekon omassa solmussaan. Tapahtumat: Syöttö (onnistui: ei) ja Kiekonmenetys / riisto (menettäjä = syöttäjä, riistäjä = linjan puolustaja).
- **irtokiekko** (muuten): kiekko jää linjan puolustajan solmuun (`loosePuckSpots.failedPass`), ja M-6:n kamppailu ratkaistaan, hyökkääjäpuolena syöttänyt joukkue.

### Maskin xG (D-047)

Laukaus käyttää `crease`-vyöhykettä (`baseXg.crease`, `attackerByXgZone.crease`) solmun oman vyöhykkeen (`slot`) sijaan, kun laukaisija on `netFront`-solmussa (8, 2) ja **jompikumpi** pätee:

1. **Rebound:** laukaisija voitti `reboundSlot`-irtokiekon kamppailun, ja laukaus on sitä seuraava kiekkotoiminto.
2. **Ohjaus / syöttö maskiin:** laukaisija tuli `netFront`-solmuun `driveNet`-toiminnolla eikä ole liikkunut sen jälkeen, ja laukaus on onnistunutta syöttöä hänelle seuraava kiekkotoiminto (sama ehto kuin M-3:n kohdassa 1).

Muut laukaukset solmusta (8, 2), esim. kiekollinen luistelee sinne ja laukoo, käyttävät `slot`-vyöhykettä. Maski (M-4) ei koskaan päde `crease`-laukaukseen, koska laukaisija on itse maskissa. Royal Road (M-3) voi päteä, jos syöttö maskiin oli poikittaissyöttö, mutta (8, 2) on keskikaistalla, joten käytännössä ei. Hyväksyttynä korvaa rink.json-osion kappaleen "Maalisolmu ja `crease`" viimeisen virkkeen, koska D-047 on hyväksytty.

### Uudet tasapainoarvot tälle luonnokselle

Kaikki ovat paikkamerkkejä (`_placeholders`). Ne eivät vaikuta mihinkään ennen toteutusta.

| Polku | Arvo | Merkitys |
| --- | --- | --- |
| `checks.pass.interceptionShare` | 0,6 | Epäonnistuneista syötöistä syötönkatkojen osuus (D-040) |
| `checks.block.maxLaneDistance` | 1 | Blokkaajaehdokkaan suurin etäisyys laukauslinjasta (M-5) |
| `checks.block.modifiers.laneDistance` | [0,0, 1,0] | Logit laukaisijan hyväksi blokkaajan linjaetäisyyden mukaan (M-5) |
| `checks.loosePuck.extraPlayerRadius` | 1 | `extraPlayer`-laskennan säde (M-6) |
| `checks.loosePuck.modifiers.distancePerNode` | 0,5 | Logit per solmu kamppailijoiden etäisyyserosta (M-6) |
| `pressure.underPressureNodes` | 1 | Paineen alla -säde (M-7) |
| `time.secondsPerAction.systemStep` | 2 | Järjestelmätilan askel: kiekollinen joukkue pitää kiekkoa (D-036) tai irtokiekko jäi ilman voittajaa (M-6) |
| `loosePuckSpots` | ks. yllä | Ei vielä datassa, odottaa lataajan tukea (D-030) |
