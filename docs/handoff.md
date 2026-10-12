# Handoff

Tilanne 2026-10-12 (paluukarvaus pois, Unity-toiston vaiheet 2–5 ja esityskerros D-088 valmiit). Tämä tiedosto auttaa jatkamaan työtä uudessa istunnossa. Päätökset ovat tiedostossa `docs/decisions-log.md` ja kysymykset tiedostossa `docs/questions.md`. Tämä tiedosto ei korvaa niitä.

## Missä ollaan

- Virstanpylväät 1–4 on hyväksytty (M3: D-068, M4: D-083).
- Virstanpylväs 4 lyhyesti:
  - Valmentajarajapinta, omat satunnaisvirrat, aikalisä ja AI:t `rotation`, `random`, `randomTactics`, `randomLines`, `rulebased` ja `adaptive` (D-077).
  - Taktiikan vastapeli (D-078, E-005):
    - K1: karvaus haastaa kokoamisen omassa päässä.
    - K2: `rulebased` valitsee siirtymän vastustajan järjestelmän mukaan.
    - K3: taktiikkamatriisi, `batch --tactic-matrix`.
    - K4: voimasuhde- ja karvaajamuokkaajat (M-9).
  - M-8:n voimasuhteen laskenta korjattu (D-080).
    - N = kiekollinen ja kiekon tasalla tai sen edellä olevat.
    - M = kiekon ja oman maalin välissä olevat.
  - Uusi mittari: voimasuhdejakauma Wisehockeyn datasta, `targets.json` (D-081).
  - `adaptive` valitsee järjestelmänsä vastustajan siirtymätyylin mukaan julkisesta lokista (A-T2, D-082).
- 878 testiä menee läpi, ja `validate` hyväksyy datan.

## Päätulokset (batch 5000 / pari, siemen 1, `reports/d082-batch5000`)

`reports/` ei ole repossa (sääntö 5).

- **Taitotavoite täyttyy:** `rulebased` vs `random` 79,7 % (95 %:n luottamusväli 78,7–80,7). Tavoite on vähintään 75 %.
- **`adaptive`:**
  - vs `rulebased` 43,3 %
  - vs `random` 75,1 %
  - vs `randomLines` 66,2 %
  - vs `randomTactics` 56,8 %
  - Vastapelattu aika `rulebased`ia vastaan on 71 %. Se hyväksyttiin, ja se hiotaan virstanpylväässä 5.
- **Taktiikkamatriisi M-8:n korjauksen jälkeen** (`reports/tactic-counterplay/m8fix`, pisteet):

  | Oma siirtymä | forecheck212 | trap122 |
  | --- | --- | --- |
  | Suorahyökkäys | 50,0 % | 54,1 % |
  | Uudelleenryhmittyminen | 33,6 % | 73,4 % |

  Uudelleenryhmittyminen karvausta vastaan jää alle rajan 35 %, mutta luottamusväli 31,8–35,4 sisältää rajan, joten K4:ää ei säädetty (D-082).
- **Viimeistelytuuri** selittää noin 90 % maalieron varianssista ja paikkojen luonti noin 27 %.
- **Voimasuhdejakauma** (onnistuneet alueelletuonnit, `reports/d081-strength`):

  | | Simulaatio | Tavoite |
  | --- | --- | --- |
  | Alivoima | 22 % | 50–75 % |
  | Tasavoima | 28 % | 20–40 % |
  | Ylivoima | 49 % | 0–15 % |

  Syöttämällä tehdyissä alueelletuonneissa N on aina 3, koska syöttötuloja tulee vain kuviosta `middleDriveWidePass`.

## Virstanpylväs 5: kalibrointikierros (voimasuhteet, D-083–D-087)

- **E-006 (D-084):** kiekon tasalla olevia ei lasketa kummallekaan puolelle.
- **Paluukarvaus (D-085–D-087):** vastustajan suorahyökkäyksessä kiekon väärällä puolella oleva puolustaja luistelee omaa kaistaansa kohti omaa maalia, enintään `transitions.backcheckNodesPerBeat` solmua per toiminto.
- Mittaukset (batch 1000, `rulebased` vs `rulebased`):

  | Tila | Alivoima / tasavoima / ylivoima | `rulebased` vs `random` | Maalit |
  | --- | --- | --- | --- |
  | Tavoite | 50–75 / 20–40 / 0–15 % | ≥ 75 % | 4–7 |
  | E-006 ilman paluukarvausta (k = 0) | 66,4 / 33,6 / 0,0 ✅ | 78,7 % | 6,41 |
  | Paluukarvaus, kohde riviä kiekon ohi | 89,4 / 10,6 / 0,0 ❌ | 78,9 % | 6,48 |
  | **Paluukarvaus, kohde kiekon tasalla (nykyinen koodi, 4530105)** | **54,2 / 13,0 / 32,8 ❌** | 79,7 % | 6,35 |

  - 11 × 5 -verkossa pelaajalla on vain kolme sijaintia suhteessa kiekkoon (takana, tasalla, edellä), joten paluukarvauksen kohderivi kääntää jakauman ääripäästä toiseen.
- **Taktiikkamatriisi** (viimeksi ajettu riviä kiekon ohi -versiolla): suorahyökkäys 50,0 / 57,2 %, uudelleenryhmittyminen 34,7 / 75,9 % (forecheck212 / trap122).
  - Rajat täyttyvät.
  - Uudelleenryhmittyminen karvausta vastaan jää rajan 35 % alle, mutta luottamusväli sisältää rajan, joten K4:ää ei ole säädetty.
- **Q-074 ja Q-075** on ratkaistu: Q-075:n peilatun kuvion bugi on korjattu.
- 878 testiä menee läpi.

## Unity-toisto, vaihe 1 (hyväksytty, D-087)

2D-toisto ylhäältä lukee vain `match --json` -viennin. Unity ei viittaa Simiin, ja JSON luetaan Newtonsoft-paketilla. Hyväksymiskriteerit siemenellä 42:

1. Kaikki 12 pelaajaa näkyvät jokaisessa tapahtumassa.
2. Tulos ja maalien ajat vastaavat tekstitulostetta.
3. Syötöt ja laukaukset alkavat tekijästä ja päättyvät `placement`-sijaintiin.
4. Aloitukset tapahtuvat aloituspisteillä.
5. Blokit pysähtyvät blokkaajaan, ja maalit menevät oikeaan verkkoon.
6. Vaihdot ovat oikein.
7. Kello ei kulje taaksepäin.
8. Askellus on deterministinen.
9. 8× nopeudella koko ottelu menee läpi muutamassa minuutissa.

Vaiheet:

1. ✅ Jerry vastasi Q-076:een ja muutti D-054:ää (D-087).
2. **Harness:** `match --json` -vientiin otsake.
   - Sisältö: `formatVersion`, erän pituus, erät, kokoonpano (id, joukkue, pelipaikka, ketju tai pari, maalivahti) ja kaukalodata `rink.json`:sta (mitat metreinä, solmut, maalisolmut).
   - Lisäksi testit.
   - Tapahtumaskeema ei muutu.
3. **Core:** `unity/HockeyCoach/Assets/HockeyCoach/Replay/Core/`.
   - asmdef `HockeyCoach.Replay.Core`, `noEngineReferences`, C# 9.
   - Sisältää DTO:t, `ReplayJsonReader`, `RinkMapping`, `TimelineBuilder` ja `ReplayCursor`.
   - Testit: `tests/HockeyCoach.Replay.Tests` (net10.0, xUnit) kääntää Core-tiedostot, generoi siemenen 42 ottelun prosessissa ja tarkistaa hyväksymiskriteerit. JSON-fixturea ei commitoida.
4. **Newtonsoft-paketti** (`com.unity.nuget.newtonsoft-json`) tiedostoon `Packages/manifest.json` Jerryn erillisellä hyväksynnällä. Jerry commitoi `packages-lock.json`:n.
5. **View:** `Replay/View/` (`ReplayBootstrap`, `RinkView`, `PlayerDiscView`, `PuckView`, `HudView`, `ReplayInput`) ja `Tests/EditMode/`-savutestit.
   - Käännös ja EditMode-testit ajetaan batchmodessa, kun Editor on kiinni (tarkista ensin `Temp/UnityLockfile`).
6. **Jerry Editorissa:**
   - import, joka luo .meta-tiedostot
   - scene `Assets/HockeyCoach/Scenes/Replay.unity` ja `ReplayBootstrap`
   - Test Runner ja silmämääräinen tarkistus
   - scenen ja .meta-tiedostojen commit
7. Agentit korjaavat Jerryn havainnot.

**Agenttien sallitut polut** (CLAUDE.md, sääntö 6):

| Oikeus | Polut |
| --- | --- |
| Saa muokata ja commitoida | Vain .cs-, .asmdef- ja .asmref-tiedostot polussa `unity/HockeyCoach/Assets/HockeyCoach/**` sekä Unityn jo luomat .meta-tiedostot niille. |
| Erillisellä hyväksynnällä | `Packages/manifest.json` |
| Aina kielletty | ProjectSettings (myös Jerryn muokkaama `QualitySettings.asset`), Library, Temp, Logs, UserSettings, csproj/sln, packages-lock, scenet, prefabit ja assetit. |

- Stageen vain nimetyt polut.

## Seuraavat askeleet järjestyksessä

1. ✅ **Paluukarvaus pois** (334bc7c): `backcheckNodesPerBeat` = 0. Batch 1000 (`reports/d087-backcheck-off`): voimasuhteet 66,5 / 33,5 / 0,0 % ✅, `rulebased` vs `random` 78,3 % (76,0–80,6) ✅, maalit 6,46 ✅. Paluukarvaustestit asettavat k = 2 itse.
2. ✅ **Unity-toiston vaiheet 2–5:**
   - Vaihe 2 (607a96d): `match --json` -otsake (`formatVersion` 1, erät, kokoonpano, kaukalo ja metrit Q-077:n oletuksella).
   - Vaihe 3 (99fb29f, 4da6b99): Core ja `tests/HockeyCoach.Replay.Tests`. C# 9 pakotetaan projektilla `tests/HockeyCoach.Replay.Core` (netstandard2.1). Kriteerit 1–9 menevät läpi siemenellä 42.
   - Vaihe 4 (fd5a547): Newtonsoft 3.2.2 `manifest.json`:iin. `packages-lock.json` on Jerryn commitoitava.
   - Vaihe 5 (605b0b6, a73f8b5, ddbfe60): View ja EditMode-savutestit, 10/10 batchmodessa. Testejä: Sim 888, Replay 93.
3. **Jerry Editorissa (vaihe 6):**
   - Import. Unity on jo luonut .meta-tiedostot batchmode-ajossa (commitoimatta).
   - Scene `Assets/HockeyCoach/Scenes/Replay.unity`: tyhjä GameObject ja `ReplayBootstrap`.
   - JSON: `dotnet run --project tools/HockeyCoach.Harness -- match --home rulebased --away random --seed 42 --json > E:/Temp/m42.json`. Polku kenttään "Match Json Path".
   - Ohjaimet: välilyönti toisto/tauko, nuolet vasen/oikea edellinen/seuraava tapahtuma, ylös/alas nopeus 1–16×, Home alkuun.
   - Test Runner ja silmämääräinen tarkistus. Commitoi scene, .meta-tiedostot ja `packages-lock.json`.
4. ✅ **Esityskerros (D-088):** spec ja `data/replay.json` (c9c1905), otsake v2 aikakustannuksineen (2f836fb), asetusten lukija (118d405), esitysaika ja toisto-ohjain (70a86cc), luistelijat (2350034), kiekko ja maali verkossa (c7ff26b), View (d4353b4). Testejä: Replay 340, Sim 890, EditMode 10/10.
   - 45 s vaihto 1×-nopeudella: siemen 42 17,1 s; `rulebased` vs `rulebased` (siemenet 1 ja 7) 22,5 s, yli tavoitteen 15–20 s (Q-082).
   - Avoimet Jerrylle: Q-082 (kiekon alaraja), Q-083 (ajoituksen tulkinnat), Q-084 (liikekerroksen havainnot: erottelun hypyt, myöhästyvät toimijat), Q-081 (tapahtumatiheys vs Wisehockey, virstanpylväs 5).
   - Toisto vaatii `formatVersion` 2 -viennin: vanha JSON näyttää uudelleenvientivirheen.
5. Agentit korjaavat Jerryn havainnot (vaihe 7). Avoimet: Q-078 (nopeus ja saman sekunnin tapahtumat), Q-079 (maalin kiekko), Q-080 (samalla solmulla olevat).
6. **Myöhemmin:**
   - Q-071: ansaitut ylivoimat. Paluukarvaus palataan säätämään samassa yhteydessä.
   - Muut virstanpylvään 5 kalibroinnit.
   - Pitkä kiekko (Q-042).
   - `compare`-komento.

## Avoimet asiat

- **Kysymykset veljelle:**
  - Q-073: karvaajien reitti, eli palaavatko hyökkääjät keskikaistaa ja pakit omia kaistojaan.
  - Q-060: minne ohilaukaukset päätyvät.
  - Q-049, Q-059, Q-040, Q-039, Q-030, Q-014, Q-004 ja Q-003.
- **Myöhemmin:**
  - Q-071: ansaitut ylivoimat.
  - Q-068: kuljetus vai päätyyn lyönti alivoimalla.
  - Q-070: kertomusraportti.
  - Q-042, Q-043, Q-041, Q-032, Q-021, Q-019 ja Q-002.
- **Odottavat Jerryä:** Q-048 ja Q-050 (Wisehockey).
- **Oletuksilla toteutetut:** Q-057, Q-058, Q-059 ja Q-062.

## Työtapa

- Agentit ja tiedostojen omistus: `CLAUDE.md`.
  - Agentit palauttavat enintään 25 rivin yhteenvedon.
  - Agentteja ajetaan rinnakkain vain, kun tehtävät ovat oikeasti riippumattomia.
- Taustalla ajettavat agentit eivät voi kirjoittaa raporttitiedostoja `reports/`-kansioon. Se on Claude Coden sisäänrakennettu esto, eikä `.claude/settings.json` ohita sitä.
  - Harness-komennot kirjoittavat raporttinsa itse.
  - Analyysiteksti palautetaan pääistunnolle, joka tallentaa sen.
- Suunnitelma näytetään Jerrylle ennen toteutusta. Muutetaan yksi asia kerrallaan ja mitataan jokaisen muutoksen jälkeen.
- Vain Jerry merkitsee kysymykset ratkaistuiksi. `unity/`-kansiossa vain sallitut polut (CLAUDE.md, sääntö 6).
