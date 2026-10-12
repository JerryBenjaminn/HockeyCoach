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
| `data/replay.json` | käytössä (D-088) | Toisto (`Replay/Core`) ja sen testit, ei Sim eikä Harness |
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

**Sallitut ylimmän tason avaimet.** Lataaja hylkää tiedoston, jos sen ylimmällä tasolla on avain, jota ei ole tässä listassa (`_`-alkuisia meta-avaimia lukuun ottamatta): `schemaVersion`, `stats`, `checkFormula`, `checks`, `positions`, `time`, `energy`, `organization`, `pressure`, `form`, `chemistry`, `familiarity`, `plays`, `chanceTypes`, `chanceClasses`, `loosePuckSpots`, `transitions`, `ai`. Lista vastaa lataajan sallittujen osioiden listaa. `transitions` (virstanpylväs 3) lisätään lataajaan ja dataan samassa commitissa (ks. Ottelu ja tilat, Lataajan muutokset). `ai` (virstanpylväs 4) lisätään samoin (ks. Valmentaja-AI:t ja aikalisä, AI-6). Osiot, joiden virstanpylväs ei ole vielä käynnissä, hyväksytään ja ohitetaan, kunnes koodi alkaa käyttää niitä.

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
| `time` | 2–3 | `periods`, `periodSeconds`, `forwardShiftSeconds`, `defenceShiftSeconds` (kolmikot ja pakkiparit vaihtuvat erikseen, D-023), `stoppageChangeMinSeconds` (virstanpylväs 3, O-2), `secondsPerAction` (myös `systemStep`: järjestelmätilan askel, D-036), `setupSeconds`, `regroupSeconds` (Q-006), `timeoutsPerTeamPerMatch` ja `timeoutRestSeconds` (virstanpylväs 4, AI-4) |
| `energy` | 3 | `start`, `drainPerSecondOnIce`, `costPerAction`, `enduranceCostReductionPerPoint`, `benchRecoveryRate` (eksponentiaalinen), `checkModifierAtZero`. Ks. O-5 |
| `positions` | 2 | `offSideCheckModifier`: väärän puolen miinus (D-024), ks. Pelaajat, pelipaikat ja ketjut |
| `organization` | 3 | `dropOnTurnover` alueittain, `dropFactorOnShotOrDump`, `dropPerCommittedPlayer`, `recoveryPerSecond`, `recoveryWeights`, `recoveryPerStatPoint`, `organizedThreshold`. Ks. O-6 |
| `pressure` | 2–3 | Joukkueen painetila: `gainOnZoneEntry`, `gainOnShot`, `gainPerSecondInZone`, `decayPerSecond`, `keepOnStoppage`, `energyDrainPerSecond`, `mentalToughnessReductionPerPoint` (O-9). Lisäksi `underPressureNodes` (virstanpylväs 2): fyysisen paineen säde, ei joukkueen painetila (Määritelmät M-7, D-049, D-056), ja `presserReactionNodes`: painostajan askeleet onnistuneen syötön jälkeisessä tahdissa (D-074, Puolustusjärjestelmät, Painostajan reaktioviive) |
| `familiarity` | 3 | `freeUses`, `penaltyPerRepeat`, `maxPenalty`, `intermissionMultiplier`. Ks. O-11 |
| `form`, `chemistry` | myöhemmin | Vire (±`maxStatDelta`), ketjukemia. Eivät käytössä virstanpylväässä 3 (vaikutus 0) |
| `plays` | 2 | `maxBeats` (4), `maxNodesPerBeat` |
| `transitions` | 3 | `rushMaxActions`, `rushShotMinBaseXg`: sisäänrakennettu suorahyökkäys. `regroupForecheckersAllowed` (D-078 K1): kuinka monta karvaajaa omalla alueella kokoaminen sietää ilman `breakout`-tarkistusta. Ks. O-7 |
| `chanceTypes` | 2–3 | `turnoverWindowSeconds`. Suorahyökkäyksen raja on `organization.organizedThreshold` (O-12); `rushOrganizationBelow` poistuu |
| `chanceClasses` | 4 | Paikkaluokkien xG-rajat (Q-004) |
| `loosePuckSpots` | 2 | Irtokiekon paikkasäännöt (D-044, D-049), ks. Määritelmät, Irtokiekon paikat. Data ja lataajan tuki commitoidaan yhdessä (D-030) |
| `ai` | 4 | Valmentaja-AI:den kynnysarvot (`random`, `estimate`, `rulebased`, `adaptive`), ks. Valmentaja-AI:t ja aikalisä, AI-3 |

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
- `zoneEntryCarry`: `modifiers.strengthDifferencePerPlayer` (voimasuhde N − M) ja `breakout`: `modifiers.perForechecker` (karvaajien määrä), ks. Määritelmät M-9 (D-078 K4).
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
| `scope` | `perMatch`, `perTeamPerMatch`, `perTeam`, `perShot`, `perShift`, `perMatchup` (ks. alla) |
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
| `perShift` | Yhden vaihdon pituus (D-071). Vaihto = yksikön yhtäjaksoinen jääaika jäälle tulosta vaihtoon tai erän loppuun (erän alku aloittaa uuden vaihdon, Q-047). Arvo = Σ jääaika / Σ vaihdot yhden joukkueen kaikista kyseisen yksikkötyypin vaihdoista koko batchissa; raportoidaan joukkueittain ja yhteensä. Kolmikko ja pari vaihtuvat yhtenä yksikkönä, joten yksikön vaihto = sen pelaajien vaihto. Maalivahti ei kuulu |
| `perMatchup` | AI-parin tulos koko batchissa (esim. voitto-osuus) |

**Yhteiset säännöt (D-071).**

- **Varsinainen peliaika (Q-052).** Kaikki haarukat koskevat varsinaista peliaikaa 3 × 20 min. Jos simulaatioon tulee jatkoaika, sen tapahtumat (erä > 3) jätetään pois kaikista mittareista. Nyt jatkoaikaa ei ole.
- **Koko ottelu vs. 5v5.** Avaimet ilman `5v5`-päätettä lasketaan kaikista pelitilanteista (D-069). `5v5`-avaimet lasketaan vain tapahtumista, joiden voimasuhde on 5 vs 5. Ennen erikoistilanteita molemmat ovat samat.
- **xG:n paikkatyyppiosuudet (Q-051).** `evenStrengthXgShareRush`, `...Turnover` ja `...ZonePlay` lasketaan toistaiseksi **ilman aloitusluokkaa**: osoittaja ja nimittäjä ovat tasakentällisten laukausten xG:t luokista suorahyökkäys, riisto ja alueella pelaaminen, ja aloituslaukaukset (O-12, paikkatyyppi aloitus) jätetään pois molemmista. Kolmen osuuden summa on 1. `evenStrengthXgShareFaceoff` on aloitusluokan osuus kaikkien neljän luokan tasakentällisestä xG:stä. Kun Jerry on täyttänyt osuudet kaikkine luokkineen, sääntö päätetään uudelleen.
- **Automaattinen vertailu (tech-spec.md, Tavoitevertailu).** Harness vertaa jokaista mittaria, jolla on sekä `min` että `max`, riippumatta `status`-arvosta: arvo < `min` → TOO LOW, arvo > `max` → TOO HIGH, muuten OK (rajat mukaan lukien). Jos kumpikin on `null`, raportti näyttää arvon ilman tilaa. Vertailtava arvo on mittarin yhteisarvo (molemmat joukkueet yhdessä `scope`-säännön mukaan) jokaiselle AI-parille erikseen; joukkuekohtaiset arvot näytetään rinnalla. `perMatchup`-mittari verrataan vain AI-pareille, joita se koskee.

**Mittarit ja niiden laskenta.** Harness laskee arvot tapahtumalokista ja erän tilannekuvista (`PeriodSnapshot`). "Laukaus" = Laukaus-tapahtuma (kaikki lopputulokset: maali, torjuttu, blokattu, ohi), joukkue = laukojan joukkue. Osuudet (`perTeam`) ovat summien suhteita koko batchista, eivät otteluiden osuuksien keskiarvoja.

| Avain | `unit` / `scope` | Laskenta |
| --- | --- | --- |
| `smartVsRandomWinRate` | share / perMatchup | (Älykkään AI:n voitot + 0,5 × tasapelit) / ottelut parissa älykäs vs. satunnainen: tasapeli on puoli voittoa (D-073). Raportti näyttää rinnalla voitot, tasapelit ja tappiot erikseen (määrät ja osuudet älykkään AI:n näkökulmasta). Tasapeli = varsinaisen peliajan lopussa tasan (Q-052, ei jatkoaikaa) |
| `goalsPerMatch` | count / perMatch | Kotimaalit + vierasmaalit |
| `goalsPerTeamPerMatch` | count / perTeamPerMatch | Joukkueen maalit (laukaukset, lopputulos maali) |
| `shotAttempts` | count / perTeamPerMatch | Joukkueen laukaukset, kaikki lopputulokset, blokatut mukaan lukien (Q-050) |
| `shotsOnTarget` | count / perTeamPerMatch | Lopputulos maali tai torjuttu |
| `shotsBlocked` | count / perTeamPerMatch | Joukkueen omat laukaukset, jotka vastustaja blokkasi |
| `shotsMissed` | count / perTeamPerMatch | Lopputulos ohi, myös kaukalon ulkopuolelle menneet (O-13) |
| `xg` | xg / perTeamPerMatch | Joukkueen laukausten xG:n summa, koko ottelu (raportin xG = yrityksen kokonaistodennäköisyys, Q-014) |
| `xg5v5` | xg / perTeamPerMatch | Sama, vain 5 vs 5 |
| `evenStrengthXgShareRush` | share / perTeam | Tasakentällinen xG, paikkatyyppi suorahyökkäys / (suorahyökkäys + riisto + alueella pelaaminen), ilman aloituksia |
| `evenStrengthXgShareTurnover` | share / perTeam | Sama, riisto |
| `evenStrengthXgShareZonePlay` | share / perTeam | Sama, alueella pelaaminen |
| `evenStrengthXgShareFaceoff` | share / perTeam | Tasakentällinen xG, paikkatyyppi aloitus / kaikkien neljän luokan tasakentällinen xG |
| `shotXgTypical` | xg / perShot | Raportti näyttää laukausten xG:n 10., 50. ja 90. persentiilin. TOO LOW, jos 10. persentiili < `min`; TOO HIGH, jos 90. persentiili > `max`; muuten OK (D-073) |
| `shotsUnderPressureShare` | share / perTeam | Laukaukset, joissa `underPressure` (M-7, myös D-070:n poikkeus) / laukaukset. Analyysin laukausten paineosuuden (68 % / 76 %) vertailumittari (D-075); paineen määritelmä odottaa Q-049:ää |
| `shots5v5` | count / perTeamPerMatch | Laukaukset 5 vs 5 |
| `shots5v5Rush`, `shots5v5Turnover`, `shots5v5ZonePlay` | count / perTeamPerMatch | Laukaukset 5 vs 5 paikkatyypin mukaan (aloituslaukaukset eivät kuulu mihinkään näistä) |
| `chances5v5Top`, `chances5v5Good`, `chances5v5Moderate` | count / perTeamPerMatch | Laukaukset 5 vs 5 paikkaluokan mukaan (Q-004) |
| `chancesAll` | count / perTeamPerMatch | Laukaukset, joilla on paikkaluokka (huippu + hyvä + kohtalainen), kaikki pelitilanteet |
| `passSuccessAll` | share / perTeam | Onnistuneet syötöt / syötöt (Syöttö-tapahtumat, myös avaussyötöt) |
| `passSuccess5v5` | share / perTeam | Sama, vain 5 vs 5 |
| `passSuccessUnderPressure` | share / perTeam | Onnistuneet paineen alla / syötöt paineen alla. Diagnostiikkamittari: analyysin 69 % / 68 % ei ole tämä luku vaan paineosuus (`passesUnderPressureShare`, D-075, Q-056), eikä tälle mittarille ole vielä analyysin havaintoa |
| `passesUnderPressureShare` | share / perTeam | Syötöt paineen alla / syötöt. Analyysin syöttöjen paineosuuden (69 % / 68 %) vertailumittari (D-075, Q-056) |
| `puckBattleWinShare`, `puckBattleNoWinnerShare`, `puckBattleLossShare` | share / perTeam | Joukkueen kamppailuvoitot, ei voittajaa, häviöt / joukkueen kamppailut. Kamppailut = Kamppailu-tapahtumat ja Kiekko päätyyn -tapahtumien kamppailut, molemmille joukkueille |
| `faceoffsPerMatch` | count / perMatch | Aloitus-tapahtumat ottelussa (erien aloitukset mukaan lukien) |
| `meanTimeBetweenStoppages` | seconds / perMatch | Ottelun arvo = 3 600 / ottelun aloitukset (sama laskutapa kuin Jerryn keruupohjassa), keskiarvo otteluista |
| `offensiveZoneTime` | seconds / perTeamPerMatch | Joukkueen hyökkäysalueaika (`OffensiveZoneSeconds`) erien summana |
| `offensiveZoneTime5v5` | seconds / perTeamPerMatch | Sama, vain 5 vs 5 |
| `zoneEntrySuccessCarry`, `zoneEntrySuccessPass` | share / perTeam | Hallitut alueelletuonnit tavalla kuljetus / syöttö: lopputulos "hallinta säilytetty" / kaikki |
| `zoneEntryStrengthDisadvantage`, `zoneEntryStrengthEven`, `zoneEntryStrengthAdvantage` | share / perTeam | Voimasuhdejakauma (M-8, D-081): hallitut alueelletuonnit, joiden lopputulos on "hallinta säilytetty", kuljetus ja syöttö yhdessä, ryhmissä N < M / N = M / N > M jaettuna samoilla onnistuneilla alueelletuonneilla. Vain onnistuneet, koska Wisehockeyn data sisältää vain kiekon säilyttäneet alueelletuonnit. Raportti näyttää lisäksi saman jakauman kaikista yrityksistä (diagnostiikka) |
| `shiftLengthForward` | seconds / perShift | Joukkueen kolmikoiden jääaika / kolmikoiden vaihdot |
| `shiftLengthDefence` | seconds / perShift | Joukkueen parien jääaika / parien vaihdot |

**Lataaja.** `scope` hyväksyy arvon `perShift` (D-071). Mittarin `_note` on meta-avain kuten muualla.

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

**Liikkuminen.** Jokainen puolustaja liikkuu tapahtumaa kohden enintään `plays.maxNodesPerBeat` askelta kohti kohdettaan (setupissa ja regroupissa rajattu askelmäärä, O-6; painostaja onnistuneen syötön jälkeen `pressure.presserReactionNodes`, D-074, alla). Askel on (sign(dx), sign(dy)), eli vinottain, kunnes toinen koordinaatti täsmää. Jos askel osuisi maalisolmuun, puolustaja kiertää maalin (D-053, alla). Samassa solmussa saa olla enintään yksi pelaaja kummastakin joukkueesta (D-058): päätesolmut ratkaistaan Solmun pelaajamäärä -säännöllä (alla). Järjestäytyneisyys on joukkueen skaalaariarvo (O-6), jota sijainnit seuraavat viiveellä; sitä ei johdeta sijainneista.

**Painostajan reaktioviive (D-074, muuttaa D-041:tä).** Kiekko liikkuu nopeammin kuin pelaaja: kun kiekko on liikkunut onnistuneella syötöllä, painostaja liikkuu seuraavan kiekkotoiminnon edellä enintään `pressure.presserReactionNodes` (1) askelta `plays.maxNodesPerBeat`-arvon (2) sijasta. Muut puolustajat liikkuvat kuten ennen.

1. **Laukaisija:** onnistunut syöttö, eli kiekko tuli vastaanottajalle (Syöttö-tapahtuma, lopputulos onnistunut). Jokainen onnistunut syöttö laukaisee viiveen uudelleen, joten syöttö → syöttö → laukaus viivästää painostajaa kahdesti.
   - **Epäonnistunut syöttö ei laukaise viivettä** (designerin suositus, D-074 ei ratkaise): syötönkatkossa kiekko vaihtaa joukkuetta, joten viivästettävää painostajaa ei ole; irtokiekko ratkaistaan kamppailulla (M-6), jonka liikkeet eivät ole tahdin vaihe 2. Irtokiekon voitto, aloitusvoitto, rebound ja kuljetus (`skate`) eivät ole syöttöjä.
2. **Mihin liikkeeseen viive kohdistuu:** täsmälleen yhteen liikkeeseen, eli syöttöä seuraavan kiekkotoiminnon D-041:n vaiheeseen 2 (puolustajien järjestelmäliike), kun seuraava toiminto on saman joukkueen kuvion tahti, aloituskuvion tahti tai suorahyökkäyksen toiminto (O-7). Kuviossa syöttö tahdissa n → viive tahdin n + 1 vaiheessa 2. Syötön oman tahdin liike tapahtui jo ennen syöttöä (vaihe 2 ennen vaihetta 3), joten siihen viive ei vaikuta.
3. **Viive raukeaa** (designerin suositus), jos syötön ja seuraavan tahdin vaiheen 2 väliin tulee mikä tahansa muu puolustajien liike tai aikakokonaisuus: järjestelmätilan askel (kuvion viimeinen tahti oli syöttö), setup tai regroup (esim. suorahyökkäys päättyy syötön jälkeen ja kuvio valitaan D-036:n mukaan), katko, vaihto, erän loppu tai kiekonmenetys. Perustelu: viive on yhden tahdin reaktio, ja näissä tilanteissa painostajalla on aikaa ehtiä kohteeseensa. Viive ei koskaan siirry myöhempään toimintoon.
4. **Painostajan tunnistus:** sama valitsin kuin M-7:n Painostaja, mutta arvioituna **ennen** vaiheen 2 liikettä, eli hyökkääjien siirtojen (vaihe 1) jälkeen, kiekollisena syötön vastaanottaja. Kiekollinen ei liiku vaiheessa 1, joten kiekon solmu on sama kuin syötön lopussa ja liikkeessä.
   - **Järjestelmä ohjaa liikettä** (M-7, kohta 1): painostaja on se rooli, jonka kohde tämän liikkeen valitussa säännössä on valittu `puckOffset` (pienin Chebyshev-pituus, sitten Manhattan, sitten `F1`, `D1`, `D2`, `F2`, `F3`; `pinch`-D1 pituudella 0). Roolit ovat samat, jotka liike itse jakaa nykyisistä sijainneista, joten painostaja on se puolustaja, joka tässä liikkeessä luistelee painostajan kohteeseen. Ei ehdokasta → ei viivettä.
   - **Suorahyökkäys** (M-7, kohta 2, D-074): painostaja on vastaanottajaa lähin puolustaja (Chebyshev, Manhattan, `C`, `LW`, `RW`, `LD`, `RD`) ennen vaihetta 2. Puolustajat liikkuvat silti järjestelmän mukaan (O-7); viive rajaa vain tämän pelaajan askeleet.
   - **Toinen yritys** (O-8): ei puolustajien liikettä ennen laukausta eikä syöttöä, joten viive ei tule kyseeseen.
   - Toiminnon hetken paine (M-7) valitaan edelleen omalla arvioinnillaan liikkeen jälkeen. Jos roolit vaihtuvat liikkeen aikana (viivästetty painostaja jää jälkeen ja toinen puolustaja on nyt lähempänä), M-7 voi valita eri pelaajan. Tätä ei yhtenäistetä, koska M-7 on puhdas funktio nykyisistä sijainneista (Q-054, kohta 3).
5. **Askeleet ja maalin kierto (D-053):** painostaja ottaa enintään N = `pressure.presserReactionNodes` askelta tavallisilla askelsäännöillä. Kiertoaskel (korvaava solmu) on yksi N:stä askeleesta. Arvolla 1 painostaja ottaa vain ensimmäisen, tarvittaessa korvatun askeleen.
6. **Solmun pelaajamäärä (D-058):** painostajan ihannepääte ja polku lasketaan kohdassa 1 lyhennetyllä askelmäärällä N. Käsittelyjärjestys (`F1`, `D1`, `D2`, `F2`, `F3`), peruutus omaa (lyhyempää) polkua pitkin ja renkaat ihannepäätteen ympärillä ovat ennallaan. Muiden puolustajien ihannepäätteet eivät muutu, mutta lopulliset päätteet voivat muuttua, koska painostaja varaa eri solmun.
7. **Ei satunnaisuutta, ei uutta tapahtumaa.** Viivettä ei kirjata tapahtumalokiin. Toteutus voi tallentaa tiedon "viive odottaa" onnistuneen syötön yhteydessä (samaan paikkaan kuin M-3:n ja D-070:n syöttötiedon) ja nollata sen kohdan 3 tilanteissa.
8. **Raja-arvot:** N ≥ `plays.maxNodesPerBeat` = ei vaikutusta; N = 0 = painostaja ei liiku syötön jälkeisessä tahdissa. Validointi: O-16.

| Syöttö | Viive seuraavassa toiminnossa |
| --- | --- |
| Kuvion `pass` tahdissa n, tahti n + 1 olemassa | kyllä, tahdin n + 1 vaihe 2 |
| Kuvion viimeinen tahti on `pass` | ei, viive raukeaa (järjestelmätilan askel tai setup ennen seuraavaa toimintoa) |
| D-036:n aloitussyöttö setupin jälkeen (kiekollinen → kuvion `puckCarrier`) | kyllä, ensimmäisen tahdin vaihe 2. Setup on ennen syöttöä (designerin suositus) |
| Aloituskuvion `pass` (D-060) | kyllä, kuten kuvion tahti. Aloitusvoitto ei ole syöttö |
| Avaussyöttö omalta alueelta (`breakout`-tarkistus) | kyllä. Tarkistuksen tyypillä ei ole väliä, kiekko liikkui syötöllä (designerin suositus) |
| Suorahyökkäyksen `pass` (O-7) | kyllä (D-074), seuraavan rush-toiminnon vaihe 2, jos rush jatkuu. Jos rush päättyy (O ≥ raja tai toiminnot käytetty), viive raukeaa |
| Epäonnistunut syöttö (syötönkatko tai irtokiekko) | ei |

Muut sijoitukset (setup, regroup, järjestelmätilan askel, irtokiekko, O-10:n ohjeet) käyttävät omia askelmääriään ilman viivettä. Laajuuden tarkennukset (epäonnistunut syöttö, raukeaminen, D-036:n aloitussyöttö, `breakout`) ovat designerin suosituksia (Q-055).

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

Kiekollinen (syöttäjä tai laukaisija) on **paineen alla**, kun puolustavan joukkueen **painostaja** on enintään `pressure.underPressureNodes` (0, D-056) solmun päässä hänestä (Chebyshev) toiminnon hetkellä. Arvolla 0 painostajan pitää olla samassa solmussa. Sama määritelmä koskee syöttöjä ja laukauksia (D-072). Säde kalibroidaan Wisehockeyn 68–76 %:iin (Q-032).

**Painostaja (D-072, Q-049 vaihtoehto 2).** Paineen antaa vain yksi puolustaja: se, joka pelaa kiekolliseen. Muut puolustajat, myös samassa solmussa olevat, eivät anna painetta. Ne vaikuttavat vain blokkiin (M-5), maskiin (M-4) ja syöttölinjaan (M-1). Painostaja valitaan toiminnon hetkellä (sijainnit kuten Määritelmien johdannossa, D-041:n vaiheiden 1 ja 2 jälkeen) näin:

1. **Järjestelmä ohjaa liikettä** (taulukko alla): järjestelmä arvioidaan toiminnon hetkellä samoin kuin liikkeessä (Puolustusjärjestelmät, Arviointi, kohdat 1–5: näkökulma, `mirrorY`, säännön valinta, roolien jako nykyisistä sijainneista, kohteiden laskenta), ja `pinch`-ohje (O-10) otetaan huomioon. Arviointi on puhdas funktio eikä käytä satunnaisuutta. Kiekollisen solmu on sama kuin vaiheen 2 liikkeessä, koska kiekollinen ei liiku kiekottomien siirroissa, joten valittu sääntö on sama kuin liikkeessä.
   - **Ehdokkaat** ovat roolit, joiden kohde valitussa säännössä on `puckOffset`, **millä tahansa** `[dx, dy]`-arvolla (D-072 ei rajaa arvoa [0, 0]:aan). `pinch`-ohjeen D1, jonka kohde on korvattu kiekon solmulla, on ehdokas kuten `puckOffset` [0, 0].
   - **Useampi ehdokas** (esim. forecheck212:n keskialue: F1 [0, 0] ja F2 [−1, 1] tai [−1, 0]; `pinch`-D1 ja F1): valitaan se, jonka `puckOffset` on pienin Chebyshev-pituudeltaan max(|dx|, |dy|), sitten pienin Manhattan-pituus |dx| + |dy|, sitten kiinteä roolijärjestys `F1`, `D1`, `D2`, `F2`, `F3` (sama kuin Solmun pelaajamäärä -säännön käsittelyjärjestys, D-058). `pinch`-D1:n pituus on 0. Esimerkit: forecheck212 keskialue → F1; `pinch`-D1 ja F1 [0, 0] → F1; trap122 hyökkäysalue → F1 ([−1, 1]).
   - **Ei ehdokasta** (säännössä ei ole `puckOffset`-kohdetta): kukaan ei anna painetta, kiekollinen ei ole paineen alla. Järjestelmä on valinnut aluepuolustuksen. Nykyisissä järjestelmissä jokaisessa säännössä on `puckOffset`-kohde.
   - **Painostaja ei ole säteellä:** jos painostajan Chebyshev-etäisyys kiekolliseen on yli `pressure.underPressureNodes`, kiekollinen **ei ole paineen alla**, vaikka joku toinen puolustaja olisi samassa solmussa.
   - Huom. trap122:n F1:n kohde hyökkäys- ja keskialueella on solmun päässä kiekollisesta ([−1, 1] tai [−1, 0], D-057). Säteellä 0 trap122 antaa siellä painetta vain, kun F1 on kohteeseen matkalla kiekollisen solmussa tai kohde on rajattu kiekon solmuun. Tämä on tarkoitettu ero: trap ohjaa, ei prässää.
2. **Järjestelmä ei ohjaa liikettä:** painostaja on kiekollista **lähin puolustaja**: pienin Chebyshev-etäisyys kiekollisen solmuun, tasatilanteessa pienin Manhattan-etäisyys, sitten pelipaikkajärjestys `C`, `LW`, `RW`, `LD`, `RD` (yleinen lähimmän sääntö, kuten M-6). Koska säde mitataan Chebyshevillä, tulos "paineen alla" on sama kuin vanhalla säännöllä (joku puolustaja säteellä); tasatilanteen ratkaisu vaikuttaa vain painostajan identiteettiin, ei tulokseen.

| Tilanne, jossa syötetään tai laukotaan | Painostaja |
| --- | --- |
| Kuvion tahti (D-041), myös D-036:n avaussyöttö setupin jälkeen ja avaussyöttö omalta alueelta (`breakout`) | järjestelmä (kohta 1) |
| Aloituskuvion tahti (D-060). Ensimmäisessä tahdissa ei ole setuppia, mutta tahdin vaihe 2 liikuttaa puolustajia järjestelmän mukaan | järjestelmä (kohta 1) |
| Suorahyökkäys (O-7) | lähin (kohta 2). Puolustajat liikkuvat rushin aikana järjestelmän mukaan (O-7), mutta paineen antaa lähin puolustaja (Q-054, kohta 6, hyväksytty D-074) |
| Toinen yritys (O-8): ei puolustajien liikettä ennen laukausta, kiekko oli irti | lähin (kohta 2) |
| Järjestelmätila, regroup, setup, irtokiekkokamppailu, aloitustarkistus | ei syöttöjä eikä laukauksia, sääntöä ei tarvita |

Muotoilun yksityiskohdat (roolien jako toiminnon hetkellä, kaikki `puckOffset`-arvot, useamman ehdokkaan valinta, ei ehdokasta → ei painetta, `pinch`, suorahyökkäys) olivat designerin suosituksia (Q-054), ja Jerry hyväksyi ne (D-074). Sama valitsin tunnistaa painostajan reaktioviiveessä (D-074, Puolustusjärjestelmät, Painostajan reaktioviive), mutta ennen tahdin puolustajaliikettä. Painostajaa ei kirjata tapahtumalokiin (uusi kenttä vaatisi tapahtumaskeeman muutoksen).

**Säteen kalibrointi (D-072).** Jos 200 ottelun mittauksessa laukausten tai syöttöjen paineosuus putoaa alle 68 %:n, designer saa nostaa `pressure.underPressureNodes`-arvon 1:een. Se on erillinen, mitattu vaihe ohjelmoijan toteutuksen ja mittauksen jälkeen, eikä arvoa muuteta tämän speksin yhteydessä. Kalibroinnin vaihe 3 (syötöt) odottaa tätä mittausta.

- **Syöttö:** tapahtuman kenttä "paineen alla" = syöttäjä on paineen alla. Kun se on tosi, syöttötarkistukseen lisätään ehtomuokkaaja `pass.modifiers.underPressure` (−0,9; analyysi 85 % → n. 70 %).
- **Nimimuutos (D-049):** `pass.modifiers.pressure` on nimetty `underPressure`:ksi (arvo ei muuttunut). Yleisten käytäntöjen mukaan `pressure`-niminen muokkaaja skaalautuu joukkueen painetilalla (hyökkäävän joukkueen momentum), eikä momentum saa heikentää hyökkäävän joukkueen omia syöttöjä. Analyysin "paineen alla" tarkoittaa fyysistä painetta syöttäjään. `underPressure` on Yleisten käytäntöjen ehtomuokkaajien listassa.
- **Laukaus:** sama ehto laukaisijalle (sama painostajan sääntö, D-072). Laukaustapahtuman kenttä `underPressure` (D-050, `stats-and-checks.md`, Tapahtumaskeema) = laukaisija on paineen alla, ja se kirjataan jokaiseen laukaukseen. Virstanpylväässä 2 se on vain raporttia varten (mittari "Laukaukset paineen alla (osuus)") eikä muuta todennäköisyyksiä. `shot.modifiers.pressure` pysyy joukkueen painetilaan sidottuna. Säteen kalibrointi: Q-032.
- **Laukaus heti syötöstä (D-070, Q-049).** Laukaisija **ei ole paineen alla**, vaikka edellinen ehto täyttyisi laukaushetkellä, kun molemmat pätevät:
  1. **Heti syötöstä:** laukaus on onnistunutta syöttöä seuraava kiekkotoiminto, ja syötön vastaanottaja on laukaisija. Sama ehto kuin M-3:n kohta 1: kuviossa syöttö tahdissa n ja `shoot` tahdissa n + 1. Mikä tahansa välissä oleva toiminto katkaisee ehdon (`skate`, `driveNet`, `dump`, toinen syöttö, järjestelmätilan askel, setup, irtokiekkokamppailu). Laukaisevan tahdin kiekottomien siirrot eivät ole toimintoja eivätkä katkaise ehtoa.
  2. **Vastaanottaja oli vapaa:** tilannekuvassa S yksikään vastustajan kenttäpelaaja ei ollut enintään `pressure.underPressureNodes` solmun päässä (Chebyshev) vastaanottajan solmusta. Arvolla 0 tämä on D-070:n sanamuoto "puolustaja ei ollut vastaanottajan solmussa". Säteen käyttö on designerin suositus (Q-053, kohta 1).

  **Tilannekuva S** = puolustajien solmut laukaisevan tahdin alussa, ennen D-041:n vaihetta 1 (hyökkääjien siirrot) ja vaihetta 2 (puolustajien järjestelmäliike). Syötön ratkaisun ja seuraavan tahdin alun välillä kukaan ei liiku, joten S on sama kuin ne puolustajien sijainnit, joilla syöttö ratkaistiin. Toteutus voi siksi tallentaa tiedon "vastaanottaja vapaa" onnistuneen syötön yhteydessä (samaan paikkaan kuin M-3:n syöttötieto), ja testi varmistaa, että tulos on sama kuin tahdin alun tilannekuvasta. Vastaanottajan solmu on myös laukaisijan solmu, koska kiekollinen ei liiku kiekottomien siirroissa.

  Jos jompikumpi ehto ei täyty, paine ratkaistaan tavallisella säännöllä (painostaja, D-072) laukaushetken sijainneista. Poikkeus vain poistaa paineen, se ei koskaan lisää sitä: laukaus on paineen alla, kun painostaja on säteellä **eikä** poikkeus päde.

  **Poikkeus ja painostaja (D-072).** D-070:n poikkeus jää voimaan sellaisenaan. Vapaan vastaanottajan ehto (kohta 2) tarkistaa edelleen **kaikki** vastustajan kenttäpelaajat tilannekuvassa S, ei vain painostajaa. Perustelu: tilannekuvassa S kiekko oli syöttäjällä, joten painostaja määräytyi syöttäjän mukaan, eikä "vastaanottajan painostajaa" ole vielä olemassa; lisäksi D-070:n sanamuoto on "puolustaja ei ollut vastaanottajan solmussa". Tämä on designerin tulkinta D-072:n lauseesta "poikkeus jää voimaan" (Q-054, kohta 5).

  Soveltamisala on designerin suositus (Q-053, kohta 2):

  | Tilanne | Poikkeus |
  | --- | --- |
  | Kuvion `pass` tahdissa n → `shoot` tahdissa n + 1 | kyllä |
  | Kuvion aloitussyöttö (D-036: kiekollinen → kuvion `puckCarrier`) → tahdin 0 `shoot` | kyllä. Setup on ennen syöttöä, ei syötön ja laukauksen välissä |
  | Aloituskuvion `pass` → `shoot` | kyllä. Aloitusvoitto ei ole syöttö: jos aloituskuvion ensimmäinen tahti on `shoot`, poikkeusta ei ole |
  | Suorahyökkäys (O-7): syöttö → laukaus seuraavana toimintona | kyllä. S = sijainnit ennen tukipelaajien siirtoja ja puolustajien liikettä (rushin toiminto etenee kuten tahti) |
  | Avaussyöttö omalta alueelta (`breakout`-tarkistus) → laukaus | sääntö on sama, tarkistuksen tyypillä ei ole väliä (käytännössä harvinainen) |
  | Toinen yritys (O-8), rebound | ei. Laukausta edeltää irtokiekkokamppailu, ei syöttö |
  | Järjestelmätila | järjestelmätilassa ei syötetä eikä laukota; järjestelmätilan askel katkaisee ehdon |
  | Epäonnistunut syöttö | ei (kiekko ei tullut laukaisijalle) |

  **Suoralaukaus vai vastaanotto ja laukaus.** Simulaatiossa ei ole erillistä suoralaukausta (one-timer): syöttö ja laukaus ovat aina eri tahdeissa tai toiminnoissa. Poikkeus koskee siksi kaikkia heti syötöstä lähteviä laukauksia. Niiden erottelu olisi uusi mekaniikka.

  **Vaikutus.** Poikkeus muuttaa vain laukaustapahtuman kentän `underPressure` ja sitä käyttävät mittarit (`shotsUnderPressureShare`). Laukaustarkistuksessa ei ole `underPressure`-ehtomuokkaajaa (`shot.modifiers.pressure` on joukkueen painetila), joten maalin, blokin ja ohilaukauksen todennäköisyydet eivät muutu. Laukaisijan solmuun tahdin aikana liikkunut puolustaja voi yhä blokata (M-5, laukaushetken sijainnit). Syötön paine (syöttäjä, `pass.modifiers.underPressure`) ei muutu. Pelkkä raporttivaikutus on designerin suositus (Q-053, kohta 3). Mittaus: 200 ottelua; jos laukauksista paineen alla on yhä yli 80 %, M-7:n vaihtoehto 2 viedään Jerrylle (D-070). Mittaus antoi 97,9 %, ja vaihtoehto 2 hyväksyttiin (D-072, Painostaja yllä).
- **Laukausnopeus** (D-052): laukaustapahtuman laukausnopeus on tyhjä (null) virstanpylvääseen 3 asti. Myöhemmin se johdetaan laukaisijan Laukaisuvoimasta pelkäksi näyttöarvoksi ilman pelivaikutusta. `tuning.json`:ssa ei ole sille arvoja.

### M-8 Alueelle tulon voimasuhde "N vs M"

Hallittu alueelletuonti on joko `skate`, jonka kohde on hyökkäysalueella ja lähtö ei (`zoneEntryCarry`, tapa "kuljetus"), tai syöttö, jonka syöttäjä on hyökkäysalueen ulkopuolella ja vastaanottaja hyökkäysalueella (tapa "syöttö"). `dump` ei ole hallittu alueelletuonti, vaan oma tapahtumansa.

Lasketaan toiminnon hetkellä ennen tarkistusta:

- **N** = hyökkäävän joukkueen kenttäpelaajat, joiden x > kiekon x (kiekollisen tai syöttäjän solmu), eli kiekon edellä. Kiekollinen tai syöttäjä lasketaan aina mukaan. Kiekon tasalla olevia ei lasketa kummallekaan puolelle, koska 5,5 m:n rivillä he eivät ole selvästi kummallakaan puolella (D-084, E-006; aiemmin D-080, Q-065).
- **M** = puolustavan joukkueen kenttäpelaajat, joiden x > kiekon x, eli vain kiekon ja oman maalinsa välissä olevat. Kiekon tasalla olevaa puolustajaa ei lasketa (D-080, D-084).
- Kirjataan muodossa "N vs M", esim. "3 vs 2". Kiekon kuljettaja = kiekollinen (kuljetus) tai vastaanottaja (syöttö). Lopputulos: hallinta säilytetty (tarkistus onnistui) tai menetetty.

### M-9 Puolustusjärjestelmän muokkaajat: voimasuhde ja karvaajat (D-078 K4)

`stats-and-checks.md`:n tarkistustaulukko antaa kuljetukselle alueelle muokkaajan "Puolustusjärjestelmä" ja avaukselle omasta päästä muokkaajan "Puolustusjärjestelmä (karvaus vs. trap)". Ne toteutetaan sijainneista lasketuilla luvuilla, ei järjestelmän id:llä. Järjestelmä vaikuttaa niihin sijoittamalla pelaajat: trap pitää useamman pelaajan kiekon takana (M kasvaa), karvaus vie hyökkääjät syvälle (f kasvaa). Sama luku toimii myös rushissa, siirtymissä ja järjestäytymättömässä tilassa, eikä uusi järjestelmä tarvitse omaa arvoaan.

Molemmat ovat tavallisia logit-muokkaajia hyökkääjän hyväksi (M kaavassa), ja ne lasketaan muiden muokkaajien (järjestäytyneisyys, energia, väärä puoli, tuttuus) päälle. Sijainnit luetaan tarkistuksen hetkellä, eli tahdin siirtojen ja puolustajien järjestelmäliikkeen jälkeen (D-041).

**Kuljetus alueelle (`zoneEntryCarry`):**

- M += `checks.zoneEntryCarry.modifiers.strengthDifferencePerPlayer` × (N − M_def).
- N ja M_def ovat M-8:n luvut samalta hetkeltä, eli samat, jotka kirjataan Hallittu alueelletuonti -tapahtumaan. Näin raportin "N vs M" ja vaikutus eivät voi olla ristiriidassa.
- Ei rajausta: N − M_def on välillä −4…+5, ja tarkistuksen 2–98 %:n raja (D-014) riittää.
- Koskee kaikkia `zoneEntryCarry`-tarkistuksia: kuvion `skate` ja rushin `skate` (O-7). Syöttämällä tehty alueelle tulo käyttää `pass`-tarkistusta, eikä muokkaaja koske sitä (Q-063).
- Paikkamerkki 0,25: 3 vs 2 → +0,25, 2 vs 2 → 0, 2 vs 3 → −0,25, 2 vs 5 → −0,75 logitia. p0 0,6:lla ja H = D: 66 %, 60 %, 54 %, 41 %.

**Avaus omasta päästä (`breakout`):**

- M += `checks.breakout.modifiers.perForechecker` × f.
- f = puolustavan joukkueen hyökkääjien (F1–F3) määrä hyökkäävän joukkueen omalla alueella (`defensive`), 0–3. Sama f kuin O-7:n K1-säännössä. M-1:n varasääntö (F1 rooliin, kun alueella ei ole ketään) koskee vain statsien keskiarvoa, ei f:ää.
- Koskee kaikkia `breakout`-tarkistuksia: kuljetus omalta alueelta (`skate`), syöttö omalta alueelta pois (D-075), rushin kuljetus ja kokoamisen tarkistus (O-7, K1).
- Paikkamerkki −0,25: f = 1 → −0,25, f = 2 → −0,5, f = 3 → −0,75 logitia. p0 0,7:llä ja H = D: 64 %, 59 %, 52 % (f = 0: 70 %). E-005:n luvuilla karvausta vastaan f on keskimäärin 2,6 ja trapia vastaan 1,6, joten ero on n. 0,25 logitia, eli n. 5 %-yksikköä.

**Kalibrointi (D-078 järjestys, kohta 3).** Kumpikin muokkaaja siirtää koko onnistumisprosenttia, koska tyypillinen N − M_def on negatiivinen ja f positiivinen. Muokkaajat otetaan käyttöön paikkamerkkiarvoilla ja mitataan taktiikkamatriisilla (2000 ottelua per solu). Sen jälkeen designer kalibroi arvot ja tarvittaessa `p0`:n omana muutoksenaan. Raporttiin tarvitaan alueelle tulon onnistuminen voimasuhteittain (kuljetus ja syöttö erikseen, esim. 3v2, 2v2, 2v3, 2v5) ja avauksen onnistuminen f:n mukaan (0–3) sekä vastustajan järjestelmän mukaan. Alueelle tulot johdetaan tapahtumalokista (N vs M on jo Hallittu alueelletuonti -tapahtumassa). Jos avauksen f:ää ei saa lokin sijainneista, programmer lisää sen raporttimerkintään (ei uutta tapahtumatyyppiä).

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
| `pressure.presserReactionNodes` | 1 | Painostajan askeleet onnistuneen syötön jälkeisessä tahdissa (D-074). Jerryn asettama, ei paikkamerkki. Odottaa lataajaa (O-16) |
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
- **Ottelun loppu:** viimeisen erän jälkeen. Tasapeli sallitaan, jatkoaikaa ei ole (D-062). Aikalisä: ks. Valmentaja-AI:t ja aikalisä, AI-4 (virstanpylväs 4). Maalivahdin vetäminen tulee erikoistilanteiden kanssa (D-077).

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

**`RotationCoach`:** vaihtaa jokaisen vaihtokelpoisen yksikön aina, kolmikot järjestyksessä 1 → 2 → 3 → 4 → 1 ja parit 1 → 2 → 3 → 1. Pelikirjan järjestys: tässä ottelussa vähiten käytetty kuvio ensin (O-11:n laskuri), tasatilanteessa latausjärjestys. Siirtymäohje `rush` (D-062). Järjestelmätilan ohjeet virstanpylvään 2 oletuksilla: `netFrontAfterShot` false, `looseChasers` 1, `pinch` false. Puolustusjärjestelmä tulee Harnessin parametrista. Valmentaja ei saa satunnaisgeneraattoria ennen virstanpylvästä 4. Virstanpylvään 4 AI:t, aikalisä ja päätösolio: ks. Valmentaja-AI:t ja aikalisä (AI-1–AI-6).

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
- **Puolustajien liike setupissa ja regroupissa:** puolustajat liikkuvat kohti järjestelmänsä kohteita enintään n = floor(t × `plays.maxNodesPerBeat` / `time.secondsPerAction.skate`) askelta, t = `setupSeconds` (6 s → 4 askelta) tai regroupissa `regroupSeconds` + `setupSeconds` (14 s → 9 askelta). Kohteet lasketaan kiekon solmusta ennen liikettä. Askeleet ja päätteet kuten Puolustusjärjestelmät, Liikkuminen (D-053, D-058). Tahdeissa liike on ennallaan (`maxNodesPerBeat` per tapahtuma, D-041), paitsi painostajan reaktioviive onnistuneen syötön jälkeen (D-074, Puolustusjärjestelmät, Painostajan reaktioviive). Näin M-8:n voimasuhde "N vs M" syntyy sijainneista.
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

**`regroup` (kokoaminen).** Jos kiekko saadaan omalla alueella tai keskialueella (saajan näkökulmasta), kuluu `time.regroupSeconds` (8): joukkue pitää kiekon, eikä kukaan hyökkääjä liiku. Sen jälkeen kuvio valitaan D-036:n mukaan setupin kanssa, ja puolustajat liikkuvat regroupin ja setupin yhteisen askelmäärän (O-6). Hyökkäysalueella kiekon saanut joukkue menee suoraan D-036:een (pelkkä setup). Keskialueen kokoaminen on tarkistukseton. Omalla alueella karvaus voi haastaa kokoamisen (D-078 K1, alla).

**Karvaus haastaa kokoamisen omassa päässä (D-078 K1).** Kun kiekollisen solmu on regroupin alkaessa joukkueen omalla alueella (`defensive`, sen omasta näkökulmasta), regroup etenee näin. Merkinnät: r = `plays.maxNodesPerBeat` / `time.secondsPerAction.skate`, n_tot = floor((`regroupSeconds` + `setupSeconds`) × r) (O-6:n nykyinen yhteinen askelmäärä), n_reg = floor(`regroupSeconds` × r), n_set = n_tot − n_reg. Nykydatalla n_reg = 5, n_set = 4, n_tot = 9.

1. **Puolustajien liike.** Kun regroup on alkanut (kokonaisuus mahtuu kelloon kuten nykyisessä `regroup`-askeleessa; muuten toimitaan kuten nyt), puolustava joukkue liikkuu n_reg askelta kohti järjestelmänsä kohteita, jotka lasketaan kiekon solmusta ennen liikettä (O-6, D-053, D-058). Hyökkääjät eivät liiku.
2. **Aika.** `regroupSeconds` kuluu yhtenä kokonaisuutena (O-1, O-14). Kuluminen tapahtuu kummassakin lopputuloksessa, ja sekuntivaikutukset lasketaan kokonaisuutta edeltävästä tilasta kuten ennen.
3. **Karvaajat.** f = puolustavan joukkueen hyökkääjien (järjestelmäroolit F1–F3) määrä kokoavan joukkueen omalla alueella liikkeen jälkeen, 0–3. f on todellinen määrä: M-1:n varasääntö (F1 rooliin, kun alueella ei ole ketään) ei kasvata f:ää.
4. **Tarkistus.** Jos f > `transitions.regroupForecheckersAllowed`, kiekollinen ratkaisee yhden `breakout`-tarkistuksen ajan kulumisen jälkeen. Tarkistus käyttää samoja rooleja ja muokkaajia kuin muut `breakout`-tarkistukset: `carrier` = kiekollinen, `forecheckers` = M-1, `modifiers.organization` (O-6, puolustavan joukkueen O kulumisen jälkeen), energia (O-5), väärän puolen miinus ja K4:n jälkeen `modifiers.perForechecker` (M-9). Tuttuusrangaistus on 0, koska kuviota ei ole käynnissä. Osallistujat maksavat `energy.costPerAction`-arvon (O-5). Tarkistuksella ei ole omaa `secondsPerAction`-aikaa, koska se sisältyy `regroupSeconds`-aikaan. Jos f ≤ raja, tarkistusta ei tehdä.
5. **Epäonnistuminen = riisto.** Lähin karvaaja saa kiekon kiekollisen solmussa samalla polulla kuin epäonnistuneessa `skate`-kuljetuksessa: tapahtuma Kiekonmenetys / riisto (menettäjä = kiekollinen, riistäjä = karvaaja), järjestäytyneisyyden pudotus `dropOnTurnover.defensive` (O-6, kokoavan joukkueen näkökulmasta) ja riistäjän joukkueen siirtymä (O-7, hyökkäysalueella saatu kiekko). Lähin karvaaja = pienin Chebyshev-etäisyys kiekollisen solmuun, sitten Manhattan, sitten pelipaikkajärjestys `C`, `LW`, `RW`. Ehdokkaita ovat vain kohdan 3 f karvaajaa.
6. **Onnistuminen.** Kokoaminen jatkuu nykyiseen tapaan: kuvio valitaan D-036:n mukaan, ja puolustajat liikkuvat setupissa jäljellä olevat n_set askelta. Näin yhteinen askelmäärä on sama n_tot kuin ilman tarkistusta. Onnistunut tarkistus ei kirjaa uutta tapahtumaa (uusia tapahtumatyyppejä ei lisätä).
7. **Kun tarkistusta ei tehdä** (f ≤ raja), jatketaan kuten kohdassa 6. Kohdan 1 liike tehdään aina omalla alueella, joten setupissa liikutaan n_set askelta myös silloin.

Raja `transitions.regroupForecheckersAllowed` = 1: 1-2-2-trapin F1 yksin ei estä kokoamista, mutta kaksi tai kolme syvällä karvaavaa (2-1-2) haastaa sen. Kiekkoperuste: 2-1-2-karvaus on rakennettu estämään rauhallinen kokoaminen omassa päässä, ja trapia vastaan kiekkoa kierrätetään ja kootaan (E-005). Testit: f = raja → ei tarkistusta, f = raja + 1 → tarkistus; epäonnistuminen tuottaa riiston ja oikean riistäjän; askelmäärä n_reg + n_set = n_tot; keskialueen kokoaminen ei koskaan tarkista; determinismi (sama siemen → sama loki).

#### Paluukarvaus suorahyökkäyksessä (D-084 kohta 2, hyväksytty D-085)

**Valittu tapa: (b) väärällä puolella kiekkoa oleva puolustaja luistelee kohti omaa maaliaan.** Perustelut:

- (b) on paikallinen sääntö. Se lukee vain nykyiset sijainnit, koskee vain väärällä puolella olevia ja vain rushia. Muut puolustajat, kuviot, setup ja regroup pysyvät ennallaan.
- (a) vaatisi kiekollisen liikkeen ennustamisen ennen toimintoa. Ennustettava olisi `skate`n kohde, syötön vastaanottaja tai laukaus, ja `skate` voi epäonnistua. Ennuste muuttaisi kaikkien viiden kohteet ja säännön valinnan (alue), eli myös kuvioiden tahdit.
- (a) veisi F1:n (`puckOffset` [0, 0]) kiekollisen solmusta tämän tulevaan solmuun. Paine (M-7, säde 0) ja `breakout`-tarkistuksen f putoaisivat silloin lähes kaikista tahdeista.
- Arvio samasta otoksesta kuin E-006: (a) lisäisi F1:n M:ään lähes jokaisessa kuljetuksessa (1v1 → 1v2), ja N < M nousisi yli 75 %:n.

**Väärä puoli.** Puolustaja on väärällä puolella, kun x_p < x_k hyökkäävän joukkueen näkökulmasta. x_p on puolustajan x ja x_k kiekon x eli kiekollisen solmu. Puolustajan omassa näkökulmassa ehto on x_p > x_k. Säännössä:

- Kiekon tasalla oleva (x_p = x_k) ei ole väärällä puolella. Ehto on M-8:n (D-084) peilikuva, joten tasalla oleva on neutraali kummassakin.
- Kaikki viisi kenttäpelaajaa ovat mukana. Pelipaikka ja rooli eivät vaikuta. E-006:n otoksessa väärällä puolella olleet olivat kaikki hyökkääjiä.
- Ehto arvioidaan jokaisen liikkeen alussa erikseen ennen vaihetta 2, eli hyökkääjien siirtojen (vaihe 1) jälkeen.

**Milloin.** Sääntö koskee vain vastustajan suorahyökkäystä (O-7 `rush`), sen jokaisen toiminnon D-041:n vaihetta 2 (puolustajien järjestelmäliike), myös ensimmäistä. Se ei koske:

- kuvion tai aloituskuvion tahteja
- setuppia
- regroupia, ei myöskään K1:n n_reg-liikettä (alla)
- järjestelmätilan askelta
- irtokiekkoa
- O-10:n ohjeita
- toista yritystä (O-8), jossa puolustajat eivät liiku

Kun rush päättyy (O ≥ raja tai toiminnot käytetty), sääntö lakkaa heti, ja setup liikkuu järjestelmän mukaan kuten ennen.

**Liike.**

1. Roolit (F1–F3, D1–D2) ja järjestelmän sääntö jaetaan kuten ennen kaikille viidelle. Roolia tarvitaan D-058:n käsittelyjärjestykseen ja M-7:n painostajaan.
2. Väärällä puolella olevan pelaajan järjestelmäkohde ohitetaan tässä liikkeessä. Ohitus koskee myös `pinch`-D1:n korvattua kohdetta ja trap122:n F1:n siirtymää.
   - Pelaajan uusi kohde on (x_t, y_p) hyökkäävän joukkueen näkökulmasta.
   - x_t = x_k, eli kiekon rivi: paluukarvaaja saa kuljettajan kiinni, mutta ei ohita häntä (D-086; aiemmin min(x_k + 1, `opponentGoal.x` − 1), joka ylikorjasi voimasuhteet 89,4 / 10,6 / 0,0 %:iin). Kiekon tasalla oleva puolustaja ei kuulu M:ään (M-8, E-006).
   - y_p on puolustajan oma kaista, eli kaista ei vaihdu.
3. Askel on suoraan kohti omaa maalia: (+1, 0) hyökkäävän joukkueen näkökulmasta. Askeleita otetaan enintään k = `transitions.backcheckNodesPerBeat`, kunnes x = x_t.
   - Pelaaja ei ylitä kohdettaan, joten tasalla tai edellä olevaksi ehtinyt jää riville x_t.
   - Jos askel osuisi maalisolmuun, kierto tehdään D-053:n mukaan. Näin käy vain hyökkäävän joukkueen oman maalin takaa (x = 0, y = 2) lähtevälle, ja kiertoaskel on yksi k:sta.
4. **D-074:** jos väärällä puolella oleva on rushin viivästetty painostaja (lähin syötön vastaanottajaan), askeleita on min(k, `pressure.presserReactionNodes`).
5. **D-058:** paluukarvaajan ihannepääte on kohdan 3 pääte, ja polku on sen suora polku. Kaikki viisi käsitellään samassa sijoituksessa tavallisella järjestyksellä (`F1`, `D1`, `D2`, `F2`, `F3`). Peruutus omaa polkua pitkin ja renkaat ovat ennallaan.
6. Muut puolustajat (tasalla tai kiekon ja maalin välissä) liikkuvat järjestelmän mukaan `plays.maxNodesPerBeat` askelta kuten ennen. Sama koskee painostajaa, joka ei ole väärällä puolella. Jos järjestelmäkohde vie tasalla olevan väärälle puolelle (esim. forecheck212:n F2 (9, 3) omasta näkökulmasta), sääntö koskee häntä seuraavassa liikkeessä.

**Ei muutu:**

- M-7: rushin paine on edelleen lähin puolustaja toiminnon hetkellä.
- M-8/M-9: luvut lasketaan sijainneista kuten ennen. Paluukarvaus muuttaa vain sijainteja.
- O-6: O on skaalaari eikä muutu.
- K1: regroup ei ole rush.
- `forecheckers` (M-1) ja f lasketaan liikkeen jälkeisistä sijainneista kuten ennen.

Ei satunnaisuutta, ei uutta tapahtumaa eikä uutta tapahtumakenttää.

**Tasapainoarvo.** Lisätään `transitions`-osioon olemassa olevien rinnalle, samassa commitissa lataajan tuen kanssa (D-030). Polku lisätään `_placeholders`-listaan.

| Polku | Arvo | Hylätään, jos |
| --- | --- | --- |
| `transitions.backcheckNodesPerBeat` | 0 (D-087; aiemmin 2) | ei kokonaisluku, < 0 tai > 2 × `plays.maxNodesPerBeat` |

- **D-087:** arvo on 0, koska kohderivillä kiekon tasa (k = 2) voimasuhdejakauma oli 54,2 / 13,0 / 32,8 % (tavoite 50–75 / 20–40 / 0–15). Arvolla 0 batch 1000 antaa 66,5 / 33,5 / 0,0 %, `rulebased` vs `random` 78,3 % ja 6,46 maalia. Paluukarvaukseen palataan Q-071:n yhteydessä.
- Arvo 0 poistaa säännön kokonaan: väärällä puolella oleva liikkuu järjestelmän mukaan kuten ennen, eikä seiso paikallaan. Näin muutoksen voi mitata samalla koodilla ilman sääntöä.
- Arvo 2 on sama nopeus kuin kiekollisella. Paluukarvaaja on silloin yhtä nopea kuin kiekollinen, ja vain suunta muuttuu.
- Arvo, joka on suurempi kuin `plays.maxNodesPerBeat`, tarkoittaa "kiekoton on nopeampi kuin kiekollinen". Se on oma päätöksensä, ja sen päättää Jerry (alla).

`_notes`-tekstin loppuun lisätään: "backcheckNodesPerBeat: during the opponent's rush, a defender behind the puck (x < puck x, attacking view) ignores his system target and skates straight toward his own goal, at most this many nodes, to the row one past the puck (D-084). 0 = off."

**Testit.**

1. Rushissa väärällä puolella oleva liikkuu suoraan (+1, 0) enintään k askelta ja pysähtyy riville x_k + 1. Esimerkiksi rako 1 → kiekon edelle, rako 2 → tasalle, rako 4 → 2 riviä kiekon taakse.
2. Tasalla oleva ja kiekon ja maalin välissä oleva liikkuvat järjestelmän mukaan, sama tulos kuin arvolla 0.
3. Ei vaikutusta kuvion tahdissa, aloituskuviossa, setupissa, regroupissa (myös K1), järjestelmätilassa, irtokiekossa eikä toisessa yrityksessä.
4. Raja x_t ≤ `opponentGoal.x` − 1. Pelaaja ei koskaan päädy maalisolmuun. Maalin takaa lähtevä kiertää D-053:n mukaan.
5. D-074: väärällä puolella oleva viivästetty painostaja ottaa min(k, `presserReactionNodes`) askelta.
6. D-058: kaksi paluukarvaajaa samalla kaistalla eivät päädy samaan solmuun (peruutus tai rengas). Invarianttitesti pysyy voimassa.
7. Peilisymmetria: koti/vieras ja vasen/oikea kaista tuottavat peilikuvat.
8. Arvo 0 tuottaa saman tapahtumalokin kuin muutosta edeltävä koodi samalla siemenellä. Sama siemen tuottaa aina saman lokin.
9. Lataaja: puuttuva avain, ei-kokonaisluku, −1 ja 2 × `maxNodesPerBeat` + 1 hylätään. 0 hyväksytään.

**Arvioitu vaikutus** (designerin arvio, ei mitattu; esimerkki forecheck212, riisto hyökkäävän joukkueen omalla alueella x = 3, alueelletuonti toisella rush-toiminnolla kiekon ollessa x = 5):

| | Väärällä puolella | Tasalla | Edellä (M) | N vs M (E-006) |
| --- | --- | --- | --- | --- |
| Nyt | F2 (x = 3) | F1, F3, D1 | D2 | 1v1 |
| k = 2 | – | F1, F2, F3, D1 | D2 | 1v1 |
| k = 3 | – | – | kaikki viisi | 1v5 |

- **k = 2:** syvä karvaaja (F2) lähtee takaisin jo ensimmäisessä toiminnossa eikä pidä forecheck212:n kohdetta (9, 3). Koska nopeudet ovat samat, hän ehtii vain kiekon tasalle, ja tasalla olevaa ei lasketa (D-084).
  - Väärällä puolella olevia oman alueen riistojen jälkeen arviolta 1,64 → 0,6–0,9 ja kaikissa tuonneissa 0,65 → 0,3–0,4.
  - Voimasuhdejakauma (E-006:n arvio 64,6 / 35,4 / 0,0) muuttuu enintään muutaman %-yksikön. Ylivoimaisten osuus oman alueen riistojen jälkeen on E-006:n määritelmällä jo n. 0 %.
  - Pari 2v2-tilannetta voi muuttua 2v3:ksi, kun rako 1 muuttuu edellä olevaksi.
  - Taktiikka (D-078): karvausta vastaan rush − regroup pienenee arviolta 0–3 %-yks., eli raja +5 pysyy. Trapia vastaan vaikutus on pieni, koska trapin pelaajat ovat jo kiekon takana.
- **k = 3:** ylikorjaa. Tasalle jäävät ehtivät kiekon edelle, joten oman alueen riistojen jälkeen tulee 1v4–1v5.
  - N < M nousee arviolta yli 80 %:n (haarukka 50–75).
  - Karvauksen rush-haavoittuvuus katoaa, ja D-078:n raja (rush − regroup karvausta vastaan ≥ +5) on vaarassa.

**Mittaus** (D-084 kohta 3, E-006:n batchin jälkeen):

1. `batch --matches 1000 --matchups all --seed 1` arvoilla 0 ja 2 (sama koodi).
2. Mitataan:
   - `zoneEntryStrength*`
   - alueelletuontien onnistuminen voimasuhteittain
   - `zoneEntrySuccessCarry`
   - maalit per ottelu
3. Sen jälkeen taktiikkamatriisi 2000 / solu ja D-078:n rajat.

**Harness tarvitsee uuden raporttiosion** (programmer). Se ei ole tavoite eikä uusi tapahtuma. Osio raportoi onnistuneiden hallittujen alueelletuontien N vs M -ryhmäosuudet ja keskimääräiset puolustajat kiekon takana / tasalla / edellä (M-8:n x-vertailu, placement-kentästä). Luvut jaetaan näin:

- hallinnan alkupaikan alueen mukaan hyökkäävän joukkueen näkökulmasta (oma alue, keskialue, hyökkäysalue; hallinta kuten O-12)
- tavan mukaan (rush tai kuvio)

E-006:n luvut (1,64 ja 85–91 %) laskettiin käsin, joten niitä ei nyt saa batchista.

**Avoimet kysymykset Jerrylle:**

1. Riittääkö k = 2 (vain suunta), vai haluatko kiekottoman olevan nopeampi kuin kiekollisen? Jälkimmäinen olisi uusi päätös, ja arvio näyttää ylikorjauksen.
2. Toteutetaanko paluukarvaus lainkaan, jos E-006:n batch jo täyttää haarukat? Hyöty olisi realismi: syvä karvaaja ei enää jää kohteeseensa (9, 3), kun vastustaja lähtee.
3. Kaista: suoraan omalla kaistalla (speksi) vai keskikaistan kautta? Oikeassa kiekossa paluukarvaaja tulee usein keskeltä ja ottaa heikon puolen pelaajan. Kysymys veljelle, jos halutaan tarkentaa.

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
| `pressure.presserReactionNodes` | 1 | odottaa lataajaa | D-074 (Jerryn arvo, ei paikkamerkki) |
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
| `pressure.presserReactionNodes` (D-074) | ei kokonaisluku, < 0 tai > `plays.maxNodesPerBeat` |
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

**Painostajan reaktioviive (D-074).** `pressure.presserReactionNodes` lisätään `pressure`-osion sallittuihin ja pakollisiin avaimiin (`PressureConfig`) ja dataan samassa commitissa (D-030), olemassa olevien avainten rinnalle `underPressureNodes`-avaimen jälkeen: `"presserReactionNodes": 1`. Arvo ei ole paikkamerkki (Jerryn päätös), joten sitä ei lisätä `_placeholders`-listaan. Osion `_notes`-tekstiin lisätään: "presserReactionNodes: after a successful pass the presser (M-7 selector, evaluated before the defenders' move) moves at most this many nodes before the next action instead of plays.maxNodesPerBeat (D-074)."

## Valmentaja-AI:t ja aikalisä (virstanpylväs 4) – D-077

Tämä osio on virstanpylvään 4 toteutusspeksi: `ai`-osio `tuning.json`:iin (Q-057 a, D-030:n prosessi), AI:den tarkat säännöt, aikalisä ja valmentajan päätösolio. Koordinaatit ja näkökulma kuten Määritelmissä. **Ottelusekunti** = (erä − 1) × `time.periodSeconds` + kello. **Viimeinen erä** = erä `time.periods`. **Jäljellä** = `time.periodSeconds` − kello. **Johto** = omat maalit − vastustajan maalit (julkinen loki).

**Taito vai tuuri (D-077 kohdat 4 ja 8).** Arvoja ei säädetä voittoprosenttia kohti ennen ensimmäistä batch-raporttia. Alla olevat luvut ovat kiekkotietoon perustuvia alkuarvauksia (`_placeholders`), eivät viritettyjä. Ensisijainen mittaus on `rulebased` vs `random`, ja `adaptive` raportoidaan rinnalla.

### AI-1 Julkinen tieto ja satunnaisuus (D-077 kohdat 1 ja 3)

- **Valmentaja näkee vain julkisen tiedon:** kummankin joukkueen jäällä olevat yksiköt, kello, erä, maalitilanne, oman joukkueen yksiköiden jää- ja penkkiajat, julkinen tapahtumaloki (tapahtumaskeeman tapahtumat kenttineen, ei raporttimerkintöjä eikä jäljityksiä), käytettävissä olevat aikalisät, oman joukkueen kokoonpano ja statsit sekä ladatut kuviot ja järjestelmät. **Todellinen energia, järjestäytyneisyys ja painetila eivät ole julkisia** (Q-058).
- **Energia-arvio.** Sääntöpohjaiset AI:t arvioivat oman pelaajan energian julkisesta tiedosta (vaihtotapahtumat ja kello). Arvio Ê_p on erän alussa `energy.start`. Jäällä t sekuntia: Ê −= (`energy.drainPerSecondOnIce` + `ai.estimate.extraDrainPerSecondOnIce`) × f_p × t (f_p kuten O-5; oma Kestävyys on oman joukkueen tietoa). Penkillä t sekuntia: Ê ← 1 − (1 − Ê) × exp(−`energy.benchRecoveryRate` × t). Aikalisä kuten AI-4. Arvio rajataan välille 0–1. Yksikön arvio Ê_u on sen pelaajien arvioiden keskiarvo. `extraDrainPerSecondOnIce` kattaa keskimäärin tarkistusmaksut ja paineen kulutuksen, joita valmentaja ei näe.
- **Satunnaisuus.** Valmentaja saa oman `Sim.Random`-virran, joka johdetaan ottelun siemenestä (CLAUDE.md sääntö 1, D-077 kohta 1). `rulebased` ja `adaptive` eivät arvo mitään. Arvonnat: kokonaisluku välillä [0, n) = `NextInt(n)`, totuusarvo = `NextInt(2) == 1`, todennäköisyys p = `Chance(p)`. Satunnainen järjestys = Fisher–Yates: i = n − 1 … 1, j = `NextInt(i + 1)`, vaihda i ja j.
- **Kandidaattien järjestys** ennen arvontaa tai tasatilanteen ratkaisua: yksiköt indeksin mukaan, kuviot latausjärjestyksessä, järjestelmät `id`:n ordinaalijärjestyksessä, siirtymäohjeet `rush`, `regroup`.

### AI-2 Säännöt AI-tyypeittäin

Jokainen AI toteuttaa `ICoach`-rajapinnan (AI-5). Ketjut ja taktiikka ovat erillisiä osia, joita diagnostiikkavariantit yhdistelevät (D-077 kohta 2):

| AI | Ketjut (katkolla ja lennosta) | Taktiikka (kuviot, järjestelmä, siirtymä, järjestelmätilan ohjeet, aikalisä) |
| --- | --- | --- |
| `random` | satunnainen (R-L) | satunnainen (R-T) |
| `randomTactics` | `RotationCoach`-kierto (O-4) | satunnainen (R-T) |
| `randomLines` | satunnainen (R-L) | sääntöpohjainen (S-T) |
| `rulebased` | sääntöpohjainen (S-L) | sääntöpohjainen (S-T) |
| `adaptive` | sääntöpohjainen (S-L) | sopeutuva (A-T) |
| `rotation` (`RotationCoach`) | kierto | O-4, ei aikalisää |

**R-L Satunnaiset ketjut.**

- **Katkolla:** jos kolmikko saa vaihtua, uusi kolmikko = `NextInt(kolmikoiden määrä)` kaikista kolmikoista. Nykyinen on mukana, joten pysyminen on yksi tulos. Sitten sama pareille. Vaihtokelvoton yksikkö pysyy ilman arvontaa.
- **Lennosta:** kummallekin vaihtokelpoiselle yksikkötyypille (kolmikko, sitten pari) `Chance(ai.random.onTheFlyChangeProbability)`. Jos tosi, uusi yksikkö = `NextInt(penkillä olevien määrä)` penkillä olevista indeksijärjestyksessä. Erillinen todennäköisyys tarvitaan, koska lennosta vaihdon tilaisuuksia on vuoron aikana monta.

**R-T Satunnainen taktiikka** (katkolla, arvontajärjestys kiinteä):

1. Aikalisä: vain jos aikalisä on käytettävissä (AI-4), `Chance(ai.random.timeoutProbabilityPerStoppage)`.
2. Ketjut (R-L, jos AI käyttää satunnaisia ketjuja).
3. Kuviot: koko kirjasto satunnaisessa järjestyksessä (Fisher–Yates latausjärjestyksestä).
4. Järjestelmä: `NextInt(järjestelmien määrä)`.
5. Siirtymäohje: `NextInt(2)` → `rush` / `regroup`.
6. `netFrontAfterShot`: totuusarvo.
7. `looseChasers`: 1 + `NextInt(2)`.
8. `pinch`: totuusarvo.

`randomTactics`-valmentajalla kohta 2 on kierto (ei arvontaa). Aikalisän jälkeisellä kierroksella (AI-4) kohta 1 ohitetaan. Lennosta vaihdossa R-T ei arvo mitään, koska suunnitelma pysyy seuraavaan katkoon.

**S-L Sääntöpohjaiset ketjut** (kolmikko ja pari erikseen, kolmikko ensin; sama sääntö katkolla ja lennosta):

1. Jos yksikkö ei saa vaihtua (O-2), se pysyy.
2. Paras penkkiyksikkö b = suurin Ê_u. Tasatilanteessa kiertojärjestyksessä seuraava nykyisestä ((nykyinen + k) mod n, pienin k ≥ 1). Jos nykyinen on −1 (ottelun alku), pienin indeksi, eli ottelun ensimmäiseen aloitukseen tulevat yksiköt 0.
3. Vaihda b:hen, jos Ê_nykyinen < `ai.rulebased.changeEnergyThreshold` tai Ê_b − Ê_nykyinen ≥ `ai.rulebased.freshnessMargin`. Muuten pysy.

**S-T Sääntöpohjainen taktiikka** (katkolla):

1. **Tila.** Viimeisessä erässä, kun jäljellä ≤ `ai.rulebased.lateGameSeconds`: johto ≥ `protectLeadGoals` → `protect`; johto ≤ −`chaseDeficitGoals` → `chase`. Muuten `normal`. Järjestelmä, siirtymäohje ja järjestelmätilan ohjeet luetaan suoraan `ai.rulebased.modes.<tila>`-objektista, paitsi siirtymäohje tilassa `normal` (kohta 2, Siirtymäohje vastustajan järjestelmän mukaan).
2. **Vastustajan järjestelmä s.** Viimeisin julkinen tapahtuma, jossa vastustaja puolusti ja jolla on järjestelmän id (D-045). Jos sellaista ei ole, s puuttuu ja kaikki bonukset ovat 0.
   - **Siirtymäohje vastustajan järjestelmän mukaan (D-078 K2).** Tilassa `normal`: jos s tiedetään ja `ai.rulebased.transitionBySystem` sisältää avaimen s, siirtymäohje = `transitionBySystem[s]`. Muuten (s puuttuu tai avainta ei ole) `modes.normal.transition`. Tiloissa `protect` ja `chase` käytetään aina `modes`-arvoa, koska tilanne ohittaa vastapelin. Kiekkoperuste: 2-1-2-karvausta vastaan suorahyökkäys karistaa syvällä olevat karvaajat ennen kuin kokoaminen jää kiinni (O-7, K1), ja 1-2-2-trapia vastaan kiekko kootaan ja kuvio pelataan järjestäytyneesti (E-005).
3. **Kuviot.** Pisteet kuviolle k: B(k) = `playBonusBySystem[s][k]` (puuttuva = 0) − L(u_k + 1), jossa L on O-11:n tuttuusrangaistus ja u_k oma käyttölaskuri (julkinen: tapahtumien kuvio-id:t). Järjestys: suurempi B, sitten pienempi u_k, sitten latausjärjestys. Koko kirjasto palautetaan. Samoilla bonuksilla tämä on O-4:n kierto, joten tuttuus ohjaa vaihtelua. Bonus on logit-yksikköä, jotta se on verrannollinen tuttuusrangaistukseen.
4. **Aikalisä:** AI-4, Sääntöpohjainen laukaisu.

**A-T Sopeutuva taktiikka.** Kuten S-T kahdella erolla. Ikkuna = julkiset tapahtumat, joiden ottelusekunti on > nykyhetki − `ai.adaptive.windowSeconds`.

1. **Vastustajan järjestelmäjakauma** S-T:n kohdan 2 yhden havainnon sijaan. n_s = ikkunan tapahtumat, joissa vastustaja puolusti järjestelmällä s; N = Σ n_s; S = ladattujen järjestelmien määrä. P(s) = (n_s + `priorWeight` / S) / (N + `priorWeight`). Kuvion bonus = Σ_s P(s) × `playBonusBySystem[s][k]`; muuten S-T:n kohta 3 ennallaan.
2. **Oma järjestelmä tilassa `normal`.** Tilat `protect` ja `chase` käyttävät `modes`-arvoja kuten S-T. Vastustajan hallittujen alueelle tulojen osuus ikkunassa: c = vastustajan Hallittu alueelletuonti -tapahtumat (tapa kuljetus tai syöttö), d = vastustajan Kiekko päätyyn -tapahtumat; q = (c + `priorWeight` × 0,5) / (c + d + `priorWeight`). Jos q ≥ `controlledEntryShareForPassive` → `passiveSystem`; jos q ≤ `controlledEntryShareForAggressive` → `aggressiveSystem`; muuten edellinen oma valinta (hystereesi; ensimmäisellä kerralla `modes.normal.system`). Peruste: trap pysäyttää keskialueen kuljetukset ja pakottaa kiekon päätyyn, ja kiekkoa päätyyn pelaavaa vastustajaa vastaan karvaus kannattaa. **D-080 kohta 3 korvaa tämän kohdan.** Korvaava speksi on alla (A-T2, Oma järjestelmä vastustajan siirtymätyylin mukaan), hyväksytty (Q-067, D-082).
3. **Siirtymäohje tilassa `normal`** (D-078 K2): S-T:n kohdan 2 siirtymäsääntö, mutta s = todennäköisin järjestelmä, eli suurin P(s) kohdan 1 jakaumasta. Tasatilanteessa järjestelmä `id`:n ordinaalijärjestyksessä ensimmäinen (AI-1). Jos ikkunassa ei ole yhtään havaintoa (N = 0), s puuttuu ja käytetään `modes.normal.transition`ia, koska pelkkä priori antaa kaikille saman P(s):n.
4. Järjestelmätilan ohjeet, ketjut ja aikalisä kuten `rulebased`.

#### A-T2 Oma järjestelmä vastustajan siirtymätyylin mukaan (D-080 kohta 3, hyväksytty D-082)

Korvaa A-T:n kohdan 2. Kohdat 1, 3 ja 4 pysyvät ennallaan. Sääntö: vastustaja hyökkää paljon suorahyökkäyksillä → `passiveSystem` (trap122); vastustaja kokoaa paljon → `aggressiveSystem` (forecheck212). Kiekkoperuste: 1-2-2-trap sulkee keskialueen ja kaistat, joita pitkin suorahyökkäys tulee, ja 2-1-2-karvaus haastaa rauhallisen kokoamisen omassa päässä (O-7, D-078 K1). Videovalmentaja tekee saman havainnon: hyökkääkö vastustaja siirtymästä vai asettuuko se kuvioon.

**1. Signaali: julkinen merkki on jo olemassa, skeemamuutosta ei tarvita.** Tapahtumaskeemassa ei ole kenttää "siirtymäohje", mutta jokaisessa tapahtumassa on kiekollisen joukkueen kuvion id (D-045), ja se on julkinen (AI-1). O-7:n mukaan suorahyökkäys pelataan sisäänrakennetuilla säännöillä ilman kuviota, joten sen tapahtumissa kuvio-id on tyhjä. Kokoamisen jälkeen kuvio valitaan D-036:n mukaan, joten ensimmäisessä kiekollisen toiminnossa kuvio-id on asetettu. Siirtymä luokitellaan siis vastustajan **hallinnan ensimmäisestä toimintotapahtumasta**.

**2. Luokittelusääntö (puhdas funktio julkisesta lokista, deterministinen).**

- **Toimija** kuten nykyinen `PublicLog.Actor`: Syöttö → syöttäjä, Hallittu alueelletuonti → kiekon kuljettaja, Laukaus → laukoja, Kiekko päätyyn → ampuja, Kiekonmenetys → menettäjä. Toimijan joukkue = oma, jos id on oman kokoonpanon kenttäpelaaja, muuten vastustaja. Kamppailu, Vaihto ja Aikalisä eivät ole toimintotapahtumia, ja ne ohitetaan.
- Loki käydään läpi alusta järjestyksessä. Tila: hallitseva joukkue h (oma, vastustaja tai ei kumpikaan), alkuperä (aloitus tai siirtymä) ja kummallekin joukkueelle lippu "ensimmäinen toiminto odottaa".
  1. **Erän alku ja Pelikatko:** h = ei kumpikaan, alkuperä = aloitus, liput pois.
  2. **Aloitus:** h = voittajan joukkue, alkuperä = aloitus, liput pois. Aloituksesta alkanut hallinta ei ole siirtymä (D-060), eikä sitä lasketa.
  3. **Toimintotapahtuma, toimijan joukkue X ≠ h:** kiekko vaihtoi joukkuetta ilman julkista riistoa (syötönkatko, irtokiekon voitto). h = X, alkuperä = siirtymä, X:n lippu päälle. Käsitellään sitten kuten kohta 4.
  4. **Toimintotapahtuma, X = h, X:n lippu päällä ja alkuperä = siirtymä:** lippu pois. Jos X on vastustaja, tapahtuma luokitellaan: Kiekonmenetys → **ei luokitella** (hallinta päättyi ennen kuin tapa näkyi; esim. epäonnistunut kokoaminen K1 tai rushin ensimmäinen kuljetus); kuvio-id tyhjä → **suorahyökkäys**; kuvio-id asetettu → **kokoaminen**.
  5. **Kiekonmenetys** käsitellään ensin menettäjän toimintotapahtumana (kohdat 3–4) ja sen jälkeen: h = riistäjän joukkue, alkuperä = siirtymä, riistäjän joukkueen lippu päälle.
- **Luokiteltu havainto** tallentaa luokan ja tapahtuman järjestelmä-id:n (D-045: puolustavan joukkueen eli `adaptive`n oma järjestelmä sillä hetkellä). Jos järjestelmä-id on tyhjä, havaintoa ei käytetä.
- **Tunnetut virhelähteet:** (a) rush, joka päättyy heti ilman toimintoa, koska puolustus on järjestäytynyt (O-7, O ≥ 0,8), näkyy kokoamisena, koska joukkue siirtyy kuvioon; (b) joukkue, jonka kuviolista on tyhjä tai jolle mikään kuvio ei sovi, pelaa järjestelmätilassa ilman kuvio-id:tä ja näkyy suorahyökkäyksenä. Kumpikin on oikeassa kiekossakin sama asia kuin havaittu tyyli (hyökkääkö joukkue kuviosta vai ei), joten virhe hyväksytään ja mitataan (kohta 7). O-8:n toinen yritys ei aloita uutta hallintaa (hyökkääjäpuoli jatkaa), joten se ei vaikuta.
- Yhden ottelun koeajossa (rulebased vs adaptive, siemenet 42 ja 7) luokiteltuja havaintoja oli n. 50–80 per joukkue per ottelu, eli n. yksi 45–70 sekunnin välein, ja katkoja 29–35 per ottelu (n. 2–3 havaintoa katkojen välissä). `rulebased`in havainnoista trapia vastaan 97 % oli kokoamisia ja `adaptive`n havainnoista karvausta vastaan 92 % suorahyökkäyksiä, eli luokittelu seuraa siirtymäohjetta.

**3. Ikkuna ja osuus.** Valintahetkellä oma nykyinen järjestelmävalinta on c (tilan `normal` muisti; ottelun alussa `modes.normal.system`). Ikkuna = viimeiset `ai.adaptive.styleWindowEvents` luokiteltua havaintoa, **joiden järjestelmä-id = c**. n = ikkunan havaintojen määrä (≤ ikkuna), r = suorahyökkäysten määrä / n. Ikkuna on tapahtumamäärä eikä aikaa, koska Jerryn ehto on "selvä vähintään kynnyksen verran tapahtumia". Ikkuna ei rajaudu erään eikä `windowSeconds`-aikaan; `windowSeconds` ja `priorWeight` koskevat edelleen vain kohtia 1 ja 3.

Rajaus järjestelmä-id:llä on heilunnan eston ydin: se mittaa vastustajan tyyliä **nykyistä omaa järjestelmää vastaan**. Vaihdon jälkeen ikkuna on tyhjä, ja uusi vaihto vaatii vähintään `styleMinEvents` uutta havaintoa uutta järjestelmää vastaan. Vanhat havainnot toista järjestelmää vastaan eivät voi laukaista vaihtoa takaisin.

**4. Päätös tilassa `normal`, vain katkolla** (`AtStoppage`, myös erän alku ja aikalisän jälkeinen kierros; `OnTheFly` ei muuta suunnitelmaa, AI-5):

1. Jos n < `ai.adaptive.styleMinEvents` → c pysyy (ei dataa tai liian vähän dataa).
2. Muuten, jos r ≥ `ai.adaptive.rushShareForPassive` → c = `passiveSystem`.
3. Muuten, jos r ≤ `ai.adaptive.rushShareForAggressive` → c = `aggressiveSystem`.
4. Muuten c pysyy (hystereesi: kaista rajojen välissä).

Oma järjestelmä = c. Vertailut ovat liukulukuvertailuja r:n ja rajan välillä; paikkamerkkirajat 0,65 ja 0,35 eivät osu yhteenkään osuuteen k / n, kun n < 20, joten tasatilanteita ei synny. Aikalisän jälkeinen uusi kutsu samalla katkolla antaa saman tuloksen: jos ensimmäinen kutsu vaihtoi c:n, uuden c:n ikkuna on tyhjä (n = 0 < `styleMinEvents`), joten c pysyy.

**5. Tilat ja muut kohdat.**

- **`protect` ja `chase`:** oma järjestelmä on `modes.<tila>.system` kuten S-T:ssä. c:tä ei muuteta näissä tiloissa, ja se on muistissa, kun tila palaa `normal`iksi. Havaintoja kerätään koko ajan, mutta ikkunaan lasketaan vain ne, joiden järjestelmä-id = c.
- **Kohta 1 (vastustajan järjestelmäjakauma ja kuviobonus)** ja **kohta 3 (oma siirtymäohje todennäköisimmän vastustajan järjestelmän mukaan)** eivät muutu, eivätkä ne käytä tämän kohdan havaintoja.
- Kuvio- ja järjestelmä-id:t luetaan vain julkisesta lokista (AI-1, D-077 kohta 3). Ei satunnaisuutta: sama julkinen loki ja sama c → sama päätös.

**6. Tasapainoarvot (`ai.adaptive`, paikkamerkkejä).** Korvaavat avaimet `controlledEntryShareForPassive` ja `controlledEntryShareForAggressive`, jotka poistetaan samassa commitissa (tuntematon avain hylätään, AI-3). Kaikki uudet polut `_placeholders`-listaan.

| Polku | Paikkamerkki | Yksikkö | Hylätään, jos | Peruste |
| --- | --- | --- | --- | --- |
| `ai.adaptive.styleWindowEvents` | 6 | luokiteltuja havaintoja | ei kokonaisluku tai < 1 | n. 2–3 katkoväliä |
| `ai.adaptive.styleMinEvents` | 4 | luokiteltuja havaintoja | ei kokonaisluku, < 1 tai > `styleWindowEvents` | "selvä vähintään 4 tapahtuman ajan"; n. 1,5–2 katkoväliä |
| `ai.adaptive.rushShareForPassive` | 0,65 | suorahyökkäysten osuus | ei välillä [0, 1] | n = 4: vähintään 3/4; n = 6: vähintään 4/6 |
| `ai.adaptive.rushShareForAggressive` | 0,35 | suorahyökkäysten osuus | ei välillä [0, 1] tai ≥ `rushShareForPassive` | n = 4: enintään 1/4; n = 6: enintään 2/6 |

```json
"adaptive": {
  "windowSeconds": 600,
  "priorWeight": 10,
  "passiveSystem": "trap122",
  "aggressiveSystem": "forecheck212",
  "styleWindowEvents": 6,
  "styleMinEvents": 4,
  "rushShareForPassive": 0.65,
  "rushShareForAggressive": 0.35
}
```

`ai._notes`-tekstiin lisätään: "adaptive system: opponent transition style from the public log (first action of a non-faceoff possession: empty play id = rush, play id = regroup), counted only against adaptive's current system; switch at a stoppage when at least styleMinEvents such observations show a clear share (data-schema.md A-T2, D-080)."

**7. Vastapeli `rulebased`ia vastaan ja odotettu vaikutus.**

- **`rulebased` (K2) reagoi yhden katkon viiveellä:** se lukee `adaptive`n viimeisimmän järjestelmän (S-T kohta 2) ja pelaa sitä vastaan seuraavasta katkosta alkaen (karvaus → rush, trap → regroup). Kierre säilyy: `adaptive` karvaus → `rulebased` rush → `adaptive` trap → `rulebased` regroup → `adaptive` karvaus …
- **Miksi kierre jää:** `rulebased` vastaa aina `adaptive`n nykyiseen järjestelmään, ja `adaptive` vastaa `rulebased`in viimeisimpään siirtymätyyliin. Kumpikin on toisen paras vastaus viiveellä, eli kyse on kolikkopelistä (matching pennies). Jos viive on yhtä pitkä, kumpikin voittaa puolet katkoväleistä (laskettu esimerkki: vuorottelu, `adaptive` voittaa joka toisen katkovälin, n. 50 %). Jokainen heilunnan esto pidentää `adaptive`n viivettä ja antaa `rulebased`ille lisää vastapeliaikaa. Heilunnan esto ei siis voi nostaa `adaptive`a yli n. 50 %:n `rulebased`ia vastaan; se tekee `adaptive`sta vakaan ja kilpailukykyisen yksiulotteista vastustajaa vastaan.
- **Miksi se on hyväksyttävää:** pilari "taito voittaa tuurin": nopeampi lukija voittaa. Pelaaja, joka pelaa aina samaa tyyliä, häviää `adaptive`lle, ja `adaptive` häviää valmentajalle, joka vaihtaa vastapeliin heti. Ennen D-080:aa `adaptive` lukittui trappiin, jota vastaan `rulebased` kokosi n. 77 % ajasta (30,2 %). Uudessa säännössä `adaptive`n vastapeliaika jää vaihdon jälkeen n. 1 katkoväliin ja vastapelattu aika n. 1,5–2 katkoväliin, eli vastapelattu osuus laskee n. 77 %:sta n. 55–65 %:iin.
- **Odotettu tulos (arvio, ei tavoite):** `adaptive` vs `rulebased` 38–48 % (ennen 30,2 %; koe B ilman vaihtoa 50,2 %); vs `randomLines` 60–72 % (ennen 52,2 %, koe B 75,4 %); vs `random` 75–82 %; vs `randomTactics` 55–65 %. Jos vs `rulebased` jää alle 38 %:n, designer pienentää ensin `styleMinEvents`-arvoa (lyhyempi viive) yhtenä muutoksena.

**8. Mittaus (designer, toteutuksen jälkeen).** Batch 5000 ottelua per pari, siemen 1: `adaptive` vs `rulebased`, `random`, `randomTactics`, `randomLines`, ja vertailuksi `rulebased` vs samat (ei saa muuttua, koska `rulebased` ei muutu). Raporttiin tarvitaan (programmer, raporttimerkintä, ei uutta tapahtumatyyppiä): (a) `adaptive`n järjestelmävaihtojen määrä per ottelu ja aika kummassakin järjestelmässä; (b) osuus ajasta, jolloin vastustajan siirtymäohje on oma järjestelmän vastapeli (karvaus + rush tai trap + regroup); (c) luokittelun osuma: luokiteltu siirtymä vs vastustajan todellinen siirtymäohje (harness tietää sen), tavoite ≥ 90 %. Lisäksi taktiikkamatriisi ei muutu, koska `rotation` ei käytä A-T:tä.

**9. Testit (programmer).**

1. Luokittelu: riisto → vastustajan Hallittu alueelletuonti ilman kuvio-id:tä → suorahyökkäys; riisto → vastustajan Syöttö kuvio-id:llä → kokoaminen; ensimmäinen toiminto Kiekonmenetys → ei luokitella; aloituksesta alkanut hallinta → ei luokitella; oma epäonnistunut syöttö ja sitten vastustajan toiminto (ei julkista riistoa) → uusi hallinta luokitellaan; Kamppailu, Vaihto ja Aikalisä eivät katkaise eivätkä aloita hallintaa; Pelikatko nollaa tilan; vain hallinnan ensimmäinen toiminto luokitellaan; omat siirtymät eivät koskaan näy havaintoina; tyhjä järjestelmä-id → ei havaintoa.
2. Ikkuna: vain järjestelmä-id = c lasketaan; vain viimeiset `styleWindowEvents` havaintoa.
3. Päätös: n = `styleMinEvents` − 1 → c pysyy; 3/4 rush → `passiveSystem`; 1/4 → `aggressiveSystem`; 2/4 → c pysyy; tyhjä loki → `modes.normal.system`.
4. Vaihdon jälkeen ikkuna on tyhjä, joten seuraava vaihto vaatii `styleMinEvents` uutta havaintoa; aikalisän jälkeinen kierros samalla katkolla antaa saman c:n.
5. `protect`/`chase` → `modes`-järjestelmä, c säilyy ja palautuu tilassa `normal`.
6. Determinismi: sama loki → sama päätös, ei satunnaisvirtaa.
7. Lataaja: avainten hylkäysehdot (taulukko yllä), vanhat `controlledEntryShare*`-avaimet hylätään tuntemattomina, uudet polut `_placeholders`-listassa.

### AI-3 `ai`-osio (`tuning.json`, uusi ylimmän tason osio)

Kaikki avaimet ovat pakollisia, ja tuntematon avain hylätään (tyypitetty osio). Kaikki arvot ovat paikkamerkkejä, paitsi `passiveSystem` ja `aggressiveSystem`.

| Polku | Oletus | Yksikkö | Hylätään, jos | Käyttäjä |
| --- | --- | --- | --- | --- |
| `ai.random.onTheFlyChangeProbability` | 0,5 | todennäköisyys per tilaisuus ja yksikkötyyppi | ei välillä [0, 1] | `random`, `randomLines` |
| `ai.random.timeoutProbabilityPerStoppage` | 0,02 | todennäköisyys per sallittu katko (n. 47 aloitusta → aikalisä n. 60 %:ssa otteluista) | ei välillä [0, 1] | `random`, `randomTactics` |
| `ai.estimate.extraDrainPerSecondOnIce` | 0,0011 | energiaa per pelisekunti (45 s vuoro: 0,25 + 0,05 ≈ O-5:n 0,30) | < 0 tai > 1 | S-L, AI-4 |
| `ai.rulebased.changeEnergyThreshold` | 0,7 | energia 0–1 | ei välillä [0, 1] | S-L |
| `ai.rulebased.freshnessMargin` | 0,15 | energia 0–1 | ei välillä [0, 1] | S-L |
| `ai.rulebased.lateGameSeconds` | 300 | pelisekuntia | ≤ 0 tai > `time.periodSeconds` | S-T, A-T |
| `ai.rulebased.protectLeadGoals`, `chaseDeficitGoals` | 1, 1 | maaleja | ei kokonaisluku tai < 1 | S-T, A-T |
| `ai.rulebased.modes` | ks. JSON | tila → `system`, `transition`, `netFrontAfterShot`, `looseChasers`, `pinch` | avaimet eivät ole tasan `normal`, `protect`, `chase`; kentästä puuttuu tai on ylimääräinen; tuntematon järjestelmä; `transition` ei `rush`/`regroup`; `looseChasers` ∉ {1, 2}; totuusarvo ei bool | S-T, A-T |
| `ai.rulebased.playBonusBySystem` | ks. JSON | järjestelmä → kuvio → logit | tuntematon järjestelmä tai kuvio; arvo ei välillä [−2, 2] | S-T, A-T |
| `ai.rulebased.transitionBySystem` (D-078 K2) | `forecheck212` → `rush`, `trap122` → `regroup` | vastustajan järjestelmä → siirtymäohje | tuntematon järjestelmä; arvo ei `rush`/`regroup`. Kaikkia järjestelmiä ei tarvitse luetella (puuttuva = `modes.normal.transition`); tyhjä objekti sallittu | S-T (kohta 2), A-T (kohta 3) |
| `ai.rulebased.timeout.tiredEnergy` | 0,5 | energia 0–1 | ei välillä [0, 1] | AI-4 |
| `ai.rulebased.timeout.lateGameSeconds` | 120 | pelisekuntia | ≤ 0 tai > `time.periodSeconds` | AI-4 |
| `ai.rulebased.timeout.minDeficitGoals`, `maxDeficitGoals` | 1, 2 | maaleja | ei kokonaislukuja, min < 1 tai max < min | AI-4 |
| `ai.adaptive.windowSeconds` | 600 | ottelusekunteja | ≤ 0 | A-T |
| `ai.adaptive.priorWeight` | 10 | näennäistapahtumia | < 0 | A-T |
| `ai.adaptive.passiveSystem`, `aggressiveSystem` | `trap122`, `forecheck212` | järjestelmän id | tuntematon järjestelmä tai sama id molemmissa | A-T |
| `ai.adaptive.controlledEntryShareForPassive`, `controlledEntryShareForAggressive` | 0,6, 0,4 | osuus | ei välillä [0, 1] tai aggressive ≥ passive | A-T |

Järjestelmä- ja kuviotunnukset tarkistetaan ladattuja `data/systems`- ja `data/plays`-tiedostoja vasten samassa `validate`-ajossa (ristiintarkistus).

**`playBonusBySystem`-alkuarvot (kiekkoperuste):** 2-1-2-karvausta vastaan nopea vastasuuntainen avaus (`breakoutReverse` +0,2) karistaa kaksi karvaajaa, ja laitaa pitkin avaaminen (`breakoutUpWall` −0,1) ajaa karvaajien ansaan. 1-2-2-trappia vastaan kiekko päätyyn (`dumpAndChase` +0,2) on klassinen vastalääke, ja laidan kuljetus (`wideCarryDrive` −0,1) menee suoraan trapin ohjaamaan laitaan.

### AI-4 Aikalisä (D-062 muutettu, D-077 kohta 6)

- **Kuka ja milloin.** Valmentaja päätöksessään (AI-5) katkolla, jonka syy on maali, maalivahti pitää kiekon tai kiekko ulos kaukalosta (myöhemmin myös pitkä kiekko ja paitsio). Ei erän alussa eikä syyllä "erän loppu".
- **Määrä.** Enintään `time.timeoutsPerTeamPerMatch` (1) per joukkue per ottelu (IIHF ja NHL: yksi). Käytettävissä = joukkueen käyttämät < raja. Ei siirry erästä toiseen eikä palaudu.
- **Järjestys katkolla:**
  1. Pelikatko-tapahtuma, paine × `keepOnStoppage` (O-13).
  2. Molemmat valmentajat päättävät samanaikaisesti (D-077 kohta 5): ketjut, suunnitelma ja aikalisä.
  3. Jos kumpikaan ei ottanut aikalisää, päätökset pannaan toimeen kuten ennen.
  4. Muuten jokaisesta aikalisästä (kotijoukkue ensin) kirjataan Aikalisä-tapahtuma ja sovelletaan vaikutus. Sen jälkeen **molempia valmentajia kutsutaan uudelleen** (aikalisän jälkeinen kierros), ja kierroksen päätökset korvaavat ensimmäiset. Uudella kierroksella aikalisää ei voi ottaa.
  5. Katkovaihdot ja aloitusasettelu (O-2, O-3).
- **Vaikutus.** Kello on pysähdyksissä: aikaa ei kulu, kokonaisuuden kestoa (O-1) ei ole, jääaika ei kasva eikä vaihtokelpoisuus (`stoppageChangeMinSeconds`) muutu. Jokaisen kenttäpelaajan energia molemmissa joukkueissa, jäällä ja penkillä: E ← 1 − (1 − E) × exp(−`energy.benchRecoveryRate` × `time.timeoutRestSeconds`). Penkkipelaajilla tämä tulee kertyneen penkkiajan päälle (laiska laskenta realisoidaan ensin). Maalivahdin energia, paine, järjestäytyneisyys (aloituksessa joka tapauksessa 1) ja tuttuus eivät muutu. Kaksi aikalisää samalla katkolla = vaikutus kahdesti peräkkäin (Q-059).
- **Aloitus.** Aloituspiste ja kiekon saaja kuten ilman aikalisää (O-3, D-060).
- **Sääntöpohjainen laukaisu** (`rulebased`, `adaptive`, `randomLines`): ota aikalisä, jos se on käytettävissä ja jompikumpi pätee:
  1. **Väsynyt yksikkö jumissa:** oma jäällä oleva kolmikko tai pari ei saa vaihtua (O-2), ja sen Ê_u < `ai.rulebased.timeout.tiredEnergy`. Tulee merkittäväksi, kun pitkä kiekko estää vaihdon (D-077 kohta 7); silloin noudatetaan oikeaa sääntöä, ettei pitkän kiekon tehnyt joukkue saa vaihtaa aikalisän jälkeenkään.
  2. **Kirimisen aikalisä:** viimeinen erä, jäljellä ≤ `ai.rulebased.timeout.lateGameSeconds` ja `minDeficitGoals` ≤ −johto ≤ `maxDeficitGoals`.

  Aikalisän jälkeisellä kierroksella S-L arvioi yksiköt levon jälkeisillä Ê-arvioilla.
- **Tapahtuma Aikalisä:** joukkue (`home`/`away`), erä ja kello sekä kaikille tapahtumille yhteiset kentät (voimasuhteet, sijainnit katkon hetkellä, kuvio- ja järjestelmä-id, tyhjä jos ei ole). Lisäkenttiä ei tarvita, koska kesto ja lepo johdetaan `tuning.json`:sta. Tapahtuma kuuluu julkiseen lokiin.

| Polku | Oletus | Yksikkö | Hylätään, jos | Tila |
| --- | --- | --- | --- | --- |
| `time.timeoutsPerTeamPerMatch` | 1 | kpl | ei kokonaisluku tai < 0 | sääntö (IIHF, NHL, vision.md), ei paikkamerkki |
| `time.timeoutRestSeconds` | 30 | sekuntia todellista aikaa, ei pelikelloa | ≤ 0 | sääntö (IIHF, NHL), ei paikkamerkki |

30 sekuntia palauttaa n. 28 % energiavajeesta (1 − exp(−0,011 × 30)).

### AI-5 Valmentajan päätös (ohjelmoijalle)

`ICoach.AtStoppage(view)` palauttaa **`CoachDecision`**:

| Kenttä | Sisältö | Validointi (rikkomus = poikkeus, ohjelmointivirhe) |
| --- | --- | --- |
| `Units` (`LineChoice`) | Kolmikon ja parin indeksit | Vaihtokelvoton yksikkö ei saa muuttua (O-2); indeksi kokoonpanon rajoissa |
| `Plan.Plays` | Kuviot prioriteettijärjestyksessä (D-036) | Vain ladattuja kuvioita, ei kaksoiskappaleita; tyhjä lista sallittu (joukkue pelaa järjestelmätilassa) |
| `Plan.System` | Puolustusjärjestelmä | Ladattu järjestelmä |
| `Plan.Transition` | `rush` tai `regroup` (O-7) | |
| `Plan.Instructions` | `netFrontAfterShot` (bool), `looseChasers` (1 tai 2), `pinch` (bool) (O-10) | `looseChasers` ∈ {1, 2} |
| `CallTimeout` (bool, uusi) | Aikalisä tällä katkolla (AI-4) | Tosi vain, jos aikalisä on käytettävissä, katkon syy sallii sen eikä kyse ole aikalisän jälkeisestä kierroksesta |

`ICoach.OnTheFly(view)` palauttaa vain `LineChoice`:n; suunnitelma pysyy seuraavaan katkoon.

**`CoachView`** sisältää AI-1:n julkisen tiedon: oma puoli, erä, kello, maalit, katkon syy (tai erän alku), kummankin joukkueen jäällä olevat yksikköindeksit, oman joukkueen jokaisen yksikön nykyinen jääaika (jos jäällä), penkkiaika ja kertynyt jääaika, vaihtokelpoisuus (`CanChangeForwards`, `CanChangeDefence`), omat ja vastustajan käytettävissä olevat aikalisät, tieto aikalisän jälkeisestä kierroksesta, oma kuviolaskuri (`PlayUses`) ja julkinen tapahtumaloki tähän asti (vain luku). **Nykyinen `Energy`-kenttä (todellinen energia) poistetaan** (D-077 kohta 3); `RotationCoach` ei käytä sitä. Valmentajan satunnaisvirta annetaan konstruktorissa, ei näkymässä.

**`rulebased` ja `adaptive` ovat deterministisiä ilman satunnaisvirtaa:** sama julkinen tila → sama päätös. Testit: jokaiselle säännölle yksikkötesti (S-L:n kynnykset ja tasatilanteet, S-T:n tilat, A-T:n hystereesi, AI-4:n laukaisut); aikalisä ei kuluta kelloa eikä muuta vaihtokelpoisuutta; aikalisän jälkeinen kierros kutsuu molempia valmentajia; `random` tuottaa saman lokin samalla siemenellä, eikä valmentajan arvonta siirrä simulaation virtaa.

### AI-6 Lataajan muutokset

`ai` lisätään sallittuihin ylimmän tason osioihin ja `time`-osioon avaimet `timeoutsPerTeamPerMatch` ja `timeoutRestSeconds`, samassa commitissa datan kanssa (D-030). Kaikki AI-3:n taulukon `ai.*`-polut lisätään `_placeholders`-listaan, paitsi `ai.adaptive.passiveSystem` ja `ai.adaptive.aggressiveSystem`. Taulukkomaiset `ai.rulebased.modes` ja `ai.rulebased.playBonusBySystem` listataan yhtenä polkuna. `time.timeoutsPerTeamPerMatch` ja `time.timeoutRestSeconds` eivät ole paikkamerkkejä.

**Data, joka lisätään samassa commitissa lataajan tuen kanssa** (`time`-avaimet olemassa olevien rinnalle, `ai` uutena osiona):

```json
"time": {
  "timeoutsPerTeamPerMatch": 1,
  "timeoutRestSeconds": 30
},
"ai": {
  "_notes": "Coach AI thresholds (data-schema.md AI-1..AI-6, D-077). Not tuned for win rate before the first batch report (D-077 item 8). Coaches see public info only; energy is estimated from ice and bench time.",
  "random": {
    "onTheFlyChangeProbability": 0.5,
    "timeoutProbabilityPerStoppage": 0.02
  },
  "estimate": {
    "extraDrainPerSecondOnIce": 0.0011
  },
  "rulebased": {
    "changeEnergyThreshold": 0.7,
    "freshnessMargin": 0.15,
    "lateGameSeconds": 300,
    "protectLeadGoals": 1,
    "chaseDeficitGoals": 1,
    "modes": {
      "normal": { "system": "forecheck212", "transition": "rush", "netFrontAfterShot": true, "looseChasers": 1, "pinch": false },
      "protect": { "system": "trap122", "transition": "regroup", "netFrontAfterShot": false, "looseChasers": 1, "pinch": false },
      "chase": { "system": "forecheck212", "transition": "rush", "netFrontAfterShot": true, "looseChasers": 2, "pinch": true }
    },
    "playBonusBySystem": {
      "forecheck212": { "breakoutReverse": 0.2, "breakoutUpWall": -0.1 },
      "trap122": { "dumpAndChase": 0.2, "wideCarryDrive": -0.1 }
    },
    "timeout": {
      "tiredEnergy": 0.5,
      "lateGameSeconds": 120,
      "minDeficitGoals": 1,
      "maxDeficitGoals": 2
    }
  },
  "adaptive": {
    "windowSeconds": 600,
    "priorWeight": 10,
    "passiveSystem": "trap122",
    "aggressiveSystem": "forecheck212",
    "controlledEntryShareForPassive": 0.6,
    "controlledEntryShareForAggressive": 0.4
  }
}
```

## Taktiikan vastapeli (D-078): toteutusjärjestys, tasapainoarvot ja lataaja

D-078 hyväksyi E-005:n kohdat K1–K4. K3 (taktiikkamatriisi ja `--rotation-transition`) on toteutettu. Loput toteutetaan tässä järjestyksessä, yksi muutos ja mittaus kerrallaan (taktiikkamatriisi, 2000 ottelua per solu):

| Vaihe | Muutos | Speksi | Mittaus ennen seuraavaa vaihetta |
| --- | --- | --- | --- |
| 1 | K1: karvaus haastaa kokoamisen omassa päässä | O-7, Karvaus haastaa kokoamisen | Taktiikkamatriisi: rush vs regroup karvausta ja trapia vastaan, regroup karvausta vastaan ≥ 35 % |
| 2 | K4: voimasuhde- ja karvaajamuokkaajat | M-9 | Taktiikkamatriisi, alueelle tulon onnistuminen voimasuhteittain, avauksen onnistuminen f:n mukaan |
| 3 | K2: `rulebased` valitsee siirtymän vastustajan järjestelmän mukaan | AI-2 (S-T kohta 2, A-T kohta 3), AI-3 | Täysi batch: `rulebased` vs `randomTactics` ≥ 55 %, `rulebased` vs `random`, maalit per ottelu 4–7 |

D-078:n mittausrajat: karvausta vastaan rush − regroup ≥ +5 %-yks., trapia vastaan regroup − rush ≥ +5 %-yks., regroup karvausta vastaan ≥ 35 %, `rulebased` vs `randomTactics` ≥ 55 %, maalit per ottelu 4–7. E-005:n säätönuppia `transitions.regroupForecheckModifier` ei lisätä: K4:n `perForechecker` vaikuttaa myös kokoamisen tarkistukseen (O-7, kohta 4).

**Tasapainoarvot.** Kaikki avaimet ovat pakollisia siitä commitista alkaen, jossa ne lisätään (puuttuva = virhe). Data ja lataajan tuki commitoidaan yhdessä (D-030), ja polut lisätään `_placeholders`-listaan.

| Vaihe | Polku | Arvo | Hylätään, jos | Käyttäjä |
| --- | --- | --- | --- | --- |
| 1 | `transitions.regroupForecheckersAllowed` | 1 | ei kokonaisluku, < 0 tai > 3 | O-7, K1 |
| 2 | `checks.zoneEntryCarry.modifiers.strengthDifferencePerPlayer` | 0,25 | < 0 tai puuttuu | M-9 |
| 2 | `checks.breakout.modifiers.perForechecker` | −0,25 | > 0 tai puuttuu | M-9, O-7 (K1) |
| 3 | `ai.rulebased.transitionBySystem` | ks. JSON | ks. AI-3 | S-T, A-T |

Arvo 0 on sallittu kaikissa, ja se poistaa vaikutuksen (`regroupForecheckersAllowed` 3 = kokoamista ei koskaan tarkisteta). Näin jokaisen vaiheen voi mitata myös ilman muutosta samalla koodilla.

**Data, joka lisätään samassa commitissa lataajan tuen kanssa.**

Vaihe 1 (`transitions`-osioon olemassa olevien rinnalle; `_notes`-tekstin loppuun lisätään: "regroupForecheckersAllowed: in the own zone, a regroup faces one breakout check when more forecheckers (F1-F3) than this are in the zone after the defence's regroup move; failure is a takeaway by the nearest forechecker (D-078 K1)."):

```json
"transitions": {
  "regroupForecheckersAllowed": 1
}
```

Vaihe 2 (muokkaajat olemassa olevien rinnalle; `checks.breakout._notes`:n virke "Defensive system effect is a later modifier (Q-003)." korvataan: "perForechecker: logit per forechecker (F1-F3) in the attacking team's defensive zone, data-schema.md M-9 (D-078 K4)."; `checks.zoneEntryCarry._notes`:iin lisätään: "strengthDifferencePerPlayer: logit per player of N - M (M-8), data-schema.md M-9 (D-078 K4)."):

```json
"zoneEntryCarry": {
  "modifiers": { "strengthDifferencePerPlayer": 0.25 }
},
"breakout": {
  "modifiers": { "perForechecker": -0.25 }
}
```

Vaihe 3 (`ai.rulebased`-osioon `playBonusBySystem`-avaimen jälkeen; järjestelmätunnukset ristiintarkistetaan `data/systems`-tiedostoja vasten kuten AI-3:ssa):

```json
"transitionBySystem": {
  "forecheck212": "rush",
  "trap122": "regroup"
}
```

`modes.normal.transition` pysyy `rush`-arvossa. E-005 ei ehdota sen vaihtamista, koska se lukitsisi tasapainovirheen tekoälyyn.

## replay.json (toiston esityskerros, D-088)

Toiston esitysarvot. Simulaatio päättää, mihin pelaajat ja kiekko menevät, ja toisto päättää, miltä se näyttää. Tiedostoa lukevat vain toisto (`Replay/Core`) ja sen testit (`tests/HockeyCoach.Replay.Tests`), eivät Sim, Harness eikä `validate`. Designer omistaa tiedoston.

**Lataus (D-088 B).** Unity ja `dotnet test` lukevat saman tiedoston `data/replay.json`. Jos tiedosto puuttuu tai on virheellinen, lataus epäonnistuu näkyvästi (Unityssa virheteksti ruudulla ja konsolissa, testeissä poikkeus). Hiljaisia oletuksia ei ole, eikä koodissa saa olla näiden avainten arvoja. Lataaja hylkää tiedoston myös, jos `schemaVersion` ei ole 1, jos jokin alla oleva avain puuttuu tai jos jossain osiossa on tuntematon avain (`_`-alkuisia meta-avaimia lukuun ottamatta). Ylimmän tason sallitut avaimet ovat `schemaVersion`, `timing`, `goal`, `puck`, `skaters`, `goalies` ja `bench`. Kaikkien lukujen pitää olla äärellisiä.

**Yksiköt.** *Esitysaika* tarkoittaa sekunteja ruudulla 1×-nopeudella. Nopeuskerroin (`SpeedLadder`) jakaa koko esitysajan. *Peliaika* tarkoittaa ottelukellon sekunteja, ja tapahtumien `time` on peliaikaa. Kaikki nopeudet ovat metrejä per esityssekunti ja kaikki matkat metrejä kaukalon metrikoordinaatistossa (`RinkMapping`, Q-077). Kestot ovat esityssekunteja, ellei avaimen nimessä ole mainittu muuta.

### Otsakkeen aikakustannukset (`formatVersion` 2)

Harness vie `match --json` -otsakkeeseen `tuning.json`:n aikakustannukset objektina `time`. Rakenne on sama kuin `tuning.json`:ssa. Tapahtumaskeema ei muutu.

```json
"time": {
  "secondsPerAction": { "faceoff": 2, "skate": 3, "pass": 2, "shoot": 1, "driveNet": 2, "dumpIn": 2, "zoneEntry": 3, "breakout": 4, "deke": 2, "hit": 2, "loosePuck": 3, "rebound": 1, "systemStep": 2 },
  "setupSeconds": 6,
  "regroupSeconds": 8
}
```

- Harness vie koko `time.secondsPerAction`-objektin sekä arvot `time.setupSeconds` ja `time.regroupSeconds`.
- Toisto käyttää näitä polkuja: `time.secondsPerAction.faceoff`, `.skate`, `.pass`, `.shoot`, `.dumpIn`, `.loosePuck`, `.rebound` ja `.systemStep` sekä `time.setupSeconds` ja `time.regroupSeconds`.
- Lukija hylkää `formatVersion` 2 -tiedoston, josta jokin näistä puuttuu tai jossa arvo on negatiivinen. `formatVersion` 1 hylätään selkeällä viestillä, jossa pyydetään viemään ottelu uudelleen.

### Esitysaika: välin jako toimintaan ja kuolleeseen aikaan

Tapahtuman `time` on hetki, jolloin sen toiminto päättyy. Väli g = edellisen tapahtuman ja tämän tapahtuman pelikellojen erotus (erän ensimmäiselle tapahtumalle g = `time`). Väli koostuu lokiin kirjaamattomista vaiheista (kokoaminen, uudelleenryhmittyminen, järjestelmän askel, kirjaamaton luistelu tai maalille ajo) ja niiden perässä tapahtuman omasta toiminnosta.

1. **Toiminnon kustannus a** (peliaikaa, otsakkeesta), aina enintään g:
   - `Faceoff`: `faceoff`.
   - `Pass`: `pass`.
   - `Shot`: `shoot`.
   - `DumpIn`: `dumpIn`.
   - `PuckBattle`: `loosePuck`. Jos edellinen tapahtuma on `Shot`, lisätään `rebound`. Jos edellinen tapahtuma on `PuckBattle`, lisätään `systemStep`.
   - `ControlledZoneEntry`: `skate`, jos `method` on `Carry`, ja 0, jos `method` on `Pass` (syöttö on samalla sekunnilla oma tapahtumansa).
   - `Turnover`: `skate`.
   - `Stoppage`, `LineChange`, `Timeout`: 0.
2. **Jäännös** r = g − a.
3. **Kuollut aika** k:
   - k = `setupSeconds` + `regroupSeconds`, jos r ≥ `setupSeconds` + `regroupSeconds`.
   - Muuten k = `setupSeconds`, jos r ≥ `setupSeconds`.
   - Muuten k = 0.

   Loput, eli g − k, on toimintaa. Järjestelmän askel (2 s) jää toiminnaksi, koska se ei erotu maalille ajosta. Siemenellä 42 kuollutta aikaa on n. 40 % ottelukellosta.
4. **Vaiheet välin sisällä** järjestyksessä:
   1. kuollut aika k, kesto k / `timing.deadCompression`
   2. muu toiminta (r − k), kesto (r − k) / `timing.actionCompression`
   3. **toimintaikkuna** a, kesto a / `timing.actionCompression`

   Kiekko lähtee toimintaikkunan alussa, eli kohdassa, jossa pelikello on `time` − a.
5. **Alarajat.** Jokainen alaraja venyttää välin kaikkia vaiheita samassa suhteessa:
   - **Kiekon alaraja:** toimintaikkuna ≥ kiekon reitin kesto (ks. Kiekko). Venytys tehdään ennen hidastusta. `puck.floorMode` valitsee laskutavan (Q-082, D-089 A3): `allLegs` laskee koko reitin, `firstLeg` vain ensimmäisen osuuden (laukaus torjujaan, verkkoon tai ohipisteeseen; jälkimmäinen osuus jää pois).
   - **Luistelijan alaraja:** välin kesto ≥ suurin toimijan siirtymä / `skaters.topSpeed`. Siirtymä on edellisen ja tämän tapahtuman solmujen välinen matka metreinä ilman poikkeamia. Toimijat ovat tapahtuman kentissä nimetyt pelaajat (`passerId`, `receiverId`, `shooterId`, `loserId`, `takerId`, `carrierId`, `homeCentreId`, `awayCentreId`, `attackerIds`, `defenderIds`) sekä `placement.puckCarrierId` ennen ja jälkeen tapahtuman.
     - Muut pelaajat eivät venytä väliä, vaan voivat jäädä jälkeen (ks. Luistelijat).
     - Rajaa ei sovelleta katkon sarjan sisällä.
   - **Vähimmäiskesto (Q-078):** välin kesto ≥ `timing.minEventSeconds`. Lentävässä vaihdossa (`LineChange`, joka ei kuulu katkon sarjaan) raja on `timing.lineChangeSeconds`. Samalla sekunnilla olevat tapahtumat näkyvät näin jatkuvassa toistossa.
6. **Pelikello** etenee kunkin vaiheen sisällä lineaarisesti esitysajan suhteen. Jäädytetyissä vaiheissa (alla) kello pysyy paikallaan. Kello ei koskaan kulje taaksepäin, ja se näyttää aina oikeaa peliaikaa.

**Maali.** Kun `Shot` päättyy maaliin (`outcome` `Goal`), sen toimintaikkunan kesto (kiekon alarajan jälkeen) jaetaan luvulla `goal.slowMoRate`, ja tulos on vähintään `goal.slowMoMinSeconds`. Perään tulee jäädytetty pito `goal.holdSeconds`. Kiekko pysyy verkossa koko ajan (Q-079).

**Katkon sarja.** Sarja alkaa `Stoppage`-tapahtumasta. Siihen kuuluvat kaikki seuraavat tapahtumat, joilla on sama pelikello (`LineChange`, `Timeout`), ja sarja päättyy seuraavaan `Faceoff`-tapahtumaan.

1. `Stoppage`-väli lasketaan tavalliseen tapaan. Perään tulee jäädytetty pito `timing.stoppageHoldSeconds`.
2. `LineChange` sarjan sisällä kestää 0 s. Sen vaihdot näytetään vaihtovaiheessa (kohta 4).
3. `Timeout` kestää `timing.timeoutHoldSeconds` jäädytettynä.
4. **Vaihtovaihe (D-089 A1)** on jäädytetty vaihe `Faceoff`-välin alussa, ennen lähestymistä. Se on mukana vain, jos sarja vaihtaa jonkun kenttäpelaajan (katkon ja `Faceoff`-tapahtuman kenttäpelaajat eroavat).
   - Järjestys on pito → vaihdot (ulos penkille, sitten sisään) → asettuminen aloitukseen. Paikallaan pysyvät pelaajat seisovat vaihtovaiheen ajan.
   - Lähtevät pelaajat luistelevat katkon sijainnistaan oman penkkinsä poistumiskohtaan (ks. Penkit), saapuvat sinne yhtä aikaa vaiheen lopussa ja poistuvat jäältä. Nopeusprofiili alkaa ja päättyy lepoon, kuten lähestymisessä.
   - Kesto = suurin lähtevän pelaajan siirtymä (solmusta poistumiskohtaan) / `timing.stoppageChangeSpeed`, rajattuna välille [`stoppageChangeMinSeconds`, `stoppageChangeMaxSeconds`].
   - Tuleva pelaaja ilmestyy oman penkkinsä sisääntulokohtaan, kun hänen lähtevä parinsa on enintään `bench.enterWithinMetres` päässä poistumiskohdasta, ja odottaa siinä lähestymisen alkuun. Parit muodostetaan saman `LineChange`-tapahtuman `outgoingIds`- ja `incomingIds`-listoista nousevassa id-järjestyksessä. Tuleva pelaaja ilman paria ilmestyy vaiheen alussa.
5. **Aloitusta edeltävä lähestyminen** on jäädytetty vaihe vaihtovaiheen jälkeen, ennen `Faceoff`-väliä.
   - Kaikki kenttäpelaajat luistelevat nykyisistä esityssijainneistaan `Faceoff`-tapahtuman sijainteihin. Välissä olevat sijainnit eivät ole kohteita. Tulevat pelaajat lähtevät penkin sisääntulokohdasta.
   - Kesto = suurin siirtymä / `timing.faceoffApproachSpeed`, rajattuna välille [`faceoffApproachMinSeconds`, `faceoffApproachMaxSeconds`]. Siirtymä mitataan solmuista ilman poikkeamia (tulevilla sisääntulokohdasta). Lähteneet pelaajat eivät ole mukana. Kaikki pelaajat saapuvat samaan aikaan, ja nopeusprofiili alkaa ja päättyy lepoon (s = L(3x² − 2x³)).
6. Sitten `Faceoff`-väli lasketaan tavalliseen tapaan (a = `faceoff`).

**Erät.** Kun `Stoppage`-tapahtuman syy on `PeriodEnd`, pidon jälkeen tulee jäädytetty tauko `timing.periodBreakSeconds`. Erän alussa pelaajat ovat suoraan ensimmäisen tapahtuman sijainneissa, eikä lähestymistä ole. Erän alun `LineChange` (kello 0) kestää 0 s.

**Mitoitus.** Oletusarvoilla 45 s vaihto kestää 1×-nopeudella:

| Ottelu | Vaihto 1× | Ottelu 1× | Ottelu 8× |
| --- | --- | --- | --- |
| Siemen 42, `rulebased` vs `random` | 15,5 s | 20,6 min | 2,6 min |
| Siemenet 1 ja 7, `rulebased` vs `rulebased` | 19,6 s | 26,1 min | 3,3 min |

Tavoite on 15–20 s (D-088 A). Suurin säätövara on `actionCompression`. Kiekon ja luistelijan alarajat estävät tiivistämästä toimintaa paljon nopeammaksi kuin 3×.

### Kiekko

Kiekon reitti alkaa toimintaikkunan alussa ja kulkee alla olevilla nopeuksilla. Kun reitti on kuljettu, loppuväli on pelaajien liikettä: kiekko on haltijalla (kuljetettu kiekko) tai paikallaan loppusolmussa, jos kiekko on irti. Reitin päätepisteet ovat pelaajien **esityssijainteja** sillä hetkellä, eivät solmuja. Vastaanottajaan päättyvä reitti hakeutuu vastaanottajan liikkuvaan sijaintiin. Ennen toimintaikkunaa kiekko on edellisen tilan mukaan haltijalla tai paikallaan.

| Tapahtuma | Reitti | Nopeus |
| --- | --- | --- |
| `Pass`, onnistunut | syöttäjä → vastaanottaja | `passSpeed`, tai `longPassSpeed`, jos suora matka ≥ `longPassMinMetres` |
| `Pass`, epäonnistunut | syöttäjä → `placement.puckNodeId` (tai uusi haltija) | kuten yllä |
| `Shot`, `Goal` | ampuja → verkko: maalisuun keskikohta, `goal.netInsideMetres` maaliviivan takana | `shotSpeed` |
| `Shot`, `Saved` | ampuja → maalivahdin esityssijainti → `placement.puckNodeId` | `shotSpeed`, sitten `reboundSpeed` |
| `Shot`, `Blocked` | ampuja → blokkaajan esityssijainti → `placement.puckNodeId` | `shotSpeed`, sitten `blockDeflectSpeed` |
| `Shot`, `Missed` | ampuja → ohipiste → `placement.puckNodeId` | `shotSpeed`, sitten `reboundSpeed` |
| `DumpIn` | ampuja → `placement.puckNodeId` | `dumpInSpeed` |
| `Faceoff` | Kiekko on aloituspisteessä `faceoffDrawDelaySeconds` toimintaikkunan alusta, sitten aloituspiste → haltija tai `puckNodeId` | `faceoffDrawSpeed` |
| `Turnover`, `PuckBattle` ja muut kiekon haltijan vaihdot | nykyinen esityssijainti → uusi haltija tai `puckNodeId` | `loosePuckSpeed` |
| `ControlledZoneEntry` (`Carry`) | kuljetettu kiekko | – |

- **Ohipiste.** Ohipiste on maaliviivalla, maalin sivussa: maalin keskikohdasta mitattuna maalin puolikas leveys (1,83 m / 2) + `missWideMetres`. Puoli valitaan `placement.puckNodeId`:n puolelta keskikaistaan nähden. Jos solmu on keskikaistalla, valitaan ampujan puoli. Jos ampujakin on keskikaistalla, valitaan vasen eli y = 0 -puoli.
- **Kiekon reitin kesto** on osuuksien pituudet jaettuna kunkin osuuden nopeudella. `Faceoff`-tapahtumassa lisätään `faceoffDrawDelaySeconds`. Kiekon alaraja käyttää tätä kestoa (`puck.floorMode` `firstLeg`: vain ensimmäinen osuus). Reitin päät ovat alarajassa solmusijainteja ilman poikkeamia: syöttäjän, ampujan tai edellisen haltijan solmu tämän tapahtuman placementista (Q-082, Q-083).
- **Kuljetettu kiekko** on haltijan esityssijainnissa + `carriedLeadMetres` liikesuuntaan. Jos haltijan nopeus on alle `carriedHeadingMinSpeed`, kiekko on kohti vastustajan maalia (kotijoukkue hyökkää +x).
- **Maalin kiekko (Q-079).** Maalin jälkeen kiekko pysyy verkossa maalin pidon, katkon sarjan ja lähestymisen ajan. Se siirtyy aloituspisteeseen vasta `Faceoff`-välin toimintaikkunan alussa. Hyväksymiskriteeri 3: syötöt ja laukaukset päättyvät `placement`-sijaintiin, paitsi maalit, jotka päättyvät verkkoon.

### Luistelijat

**Solmukohteet.** Jokaisella kentällä olevalla pelaajalla on kohde jokaisen välin lopussa. Se on `placement`-solmun metrisijainti + pelaajan oma poikkeama. Lähestymisen kohteet ovat `Faceoff`-tapahtuman sijainnit. Kohdeaika on välin loppuhetki esitysajassa miinus `anticipationSeconds`. Ennakointia ei käytetä `Stoppage`-tapahtumaan päättyvässä välissä, katkon sarjassa, `Faceoff`-välissä eikä erän alussa.

**Reitti.** Centripetal Catmull-Rom kohteiden P(i−1) → P(i) välillä, naapureina P(i−2) ja P(i+1). Reunoilla päätepiste kopioidaan naapuriksi. Parametri α on `pathAlpha` (0,5 = centripetal). Piste lasketaan kaavalla (1 − `pathTension`) × CR(u) + `pathTension` × suora(u). Jos P(i−1) = P(i), pelaaja seisoo.

**Nopeusprofiili** määritellään kaarenpituuden mukaan.

- Kohteessa nopeus on 0, jos pelaaja seisoo kohteen kummallakaan puolella tai jos reitti kääntyy kohteessa enemmän kuin `stopTurnDegrees`. Muuten nopeus on viereisten välien keskinopeuksien minimi.
- Välin sisällä profiili on trapetsi: kiihdytys `acceleration`, matkanopeus enintään `topSpeed` ja jarrutus `deceleration`.
- Jos trapetsi ei mahdu väliin, käytetään vakionopeutta pituus / aika.
- **Myöhästyminen.** Jos vakionopeuskin ylittäisi `topSpeed`:n, pelaaja saapuu myöhässä. Saapumisaika T'(i) = max(kohdeaika(i), T'(i−1) + pituus / `topSpeed`), ja seuraava väli alkaa siitä. Saapumisajat lasketaan valmiiksi aikajanaa rakennettaessa, joten näytteistys ei riipu aiemmista ruuduista. Luistelijan alaraja takaa, että toimijat ehtivät.
- Lähestymisessä myöhästyminen nollautuu.

**Pelaajan oma poikkeama (Q-080)** riippuu vain ottelun siemenestä (`seed`) ja pelaajan id:stä, joten se on sama koko ottelun ajan. Maalivahdeilla poikkeamaa ei ole.

- Hajautus on SplitMix64, kaikki laskut 64-bittisinä etumerkittöminä:
  - s = `seed` + 0x9E3779B97F4A7C15 × (id + 1)
  - hₖ = mix(s + k × 0x9E3779B97F4A7C15), kun k = 1, 2, 3
  - mix(z): z ^= z >> 30; z *= 0xBF58476D1CE4E5B9; z ^= z >> 27; z *= 0x94D049BB133111EB; z ^= z >> 31
  - uₖ = (hₖ >> 11) × 2⁻⁵³
- Poikkeama = `nodeOffsetRadiusMetres` × √u₁ × (cos 2πu₂, sin 2πu₂).
- Joustoliikkeen vaihe φ = 2πu₃.

**Joustoliike** lasketaan esitysajan t funktiona (kello voi olla jäädytetty):

`idleAmplitudeMetres` × (1 − min(1, v / `topSpeed`)) × (sin(2π f t + φ), sin(2π f `idleYFrequencyRatio` t + 2φ))

- f = `idleFrequencyHz`.
- v on pelaajan hetkellinen nopeus nopeusprofiilista.
- Maalivahdeilla joustoliikettä ei ole.

**Erottelu.** Erottelu tehdään jokaisessa näytteessä poikkeaman ja joustoliikkeen jälkeen, kaikille kentällä oleville kenttäpelaajille (molemmat joukkueet). Toistoja on `separationIterations`.

- Parit käydään läpi id-järjestyksessä (i < j). Jos etäisyys d < `separationMinMetres`, kumpaakin siirretään (`separationMinMetres` − d) / 2 poispäin toisistaan.
- Jos d < 10⁻⁶, suunta on pienemmän id:n kulma 2πu₂.
- Maalivahteja ei siirretä, eivätkä ne siirrä muita.
- Erottelu on tilaton: se riippuu vain saman näytteen sijainneista.
- Kuljetettu kiekko lasketaan erottelun jälkeen. `ReplayViewStyle.teamShift` poistuu.

**Penkit (D-089 A1).** Kummallakin joukkueella on oma penkki y = 0 -laidan puolella, oman puolustuspäänsä puoliskolla (koti hyökkää +x, joten kotipenkki on keskiviivasta −x-suuntaan). Penkin etureunan keskikohta on x = keskiviiva + `bench.homeFromCentreMetres` (koti) tai `bench.awayFromCentreMetres` (vieras), y = `bench.frontYMetres`.

- Poistumiskohta on penkin etureunan keskikohta − `bench.doorSpacingMetres` / 2 x-suunnassa, ja sisääntulokohta on keskikohta + `doorSpacingMetres` / 2. Lähtevät ja tulevat pelaajat eivät siksi käytä samaa pistettä.
- **Lentävä vaihto.** Lähtevä pelaaja luistelee tavallisella profiililla oman penkkinsä poistumiskohtaan. Kohdeaika on `LineChange`-välin kohdeaika, ja myöhästyminen on sallittu. Pelaaja häviää saavuttaessaan kohdan. Tuleva pelaaja ilmestyy sisääntulokohtaan, kun lähtevä pari (kuten vaihtovaiheessa) on enintään `bench.enterWithinMetres` päässä poistumiskohdasta, ja ilman paria `LineChange`-välin alussa. Hänen ensimmäinen kohteensa on oma `placement`-solmu (myöhästyminen sallittu).
- **Katkon vaihto:** ks. katkon sarjan vaihtovaihe.
- **Hyväksymiskriteeri 1.** Lähtevät pelaajat eivät kuulu kahteentoista. Tuleva pelaaja, joka odottaa vielä penkillä, kuuluu tapahtuman kahteentoista, mutta sitä ei piirretä (näytteessä `IsWaiting`). Hänen lähtevä parinsa on silloin vielä jäällä, joten piirrettyjä pelaajia on aina vähintään 12.
- **Erottelu ja vaihtuvat pelaajat.** Lähtevät, odottavat ja vielä sisään luistelevat pelaajat jäävät erottelun ulkopuolelle: niitä ei siirretä, eivätkä ne siirrä muita. Sisään luisteleva on pelaaja, joka ei ole vielä saapunut ensimmäiseen kohteeseensa penkin jälkeen. Näin erottelu ei työnnä vaihtuvia pelaajia toistensa läpi.

### Maalivahdit

Maalivahdin sijainti määräytyy kiekon mukaan.

- x on maaliviiva + `goalies.depthMetres` kohti keskiviivaa.
- y = maalin keskikohta + clamp(`lateralTrackFactor` × (kiekon y − maalin keskikohdan y), −`lateralMaxMetres`, `lateralMaxMetres`).
- Kiekon y luetaan samasta näytteestä.
- Torjunnan reitti kulkee tämän sijainnin kautta.

### Avaimet

| Polku | Oletus | Yksikkö | Hylätään, jos |
| --- | --- | --- | --- |
| `timing.actionCompression` | 3,0 | peliaika / esitysaika | ≤ 0 tai > 20 |
| `timing.deadCompression` | 6,0 | peliaika / esitysaika | < `actionCompression` tai > 50 |
| `timing.minEventSeconds` | 0,4 | s | < 0 tai > 5 |
| `timing.lineChangeSeconds` | 0,4 | s | < 0 tai > 5 |
| `timing.stoppageHoldSeconds` | 0,75 | s | < 0 tai > 10 |
| `timing.timeoutHoldSeconds` | 1,5 | s | < 0 tai > 10 |
| `timing.periodBreakSeconds` | 2,0 | s | < 0 tai > 30 |
| `timing.faceoffApproachSpeed` | 6,0 | m/s | ≤ 0 tai > 15 |
| `timing.faceoffApproachMinSeconds` | 1,0 | s | < 0 tai > `faceoffApproachMaxSeconds` |
| `timing.faceoffApproachMaxSeconds` | 2,0 | s | ≤ 0 tai > 10 |
| `timing.stoppageChangeSpeed` | 6,0 | m/s | ≤ 0 tai > 15 |
| `timing.stoppageChangeMinSeconds` | 0,5 | s | < 0 tai > `stoppageChangeMaxSeconds` |
| `timing.stoppageChangeMaxSeconds` | 2,5 | s | ≤ 0 tai > 10 |
| `goal.slowMoRate` | 0,35 | esitysnopeuden kerroin | ≤ 0 tai > 1 |
| `goal.slowMoMinSeconds` | 1,5 | s | < 0 tai > 10 |
| `goal.holdSeconds` | 2,0 | s | < 0 tai > 10 |
| `goal.netInsideMetres` | 0,5 | m | < 0 tai > 1,12 (maalin syvyys) |
| `puck.passSpeed` | 20 | m/s | < 5 tai > 50 |
| `puck.longPassSpeed` | 24 | m/s | < `passSpeed` tai > 50 |
| `puck.longPassMinMetres` | 25 | m | ≤ 0 |
| `puck.shotSpeed` | 32 | m/s | < 10 tai > 50 |
| `puck.dumpInSpeed` | 20 | m/s | < 5 tai > 50 |
| `puck.reboundSpeed` | 10 | m/s | ≤ 0 tai > 50 |
| `puck.blockDeflectSpeed` | 8 | m/s | ≤ 0 tai > 50 |
| `puck.loosePuckSpeed` | 6 | m/s | ≤ 0 tai > 50 |
| `puck.faceoffDrawSpeed` | 8 | m/s | ≤ 0 tai > 50 |
| `puck.faceoffDrawDelaySeconds` | 0,3 | s | < 0 tai > 2 |
| `puck.missWideMetres` | 1,5 | m | < 0 tai > 5 |
| `puck.carriedLeadMetres` | 0,6 | m | < 0 tai > 2 |
| `puck.carriedHeadingMinSpeed` | 0,5 | m/s | < 0 |
| `puck.floorMode` | `"allLegs"` | merkkijono | muu kuin `"allLegs"` tai `"firstLeg"` |
| `skaters.topSpeed` | 8,5 | m/s | < 3 tai > 12 |
| `skaters.acceleration` | 4,0 | m/s² | ≤ 0 tai > 20 |
| `skaters.deceleration` | 6,0 | m/s² | ≤ 0 tai > 20 |
| `skaters.pathAlpha` | 0,5 | – | < 0 tai > 1 |
| `skaters.pathTension` | 0,0 | – | < 0 tai > 1 |
| `skaters.stopTurnDegrees` | 120 | astetta | < 0 tai > 180 |
| `skaters.anticipationSeconds` | 0,3 | s | < 0 tai > 2 |
| `skaters.nodeOffsetRadiusMetres` | 1,0 | m | < 0 tai > 2,5 |
| `skaters.separationMinMetres` | 2,0 | m | < 0 tai > 4 |
| `skaters.separationIterations` | 3 | kpl | ei kokonaisluku, < 0 tai > 10 |
| `skaters.idleAmplitudeMetres` | 0,3 | m | < 0 tai > 1 |
| `skaters.idleFrequencyHz` | 0,4 | Hz | < 0 tai > 3 |
| `skaters.idleYFrequencyRatio` | 0,7 | – | ≤ 0 tai > 3 |
| `goalies.depthMetres` | 0,6 | m | < 0 tai > 3 |
| `goalies.lateralTrackFactor` | 0,15 | – | < 0 tai > 1 |
| `goalies.lateralMaxMetres` | 0,9 | m | < 0 tai > 2 |
| `bench.homeFromCentreMetres` | −6,0 | m keskiviivasta +x-suuntaan | itseisarvo + `doorSpacingMetres` / 2 > kaukalon puolikas pituus |
| `bench.awayFromCentreMetres` | 6,0 | m keskiviivasta +x-suuntaan | itseisarvo + `doorSpacingMetres` / 2 > kaukalon puolikas pituus |
| `bench.frontYMetres` | 1,0 | m y = 0 -laidasta | < 0 tai > kaukalon leveys |
| `bench.doorSpacingMetres` | 3,0 | m | < 0 tai > 10 |
| `bench.enterWithinMetres` | 3,0 | m | < 0 tai > 20 |

Arvo 0 poistaa vaikutuksen niissä avaimissa, jotka sen sallivat. Esimerkiksi `anticipationSeconds` 0 poistaa ennakoinnin, `nodeOffsetRadiusMetres` 0 poikkeamat, `separationIterations` 0 erottelun ja `idleAmplitudeMetres` 0 joustoliikkeen. Levykkeiden koot ja värit eivät kuulu tähän tiedostoon, vaan ne ovat edelleen Viewn `ReplayViewStyle`-asetuksia Inspectorissa. Erottelu ei tarvitse niitä.
