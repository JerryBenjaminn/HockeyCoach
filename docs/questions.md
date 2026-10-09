# Avoimet kysymykset

Agentit kirjaavat tänne kysymykset, joihin dokumentit eivät vastaa. Jerry vastaa ja merkitsee kysymyksen ratkaistuksi. Ratkaistut siirretään alas.

Kirjaa uusin ylimmäksi. Muoto:

<!--
### Q-001 · YYYY-MM-DD · kirjaaja: programmer / game-designer
**Kysymys:** mitä pitää päättää.
**Konteksti:** mihin tehtävään liittyy, mikä dokumentin kohta on epäselvä.
**Ehdotettu oletus:** mitä käytetään sillä välin (jos jotain).
**Vastaus:** (Jerry täyttää)
-->

## Avoimet

### Q-013 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** miten lasketaan tarkistus, jonka toisella puolella ei ole statseja (blokki: hyökkääjä "–", rebound: hyökkääjä "–")?
**Konteksti:** `docs/stats-and-checks.md`, Tarkistukset. Painot summautuvat 1:een kummallakin puolella (D-017), mutta näissä kahdessa toinen puoli on tyhjä.
**Ehdotettu oletus:** tyhjä puoli saa arvon `stats.neutralValue` (10,5, asteikon keskikohta), jolloin p0 vastaa keskitasoista puolustajaa tai maalivahtia. Lataaja sallii tyhjän puolen vain tarkistuksissa `block` ja `rebound`. Vaihtoehto: laukaisijan stat (esim. Laukaisutarkkuus blokissa, Laukaisuvoima reboundissa) hyökkääjäpuolelle.

### Q-012 · 2026-10-09 · kirjaaja: game-designer
**Kysymys veljelle:** mitkä alueet lasketaan slotiksi ja mitkä puolustusjärjestelmät kannattaa toteuttaa ensin?
**Konteksti:** `data/rink.json` merkitsee slotiksi (`isSlot`) hyökkäyspään keskikaistan kolme solmua: maalin edusta, matala slotti ja korkea slotti. `docs/data-schema.md` kertoo järjestelmien luonnosskeeman. Liittyy Q-003:een.
**Ehdotettu oletus:** slotti on keskikaista aloituspisteiden välissä maalin edestä ympyröiden yläreunaan. Ensimmäiset järjestelmät ovat tunnettuja ja toisistaan selvästi erottuvia: karvaus 2-1-2, trap 1-2-2 ja omassa päässä aluepuolustus (box + 1); mies miestä -puolustus myöhemmin. Veljeltä kysytään, ovatko nämä oikeat kolme ja mitä niiden heikkoudet ovat (vision.md: jokainen järjestelmä häviää jollekin hyökkäystyylille).

### Q-011 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** jaetaanko puolustusjärjestelmän roolit (F1 painostaja, F2, F3, D1, D2) etäisyyden mukaan kiekkoon vai kiinteästi pelipaikan mukaan?
**Konteksti:** `docs/data-schema.md`, Puolustusjärjestelmät. Oikeassa kiekossa ensimmäinen karvaaja on se, joka on lähimpänä, ei aina sentteri.
**Ehdotettu oletus:** etäisyyden mukaan joka tapahtuman jälkeen. Hyökkääjistä lähin on F1, tasatilanteessa järjestys C, LW, RW. Pakeista lähin on D1.

### Q-010 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** missä järjestyksessä laukauksen lopputulokset (blokattu, ohi, torjuttu, maali) ratkaistaan?
**Konteksti:** tapahtumaskeeman laukauksella on neljä lopputulosta, mutta tarkistustaulukossa on vain blokki ja laukaus. Ohi menneille laukauksille ei ole sääntöä.
**Ehdotettu oletus:** blokki (`checks.block`) → maalia kohti vai ohi (`checks.shot.onTargetShare`, alustavasti 0,55, ei statseja) → maalitarkistus (`checks.shot`). Myöhemmin Laukaisutarkkuus voi vaikuttaa ohilaukauksiin, mutta se on uusi mekaniikka ja vaatii hyväksynnän.

### Q-009 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** mihin xG-mittakaava sidotaan, jotta ottelussa syntyy 4–7 maalia, ja mitä analyysityökalun xG tarkalleen mittaa?
**Konteksti:** vision.md (4–7 maalia), `docs/stats-and-checks.md` (xG per joukkue 1,2–5,0, yksittäinen laukaus tyypillisesti 0,003–0,05), `tuning.json` `checks.shot.baseXg`. Kaksi ongelmaa:
1. Todennäköisyyden alaraja 0,02 (D-014) nostaa kaukolaukausten xG:n (alustavasti 0,005–0,01) 0,02:een. Pitäisikö laukauksella olla oma alaraja, vai hyväksytäänkö tämä?
2. Lasketaanko xG laukausyritystä kohden (myös blokatut ja ohi menneet) vai maalia kohti mennyttä laukausta kohden? Tämä ratkaisee, onko `baseXg` maalitarkistuksen p0 sellaisenaan vai jaetaanko se läpimenon ja maalia kohti -osuuden todennäköisyydellä.
**Kysymys veljelle (liittyy Q-004):** mitä laukauksia analyysityökalun xG kattaa (kaikki yritykset, blokkaamattomat vai maalia kohti menneet)?
**Ehdotettu oletus:** `baseXg` on maalin todennäköisyys maalia kohti menneestä laukauksesta, raportin xG on yrityksen kokonaistodennäköisyys (läpi × maalia kohti × maali). Alaraja 0,02 koskee myös laukauksia, kunnes Jerry päättää toisin. Mittakaava kalibroidaan maalimäärään 4–7.

### Q-008 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** ovatko kaikki muokkaajat (järjestäytyneisyys, paine, energia, poikittaissyöttö jne.) logit-yksiköitä, jotka lisätään kaavan summaan M?
**Konteksti:** `docs/stats-and-checks.md` sanoo vain, että M on muokkaajien summa. Yksikkö ratkaisee, miten `tuning.json`:n arvot luetaan.
**Ehdotettu oletus:** kyllä, logit. Esimerkki: syötön p0 0,85 ja paineen muokkaaja −0,9 antavat n. 70 %, mikä vastaa analyysin 13–15 %-yksikön pudotusta. Tilaan sidotut muokkaajat skaalautuvat lineaarisesti (`docs/data-schema.md`, Yleiset käytännöt). Tämä on dokumentoitu oletukseksi `data-schema.md`:hen.

### Q-007 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** onko irtokiekon perusjakauma tasaväkisille pelaajille 39 / 22 / 39 (voitto / ei voittajaa / häviö)?
**Konteksti:** analyysin jakauma 36 / 22 / 41 on yhden joukkueen luku, eikä se sovi perustasoksi, koska tasaväkisten jakauman pitää olla symmetrinen.
**Ehdotettu oletus:** `noWinnerShare` 0,22 analyysista, loput 0,78 jaetaan logistisella tarkistuksella, jonka p0 on 0,5. Statsiero siirtää osuutta voiton ja häviön välillä, "ei voittajaa" pysyy 22 %:ssa.

### Q-006 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** kuinka paljon aikaa kuluu, kun pelaajat asettuvat kuvion lähtösolmuihin (setup) tai kokoavat hyökkäyksen siirtymässä (regroup)?
**Konteksti:** kuvion lähtöasetelma ei yleensä vastaa pelaajien sijaintia edellisen tapahtuman jälkeen. Siirtymäohje "kokoaminen" antaa puolustuksen järjestäytyä (stats-and-checks.md), mutta aikaa ei ole määritelty.
**Ehdotettu oletus:** kiinteät ajat `time.setupSeconds` 6 ja `time.regroupSeconds` 8 (paikanpitäjiä). Aika palauttaa puolustuksen järjestäytyneisyyttä normaalisti tapahtumien tapaan.

### Q-005 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** tarkoittaako "aja maalille" kiekollista vai kiekotonta pelaajaa?
**Konteksti:** vision.md, kuvion neljä toimintoa. Kiekollisena se on käytännössä kuljetus maalille ja laukaus tai harhautus. Kiekottomana se on maalin eteen meno maskiin, ohjaukseen ja reboundiin.
**Ehdotettu oletus:** kiekoton. Pelaaja siirtyy vastustajan maalin eteen, antaa laukaukselle maskibonuksen (`checks.shot.modifiers.screen`) ja osallistuu reboundiin. Kiekollinen maalille ajo kirjoitetaan `skate`-toimintona kohti maalia ja sitä seuraavana laukauksena.

### Q-004 · 2026-10-09 · kirjaaja: Jerry
**Kysymys veljelle:** millä xG-rajoilla otteluanalyysi jakaa paikat huippu-, hyviin ja kohtalaisiin?
**Konteksti:** paikkaluokat (`docs/stats-and-checks.md`, tapahtumaskeema ja tavoitearvot).

### Q-003 · 2026-10-09 · kirjaaja: Jerry
**Kysymys veljelle:** puolustusjärjestelmien säännöt alueella pelaamisessa: mikä puolustuskuvio vastaa mitäkin hyökkäyskuviota?
**Konteksti:** `data/systems/`, kuvio × järjestelmä -matriisi.

### Q-002 · 2026-10-09 · kirjaaja: Jerry
**Kysymys:** kaukalon solmuverkon tiheys: riittääkö 9 × 5?
**Ehdotettu oletus:** 9 × 5, kunnes kuvioiden kirjoittaminen osoittaa toisin.

### Q-001 · 2026-10-09 · kirjaaja: Jerry
**Kysymys:** kuvion ja puolustusjärjestelmän tarkka JSON-skeema.
**Konteksti:** kirjoitetaan ennen virstanpylvästä 2.
**Huom. (game-designer, 2026-10-09):** luonnosvastaus on `docs/data-schema.md`:ssä, osiot "Kuviot" ja "Puolustusjärjestelmät" (tila: luonnos – odottaa Jerryn hyväksyntää). Kysymys pysyy auki, kunnes Jerry on katsonut skeeman.

## Ratkaistut
