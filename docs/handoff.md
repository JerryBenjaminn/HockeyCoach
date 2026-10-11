# Handoff

Tilanne 2026-10-11 (päivitetty virstanpylvään 4 hyväksynnän jälkeen). Tämä tiedosto auttaa jatkamaan työtä uudessa istunnossa. Päätökset ovat tiedostossa `docs/decisions-log.md` ja kysymykset tiedostossa `docs/questions.md`. Tämä tiedosto ei korvaa niitä.

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
- 841 testiä menee läpi, ja `validate` hyväksyy datan.

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

## Avoimet asiat

Kysymysten sisältö on tiedostossa `docs/questions.md`.

- **Odottavat Jerryn vastausta:** Q-048 ja Q-050 (Wisehockey).
- **Toteutettu oletuksilla:** Q-057, Q-058, Q-059 ja Q-062.
- **Kysymykset veljelle:** Q-060 (ohilaukausten päätyminen), Q-049, Q-059 (1)–(2), Q-040, Q-039, Q-030, Q-014, Q-004 ja Q-003.
- **Myöhemmin:**
  - Q-068: kuljetus vai päätyyn lyönti alivoimalla taitopäätöksenä.
  - Q-070: ottelun jälkeinen kertomusraportti.
  - Q-061: tapahtumaloki Unity-toistoon.
  - Q-042: pitkä kiekko.
  - Q-043, Q-041, Q-032, Q-021, Q-019 ja Q-002.
- **Virstanpylvääseen 5 siirretyt poikkeamat:**
  - syöttöjen paineosuus
  - laukaukset paineen alla
  - laukausyritykset
  - vaihtojen pituus
  - riistojen xG-osuus
  - `adaptive`n vastapelattu aika
  - `strengthDifferencePerPlayer`-arvon kalibrointi

## Seuraavat askeleet järjestyksessä (virstanpylväs 5, D-083)

1. **Voimasuhdejakauma:**
   1. Määritelmätesti: symmetrinen M-8, jossa myös kiekon tasalla olevat puolustajat lasketaan, mitattuna 1000 ottelulla.
   2. Jos ylivoimaisia on yhä yli 15 %, designer analysoi puolustajien paluuliikkeen siirtymissä ja hyökkääjien sijoittumisen suorahyökkäyksessä ja tekee muutosehdotuksen.
   3. Korjauksen jälkeen taktiikkamatriisi ja K4:n porras ajetaan uudelleen. K4:ää saa säätää yhden kerran, jos taktiikkarajat eivät täyty.
2. Muut virstanpylvään 5 kalibroinnit tavoitearvoihin. Jerry täyttää `targets.json`-haarukat.
3. Pitkä kiekko (Q-042) ja `compare`-komento.

## Työtapa

- Agentit ja tiedostojen omistus: `CLAUDE.md`.
  - Agentit palauttavat enintään 25 rivin yhteenvedon.
  - Agentteja ajetaan rinnakkain vain, kun tehtävät ovat oikeasti riippumattomia.
- Taustalla ajettavat agentit eivät voi kirjoittaa raporttitiedostoja `reports/`-kansioon. Se on Claude Coden sisäänrakennettu esto, eikä `.claude/settings.json` ohita sitä.
  - Harness-komennot kirjoittavat raporttinsa itse.
  - Analyysiteksti palautetaan pääistunnolle, joka tallentaa sen.
- Suunnitelma näytetään Jerrylle ennen toteutusta. Muutetaan yksi asia kerrallaan ja mitataan jokaisen muutoksen jälkeen.
- Vain Jerry merkitsee kysymykset ratkaistuiksi. `unity/`-kansioon ei kosketa.
