# Kiekkovalmentaja – dataskeemat

Oct 9, 2026 · game-designer

Tämä dokumentti on datatiedostojen skeemojen ainoa totuus (D-013). Designer omistaa ja ylläpitää sitä. Jos data, koodi ja tämä dokumentti ovat ristiriidassa, tämä dokumentti voittaa, ellei lukittu päätös sano muuta.

| Tiedosto | Tila | Kuka lukee |
| --- | --- | --- |
| `data/rink.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/tuning.json` | käytössä | Sim (`Sim.Config`), Harness |
| `data/targets.json` | käytössä | Harness (raportin tavoitevertailu) |
| `data/plays/*.json` | luonnos – odottaa Jerryn hyväksyntää | Sim (`Sim.Tactics`) |
| `data/systems/*.json` | luonnos – odottaa Jerryn hyväksyntää | Sim (`Sim.Tactics`) |
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

- Kaukalo on `length × width` -solmuverkko. Nykyinen koko on 9 × 5 (Q-002, E-002). Koodi ei saa olettaa kokoa, vaan lukee sen `rink.json`:sta.
- `x` kulkee pituussuunnassa 0 … length − 1, `y` leveyssuunnassa 0 … width − 1.
- **Jokainen tiedosto kirjoitetaan oman joukkueen näkökulmasta:** oma maali on x = 0, vastustajan maali x = length − 1, ja joukkue hyökkää kohti kasvavaa x:ää. y = 0 on vasen laita, kun katsotaan kohti vastustajan maalia.
- **Vastustajan näkökulma** saadaan kierrolla 180°: (x, y) → (length − 1 − x, width − 1 − y). Kierto vaihtaa alueet (defensive ↔ offensive) ja puolet (vasen ↔ oikea). Kaukalon tila tallennetaan simulaatiossa yhdessä kiinteässä koordinaatistossa (kotijoukkueen näkökulma); vierasjoukkueen data kierretään latauksen jälkeen.
- **Solmun id** johdetaan, sitä ei tallenneta: `id = x * width + y` (9 × 5 -verkossa `x * 5 + y`, ids 0–44).
- **Etäisyys** solmujen välillä on Chebyshev-etäisyys max(|dx|, |dy|), ellei toisin mainita. Syöttölinja on suora jana solmujen välillä.
- Koordinaatit kirjoitetaan kuvioissa ja järjestelmissä taulukkona `[x, y]`, kaukalotiedostossa objektina `{"x": .., "y": ..}`.

## rink.json

| Kenttä | Tyyppi | Kuvaus |
| --- | --- | --- |
| `schemaVersion` | int | 1 |
| `length`, `width` | int | Verkon koko. Nyt 9 ja 5 |
| `ownGoal`, `opponentGoal` | `{x, y}` | Maalien solmut. Nyt (0, 2) ja (8, 2), molemmat maaliviivalla |
| `zones` | lista `{id, xMin, xMax}` | Alueet x-väleinä: `defensive` 0–2, `neutral` 3–5, `offensive` 6–8. Jokainen x kuuluu tasan yhteen alueeseen |
| `xgZones` | lista merkkijonoja | Laukaisuvyöhykkeiden nimet. Jokaisella on `tuning.json`:ssa `checks.shot.baseXg`- ja `checks.shot.attackerByXgZone`-arvo |
| `faceoffSpots` | lista `{id, x, y}` | Aloituspisteet, nimet oman joukkueen näkökulmasta |
| `nodes` | lista `{x, y, zone, xgZone, isSlot}` | Kaikki `length × width` solmua id-järjestyksessä |

**Solmun kentät.** `zone` on alueen id. `xgZone` ja `isSlot` kuvaavat laukausta **vastustajan maalia kohti** solmun omasta näkökulmasta. Puolustava joukkue löytää suojattavan slottinsa kierrolla: hyökkääjän solmu (7, 2) on puolustajan näkökulmasta (1, 2).

**Alueiden perustelu.** Maaliviivat ovat x = 0 ja x = 8, joten solmuväli on n. 22 jalkaa (178 ft / 8). Siniviiva on 64 jalan päässä maaliviivasta, eli x ≈ 2,9 ja x ≈ 5,1. Keskiviiva on x = 4.

**Laukaisuvyöhykkeet (hyökkäyspää, 9 × 5).** Rivit x = 6–8 ylhäältä alas (y 0 … 4):

| x | y = 0 | y = 1 | y = 2 | y = 3 | y = 4 |
| --- | --- | --- | --- | --- | --- |
| 8 (maaliviiva) | boards | lowAngle | crease | lowAngle | boards |
| 7 (aloituspisteet) | boards | circle | slot | circle | boards |
| 6 (ympyröiden yläreuna) | boards | point | highSlot | point | boards |

x ≤ 5 on `longRange`. `isSlot` on tosi solmuissa (6, 2), (7, 2) ja (8, 2). Slotin rajaus ja `point`-vyöhykkeen sijainti x = 6:lla (siniviiva on x ≈ 5,1) ovat kompromisseja 9 × 5 -verkossa, ks. Q-015 ja E-002.

**Aloituspisteet.** `center` (4, 2), `defensiveLeft` (1, 1), `defensiveRight` (1, 3), `neutralDefensiveLeft` (3, 1), `neutralDefensiveRight` (3, 3), `neutralOffensiveLeft` (5, 1), `neutralOffensiveRight` (5, 3), `offensiveLeft` (7, 1), `offensiveRight` (7, 3). Kierrossa `offensiveLeft` ↔ `defensiveRight` jne.

**Validointi.** Solmuja on tasan `length × width`, järjestys on id-järjestys, jokainen `zone` löytyy `zones`-listasta ja jokainen `xgZone` `xgZones`-listasta, aloituspisteet ovat verkon sisällä ja niiden id:t ovat yksilöllisiä.

## tuning.json

Kaikki tasapainoarvot (CLAUDE.md, sääntö 2). Ylimmän tason osiot:

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

**Tarkistukset nyt:** kaksipuoliset `faceoff`, `pass`, `zoneEntryCarry`, `deke`, `breakout`, `shot`, `loosePuck`, `hit`; yksipuoliset (`side: "defender"`) `block` ja `rebound`; `dumpIn` (`noCheck`, vain muokkaaja irtokiekkoon). Erikoisrakenteet:

- `shot`: ei `p0`- eikä `attacker`-kenttää. Perustaso on `baseXg[xgZone]` ja hyökkääjän painot `attackerByXgZone[xgZone]` (laukaisijan solmu hänen näkökulmastaan). Järjestys: blokki → `onTargetShare` (maalia kohti vai ohi) → maalitarkistus (Q-010). Omat todennäköisyysrajat `minProbability` ja `maxProbability` (D-014, Q-009).
- `loosePuck`: kolme lopputulosta. Ensin `noWinnerShare`, sitten jäljelle jäävä osuus jaetaan voittoon ja häviöön logistisella tarkistuksella (Q-007).
- `rebound`: onnistuminen = rebound slottiin. Muuten maalivahti hallitsee kiekon: `controlledHoldShare` pitää (katko), loput kulmaan.

**Validointi.** Lataaja ohittaa `_`-avaimet ja hylkää tiedoston, jos: `kind` puuttuu tai on tuntematon; `twoSided`-tarkistukselta puuttuu jompikumpi puoli (`shot`: `attackerByXgZone` korvaa `attacker`-puolen) tai sillä on `side`; `oneSided`-tarkistukselta puuttuu `side`, `side`-puoli puuttuu tai toinen puoli on kirjoitettu; `noCheck`-tarkistuksella on `p0`, `attacker` tai `defender`; läsnä olevan puolen painojen summa poikkeaa 1:stä yli 1e-6; stat- tai roolinimi on tuntematon; p0 tai osuus on välin (0, 1) ulkopuolella; `checkFormula`:n tai `checks.shot`:n `minProbability` ≥ `maxProbability` tai jompikumpi on välin (0, 1) ulkopuolella; `referenceValue` on välin `stats.min`–`stats.max` ulkopuolella; `baseXg` tai `attackerByXgZone` ei kata täsmälleen `rink.json`:n `xgZones`-listaa, tai arvosanarajat eivät kata väliä `min`–`max` aukottomasti.

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

## Kuviot (`data/plays/*.json`) – luonnos – odottaa Jerryn hyväksyntää

Vastaa kysymykseen Q-001 (kuvion osalta). Yksi kuvio per tiedosto, tiedoston nimi = `id`.

```json
{
  "schemaVersion": 1,
  "id": "pointShotScreen",
  "name": "Point shot with screen",
  "type": "offensiveZone",
  "mirrorable": true,
  "start": {
    "puckCarrier": "LW",
    "positions": { "LW": [7, 0], "C": [7, 3], "RW": [8, 4], "LD": [6, 1], "RD": [6, 3] }
  },
  "beats": [
    {
      "moves": { "C": [7, 2], "RW": [8, 2] },
      "action": { "type": "pass", "from": "LW", "to": "LD" }
    },
    {
      "moves": {},
      "action": { "type": "shoot", "by": "LD" }
    }
  ]
}
```

| Kenttä | Kuvaus |
| --- | --- |
| `id` | camelCase, yksilöllinen, sama kuin tiedostonimi |
| `name` | Näyttönimi (lokalisointi myöhemmin) |
| `type` | `breakout` (avaus), `zoneEntry` (alueelle tulo), `offensiveZone` (alueella pelaaminen), `faceoff` (aloitus), `powerPlay` (ylivoima, myöhemmin) |
| `faceoffSpot` | Vain `faceoff`-tyypillä: `rink.json`:n aloituspisteen id |
| `mirrorable` | Jos tosi, simulaatio voi käyttää peilikuvaa: y → width − 1 − y, LW ↔ RW, LD ↔ RD, `...Left` ↔ `...Right` |
| `start.positions` | Viiden kenttäpelaajan lähtösolmut paikoittain `LW`, `C`, `RW`, `LD`, `RD` |
| `start.puckCarrier` | Kuka pitää kiekkoa kuvion alussa |
| `beats` | 1–`plays.maxBeats` (4) tahtia |
| `beats[].moves` | Paikka → kohdesolmu. Puuttuva paikka pysyy paikallaan. Siirto enintään `plays.maxNodesPerBeat` solmua |
| `beats[].action` | Tasan yksi kiekkotoiminto, suoritetaan siirtojen jälkeen |

**Kiekkotoiminnot** (vision.md: luistele, syötä, laukaise, aja maalille):

| `type` | Kentät | Tarkistus |
| --- | --- | --- |
| `skate` | `by`, `to` | Kiekollinen luistelee kiekon kanssa. Alueen rajan ylitys hyökkäysalueelle = `zoneEntryCarry`, puolustaja reitillä voi laukaista `deke`-tarkistuksen |
| `pass` | `from`, `to` | `pass`; vastaanottajan solmu on hänen sijaintinsa siirtojen jälkeen |
| `shoot` | `by` | `block` → `shot`; päättää kuvion |
| `driveNet` | `by` | Kiekoton pelaaja ajaa maalin eteen (vastustajan `crease`-solmu) maskiin ja reboundille (D-020). Ei tarkistusta eikä kiekon siirtoa. Kiekollinen maalille ajo kirjoitetaan `skate`-toimintona |

Jos E-001 hyväksytään, listaan tulee viides toiminto `dump`.

**Kulku.** Kun kuvio valitaan, pelaajat siirtyvät lähtösolmuihin (aika `time.setupSeconds`, Q-006). Tahdit suoritetaan järjestyksessä. Epäonnistunut tarkistus päättää kuvion (kiekonmenetys tai irtokiekko), ja peli siirtyy järjestelmätilaan. Jos viimeinen tahti ei ole laukaus, peli siirtyy järjestelmätilaan kiekko tallessa.

**Validointi.** `schemaVersion` tunnettu, `id` yksilöllinen, 1–4 tahtia, kaikki solmut verkon sisällä, viisi eri paikkaa eri solmuissa lähdössä, siirtojen pituus sallittu, `pass.from` on tahdin alussa kiekollinen ja `to` eri pelaaja, `skate.by` ja `shoot.by` ovat kiekollisia, `driveNet.by` ei ole kiekollinen, `shoot`-tahdin jälkeen ei tahteja, `faceoff`-tyypillä `faceoffSpot` löytyy kaukalosta.

## Puolustusjärjestelmät (`data/systems/*.json`) – luonnos – odottaa Jerryn hyväksyntää

Vastaa kysymykseen Q-001 (järjestelmän osalta). Säännöt ovat deterministisiä, jotta editori voi näyttää puolustajien haamut (tech-spec.md). Järjestelmä kirjoitetaan **puolustavan joukkueen omasta näkökulmasta** (oma maali x = 0).

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
        "F2": { "node": [5, 1] },
        "F3": { "node": [5, 3] },
        "D1": { "node": [3, 1] },
        "D2": { "node": [3, 3] }
      }
    },
    {
      "when": {},
      "targets": {
        "F1": { "puckOffset": [-1, 0] },
        "F2": { "node": [2, 1] },
        "F3": { "node": [2, 3] },
        "D1": { "node": [1, 1] },
        "D2": { "node": [1, 2] }
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

Järjestelmätiedostot kirjoitetaan, kun Q-001 (skeema) ja Q-003 on vastattu.

| Kenttä | Kuvaus |
| --- | --- |
| `id`, `name` | Kuten kuvioissa |
| `mirrorY` | Jos tosi, säännöt kirjoitetaan kiekon ollessa vasemmalla tai keskellä (y ≤ (width − 1) / 2), ja oikealla puolella käytetään peilikuvaa y → width − 1 − y |
| `rules` | Järjestetty lista. Ensimmäinen sääntö, jonka `when` täsmää, ratkaisee. Viimeisen säännön `when` on `{}` (aina tosi) |
| `when.puckZones` | Lista alueita, joissa kiekko on (puolustajan näkökulmasta). Puuttuu = mikä tahansa |
| `when.puckX`, `when.puckY` | Valinnaiset `[min, max]`-välit kiekon koordinaateille |
| `when.puckState` | Valinnainen: `controlled` tai `loose` |
| `targets` | Rooli → kohde. Kaikille viidelle roolille `F1`, `F2`, `F3` (jäällä oleva hyökkäyskolmikko), `D1`, `D2` (jäällä oleva pakkipari) |
| kohde `node` | Kiinteä solmu `[x, y]` |
| kohde `puckOffset` | Kiekon solmu + `[dx, dy]`, rajattuna verkon sisälle. `[0, 0]` = painostaa kiekollista |

**Roolien jako** (Q-011). Joka tapahtuman jälkeen hyökkääjät järjestetään etäisyyden mukaan kiekkoon: lähin on F1, sitten F2 ja F3. Tasatilanteessa järjestys C, LW, RW. Pakeista lähin on D1. Rooli ei ole pelipaikka, joten sentteri voi olla F2.

**Liikkuminen.** Jokainen puolustaja liikkuu tapahtumaa kohden enintään `plays.maxNodesPerBeat` solmua kohti kohdettaan. Järjestäytyneisyys johdetaan siitä, kuinka moni puolustaja on kohteessaan tai kiekon takana (stats-and-checks.md), ja sen tarkka kaava kuuluu virstanpylvääseen 3.

**Validointi.** Kaikki viisi roolia jokaisessa säännössä, solmut verkon sisällä, viimeinen sääntö kattaa kaiken, `mirrorY`-järjestelmässä säännöt eivät viittaa oikeaan puoliskoon.
