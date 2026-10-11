# Handoff

Tilanne 2026-10-11 (päivitetty virstanpylvään 5 ensimmäisen kierroksen jälkeen). Tämä tiedosto auttaa jatkamaan työtä uudessa istunnossa. Päätökset ovat tiedostossa `docs/decisions-log.md` ja kysymykset tiedostossa `docs/questions.md`. Tämä tiedosto ei korvaa niitä.

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
- 873 testiä menee läpi, ja `validate` hyväksyy datan.

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

## Virstanpylväs 5: ensimmäinen kierros (voimasuhteet, D-083–D-085)

- **E-006 (D-084):** kiekon tasalla olevia ei lasketa kummallekaan puolelle. Batch 1000 ottelua, rb vs rb: 66,4 / 33,6 / 0,0 % (tavoite 50–75 / 20–40 / 0–15), kaikki haarukoissa.
- **Paluukarvaus (D-085, `transitions.backcheckNodesPerBeat` = 2)** on toteutettu, testejä 873.

  | | E-006 (k = 0) | Paluukarvaus (k = 2) | Tavoite |
  | --- | --- | --- | --- |
  | Alivoima / tasavoima / ylivoima | 66,4 / 33,6 / 0,0 % | **89,4 / 10,6 / 0,0 %** | 50–75 / 20–40 / 0–15 |
  | Kuljetuksen onnistuminen | 63,3 % | 60,3 % | |
  | `rulebased` vs `random` | 78,7 % | 78,9 % | ≥ 75 % |
  | Maalit / ottelu (rb vs rb) | 6,41 | 6,48 | 4–7 |

  - Paluukarvaus ylikorjaa (designerin arvio oli "muutama prosenttiyksikkö"). Syy: puolustaja, joka on riviä kiekon takana, päätyy riviä kiekon ohi ja lasketaan M:ään. Kiekollinen liikkuu kaksi riviä, ja sama toistuu.
  - Oman alueen riistojen jälkeen ylivoimaisia on 0 % sekä ennen että jälkeen.
  - Raportissa on uusi osio, jossa voimasuhteet on eritelty hallinnan alkupaikan mukaan.
- **Taktiikkamatriisi paluukarvauksen kanssa** (`reports/tactic-counterplay/backcheck`):

  | Oma siirtymä | forecheck212 | trap122 |
  | --- | --- | --- |
  | Suorahyökkäys | 50,0 % | 57,2 % |
  | Uudelleenryhmittyminen | 34,7 % (32,8–36,5) | 75,9 % |

  - Rajat täyttyvät: karvausta vastaan +15,4 %-yks., trapia vastaan +18,6 %-yks.
  - Uudelleenryhmittyminen karvausta vastaan jää rajan 35 % alle, mutta luottamusväli sisältää rajan, joten K4:ää ei säädetty (D-082).
  - K4:n porras laskee voimasuhteen mukaan: 3v3 70 %, 1v1 67 %, 1v2 62 %, 1v3 57 %, 1v5 45 %.
- **Uudet kysymykset:**
  - Q-074: paluukarvauksen reunatapaus.
  - Q-075: bugi, peilattu kuvio ohittaa alueelletuontisyötön (0,0–0,1 % kuljetuksista), ei korjattu.

## Seuraavat askeleet

1. **Jerry päättää paluukarvauksen ylikorjauksesta.** Vaihtoehdot:
   - `backcheckNodesPerBeat` = 1
   - kohderivi kiekon tasalle eikä riviä sen ohi
   - hyväksytään ja palataan siihen ansaittujen ylivoimien kanssa (Q-071)
2. **Ansaitut ylivoimat (Q-071):** laiturit kiekollisen edelle, ja nopeusero tai nopea siirtymä ratkaisee.
3. **Muut virstanpylvään 5 kalibroinnit tavoitearvoihin.** Jerry täyttää `targets.json`-haarukat.
4. Pitkä kiekko (Q-042) ja `compare`-komento.

## Työtapa

- Agentit ja tiedostojen omistus: `CLAUDE.md`.
  - Agentit palauttavat enintään 25 rivin yhteenvedon.
  - Agentteja ajetaan rinnakkain vain, kun tehtävät ovat oikeasti riippumattomia.
- Taustalla ajettavat agentit eivät voi kirjoittaa raporttitiedostoja `reports/`-kansioon. Se on Claude Coden sisäänrakennettu esto, eikä `.claude/settings.json` ohita sitä.
  - Harness-komennot kirjoittavat raporttinsa itse.
  - Analyysiteksti palautetaan pääistunnolle, joka tallentaa sen.
- Suunnitelma näytetään Jerrylle ennen toteutusta. Muutetaan yksi asia kerrallaan ja mitataan jokaisen muutoksen jälkeen.
- Vain Jerry merkitsee kysymykset ratkaistuiksi. `unity/`-kansioon ei kosketa.
