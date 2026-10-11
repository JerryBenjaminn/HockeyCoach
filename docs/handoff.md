# Handoff

Tilanne 2026-10-11. Tämä tiedosto auttaa jatkamaan työtä uudessa istunnossa. Päätökset ovat tiedostossa `docs/decisions-log.md` ja kysymykset tiedostossa `docs/questions.md`. Tämä tiedosto ei korvaa niitä.

## Missä ollaan

- Virstanpylväät 1–3 on hyväksytty (M3: D-068).
- Kalibrointikierros on tehty (D-069–D-076).
- Virstanpylväs 4 on toteutettu ja pushattu (D-077).
  - Valmentajarajapinta, omat satunnaisvirrat ja aikalisä.
  - AI:t `rotation`, `random`, `randomTactics`, `randomLines`, `rulebased` ja `adaptive`.
  - Kokoonpanot johdetaan parin siemenestä, ja `--mirror` antaa molemmille samat kokoonpanot.
  - `batch`-komento ja taito vs tuuri -raportti.
- Ensimmäinen batch-raportti on tiedostossa `reports/m4-batch5000/batch-report.md`.
  - Ajo: 5000 ottelua per AI-pari, 21 paria, siemen 1.
  - `reports/` ei ole repossa (sääntö 5).
- 734 testiä menee läpi, ja `validate` hyväksyy datan.

## Päätulokset (ensimmäinen batch)

- `rulebased` vs `random` saa 64,9 % pisteistä (95 %:n luottamusväli 63,7–66,1). Tavoite on 75 %.
- Taitoero tulee lähes kokonaan ketjujen hallinnasta.
  - `rulebased` vs `randomLines`: 74,0 %.
  - `rulebased` vs `randomTactics`: 45,4 %.
- Sääntöpohjainen taktiikka häviää satunnaiselle taktiikalle. Suorahyökkäys tuottaa 2,21 xG / 60 min, uudelleenryhmittyminen 1,12 xG / 60 min.
- Viimeistelytuuri selittää 82–90 % maalieron varianssista ja paikkojen luonti 20–25 %.
  - Jako on sama kaikissa AI-pareissa.
  - Joukkue, jolla on enemmän xG:tä, voittaa 56,8 % otteluista.
- Jerry ei ole vielä päättänyt keinoja taitoeron kasvattamiseen (D-077: ei säätöä ennen raporttia).

## Avoimet asiat

Kysymysten sisältö on tiedostossa `docs/questions.md`.

- **Odottavat Jerryn vastausta:** Q-048 ja Q-050 (Jerry tarkistaa Wisehockeysta).
- **Toteutettu oletuksilla, odottaa vahvistusta:** Q-057, Q-058 ja Q-059.
- **Kysymykset veljelle:** Q-049 (paineen määritelmä), Q-059 (1)–(2), Q-040, Q-039, Q-030, Q-014, Q-004 ja Q-003.
- **Myöhemmin päätettävät:** Q-042 (pitkä kiekko, nyt vuorossa), Q-043, Q-041, Q-032, Q-021, Q-019 ja Q-002.
- **Virstanpylvääseen 5 siirretyt poikkeamat** ovat tiedostossa `docs/decisions-log.md`:
  - syöttöjen paineosuus (91,5 %, kun `rulebased` pelaa `rulebased`-AI:ta vastaan)
  - laukaukset paineen alla
  - laukausyritykset
  - vaihtojen pituus
  - riistojen xG-osuus

## Seuraavat askeleet järjestyksessä

1. **Taktiikan vastapeli:** trap vs suorahyökkäys ja uudelleenryhmittyminen vs trap. Tavoitteena on, että oikea taktinen valinta voittaa satunnaisen.
   - Designer tekee speksin, Jerry hyväksyy, programmer toteuttaa, ja sen jälkeen ajetaan uusi batch.
2. **Pitkä kiekko** (Q-042): designerin speksi ja O-2:n poikkeus ensin.
3. **`compare`-komento:** kahden `tuning.json`-tiedoston vertailu.
4. **Virstanpylväs 5:** kalibrointi tavoitearvoihin.
   - Jerry täyttää `targets.json`-haarukat, ja edellä luetellut poikkeamat korjataan.

## Työtapa

- Agentit ja tiedostojen omistus: `CLAUDE.md`. Suunnitelma näytetään Jerrylle ennen toteutusta.
- Muutetaan yksi asia kerrallaan, ja jokaisen muutoksen jälkeen ajetaan 200 ottelun raportti.
- Vain Jerry merkitsee kysymykset ratkaistuiksi. `unity/`-kansioon ei kosketa.
