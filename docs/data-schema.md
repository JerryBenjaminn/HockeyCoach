# Kiekkovalmentaja – dataskeemat

Oct 9, 2026 · game-designer

Tämä dokumentti on datatiedostojen skeemojen ainoa totuus (D-013). Designer omistaa ja ylläpitää sitä. Jos data, koodi ja tämä dokumentti ovat ristiriidassa, tämä dokumentti voittaa, ellei lukittu päätös sano muuta.

| Tiedosto | Tila | Kuka lukee |
| --- | --- | --- |
| `data/rink.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/tuning.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/targets.json` | käytössä | Harness (raportin tavoitevertailu) |
| `data/plays/*.json` | käytössä (skeema D-034) | Sim (`Sim.Tactics`) |
| `data/systems/*.json` | käytössä (skeema D-034) | Sim (`Sim.Tactics`) |
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
| Energia, paine, järjestäytyneisyys | 0–1 | `"organizedThreshold": 0.8` |
| Muutosnopeus | 0–1 per pelisekunti | `"recoveryPerSecond": 0.04` |
| Muokkaaja | logit-yksikköä, lisätään summaan M | `"crossIce": -0.4` |
| Etäisyys | solmuja (Chebyshev-etäisyys, ks. kaukalo) | `"maxNodesPerBeat": 2` |

**Muokkaajat logit-yksikköinä.** Jokainen muokkaaja lisätään tarkistuskaavan summaan M (`docs/stats-and-checks.md`, Tarkistukset). Positiivinen arvo auttaa hyökkääjäpuolta. Avaimen nimi kertoo, miten arvo skaalautuu:

| Nimen muoto | Merkitys |
| --- | --- |
| `pressure`, `organization`, `goalieEnergy` (tilaan sidottu) | Logit täydellä vaikutuksella, lineaarisesti välissä. Järjestäytyneisyys: arvo × (1 − puolustuksen järjestäytyneisyys). Paine: arvo × paine. Energia: arvo × (1 − energia) |
| `crossIce`, `royalRoad`, `screen`, `screenContested`, `underPressure`, `homeAdvantage` (ehto) | Logit, kun ehto on tosi, muuten 0. `screenContested` korvaa `screen`-arvon, ei lisäydy siihen (M-4) |
| `energy.checkModifierAtZero`, `familiarity` (kaikkia tarkistuksia tai kuvion tarkistuksia koskevat) | Omissa osioissaan, ks. Ottelu ja tilat O-5 ja O-11 |
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

**Maalisolmu ja `crease`** (D-033). (9, 2) on vastustajan maalisolmu: kukaan ei laukaise sieltä, joten se ei ole slottisolmu (`isSlot: false`), ja sen `xgZone`-arvo `crease` on vain nimilappu, jota laukaus ei koskaan käytä. Maalin edustan laukaukset lähtevät solmusta (8, 2) ja käyttävät `slot`-vyöhykettä. Solmu (8, 2) kattaa 22 jalan verkossa sekä maalin edustan että matalan slotin, eikä niitä voi erottaa sijainnilla. Poikkeus: maalin edustan tilannelaukaukset (rebound ja syöttö maskiin ajaneelle) käyttävät `crease`-vyöhykettä (`checks.shot.baseXg.crease`, `attackerByXgZone.crease`), ks. Määritelmät, Maskin xG (D-047).

**Aloituspisteet.** `center` (5, 2), `defensiveLeft` (2, 1), `defensiveRight` (2, 3), `neutralDefensiveLeft` (4, 1), `neutralDefensiveRight` (4, 3), `neutralOffensiveLeft` (6, 1), `neutralOffensiveRight` (6, 3), `offensiveLeft` (8, 1), `offensiveRight` (8, 3). Kierrossa `offensiveLeft` ↔ `defensiveRight`, `neutralOffensiveLeft` ↔ `neutralDefensiveRight` jne., `center` kiertyy itsekseen.

**Validointi.** Lataaja hylkää tiedoston, jos: solmuja ei ole tasan `length × width` tai ne eivät ole id-järjestyksessä; solmulla on tuntematon kenttä (myös vanha `zone`, Q-022); `zones`-välit eivät kata jokaista x:ää 0 … length − 1 tasan kerran tai alueen id on tuntematon (`defensive`, `neutral`, `offensive`); `xgZone` ei löydy `xgZones`-listasta; `ownGoal`, `opponentGoal` tai aloituspiste on verkon ulkopuolella; aloituspisteiden id:t eivät ole yksilöllisiä.

Uudet säännöt peilausta ja maalisolmuja varten (D-033, D-034; lataaja ei vielä tarkista, programmer lisää ennen kuvioiden lataajaa): maalisolmut ovat keskikaistalla (y = (width − 1) / 2) ja toistensa kuvat kierrossa; maalisolmun `isSlot` on epätosi; maalisolmu ei ole aloituspiste; kaukalo on symmetrinen y-suunnassa (solmuilla (x, y) ja (x, width − 1 − y) on sama `xgZone` ja `isSlot`); jokaisella `...Left`-aloituspisteellä on `...Right`-pari peilikuvasolmussa ja päinvastoin, ja keskikaistan aloituspisteen id ei pääty `Left` tai `Right`.

## tuning.json

Kaikki tasapainoarvot (CLAUDE.md, sääntö 2).

**Sallitut ylimmän tason avaimet.** Lataaja hylkää tiedoston, jos sen ylimmällä tasolla on avain, jota ei ole tässä listassa (`_`-alkuisia meta-avaimia lukuun ottamatta): `schemaVersion`, `stats`, `checkFormula`, `checks`, `positions`, `time`, `energy`, `organization`, `pressure`, `form`, `chemistry`, `familiarity`, `plays`, `chanceTypes`, `chanceClasses`, `loosePuckSpots`, `transitions`. Lista vastaa lataajan sallittujen osioiden listaa. `transitions` (virstanpylväs 3) lisätään lataajaan ja dataan samassa commitissa (ks. Ottelu ja tilat, Lataajan muutokset). Osiot, joiden virstanpylväs ei ole vielä käynnissä, hyväksytään ja ohitetaan, kunnes koodi alkaa käyttää niitä.

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
| `time` | 2–3 | `periods`, `periodSeconds`, `forwardShiftSeconds`, `defenceShiftSeconds` (kolmikot ja pakkiparit vaihtuvat erikseen, D-023), `stoppageChangeMinSeconds` (virstanpylväs 3, O-2), `secondsPerAction` (myös `systemStep`: järjestelmätilan askel, D-036), `setupSeconds`, `regroupSeconds` (Q-006) |
| `energy` | 3 | `start`, `drainPerSecondOnIce`, `costPerAction`, `enduranceCostReductionPerPoint`, `benchRecoveryRate` (eksponentiaalinen), `checkModifierAtZero`. Ks. O-5 |
| `positions` | 2 | `offSideCheckModifier`: väärän puolen miinus (D-024), ks. Pelaajat, pelipaikat ja ketjut |
| `organization` | 3 | `dropOnTurnover` alueittain, `dropFactorOnShotOrDump`, `dropPerCommittedPlayer`, `recoveryPerSecond`, `recoveryWeights`, `recoveryPerStatPoint`, `organizedThreshold`. Ks. O-6 |
| `pressure` | 2–3 | Joukkueen painetila: `gainOnZoneEntry`, `gainOnShot`, `gainPerSecondInZone`, `decayPerSecond`, `keepOnStoppage`, `energyDrainPerSecond`, `mentalToughnessReductionPerPoint` (O-9). Lisäksi `underPressureNodes` (virstanpylväs 2): fyysisen paineen säde, ei joukkueen painetila (Määritelmät M-7, D-049, D-056) |
| `familiarity` | 3 | `freeUses`, `penaltyPerRepeat`, `maxPenalty`, `intermissionMultiplier`. Ks. O-11 |
| `form`, `chemistry` | myöhemmin | Vire (±`maxStatDelta`), ketjukemia. Eivät käytössä virstanpylväässä 3 (vaikutus 0) |
| `plays` | 2 | `maxBeats` (4), `maxNodesPerBeat` |
| `transitions` | 3 | `rushMaxActions`, `rushShotMinBaseXg`: sisäänrakennettu suorahyökkäys. Ks. O-7 |
| `chanceTypes` | 2–3 | `turnoverWindowSeconds`. Suorahyökkäyksen raja on `organization.organizedThreshold` (O-12); `rushOrganizationBelow` poistuu |
| `chanceClasses` | 4 | Paikkaluokkien xG-rajat (Q-004) |
| `loosePuckSpots` | 2 | Irtokiekon paikkasäännöt (D-044, D-049), ks. Määritelmät, Irtokiekon paikat. Data ja lataajan tuki commitoidaan yhdessä (D-030) |

### Tarkistus (`checks.<nimi>`)

```json
"pass": {
  "kind": "twoSided",
  "p0": 0.85,
  "attacker": { "passer": { "passing": 0.6 }, "receiver": { "hands": 0.4 } },
  "defender": { "nearestDefender": { "awareness": 0.5, "positioning": 0.5 } },
  "modifiers": { "crossIce": -0.4, "underPressure": -0.9 }
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

- `shot`: ei `p0`- eikä `attacker`-kenttää. Perustaso on `baseXg[xgZone]` ja hyökkääjän painot `attackerByXgZone[xgZone]` (laukaisijan solmu hänen näkökulmastaan). Järjestys: blokki → `onTargetShare` (maalia kohti vai ohi) → ohi menneelle `missedOutOfPlayShare` (ulos kaukalosta, O-13) / maalia kohti menneelle maalitarkistus (Q-010). Omat todennäköisyysrajat `minProbability` ja `maxProbability` (D-014, Q-009). `modifiers.screenContested` korvaa `modifiers.screen`-arvon, kun maskissa on puolustaja (M-4).
- Järjestäytyneisyyden muokkaaja `modifiers.organization` (O-6) on tarkistuksissa `pass`, `zoneEntryCarry`, `deke`, `breakout`, `shot` ja `block`. Muissa tarkistuksissa sitä ei ole, eikä järjestäytyneisyys vaikuta niihin.
- `loosePuck`: kolme lopputulosta. Ensin `noWinnerShare`, sitten jäljelle jäävä osuus jaetaan voittoon ja häviöön logistisella tarkistuksella (Q-007). `extraPlayerRadius` ja `modifiers.distancePerNode`: kamppailijat ja lisäpelaaja, ks. Määritelmät M-6.
- `pass`: `interceptionShare` = epäonnistuneista syötöistä syötönkatkojen osuus, loput irtokiekkoja (D-040, ks. Määritelmät). `modifiers.underPressure` on ehtomuokkaaja: syöttäjä on paineen alla (M-7, D-049). Se ei skaalaudu joukkueen painetilalla.
- `block`: `maxLaneDistance` (blokkaajaehdokkaan suurin etäisyys laukauslinjasta) ja `modifiers.laneDistance` (taulukko), ks. Määritelmät M-5.
- `rebound`: onnistuminen = rebound slottiin. Muuten maalivahti hallitsee kiekon: `controlledHoldShare` pitää (katko), loput kulmaan.

**Validointi.** Lataaja ohittaa `_`-avaimet ja hylkää tiedoston, jos: `kind` puuttuu tai on tuntematon; `twoSided`-tarkistukselta puuttuu jompikumpi puoli (`shot`: `attackerByXgZone` korvaa `attacker`-puolen) tai sillä on `side`; `oneSided`-tarkistukselta puuttuu `side`, `side`-puoli puuttuu tai toinen puoli on kirjoitettu; `noCheck`-tarkistuksella on `p0`, `attacker` tai `defender`; läsnä olevan puolen painojen summa poikkeaa 1:stä yli 1e-6; stat- tai roolinimi on tuntematon; p0 tai osuus on välin (0, 1) ulkopuolella; `checkFormula`:n tai `checks.shot`:n `minProbability` ≥ `maxProbability` tai jompikumpi on välin (0, 1) ulkopuolella; `referenceValue` on välin `stats.min`–`stats.max` ulkopuolella; `baseXg` tai `attackerByXgZone` ei kata täsmälleen `rink.json`:n `xgZones`-listaa; jokin `baseXg`-arvo on välin 0–1 ulkopuolella tai suurempi kuin `checks.shot.maxProbability` (Q-023: perustaso, jota laukauksen yläraja leikkaisi jo ennen statseja, on datavirhe); tai arvosanarajat eivät kata väliä `min`–`max` aukottomasti. Ylimmän tason tuntematon avain hylätään (ks. Sallitut ylimmän tason avaimet).

## Pelaajat, pelipaikat ja ketjut

Pelaajadatan skeema tulee myöhemmin (`roles.json`, D-018). Tämä osio kertoo sopimukset, joihin muu data jo nojaa.

**Pelipaikat** (D-024): `C`, `LW`, `RW`, `LD`, `RD`. Jokaisella kenttäpelaajalla on yksi ensisijainen pelipaikka. Jos pelaaja pelaa ensisijaisen paikkansa vastakkaisella puolella (`LW` ↔ `RW`, `LD` ↔ `RD`), jokaiseen tarkistukseen, johon hän osallistuu, lisätään `positions.offSideCheckModifier` (logit, negatiivinen) hänen oman puolensa vahingoksi: hyökkääjäpuolella se lisätään summaan M, puolustajapuolella vähennetään. Muokkaaja lasketaan kerran puolta kohden, vaikka puolella olisi useampi väärän puolen pelaaja. Sentterillä ei ole puolta. Muut paikkavaihdot (esim. sentteri laidassa, hyökkääjä pakkina) eivät ole vielä määriteltyjä.

**Ketjut** (D-023): hyökkäyskolmikot (`LW`, `C`, `RW`) ja pakkiparit (`LD`, `RD`) ovat erillisiä yksiköitä, jotka vaihtuvat eri tahtiin (`time.forwardShiftSeconds`, `time.defenceShiftSeconds`). Jäällä olevat viisi kenttäpelaajaa ovat aina yksi kolmikko ja yksi pari, ja mikä tahansa kolmikko voi pelata minkä tahansa parin kanssa. Kuvioiden paikat `LW`, `C`, `RW` viittaavat jäällä olevaan kolmikkoon ja `LD`, `RD` jäällä olevaan pariin. Järjestelmän roolit `F1`–`F3` jaetaan kolmikon ja `D1`–`D2` parin pelaajille. Ketjukemia (`chemistry`) kertyy yksikön sisällä; kolmikon ja parin välinen kemia on avoin virstanpylväälle 3.

## targets.json

Tavoitehaarukoiden ainoa totuus (D-016). Vain haarukoita, ei otteluiden raakadataa (CLAUDE.md, sääntö 5). Havainnot, joista haarukat johdetaan, ovat `docs/stats-and-checks.md`:n Tavoitearvot-taulukossa.

```json
"goalsPerMatch": { "_note": "Both teams combined ...", "unit": "count", "scope": "perMatch", "min": 4, "max": 7, "status": "approved", "source": "vision.md, Onnistumisen kriteerit" }
```

| Kenttä | Kuvaus |
| --- | --- |
| `metrics` | Mittarin nimi → määrittely. Raportti käyttää samaa nimeä |
| `_note` | Vapaaehtoinen meta-avain: mittarin tarkka määrittely ihmisille. Lataaja ohittaa sen |
| `unit` | `share` (0–1), `count`, `xg`, `seconds` |
| `scope` | `perMatch`, `perTeamPerMatch`, `perTeam`, `perShot`, `perMatchup` (ks. alla) |
| `min`, `max` | Haarukka (mukaan lukien). `null`, kun haarukkaa ei vielä tiedetä |
| `status` | `approved` (Jerry asettanut) tai `placeholder` (ei haarukkaa, raportti näyttää arvon ilman OK/korkea/matala-tilaa) |
| `source` | Mistä mittari tulee |

**Laajuudet (`scope`).**

| `scope` | Miten raportti laskee arvon |
| --- | --- |
| `perMatch` | **Molemmat joukkueet yhteensä** yhdessä ottelussa (koti + vieras), keskiarvo batchin kaikista otteluista. `goalsPerMatch` = kotimaalit + vierasmaalit varsinaisella peliajalla 3 × 1 200 s (vision.md: "Ottelussa syntyy keskimäärin 4–7 maalia"). Virstanpylväässä 3 tasapeli sallitaan eikä jatkoaikaa ole |
| `perTeamPerMatch` | Yhden joukkueen arvo yhdessä ottelussa, keskiarvo kaikista (joukkue, ottelu) -pareista |
| `perTeam` | Osuus yhden joukkueen tapahtumista koko batchissa (esim. syötöt), raportoidaan joukkueittain ja yhteensä |
| `perShot` | Yhden laukauksen arvo |
| `perMatchup` | AI-parin tulos koko batchissa (esim. voitto-osuus) |

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

**Kulku.** Kun kuvio valitaan, hyökkäävän joukkueen pelaajat siirtyvät lähtösolmuihin (aika `time.setupSeconds`, Q-006). Puolustajat eivät siirry suoraan järjestelmänsä kohteisiin, vaan liikkuvat setupin aikana rajatun määrän askelia (D-062, ks. Ottelu ja tilat O-6). Tahdit suoritetaan järjestyksessä: ensin tahdin siirrot, sitten toiminto. Kiekollinen on tahdin alussa se, jolla kiekko on edellisen tahdin jälkeen (ensimmäisessä tahdissa `start.puckCarrier`). Epäonnistunut tarkistus päättää kuvion (kiekonmenetys tai irtokiekko), ja peli siirtyy järjestelmätilaan. `shoot` ja `dump` päättävät kuvion aina. Jos viimeinen tahti ei ole `shoot` tai `dump`, peli siirtyy järjestelmätilaan kiekko tallessa.

**Maski.** `checks.shot.modifiers.screen` pätee, kun laukaus menee maalia kohti, joku laukaisijan joukkuetoveri on vastustajan maalin edustalla (`netFront`), laukaisija itse ei ole siellä ja laukaisija on maaliviivan edessä (Määritelmät M-4). Kuviossa maalin edustalle pääsee vain `driveNet`-toiminnolla.

**Peilaus** (D-034, ehto 3). Jokaisen kuvion voi pelata peilattuna, eikä kuvio voi kieltää sitä. Peilattu kuvio saadaan muunnoksella (ks. Koordinaatit, Peilaus): solmut y → width − 1 − y, pelipaikat `LW` ↔ `RW` ja `LD` ↔ `RD`, `faceoffSpot` `...Left` ↔ `...Right`. Pelipaikat vaihtuvat, jotta vasen laituri pelaa peilikuvassakin vasenta laitaa eikä saa väärän puolen miinusta. Koska kaukalo on symmetrinen y-suunnassa, validin kuvion peilikuva on aina validi, eikä sitä validoida erikseen. Simulaatio valitsee puolen deterministisesti:

- Kuvion **kirjoituspuoli** on kiekollisen lähtösolmun puoli (`start.positions[start.puckCarrier]`): vasen (y < 2), oikea (y > 2) tai keskikaista.
- `faceoff`-kuvio peilataan, kun aloitus on `faceoffSpot`-pisteen peilikuvapisteessä. Kuviota käytetään vain sen omassa ja peilikuvan pisteessä.

**Aloituksen jälkeen (D-060).** Aloitustarkistus ratkaisee vain, kumpi joukkue saa kiekon. Jos voittajalla on aloituskuvio pisteelle, kuvion (tarvittaessa peilatun) `start.puckCarrier` saa kiekon lähtösolmussaan ilman syöttöä ja ilman tarkistusta, ja kuvio alkaa ensimmäisestä tahdista ilman setup-aikaa (pelaajat ovat jo aloitusasettelussa). Jos aloituskuviota ei ole, kiekko menee aloituspisteen puoleiselle pakille voittajan näkökulmasta: pisteen y < keskikaista tai y = keskikaista → `LD`, y > keskikaista → `RD`. Sen jälkeen kuvio valitaan D-036:n mukaan. Aloituskuvion kiekollinen on yleensä pakki (esim. `offensiveFaceoffPointShot`: `LD`).
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
- **syöttö menee suoraan maalin läpi** (D-051): syöttäjä ja vastaanottaja ovat molemmat keskikaistalla (y = (width − 1) / 2) ja saman maalisolmun eri puolilla (toisen x < maalisolmun x ja toisen x > maalisolmun x), kumman tahansa maalin. Solmut luetaan tahdin siirtojen jälkeen, kuten syötössä muutenkin. Muut syötöt saavat kulkea maalisolmun kautta (M-1), esim. vino syöttö maalin takaa (10, 1) slottiin (8, 3);
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

**Liikkuminen.** Jokainen puolustaja liikkuu tapahtumaa kohden enintään `plays.maxNodesPerBeat` askelta kohti kohdettaan (setupissa ja regroupissa rajattu askelmäärä, O-6). Askel on (sign(dx), sign(dy)), eli vinottain, kunnes toinen koordinaatti täsmää. Jos askel osuisi maalisolmuun, puolustaja kiertää maalin (D-053, alla). Samassa solmussa saa olla enintään yksi pelaaja kummastakin joukkueesta (D-058): päätesolmut ratkaistaan Solmun pelaajamäärä -säännöllä (alla). Järjestäytyneisyys on joukkueen skaalaariarvo (O-6), jota sijainnit seuraavat viiveellä; sitä ei johdeta sijainneista.

**Solmun pelaajamäärä (D-058).** Samassa solmussa voi olla enintään yksi pelaaja kummastakin joukkueesta; hyökkääjä ja puolustaja samassa solmussa on sallittu. Sääntö koskee jokaista sijoitusta, jossa liikkuu yksi tai useampi saman joukkueen pelaaja: puolustusjärjestelmän liike, setup ja regroup, järjestelmätilan askel, suorahyökkäyksen liikkeet (O-7), järjestelmätilan ohjeet (O-10), irtokiekon voittajan sijoitus, vaihto ja aloitusasettelu. Kuvion siirrot ja lähtösolmut on jo validoitu erillisiksi.

1. Jokaiselle liikkujalle lasketaan ihannepääte ja polku (askeleiden solmut lähtösolmusta päätteeseen) nykyisillä askelsäännöillä (D-053 mukaan lukien).
2. Varatut solmut aluksi = joukkuetovereiden solmut, jotka eivät liiku tässä sijoituksessa.
3. Liikkujat käsitellään kiinteässä järjestyksessä. Puolustava joukkue (järjestelmä): `F1`, `D1`, `D2`, `F2`, `F3`. Hyökkäävä joukkue: kiekollinen ensin, sitten `C`, `LW`, `RW`, `LD`, `RD` (kiekollinen ohitetaan). Muut sijoitukset (vaihto, aloitus): `C`, `LW`, `RW`, `LD`, `RD`.
4. Jos pääte on vapaa, liikkuja menee sinne ja pääte merkitään varatuksi. Muuten liikkuja peruuttaa omaa polkuaan solmu kerrallaan kohti lähtösolmua ja pysähtyy ensimmäiseen vapaaseen solmuun (lähtösolmu mukaan lukien).
5. Jos koko polku on varattu, liikkuja menee lähimpään vapaaseen solmuun ihannepäätteen ympärillä: ensin Chebyshev-rengas 1, sitten 2, 3 jne. Renkaan sisällä etusija (Q-043, toteutettu oletus), liikkujan joukkueen omassa näkökulmassa: pienempi Chebyshev-etäisyys omaan maalisolmuun, sitten pienempi Manhattan-etäisyys omaan maalisolmuun, sitten pienempi |y − keskikaista|, sitten pienempi y, sitten pienempi x. Maalisolmu ei koskaan kelpaa.

Sääntö ei käytä satunnaisuutta. Invarianttitesti tarkistaa jokaisen tapahtuman jälkeen, ettei kahta saman joukkueen pelaajaa ole samassa solmussa.

**Maalin kierto** (D-053). Puolustaja, jonka askel osuisi maalisolmuun G = (gx, gy) (kumpi tahansa maali), kiertää maalin sille puolelle, jolla kiekko on. Kaikki koordinaatit luetaan samassa näkökulmassa (puolustajan oma näkökulma, kuten järjestelmän arvioinnissa). Puolustajan solmu on (x0, y0), ja estetty askel on (sx, sy) = (sign(dx), sign(dy)).

1. **Kiertopuoli s:** kiekon y < gy → s = −1 (y − 1 -puoli); kiekon y > gy → s = +1 (y + 1 -puoli).
2. **Tasatilanne, kiekko keskikaistalla** (kiekon y = gy): s on puolustajan oma puoli, eli y0 < gy → −1 ja y0 > gy → +1 (lyhin reitti, ei ylitystä maalin edestä). Jos myös puolustaja on keskikaistalla (y0 = gy, suoraan maalin edessä tai takana), s = −1, eli puolustajan näkökulmasta vasen puoli, samoin kuin roolien jaossa keskikaista lasketaan vasemmaksi.
3. **Kun sx ≠ 0** (puolustaja ohittaisi maalin x-suunnassa): korvaava solmu on (gx, gy + s). Jos se on yhden askeleen päässä (|gy + s − y0| ≤ 1), askel otetaan sinne. Muuten puolustaja on maalin vastakkaisella puolella vinossa askeleessa, ja hän ottaa askeleen (0, s) solmuun (x0, gy) maalin eteen tai taakse. Seuraava askel jatkaa kiertoa.
4. **Kun sx = 0** (puolustaja on maaliviivalla ja kohde maalin toisella puolella, Q-029): kierto kulkee maalin edestä, askel (c, sy), jossa c on keskiviivaa kohti (oma maali c = +1, vastustajan maali c = −1). Askel osuu maalin edustalle (`netFront`), ja seuraava askel vie kohteen puolelle. Kiekon puoli ei vaikuta tähän tapaukseen, koska kohde ratkaisee puolen.

Korvaava solmu ei koskaan ole maalisolmu ja on aina verkon sisällä. Esimerkkejä (oma maali (1, 2)): puolustaja (2, 2), kohde (0, 2), kiekko (0, 3) → (1, 3). Sama, kiekko (0, 2) → (1, 1) (kohta 2, vasen). Puolustaja (0, 1), kohde (2, 3), kiekko (3, 4) → (0, 2), seuraavaksi (1, 3). Puolustaja (1, 1), kohde (1, 3) → (2, 2), seuraavaksi (1, 3).

**Validointi.** Lataaja hylkää järjestelmän, jos:

- `schemaVersion` on tuntematon, `id` ei ole yksilöllinen tai tiedostossa on tuntematon kenttä;
- `rules` on tyhjä, **viimeisen säännön `when` ei ole tyhjä `{}`**, tai jonkin muun säännön `when` on tyhjä (sitä seuraavat säännöt eivät koskaan täsmäisi);
- `when`-avain on tuntematon, `puckZones` on tyhjä tai sisältää tuntemattoman alueen, väli on `min > max` tai verkon ulkopuolella, tai `puckState` on tuntematon;
- `mirrorY`-järjestelmässä `when.puckY`-väli ulottuu oikealle puoliskolle (y > 2);
- säännöltä puuttuu jokin viidestä roolista tai siinä on tuntematon rooli, tai kohteella on muu kuin tasan yksi kentistä `node` ja `puckOffset`;
- `node` on verkon ulkopuolella tai **maalisolmu** (kumpi tahansa, D-033). Kohdesolmut saavat olla oikealla puolella;
- **kaksi roolia saa aina saman kohteen** (D-058, D-062, Q-043): samassa säännössä kahdella roolilla on sama `node` tai sama `puckOffset`. Kiekon sijainnista riippuvat osumat (esim. F1:n `puckOffset` [0, 0] osuu jonkun kiinteään `node`-kohteeseen vain joillakin kiekon solmuilla) eivät ole validointivirhe, vaan ne ratkaistaan ajon aikana Solmun pelaajamäärä -säännöllä.

Designerin käytäntö (ei validointisääntö): nykyiset järjestelmät on kirjoitettu niin, että viisi kohdetta ovat erilliset jokaisella kiekon solmulla. Kun kiekko on solmussa, joka on jonkun kiinteä kohde, tarkempi `puckX`/`puckY`-sääntö ennen yleistä sääntöä siirtää kyseisen pelaajan viereiseen solmuun (esim. trap122: vahvan puolen hyökkääjä sulkee laidan (6, 0) tai (4, 0); omassa päässä D1 laskeutuu vahvan puolen tolpalle (1, 1)). Liikkeen aikana syntyvät päällekkäisyydet ratkaistaan silti ajon aikana.

**trap122:n F1 (D-057, D-062).** Hyökkäys- ja keskialueella F1:n kohde on yhden solmun päässä kiekollisesta, kiekon ja keskikaistan välissä: (kiekon x − 1, kiekon y + sign(keskikaista − kiekon y)) puolustajan näkökulmasta, kanonisella puolella `puckOffset` [−1, 1] ja keskikaistalla [−1, 0]. F1 ohjaa laitaan eikä prässää. Omassa päässä F1 menee suoraan kiekolliseen (`puckOffset` [0, 0]). forecheck212:n F1 menee kaikkialla suoraan kiekolliseen.

## Määritelmät (D-048) – hyväksytty (D-049)

Tämä osio määrittelee D-048:n käsitteet niin tarkasti, että ne voi toteuttaa deterministisesti 11 × 5 -verkossa. Jerry hyväksyi määritelmät M-1–M-8 sellaisinaan virstanpylvääseen 2 (D-049), samoin D-040:n, D-044:n ja D-047:n tarkennukset. Kaikki koordinaatit ovat **hyökkäävän joukkueen näkökulmasta** (vastustajan maali (9, 2), `netFront` (8, 2), keskikaista y = 2), ellei toisin mainita. Sijainnit luetaan sillä hetkellä, kun toiminto ratkaistaan, eli tahdin hyökkääjien siirtojen ja puolustajien järjestelmäliikkeen jälkeen (D-041). "Puolustaja" tarkoittaa puolustavan joukkueen viittä kenttäpelaajaa; maalivahti ei ole näissä säännöissä koskaan puolustaja. Säännöt eivät käytä satunnaisuutta, ellei sitä erikseen mainita.

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

**Maalisolmu linjalla** (D-051). Syöttölinja ja luistelureitti saavat kulkea maalisolmun kautta. Maalisolmussa ei ole puolustajaa, joten se ei vaikuta riskiin. Poikkeus: syöttö suoraan maalin läpi (syöttäjä ja vastaanottaja molemmat keskikaistalla maalin eri puolilla) hylätään kuvion validoinnissa (Kuviot, Validointi). Laukauslinjasta maalisolmu jätetään pois (M-5).

**Syöttäjän solmu ei kuulu syöttölinjaan (D-062).** Syötössä linjan solmuista jätetään pois syöttäjän oma solmu, sekä etäisyyden että linjan puolustajan (syötönkatkaisija, `nearestDefender`, `loosePuckSpots.failedPass`) laskennassa. Läheisyys syöttäjään kuuluu paineelle (M-7), linja syötönkatkolle; näin samasta puolustajasta ei tule kahta miinusta (`laneDefenderDistance[0]` ja `underPressure`). Linjaan jää aina vähintään vastaanottajan solmu. Luistelureitissä ja laukauslinjassa lähtösolmu kuuluu linjaan kuten ennen.

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
4. Ainakin toinen päätepiste on aloituspisteiden rivillä tai maaliviivalla: x ≥ `opponentGoal.x` − 1 (11 × 5 -verkossa x ≥ 8, sama yhden askeleen sääntö kuin `netFront`). Pelkkä pakilta pakille -syöttö siniviivalla (molemmat x = 7) ei ole Royal Road: maalivahti ehtii siirtyä, eikä analyysi laske sitä slotin poikki menevänä syöttönä.

Kiekon kieli: poikittaissyöttö slotin yli hyökkäysalueella juuri ennen laukausta pakottaa maalivahdin sivuttaisliikkeeseen.

### M-4 Maski (`screen`)

Laukauksen maalitarkistukseen lisätään `shot.modifiers.screen`, kun kaikki pätevät:

1. joku laukaisijan joukkuetoveri on vastustajan maalin edustalla (`netFront`, (8, 2)) laukaushetkellä, riippumatta siitä, miten hän sinne tuli (kuviossa `driveNet`, järjestelmätilassa myöhemmin `netFrontAfterShot`);
2. laukaisija itse ei ole `netFront`-solmussa;
3. laukaisija on maaliviivan edessä (x < `opponentGoal.x`). Maalin takaa ei laukota maskin läpi.

Muokkaaja on ehto, ei määrä: kaksi pelaajaa maskissa ei tuplaa sitä. **Puolustaja maskissa (Q-031, D-062):** jos vähintään yksi puolustavan joukkueen kenttäpelaaja on samassa `netFront`-solmussa laukaushetkellä, käytetään `screen`-arvon (0,3) sijaan `shot.modifiers.screenContested` (0,15). Arvot eivät lisäydy toisiinsa. Virstanpylväässä 2 puolustaja ei vaikuttanut maskiin. Kuviot-osion Maski-kappale on päivitetty vastaamaan tätä (D-049).

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
6. Virstanpylväässä 2 järjestelmätilan ohje `looseChasers` oli kiinteä: vain kohdan 2 lähin pelaaja kamppailee (D-046). Virstanpylväässä 3 kamppailija on edelleen kohdan 2 lähin pelaaja, mutta `looseChasers` 2 ja `netFrontAfterShot` tuovat lisäpelaajia lähemmäs ennen kamppailua (O-10), mikä näkyy `extraPlayer`-laskennassa ja seuraavan kamppailun kamppailijoissa.
7. Toinen yritys (D-059, D-065): jos hyökkääjäpuolen kamppailija voittaa kamppailun hyökkäysalueen slotissa, hän laukoo heti (O-8).

### M-7 Paineen alla (`underPressure`)

Pelaaja on **paineen alla**, kun ainakin yksi vastustajan kenttäpelaaja on enintään `pressure.underPressureNodes` (0, D-056) solmun päässä hänestä (Chebyshev) toiminnon hetkellä. Arvolla 0 painostajan pitää olla samassa solmussa. Säde kalibroidaan Wisehockeyn 68–76 %:iin (Q-032).

- **Syöttö:** tapahtuman kenttä "paineen alla" = syöttäjä on paineen alla. Kun se on tosi, syöttötarkistukseen lisätään ehtomuokkaaja `pass.modifiers.underPressure` (−0,9; analyysi 85 % → n. 70 %).
- **Nimimuutos (D-049):** `pass.modifiers.pressure` on nimetty `underPressure`:ksi (arvo ei muuttunut). Yleisten käytäntöjen mukaan `pressure`-niminen muokkaaja skaalautuu joukkueen painetilalla (hyökkäävän joukkueen momentum), eikä momentum saa heikentää hyökkäävän joukkueen omia syöttöjä. Analyysin "paineen alla" tarkoittaa fyysistä painetta syöttäjään. `underPressure` on Yleisten käytäntöjen ehtomuokkaajien listassa.
- **Laukaus:** sama ehto laukaisijalle. Laukaustapahtuman kenttä `underPressure` (D-050, `stats-and-checks.md`, Tapahtumaskeema) = laukaisija on paineen alla, ja se kirjataan jokaiseen laukaukseen. Virstanpylväässä 2 se on vain raporttia varten (mittari "Laukaukset paineen alla (osuus)") eikä muuta todennäköisyyksiä. `shot.modifiers.pressure` pysyy joukkueen painetilaan sidottuna. Säteen kalibrointi: Q-032.
- **Laukausnopeus** (D-052): laukaustapahtuman laukausnopeus on tyhjä (null) virstanpylvääseen 3 asti. Myöhemmin se johdetaan laukaisijan Laukaisuvoimasta pelkäksi näyttöarvoksi ilman pelivaikutusta. `tuning.json`:ssa ei ole sille arvoja.

### M-8 Alueelle tulon voimasuhde "N vs M"

Hallittu alueelletuonti on joko `skate`, jonka kohde on hyökkäysalueella ja lähtö ei (`zoneEntryCarry`, tapa "kuljetus"), tai syöttö, jonka syöttäjä on hyökkäysalueen ulkopuolella ja vastaanottaja hyökkäysalueella (tapa "syöttö"). `dump` ei ole hallittu alueelletuonti, vaan oma tapahtumansa.

Lasketaan toiminnon hetkellä ennen tarkistusta:

- **N** = hyökkäävän joukkueen kenttäpelaajat, joiden x ≥ keskiviiva (x ≥ 5). Kiekollinen tai syöttäjä lasketaan aina mukaan.
- **M** = puolustavan joukkueen kenttäpelaajat, joiden x ≥ kiekon x (kiekollisen tai syöttäjän solmu), eli kiekon tasalla tai kiekon ja oman maalinsa välissä.
- Kirjataan muodossa "N vs M", esim. "3 vs 2". Kiekon kuljettaja = kiekollinen (kuljetus) tai vastaanottaja (syöttö). Lopputulos: hallinta säilytetty (tarkistus onnistui) tai menetetty.

### Irtokiekon paikat (D-044) ja epäonnistunut syöttö (D-040)

Paikkasäännöt ovat `tuning.json`:n ylimmän tason osiossa `loosePuckSpots` (D-044, hyväksytty D-049). Arvo on säännön nimi alla olevasta taulukosta. Osio vaatii lataajan tuen (D-030), joten data ja lataajan tuki commitoidaan yhdessä:

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
| `missedShot` | laukaus ohi (`onTargetShare`) eikä mennyt ulos kaukalosta (osuus 1 − `missedOutOfPlayShare`, O-13) | `endRowShooterLane`: päätyrivi laukaisijan kaistalla | (10, laukaisijan y) |
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

Muut laukaukset solmusta (8, 2), esim. kiekollinen luistelee sinne ja laukoo, käyttävät `slot`-vyöhykettä. Maski (M-4) ei koskaan päde `crease`-laukaukseen, koska laukaisija on itse maskissa. Royal Road (M-3) voi päteä, jos syöttö maskiin oli poikittaissyöttö, mutta (8, 2) on keskikaistalla, joten käytännössä ei. rink.json-osion kappaleen "Maalisolmu ja `crease`" viimeinen virke on korvattu tällä (D-047, D-049).

### Määritelmien tasapainoarvot

Numeroarvot ovat paikkamerkkejä (`_placeholders`), jotka kalibroidaan simulaatiolla. `loosePuckSpots` ei ole paikkamerkki vaan hyväksytyt säännöt (D-044).

| Polku | Arvo | Merkitys |
| --- | --- | --- |
| `checks.pass.interceptionShare` | 0,6 | Epäonnistuneista syötöistä syötönkatkojen osuus (D-040) |
| `checks.block.maxLaneDistance` | 1 | Blokkaajaehdokkaan suurin etäisyys laukauslinjasta (M-5) |
| `checks.block.modifiers.laneDistance` | [0,0, 1,0] | Logit laukaisijan hyväksi blokkaajan linjaetäisyyden mukaan (M-5) |
| `checks.loosePuck.extraPlayerRadius` | 1 | `extraPlayer`-laskennan säde (M-6) |
| `checks.loosePuck.modifiers.distancePerNode` | 0,5 | Logit per solmu kamppailijoiden etäisyyserosta (M-6) |
| `pressure.underPressureNodes` | 0 | Paineen alla -säde (M-7, D-056; kalibrointi Q-032). Jerryn asettama, ei paikkamerkki |
| `checks.pass.modifiers.underPressure` | −0,9 | Entinen `checks.pass.modifiers.pressure`, nimetty uudelleen (D-049), arvo ennallaan |
| `time.secondsPerAction.systemStep` | 2 | Järjestelmätilan askel: kiekollinen joukkue pitää kiekkoa (D-036) tai irtokiekko jäi ilman voittajaa (M-6) |
| `loosePuckSpots` | ks. yllä | Irtokiekon paikat (D-044). Data ja lataajan tuki commitoidaan yhdessä (D-030) |

## Ottelu ja tilat (virstanpylväs 3) – hyväksytty (D-062–D-067)

Tämä osio on virstanpylvään 3 toteutusspeksi: ottelun rakenne, vaihdot, energia, järjestäytyneisyys, siirtymät, paine, tuttuus, järjestelmätilan ohjeet, toinen yritys, paikkatyypit ja katkot. Jerry hyväksyi designerin ja ohjelmoijan suunnitelmat D-062:ssa tarkennuksin (D-063–D-067). Koordinaatit ja näkökulma kuten Määritelmissä. Ajat ovat pelisekunteja, ja kaikki luvut luetaan `tuning.json`:sta alla mainituista poluista. Säännöt eivät käytä satunnaisuutta, ellei sitä erikseen mainita.

### O-1 Ottelun rakenne

- **Erät:** `time.periods` (3) erää, kukin `time.periodSeconds` (1 200) s. Kello alkaa jokaisessa erässä nollasta.
- **Kokoonpano (D-062):** joukkueella on 4 hyökkäyskolmikkoa (`LW`, `C`, `RW`), 3 pakkiparia (`LD`, `RD`) ja yksi maalivahti, joka pelaa koko ottelun. Harness generoi pelaajat siemenellä.
- **Erän alku:** aloitus pisteestä `center`. Valmentajia kutsutaan ennen aloitusta (O-4).
- **Erän raja (D-062, D-064).** Raja tarkistetaan kerran jokaista kokonaisuutta kohden ennen sen suoritusta. Kokonaisuuden kesto d on sen osien `time.*`-aikojen summa. Jos nykyhetki + d > `time.periodSeconds`, kokonaisuutta ei suoriteta lainkaan (ei tarkistuksia, ei satunnaislukuja), vaan erä päättyy. Täsmälleen rajalle päättyvä kokonaisuus suoritetaan. Kokonaisuudet:

| Kokonaisuus | Kesto d |
| --- | --- |
| Aloitus | `secondsPerAction.faceoff` |
| Setup ja kuvion ensimmäinen tahti | `setupSeconds` + D-036:n avaussyötön aika (jos tarvitaan) + ensimmäisen toiminnon aika |
| Regroup | `regroupSeconds` |
| Kuvion tahti (siirrot ja toiminto) | toiminnon aika |
| Laukaus reboundeineen | `shoot` (+ `rebound`, jos laukaus torjutaan) |
| Kiekko päätyyn | `dumpIn` + `loosePuck` |
| Irtokiekkokamppailu | `loosePuck` (uusinnassa `systemStep` + `loosePuck`) |
| Järjestelmätilan askel | `systemStep` |
| Suorahyökkäyksen toiminto (O-7) | toiminnon aika |

- **Erän loppu:** kello asetetaan arvoon `periodSeconds`. Jäljellä oleva aika kuluu tilojen osalta normaalisti (O-14: energia, järjestäytyneisyys, paine, jääaika, hyökkäysalueaika). Lokiin kirjataan Pelikatko, syy "erän loppu" (D-064), myös viimeisen erän lopussa. Kiekon tila hylätään.
- **Erätauko:** kaikkien kenttäpelaajien ja maalivahtien energia = `energy.start`, molempien joukkueiden paine = 0, järjestäytyneisyys = 1 ja tuttuuslaskurit kerrotaan arvolla `familiarity.intermissionMultiplier` (O-11). Yksiköiden vuorot alkavat alusta, joten valmentaja saa valita yksiköt erän alkuun ilman `stoppageChangeMinSeconds`-rajaa.
- **Ottelun loppu:** viimeisen erän jälkeen. Tasapeli sallitaan, jatkoaikaa ei ole (D-062). Aikalisä ja maalivahdin vetäminen tulevat virstanpylväässä 4.

### O-2 Vaihdot (D-062)

Kolmikko ja pari vaihtuvat erikseen (D-023). Yksikön **jääaika** on aika siitä, kun se tuli jäälle.

- **Katkovaihto.** Jokaisella katkolla (maali, maalivahdin pito, kiekko ulos kaukalosta) valmentaja voi vaihtaa kummankin joukkueen yksikön, jonka jääaika on vähintään `time.stoppageChangeMinSeconds` (20). Vaihto tehdään ennen aloitusasettelua, joten sisään tulevat pelaajat asettuvat suoraan aloitusasetteluun.
- **Vaihto lennosta.** Vain kiekollinen joukkue vaihtaa lennosta; puolustava joukkue ei vaihda kesken pelin. Yksikkö on vaihtokelpoinen, kun sen jääaika on vähintään `time.forwardShiftSeconds` (kolmikko) tai `time.defenceShiftSeconds` (pari). Turvallinen hetki on jompikumpi:
  1. kuvion valintakohta (D-036), kun joukkueella on kiekko ja kiekon solmu on joukkueen näkökulmasta oman alueen ulkopuolella (x > `defensive`-alueen `xMax`);
  2. heti oman `dump`-toiminnon lennon (`dumpIn`) jälkeen, ennen irtokiekkokamppailua ("dump and change"). Kamppailu ratkaistaan sisään tulleilla pelaajilla.
- **Pakotettua vaihtoa ei ole.** Omaan päähän jumiin jäänyt yksikkö väsyy (O-5).
- **Sijoitus:** sisään tuleva pelaaja ottaa korvatun pelaajan solmun samalla pelipaikalla. Vaihto ei kuluta aikaa eikä muuta järjestäytyneisyyttä.
- **Tapahtuma:** Vaihto (ketju ulos, ketju sisään), yksi tapahtuma yksikköä kohden. Samassa kohdassa järjestys: kolmikko ennen paria, kotijoukkue ennen vierasta.
- Maalivahti ei vaihdu.

### O-3 Aloituspaikat

| Edeltävä tilanne | Aloituspiste |
| --- | --- |
| Erän alku, maali | `center` |
| Maalivahti pitää kiekon (D-042) | Maalivahdin joukkueen oman pään piste laukaisijan solmun puolella maalivahdin joukkueen näkökulmasta: y < keskikaista → `defensiveLeft`, y > keskikaista → `defensiveRight`, keskikaista → `defensiveLeft` |
| Kiekko ulos kaukalosta (D-063) | Q-040:n oletus: laukaisijan solmun alueen (laukaisijan näkökulmasta) lähin aloituspiste. Etäisyys laukaisijan solmusta Chebyshev, sitten Manhattan; tasatilanteessa laukaisijan puolen piste, ja keskikaistalta `...Left` laukaisijan joukkueen näkökulmasta. Esim. laukaus (7, 2) → `offensiveLeft` |

Aloitusasettelu ja kiekon saaja kuten ennen (Q-034, D-060). Jokaisessa aloituksessa molempien joukkueiden järjestäytyneisyys = 1.

### O-4 Valmentaja (D-062)

Rajapinta (`ICoach`) on Simissä, oletusvalmentaja `RotationCoach` AI-projektissa. Simulaatio kutsuu valmentajaa:

- **katkolla ja erän alussa:** valmentaja palauttaa kolmikon ja parin (vain vaihtokelpoiset yksiköt saa vaihtaa, O-2), kuviot prioriteettijärjestyksessä, puolustusjärjestelmän, siirtymäohjeen (`rush` tai `regroup`) ja järjestelmätilan ohjeet (O-10);
- **lennosta vaihdon hetkellä:** kun kiekollisella joukkueella on vaihtokelpoinen yksikkö turvallisella hetkellä, valmentaja palauttaa sisään tulevan yksikön tai jatkaa nykyisellä.

**`RotationCoach`:** vaihtaa jokaisen vaihtokelpoisen yksikön aina, kolmikot järjestyksessä 1 → 2 → 3 → 4 → 1 ja parit 1 → 2 → 3 → 1. Pelikirjan järjestys: tässä ottelussa vähiten käytetty kuvio ensin (O-11:n laskuri), tasatilanteessa latausjärjestys. Siirtymäohje `rush` (D-062). Järjestelmätilan ohjeet virstanpylvään 2 oletuksilla: `netFrontAfterShot` false, `looseChasers` 1, `pinch` false. Puolustusjärjestelmä tulee Harnessin parametrista. Valmentaja ei saa satunnaisgeneraattoria ennen virstanpylvästä 4.

### O-5 Energia (D-062)

Energia E on 0–1, alkuarvo `energy.start` (1,0). Kenttäpelaajan kestävyyskerroin f = 1 − `energy.enduranceCostReductionPerPoint` × (Kestävyys − `checkFormula.referenceValue`). Maalivahdin henkisen kestävyyden kerroin g = 1 − `pressure.mentalToughnessReductionPerPoint` × (Henkinen kestävyys − `checkFormula.referenceValue`).

| Kuka | Milloin | Muutos |
| --- | --- | --- |
| Kenttäpelaaja jäällä | Joka pelisekunti | E −= (`energy.drainPerSecondOnIce` + `pressure.energyDrainPerSecond` × P_vastustaja) × f |
| Tarkistuksen osallistuja | Jokainen tarkistus, johon hän osallistuu | E −= `energy.costPerAction` × f |
| Kenttäpelaaja penkillä | Penkillä oloaika t, suljettu muoto | E ← 1 − (1 − E) × exp(−`energy.benchRecoveryRate` × t) |
| Maalivahti | Joka pelisekunti | E −= `pressure.energyDrainPerSecond` × P_vastustaja × g. Ei muuta kulutusta eikä palautumista ennen erätaukoa |

- **Osallistujat** (D-062): kaikki tarkistuksen roolien pelaajat molemmilta puolilta: `passer`, `receiver`, `nearestDefender` (myös blokkaaja ja syöttölinjan puolustaja), `carrier`, `shooter`, `participant` (molemmat irtokiekon kamppailijat, myös jokaisessa uusinnassa), `centre` (molemmat sentterit), `forecheckers` (jokainen karvaaja), `hitter`. Maalivahti ei maksa `costPerAction`-arvoa. `extraPlayer`-laskentaan tai maskiin osallistuminen ei ole tarkistukseen osallistumista.
- Tarkistus käyttää energiaa ennen maksua. Maksu vähennetään tarkistuksen ratkaisun jälkeen.
- P_vastustaja on vastustajan painetila (O-9). E rajataan välille 0–1. Penkkipalautuminen lasketaan suljetussa muodossa `CheckMath`-luokan kautta (saa laskea laiskasti, kun pelaaja tulee jäälle).
- **Vaikutus tarkistukseen:** M += cz × (1 − Ē_hyökkääjä) − cz × (1 − Ē_puolustaja), cz = `energy.checkModifierAtZero` (−1,2). Ē on puolen osallistujien energioiden keskiarvo. Laukauksen maalitarkistuksessa puolustajapuolena on maalivahti. Yksipuolisessa tarkistuksessa puuttuva puoli antaa 0. **Rebound** ei käytä yleistä termiä, vaan vain omaa `checks.rebound.modifiers.goalieEnergy`-muokkaajaansa (ei kahta miinusta samasta asiasta).
- **Mitoitus:** 45 sekunnin vuoro kuluttaa n. 0,30. Tasapainotilassa neljällä kolmikolla energia on vuoron alussa n. 0,91 ja keskimäärin 0,76; kolmella 0,83 / 0,68; kahdella 0,54 / 0,39.

### O-6 Järjestäytyneisyys (D-007, D-062)

Jokaisella joukkueella on skaalaariarvo O (0–1), joka kuvaa sen puolustusta. Tarkistukset lukevat puolustavan joukkueen O:n. Sijainnit seuraavat O:ta viiveellä; O:ta ei johdeta sijainneista.

- **Aloitus:** O = 1 molemmille jokaisessa aloituksessa.
- **Kiekonmenetys:** joukkue A menettää kiekon joukkueelle B, kun B saa kiekon syötönkatkolla, riistolla epäonnistuneessa kuljetuksessa tai voittaa irtokiekkokamppailun, jossa A oli hyökkääjäpuoli (M-6). Silloin

  O_A ← max(0, min(O_A, 1 − drop × k) − `organization.dropPerCommittedPlayer` × c)

  - drop = `organization.dropOnTurnover[alue]`, alue = solmu, jossa B sai kiekon, A:n näkökulmasta (`defensive` 0,35, `neutral` 0,5, `offensive` 0,65; mitä ylempänä kiekko menetetään, sitä matalammalle O putoaa);
  - k = `organization.dropFactorOnShotOrDump` (0,5), jos A:n viimeinen kiekkotoiminto ennen menetystä oli `shoot` (myös rebound, blokki ja kaukaloon jäänyt ohilaukaus) tai `dump`; muuten 1 (esim. syötönkatko tai irtokiekko epäonnistuneesta syötöstä);
  - c = A:n sitoutuneiden pelaajien määrä menetyshetkellä (O-10).
- **Palautuminen:** joka pelisekunti O += `organization.recoveryPerSecond` × (1 + `organization.recoveryPerStatPoint` × (S̄ − `checkFormula.referenceValue`)), rajattuna enintään 1:een. S̄ on joukkueen viiden jäällä olevan kenttäpelaajan keskiarvo arvosta Σ `organization.recoveryWeights[stat]` × stat (Nopeus 0,5, Pelinluku 0,5). Palautuminen jatkuu aina, myös setupin, regroupin ja järjestelmätilan aikana.
- **Vaikutus:** tarkistuksissa, joilla on `modifiers.organization`, M += arvo × (1 − O_puolustaja), aina hyökkääjän hyväksi: `pass` 0,6, `zoneEntryCarry` 0,8, `deke` 0,6, `breakout` 0,6, `shot` 0,7, `block` 0,5.
- **Raja** `organization.organizedThreshold` (0,8): suorahyökkäys päättyy (O-7) ja laukaus ei ole enää suorahyökkäys (O-12), kun O_puolustaja ≥ raja.
- **Puolustajien liike setupissa ja regroupissa:** puolustajat liikkuvat kohti järjestelmänsä kohteita enintään n = floor(t × `plays.maxNodesPerBeat` / `time.secondsPerAction.skate`) askelta, t = `setupSeconds` (6 s → 4 askelta) tai regroupissa `regroupSeconds` + `setupSeconds` (14 s → 9 askelta). Kohteet lasketaan kiekon solmusta ennen liikettä. Askeleet ja päätteet kuten Puolustusjärjestelmät, Liikkuminen (D-053, D-058). Tahdeissa liike on ennallaan (`maxNodesPerBeat` per tapahtuma, D-041). Näin M-8:n voimasuhde "N vs M" syntyy sijainneista.
- **Esimerkki:** hyökkäysalueella menetetty kiekko antaa O = 0,35; 8 s myöhemmin alueelle tullessa O ≈ 0,67 (suorahyökkäys). Regroupin ja setupin 14 s vie O:n n. 0,91:een (järjestäytynyt).

### O-7 Siirtymät (D-007, D-062)

Siirtymäohje (valmentajan päätös, oletus `rush`) koskee kiekon saamista muuten kuin aloituksessa: syötönkatko, riisto ja irtokiekon voitto. Toinen yritys (O-8) menee aina ensin. Aloituksen jälkeen noudatetaan D-060:ta.

**`rush` (suorahyökkäys sisäänrakennetuilla säännöillä, ei pelikirjan kuviota).** Ei setuppia. Joukkue tekee enintään `transitions.rushMaxActions` (4) toimintoa. Ennen jokaista toimintoa tarkistetaan lopetusehto: jos O_puolustaja ≥ `organization.organizedThreshold` tai toiminnot on käytetty, rush päättyy ja kuvio valitaan D-036:n mukaan setupin kanssa. Jokainen toiminto etenee kuten kuvion tahti (D-041): kiekottomien siirrot → puolustajien järjestelmäliike → kiekollisen toiminto.

- **Laukaussolmut S:** hyökkäysalueen solmut (hyökkäävän joukkueen näkökulmasta), joiden `checks.shot.baseXg[xgZone]` ≥ `transitions.rushShotMinBaseXg` (0,05), maalisolmu pois lukien. Nykydatalla (7, 2), (8, 1), (8, 2) ja (8, 3).
- **Kiekollisen toiminto,** ensimmäinen täsmäävä:
  1. kiekollinen on S:n solmussa → `shoot`;
  2. joku joukkuetoveri on S:n solmussa ja syöttölinjan (M-1, ilman syöttäjän solmua) etäisyys lähimpään puolustajaan on vähintään 1 → `pass` hänelle. Jos ehdokkaita on useita: suurempi `baseXg`, sitten pienempi Chebyshev-etäisyys syöttäjään, sitten pelipaikkajärjestys `C`, `LW`, `RW`, `LD`, `RD`;
  3. muuten `skate` enintään `plays.maxNodesPerBeat` askelta: hyökkäysalueen ulkopuolella suoraan kohti vastustajan päätyä omalla kaistalla (x kasvaa, y ennallaan); hyökkäysalueella kohti lähintä S:n solmua (Chebyshev, sitten Manhattan, sitten suurempi `baseXg`, sitten pienempi |y − keskikaista|, sitten pienempi y). Tarkistus kuten kuvion `skate` (`zoneEntryCarry`, `breakout` tai `deke`).
- **Kiekottomat:** hyökkääjät liikkuvat enintään `plays.maxNodesPerBeat` askelta kohti solmua (min(kiekollisen x, `opponentGoal.x` − 1), kaista), kaistat `LW` y = 1, `C` y = 2, `RW` y = 3. Pakit liikkuvat kohti solmua (max(0, min(kiekollisen x − 1, keskiviiva)), y = 1 `LD` / y = 3 `RD`), eli enintään keskiviivalle. Päätteet D-058:n mukaan.
- **Ajat:** toiminnon `secondsPerAction`. Kiekonmenetys, laukaus ja `dump` päättävät rushin normaalisti.

**`regroup` (kokoaminen).** Jos kiekko saadaan omalla alueella tai keskialueella (saajan näkökulmasta), kuluu `time.regroupSeconds` (8): joukkue pitää kiekon ilman tarkistuksia, eikä kukaan hyökkääjä liiku. Sen jälkeen kuvio valitaan D-036:n mukaan setupin kanssa, ja puolustajat liikkuvat regroupin ja setupin yhteisen askelmäärän (O-6). Hyökkäysalueella kiekon saanut joukkue menee suoraan D-036:een (pelkkä setup).

### O-8 Toinen yritys (D-059, D-065)

Kun hyökkääjäpuolen kamppailija voittaa irtokiekkokamppailun (M-6) hyökkäysalueen slotissa eli vastustajan maalin edustalla, solmuissa (7, 2) ja (8, 2) **oman joukkueensa näkökulmasta**, hän laukoo heti: ei kuvion valintaa, ei setuppia, ei siirtymäohjetta eikä puolustajien liikettä ennen laukausta. Oman pään vastaavat solmut eivät käy. Sääntö koskee kaikkia irtokiekkoja (rebound, blokattu laukaus, kaukaloon jäänyt ohilaukaus, `dump`, epäonnistunut syöttö, uusinnat), ei syötönkatkoja eikä riistoja.

- Laukaus etenee normaalisti: blokki → `onTargetShare` → ulos tai maalitarkistus, aika `secondsPerAction.shoot`.
- xG-vyöhyke: solmussa (8, 2) `crease` (maskin xG, D-047, D-059); solmussa (7, 2) solmun oma vyöhyke `highSlot`.
- Paikkatyyppi määräytyy O-12:n mukaan. Solmut (8, 1) ja (8, 3) ratkaistaan raportin perusteella (Q-041).

### O-9 Paine (momentum)

Jokaisella joukkueella on painetila P (0–1), alkuarvo 0.

- **Kasvu:** + `pressure.gainOnZoneEntry` (0,1) jokaisesta onnistuneesta hallitusta alueelle tulosta (M-8, hallinta säilyi); + `pressure.gainOnShot` (0,08) jokaisesta laukausyrityksestä (myös blokattu ja ohi); + `pressure.gainPerSecondInZone` (0,005) jokaiselta pelisekunnilta, jolloin joukkueella on kiekko hallussa hyökkäysalueellaan.
- **Lasku:** − `pressure.decayPerSecond` (0,01) jokaiselta pelisekunnilta, jolloin sekuntikasvu ei päde. P rajataan välille 0–1.
- **Katko:** molempien P kerrotaan arvolla `pressure.keepOnStoppage` (0,5). Erätauolla P = 0.
- **Vaikutus:** laukauksen maalitarkistukseen M += `checks.shot.modifiers.pressure` (0,3) × P_laukoja × g, jossa g on torjuvan maalivahdin kerroin (O-5). Lisäksi vastustajan energian kulutus (O-5).
- **Esimerkki:** 60 s hyökkäysalueella, yksi alueelle tulo ja kolme laukausta → P ≈ 0,64 → laukaukseen n. +0,19 logitia (g = 1).

### O-10 Järjestelmätilan ohjeet (D-046)

Ohjeet ovat valmentajan päätöksen kenttiä, eivät JSON-tiedostoja. Oletukset ovat virstanpylvään 2 arvot.

| Ohje | Arvot (oletus) | Toiminta |
| --- | --- | --- |
| `netFrontAfterShot` | bool (false) | Laukausyrityksen jälkeen, ennen reboundin tai irtokiekon ratkaisua, laukojan joukkueen lähin hyökkääjä (`LW`, `C`, `RW`; ei laukoja eikä jo `netFront`-solmussa oleva) liikkuu enintään `plays.maxNodesPerBeat` askelta kohti `netFront`-solmua (8, 2). Ei aikaa. Pääte D-058:n mukaan |
| `looseChasers` | 1 tai 2 (1) | Arvolla 2 ennen jokaista irtokiekkokamppailua (myös uusinnat) joukkueen toiseksi lähin kenttäpelaaja liikkuu enintään `plays.maxNodesPerBeat` askelta kohti kiekon solmua. Kamppailija on edelleen lähin (M-6). Ei aikaa |
| `pinch` | bool (false) | Kun joukkue puolustaa ja kiekko (hallussa tai irti) on joukkueen omasta näkökulmasta solmussa x ∈ {7, 8}, y ∈ {0, `width` − 1} (laita hyökkäysalueen yläosassa), D1:n järjestelmäkohde korvataan kiekon solmulla |

**Sitoutunut pelaaja:** ohjeen vuoksi liikkunut pelaaja (`netFrontAfterShot`-liikkuja ja toinen `looseChasers`-liikkuja liikkumisesta lähtien, `pinch`-D1 korvatun kohteen ajan) on sitoutunut, kunnes hänen joukkueensa seuraava setup alkaa tai tulee katko. Sitoutuneiden määrä c kasvattaa järjestäytyneisyyden pudotusta kiekonmenetyksessä (O-6).

### O-11 Tuttuus (D-066)

- **Laskuri** u[joukkue][kuvio] per ottelu. Kuvio ja sen peilikuva ovat sama kuvio (`id`). Laskuri kasvaa yhdellä, kun kuvio alkaa, myös aloituskuviot ja heti keskeytyvät kuviot.
- **Erätauko:** u ← u × `familiarity.intermissionMultiplier` (0,5, D-066: puolitus). Ei pyöristystä.
- **Rangaistus** L = min(`familiarity.maxPenalty`, `familiarity.penaltyPerRepeat` × max(0, u − `familiarity.freeUses`)), u mukaan lukien kuvion nykyinen aloitus. Arvoilla 2 / 0,08 / 0,5: käytöt 1–2 → 0, 3 → 0,08, 8 → 0,48, 9+ → 0,5. Erätauon jälkeen esim. u = 6 → 3, ja seuraava aloitus antaa u = 4 → L = 0,16.
- **Kohde:** M −= L hyökkäävän joukkueen tarkistuksissa, jotka kuuluvat käynnissä olevaan kuvioon: kuvion syötöt (myös D-036:n avaussyöttö kuvion kiekolliselle), kuljetukset (`skate`: `zoneEntryCarry`, `breakout`, `deke`) ja kuvion laukauksen `block`-tarkistus (miinus auttaa blokkaajaa). Ei maalitarkistukseen, irtokiekkoihin, aloituksiin, reboundeihin, rushiin eikä järjestelmätilaan.
- Valinta noudattaa D-036:ta. Kierto syntyy valmentajan pelikirjan järjestyksestä (O-4).

### O-12 Paikkatyypit

Laukauksen paikkatyyppi, ensimmäinen täsmäävä:

1. **Aloitus:** laukaus on aloituksen voittaneen joukkueen aloituskuvion `shoot`-toiminto, ja kuvio on pelattu katkeamatta aloituksesta asti.
2. **Kiekonriisto:** laukaisevan joukkueen viimeisin kiekon saanti vastustajalta (syötönkatko, riisto tai irtokiekon voitto, jossa vastustaja oli hyökkääjäpuoli) tapahtui keskialueella tai hyökkäysalueella (saajan näkökulmasta), ja siitä on enintään `chanceTypes.turnoverWindowSeconds` (8) s. Järjestys riisto ennen suorahyökkäystä on Q-039:n oletus.
3. **Suorahyökkäys:** hallinta alkoi hyökkäysalueen ulkopuolelta, kiekko tuli hyökkäysalueelle saman hallinnan aikana, ja O_puolustaja < `organization.organizedThreshold` laukaushetkellä.
4. **Alueella pelaaminen:** muut.

**Hallinta** alkaa, kun joukkue saa kiekon vastustajalta tai aloituksesta, ja jatkuu niin kauan kuin joukkue pitää kiekon tai voittaa irtokiekot hyökkääjäpuolena. Se päättyy, kun vastustaja saa kiekon, tai katkoon. `chanceTypes.rushOrganizationBelow` poistuu, koska se on sama arvo kuin `organization.organizedThreshold`.

### O-13 Katkot (D-063, D-064)

Katkon syyt virstanpylväässä 3: maali, maalivahti pitää kiekon, kiekko ulos kaukalosta ja erän loppu. Pitkä kiekko ja paitsio myöhemmin (Q-042).

- **Kiekko ulos kaukalosta:** kun laukaus menee ohi (`onTargetShare`), tehdään yksi `Sim.Random`-arvonta todennäköisyydellä `checks.shot.missedOutOfPlayShare` (0,25). Ulos → Pelikatko, syy "kiekko ulos kaukalosta", aloitus O-3:n mukaan. Muuten irtokiekko kohdassa `loosePuckSpots.missedShot` kuten ennen. Koskee vain ohi menneitä laukauksia.
- **Jokaisella katkolla:** paine × `keepOnStoppage` (O-9), katkovaihdot (O-2), aloitus (O = 1). Sitoutumiset päättyvät.

### O-14 Sekuntivaikutukset ja aluetta vaihtava toiminto (D-062)

Jokainen kokonaisuus (O-1) kuluttaa kellosta keston d. Sekuntivaikutukset lasketaan kerran per kokonaisuus tilasta ennen kokonaisuutta, kertomalla d:llä, kiinteässä järjestyksessä: energia → järjestäytyneisyys → paine → jääaika ja hyökkäysalueaika. Joukkueet käsitellään kotijoukkue ensin, pelaajat id-järjestyksessä. Aluetta vaihtava toiminto (esim. kuljetus siniviivan yli) lasketaan kokonaan sitä edeltävän tilan mukaan: kuljetuksen sekunnit ovat keskialueen sekunteja. **Hyökkäysalueaika** on aika, jonka kiekon solmu on joukkueen hyökkäysalueella (hallussa kummalla tahansa tai irtokiekkona). Paineen sekuntikasvu vaatii lisäksi oman hallinnan (O-9).

### O-15 Tasapainoarvot (virstanpylväs 3)

Paikkamerkit ovat `_placeholders`-listassa ja kalibroidaan simulaatiolla. "Datassa" = arvo on jo `tuning.json`:ssa (nykyinen lataaja hyväksyy sen, koska osio on tyypittämätön tai arvo on tarkistuksen lisäkenttä tai muokkaaja). "Odottaa lataajaa" = arvo lisätään dataan samassa commitissa lataajan tuen kanssa (O-16).

| Polku | Arvo | Tila | Lähde |
| --- | --- | --- | --- |
| `time.periods`, `time.periodSeconds` | 3, 1 200 | datassa | Oikea kiekko, ei paikkamerkki |
| `time.forwardShiftSeconds`, `time.defenceShiftSeconds` | 45, 50 | datassa | Visio / paikkamerkki |
| `time.stoppageChangeMinSeconds` | 20 | odottaa lataajaa | Paikkamerkki (O-2) |
| `time.setupSeconds`, `time.regroupSeconds` | 6, 8 | datassa | Q-006 |
| `energy.start` | 1,0 | datassa | |
| `energy.drainPerSecondOnIce` | 0,0055 | datassa | Uusi, paikkamerkki (O-5) |
| `energy.costPerAction` | 0,02 | datassa | Paikkamerkki, kaikki osallistujat (D-062) |
| `energy.enduranceCostReductionPerPoint` | 0,03 | datassa | Paikkamerkki |
| `energy.benchRecoveryRate` | 0,011 | datassa | Eksponentiaalinen, τ ≈ 90 s. Entinen `benchRecoveryPerSecond` 0,01 (lineaarinen), nimetty uudelleen (D-062) |
| `energy.checkModifierAtZero` | −1,2 | datassa | Entinen −0,8 |
| `organization.dropOnTurnover` | 0,35 / 0,5 / 0,65 | datassa | Entinen 0,2 / 0,4 / 0,6; merkitys "taso = 1 − drop" |
| `organization.dropFactorOnShotOrDump` | 0,5 | datassa | Uusi |
| `organization.dropPerCommittedPlayer` | 0,1 | datassa | Uusi |
| `organization.recoveryPerSecond` | 0,04 | datassa | Korvaa `recoveryPerEvent` 0,15 |
| `organization.recoveryWeights` | Nopeus 0,5, Pelinluku 0,5 | datassa | stats-and-checks.md |
| `organization.recoveryPerStatPoint` | 0,06 | datassa | Entinen 0,01; nyt kerroin (1 + arvo × (S̄ − 10,5)) |
| `organization.organizedThreshold` | 0,8 | datassa | |
| `checks.deke`, `checks.breakout`, `checks.block` `.modifiers.organization` | 0,6, 0,6, 0,5 | datassa | Uusi |
| `checks.pass`, `checks.zoneEntryCarry`, `checks.shot` `.modifiers.organization` | 0,6, 0,8, 0,7 | datassa | Ennallaan |
| `checks.shot.modifiers.screenContested` | 0,15 | datassa | Q-031 (Jerry), ei paikkamerkki |
| `checks.shot.missedOutOfPlayShare` | 0,25 | datassa | D-063, paikkamerkki |
| `pressure.gainOnZoneEntry`, `gainOnShot`, `gainPerSecondInZone` | 0,1, 0,08, 0,005 | datassa | Paikkamerkit |
| `pressure.decayPerSecond` | 0,01 | odottaa lataajaa | Uusi, paikkamerkki |
| `pressure.keepOnStoppage`, `energyDrainPerSecond`, `mentalToughnessReductionPerPoint` | 0,5, 0,002, 0,03 | datassa | Paikkamerkit |
| `pressure.underPressureNodes` | 0 | datassa | D-056 |
| `familiarity.freeUses` | 2 | datassa | Uusi, paikkamerkki |
| `familiarity.penaltyPerRepeat` | 0,08 | datassa | Entinen 0,1 |
| `familiarity.maxPenalty` | 0,5 | datassa | |
| `familiarity.intermissionMultiplier` | 0,5 | datassa | D-066 (puolitus), ei paikkamerkki |
| `transitions.rushMaxActions` | 4 | odottaa lataajaa | Uusi osio, paikkamerkki |
| `transitions.rushShotMinBaseXg` | 0,05 | odottaa lataajaa | Uusi osio, paikkamerkki |
| `chanceTypes.turnoverWindowSeconds` | 8 | datassa | |
| `chanceTypes.rushOrganizationBelow` | poistetaan | odottaa lataajaa | Sama kuin `organization.organizedThreshold` |
| `chemistry`, `form` | – | datassa | Ei käytössä virstanpylväässä 3 (vaikutus 0) |

### O-16 Lataajan muutokset ja validointi

Kaikki alla olevat avaimet ovat pakollisia (puuttuva = virhe), ja tuntematon avain hylätään osioissa, jotka lataaja tyypittää. Osiot `energy`, `organization` ja `familiarity` siirtyvät tyypitetyiksi, ja `transitions` lisätään sallittuihin osioihin.

| Polku | Hylätään, jos |
| --- | --- |
| `energy.start` | ei välillä (0, 1] |
| `energy.drainPerSecondOnIce`, `energy.costPerAction` | < 0 tai > 1 |
| `energy.enduranceCostReductionPerPoint`, `pressure.mentalToughnessReductionPerPoint` | < 0, tai kerroin 1 − arvo × (`stats.max` − `referenceValue`) ≤ 0 |
| `energy.benchRecoveryRate` | ≤ 0 |
| `energy.checkModifierAtZero` | > 0 |
| `organization.dropOnTurnover` | avaimet eivät ole tasan `defensive`, `neutral`, `offensive`, tai arvo ei ole välillä [0, 1] |
| `organization.dropFactorOnShotOrDump`, `organization.dropPerCommittedPlayer` | ei välillä [0, 1] |
| `organization.recoveryPerSecond` | ≤ 0 |
| `organization.recoveryWeights` | tuntematon kenttäpelaajan stat tai painojen summa poikkeaa 1:stä yli 1e-6 |
| `organization.recoveryPerStatPoint` | < 0, tai 1 + arvo × (`stats.min` − `referenceValue`) ≤ 0 |
| `organization.organizedThreshold` | ei välillä (0, 1] |
| `pressure.gainOnZoneEntry`, `gainOnShot`, `gainPerSecondInZone`, `decayPerSecond`, `energyDrainPerSecond` | < 0 |
| `pressure.keepOnStoppage` | ei välillä [0, 1] |
| `familiarity.freeUses`, `penaltyPerRepeat`, `maxPenalty` | < 0 |
| `familiarity.intermissionMultiplier` | ei välillä [0, 1] |
| `transitions.rushMaxActions` | ei kokonaisluku tai < 1 |
| `transitions.rushShotMinBaseXg` | ei välillä [0, 1] |
| `time.stoppageChangeMinSeconds` | ≤ 0 tai ≥ min(`forwardShiftSeconds`, `defenceShiftSeconds`) |
| `checks.shot.missedOutOfPlayShare` | ei välillä (0, 1) (nykyinen `...Share`-sääntö) |
| `modifiers.organization` | puuttuu jostakin tarkistuksesta `pass`, `zoneEntryCarry`, `deke`, `breakout`, `shot`, `block` |

`looseChasers` ∈ {1, 2} validoidaan valmentajan päätöksessä, ei `tuning.json`:ssa.

**Data, joka lisätään samassa commitissa lataajan tuen kanssa** (nykyinen lataaja hylkäisi sen):

```json
"time": {
  "stoppageChangeMinSeconds": 20
},
"pressure": {
  "decayPerSecond": 0.01
},
"transitions": {
  "_notes": "Built-in rush (D-007, D-062, data-schema.md O-7): no play, no setup. At most rushMaxActions actions; the carrier shoots from an offensive-zone node whose checks.shot.baseXg is at least rushShotMinBaseXg, else passes to a teammate on such a node with a free lane, else skates toward one. Ends when the defence reaches organization.organizedThreshold.",
  "rushMaxActions": 4,
  "rushShotMinBaseXg": 0.05
},
"chanceTypes": {
  "_notes": "Shot classification by origin (data-schema.md O-12). Rush uses organization.organizedThreshold.",
  "turnoverWindowSeconds": 8
}
```

Lisäksi lataajan tyypitys `energy`-osiolle: avain `energy.benchRecoveryPerSecond` on jo nimetty datassa uudelleen `energy.benchRecoveryRate` (eksponentiaalinen, 0,011) ja `energy.drainPerSecondOnIce` (0,0055) on lisätty; lataajan pitää vaatia nämä ja hylätä vanha nimi. Sama koskee `organization.recoveryPerSecond` (korvaa `recoveryPerEvent`), `organization.dropFactorOnShotOrDump`, `organization.dropPerCommittedPlayer`, `familiarity.freeUses` ja `familiarity.intermissionMultiplier`, jotka ovat jo datassa.

`time`- ja `pressure`-osioihin avain lisätään olemassa olevien rinnalle, `transitions` on uusi ylimmän tason osio ja `chanceTypes` korvataan kokonaan (avain `rushOrganizationBelow` poistuu). `_placeholders`-listaan lisätään `time.stoppageChangeMinSeconds`, `pressure.decayPerSecond`, `transitions.rushMaxActions` ja `transitions.rushShotMinBaseXg`, ja siitä poistetaan `chanceTypes.rushOrganizationBelow`.
