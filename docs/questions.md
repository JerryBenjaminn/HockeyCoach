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

### Q-027 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saako syöttölinja tai luistelureitti kulkea maalisolmun kautta (esim. maalin takaa (10, 1) slottiin (8, 3), linjan solmut (10, 1), (9, 2), (8, 3))?
**Konteksti:** `docs/data-schema.md`, Määritelmät M-1 (luonnos). E-002:n hyväksyntä jätti "maali esteenä" -säännön erillisen päätöksen varaan. Oikeassa kiekossa maalin takaa syötetään slottiin maalin ohi, ei läpi, mutta 22 jalan verkossa ero ei näy solmuina.
**Ehdotettu oletus:** sallitaan. Maalisolmu kuuluu linjaan, mutta siellä ei ole puolustajaa, joten se ei vaikuta riskiin. Laukauslinjasta maalisolmu jätetään pois (M-5).

### Q-026 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** lisätäänkö laukaustapahtumaan kenttä "paineen alla (kyllä/ei)"?
**Konteksti:** tavoitearvoissa on mittari "Laukaukset paineen alla (osuus)" (analyysi 68 % / 76 %), mutta tapahtumaskeeman Laukaus-tapahtumassa ei ole paine-kenttää (vain Syötössä). Raportti ei voi laskea mittaria ilman sitä. D-045 kieltää uudet tapahtumatyypit, mutta tämä on kenttä olemassa olevaan tapahtumaan, joten se vaatii `stats-and-checks.md`:n tapahtumaskeeman päivityksen.
**Ehdotettu oletus:** kyllä, kenttä lisätään. Määritelmä sama kuin syötöllä (`data-schema.md`, M-7). Virstanpylväässä 2 se vain kirjataan, eikä se muuta laukauksen todennäköisyyksiä.

### Q-021 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** kertyykö kemiaa myös hyökkäyskolmikon ja pakkiparin välille, vai vain yksikön sisällä?
**Konteksti:** D-023 (kolmikot ja parit erikseen), `tuning.json` `chemistry`. Tarvitaan virstanpylväässä 3.
**Ehdotettu oletus:** vain yksikön sisällä.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.

### Q-020 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** lasketaanko väärän puolen miinus kerran puolta kohden vai jokaisesta väärällä puolella pelaavasta pelaajasta?
**Konteksti:** D-024, `tuning.json` `positions.offSideCheckModifier`.
**Ehdotettu oletus:** kerran puolta kohden.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.

### Q-019 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saavatko muut paikkavaihdot miinuksen (esim. sentteri laidassa, hyökkääjä pakkina)?
**Konteksti:** D-024 määrittelee vain väärän puolen (LW↔RW, LD↔RD).
**Ehdotettu oletus:** ei miinusta, kunnes päätetään.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.

### Q-015 · 2026-10-09 · kirjaaja: game-designer (siirretty Q-012:sta)
**Kysymys veljelle:** mitkä alueet lasketaan slotiksi?
**Konteksti:** `data/rink.json` merkitsee slotiksi (`isSlot`) hyökkäyspään keskikaistan solmut. Q-012:n järjestelmäosa on ratkaistu (D-021).
**Ehdotettu oletus:** slotti on keskikaista aloituspisteiden välissä maalin edestä ympyröiden yläreunaan.

### Q-014 · 2026-10-09 · kirjaaja: game-designer (siirretty Q-009:stä)
**Kysymys:** lasketaanko xG laukausyritystä kohden (myös blokatut ja ohi menneet) vai maalia kohti mennyttä laukausta kohden, ja mihin xG-mittakaava sidotaan, jotta ottelussa syntyy 4–7 maalia?
**Konteksti:** `tuning.json` `checks.shot.baseXg`. Ratkaisee, onko `baseXg` maalitarkistuksen p0 sellaisenaan. Q-009:n rajakysymys on ratkaistu (D-014:n tarkennus).
**Kysymys veljelle (liittyy Q-004):** mitä laukauksia analyysityökalun xG kattaa (kaikki yritykset, blokkaamattomat vai maalia kohti menneet)?
**Ehdotettu oletus:** `baseXg` on maalin todennäköisyys maalia kohti menneestä laukauksesta, raportin xG on yrityksen kokonaistodennäköisyys (läpi × maalia kohti × maali). Mittakaava kalibroidaan maalimäärään 4–7.

### Q-011 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** jaetaanko puolustusjärjestelmän roolit (F1 painostaja, F2, F3, D1, D2) etäisyyden mukaan kiekkoon vai kiinteästi pelipaikan mukaan?
**Konteksti:** `docs/data-schema.md`, Puolustusjärjestelmät. Oikeassa kiekossa ensimmäinen karvaaja on se, joka on lähimpänä, ei aina sentteri.
**Ehdotettu oletus:** etäisyyden mukaan joka tapahtuman jälkeen. Hyökkääjistä lähin on F1, tasatilanteessa järjestys C, LW, RW. Pakeista lähin on D1.

### Q-010 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** missä järjestyksessä laukauksen lopputulokset (blokattu, ohi, torjuttu, maali) ratkaistaan?
**Konteksti:** tapahtumaskeeman laukauksella on neljä lopputulosta, mutta tarkistustaulukossa on vain blokki ja laukaus. Ohi menneille laukauksille ei ole sääntöä.
**Ehdotettu oletus:** blokki (`checks.block`) → maalia kohti vai ohi (`checks.shot.onTargetShare`, alustavasti 0,55, ei statseja) → maalitarkistus (`checks.shot`). Myöhemmin Laukaisutarkkuus voi vaikuttaa ohilaukauksiin, mutta se on uusi mekaniikka ja vaatii hyväksynnän.

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

### Q-004 · 2026-10-09 · kirjaaja: Jerry
**Kysymys veljelle:** millä xG-rajoilla otteluanalyysi jakaa paikat huippu-, hyviin ja kohtalaisiin?
**Konteksti:** paikkaluokat (`docs/stats-and-checks.md`, tapahtumaskeema ja tavoitearvot).

### Q-003 · 2026-10-09 · kirjaaja: Jerry
**Kysymys veljelle:** puolustusjärjestelmien säännöt alueella pelaamisessa: mikä puolustuskuvio vastaa mitäkin hyökkäyskuviota?
**Konteksti:** `data/systems/`, kuvio × järjestelmä -matriisi.

### Q-002 · 2026-10-09 · kirjaaja: Jerry
**Kysymys:** kaukalon solmuverkon tiheys: riittääkö 9 × 5?
**Ehdotettu oletus:** 9 × 5, kunnes kuvioiden kirjoittaminen osoittaa toisin.
**Huom. (2026-10-09):** pituus ratkaistu: E-002 hyväksytty, verkko 11 × 5 (D-027). Leveys arvioidaan kuvioiden kirjoittamisen jälkeen.

## Ratkaistut

### Q-001 · 2026-10-09 · kirjaaja: Jerry
**Kysymys:** kuvion ja puolustusjärjestelmän tarkka JSON-skeema.
**Konteksti:** kirjoitetaan ennen virstanpylvästä 2.
**Huom. (game-designer, 2026-10-09):** luonnosvastaus on `docs/data-schema.md`:ssä, osiot "Kuviot" ja "Puolustusjärjestelmät" (tila: luonnos – odottaa Jerryn hyväksyntää). Kysymys pysyy auki, kunnes Jerry on katsonut skeeman.
**Vastaus (Jerry, 2026-10-09):** hyväksytty ehdoin (D-034): toiminnot skate, pass, shoot, driveNet, dump; kuviot viittaavat pelipaikkoihin; saman kuvion voi pelata peilattuna kummallakin laidalla; järjestelmien säännöt deterministisiä ja viimeinen sääntö varasääntö. Skeema korjataan ehtojen mukaiseksi ennen virstanpylvästä 2.

### Q-025 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saako kenttäpelaajan kohde (kuviossa tai järjestelmässä) olla maalisolmussa (1,2) tai (9,2)?
**Konteksti:** 11 × 5 -verkossa maalisolmu on myös crease-solmu, johon `driveNet` vie pelaajan. Esimerkkijärjestelmässä trap122 kaksi kohdetta osuu omaan maalisolmuun. Liittyy E-002:n mainitsemaan "maali esteenä" -lisäsääntöön, joka on uusi mekaniikka.
**Ehdotettu oletus:** sallitaan, kunnes päätetään.
**Vastaus (Jerry, 2026-10-09):** ei. Kenttäpelaajan kohde ei saa olla maalisolmu. `driveNet` vie maalin eteen tai slotin solmuun, maalisolmu on vain laukauksen kohde. Poikkeus: maalivahti. Kirjattu: D-033.

### Q-024 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saako kuvion `moves` siirtää kiekollista pelaajaa, vai liikkuuko kiekollinen vain `skate`-toiminnolla?
**Konteksti:** Q-001, kuvioskeema (`docs/data-schema.md`).
**Ehdotettu oletus:** kiekollinen liikkuu vain `skate`-toiminnolla.
**Vastaus (Jerry, 2026-10-09):** kiekollinen pelaaja liikkuu vain tahdin toiminnolla `skate`, ei siirroilla. Muut liikkuvat siirroilla. Kirjattu: D-032.


### Q-023 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** pitääkö lataajan hylätä `checks.shot.baseXg`-arvo, joka on laukauksen omien rajojen (`checks.shot.minProbability`–`maxProbability`) ulkopuolella?
**Konteksti:** `docs/data-schema.md`, tuning.json → Validointi. Jos esim. `baseXg.crease` olisi yli `maxProbability`:n, tasaväkisen laukauksen maalitodennäköisyys ei olisi `baseXg` vaan raja. Nykydata on rajojen sisällä.
**Ehdotettu oletus:** ei hylätä (sääntöä ei ole skeemassa). Lataaja tarkistaa vain, että `baseXg` on välillä (0, 1).
**Vastaus (Jerry, 2026-10-09):** kyllä. `baseXg` hylätään validoinnissa, jos se on välin 0–1 ulkopuolella tai yli laukausten ylärajan. Kirjattu: D-029.

### Q-022 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** pitääkö rink.json-validoinnin vaatia, että solmun `zone` vastaa `zones`-listan x-väliä (esim. solmu x = 4 on `neutral`)?
**Konteksti:** `docs/data-schema.md`, rink.json → Validointi vaatii vain, että solmun `zone` löytyy `zones`-listasta ja että jokainen x kuuluu tasan yhteen alueeseen. Ristiriitainen solmu menisi nyt läpi. Nykydata on johdonmukainen.
**Ehdotettu oletus:** ei vielä vaadita. Jos vastaus on kyllä, lisään tarkistuksen `RinkValidator`iin.
**Vastaus (Jerry, 2026-10-09):** kyllä. Solmun vyöhyke lasketaan `zones`-osion x-väleistä eikä sitä tallenneta solmuun. Kirjattu: D-028.

### Q-018 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** tarvitaanko vasen ja oikea laitahyökkääjä erikseen (koodissa Center / Winger / Defenseman, kuvioissa LW ja RW)?
**Vastaus (Jerry, 2026-10-09):** pelipaikat C, LW, RW, LD, RD. Pelaajalla on ensisijainen pelipaikka. Väärällä puolella pelaamisen miinus paikkamerkkinä `tuning.json`:iin. Kirjattu: D-024.

### Q-017 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** onko ketju viisi pelaajaa (3 hyökkääjää + 2 pakkia) vai hyökkäyskolmikko ja erillinen pakkipari?
**Vastaus (Jerry, 2026-10-09):** hyökkäyskolmikot ja pakkiparit erikseen, ne kiertävät eri tahtiin. Kirjattu: D-023.

### Q-016 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** mihin `Sim.Config` sijoittuu moduulijärjestyksessä? Painot viittaavat Modelin stat-tyyppeihin.
**Vastaus (Jerry, 2026-10-09):** heti `Sim.Model`-moduulin yläpuolelle. Kirjattu: D-022.

### Q-013 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** miten lasketaan tarkistus, jonka toisella puolella ei ole statseja (blokki: hyökkääjä "–", rebound: hyökkääjä "–")?
**Konteksti:** `docs/stats-and-checks.md`, Tarkistukset. Painot summautuvat 1:een kummallakin puolella (D-017), mutta näissä kahdessa toinen puoli on tyhjä.
**Ehdotettu oletus:** tyhjä puoli saa arvon `stats.neutralValue` (10,5, asteikon keskikohta), jolloin p0 vastaa keskitasoista puolustajaa tai maalivahtia. Lataaja sallii tyhjän puolen vain tarkistuksissa `block` ja `rebound`. Vaihtoehto: laukaisijan stat (esim. Laukaisutarkkuus blokissa, Laukaisuvoima reboundissa) hyökkääjäpuolelle.
**Vastaus (Jerry, 2026-10-09):** ei poikkeusta D-017:ään. Lisätään erillinen yksipuolinen tarkistus: yhden osapuolen painotettu arvo vastaan referenssiarvo (oletus 10,5, `tuning.json`:ssa). D-017 koskee vain kaksipuolisia tarkistuksia. Blokki ja rebound ovat yksipuolisia. Kirjattu: D-019.

### Q-012 · 2026-10-09 · kirjaaja: game-designer
**Kysymys veljelle:** mitkä alueet lasketaan slotiksi ja mitkä puolustusjärjestelmät kannattaa toteuttaa ensin?
**Konteksti:** `data/rink.json` merkitsee slotiksi (`isSlot`) hyökkäyspään keskikaistan kolme solmua: maalin edusta, matala slotti ja korkea slotti. `docs/data-schema.md` kertoo järjestelmien luonnosskeeman. Liittyy Q-003:een.
**Ehdotettu oletus:** slotti on keskikaista aloituspisteiden välissä maalin edestä ympyröiden yläreunaan. Ensimmäiset järjestelmät ovat tunnettuja ja toisistaan selvästi erottuvia: karvaus 2-1-2, trap 1-2-2 ja omassa päässä aluepuolustus (box + 1); mies miestä -puolustus myöhemmin. Veljeltä kysytään, ovatko nämä oikeat kolme ja mitä niiden heikkoudet ovat (vision.md: jokainen järjestelmä häviää jollekin hyökkäystyylille).
**Vastaus (Jerry, 2026-10-09):** aloitetaan kahdella selvästi erilaisella järjestelmällä: 2-1-2 aggressiivinen karvaus ja 1-2-2 passiivinen / trap. Kirjattu: D-021. Slotin rajaus siirretty kysymykseen Q-015.

### Q-009 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** mihin xG-mittakaava sidotaan, jotta ottelussa syntyy 4–7 maalia, ja mitä analyysityökalun xG tarkalleen mittaa?
**Konteksti:** vision.md (4–7 maalia), `docs/stats-and-checks.md` (xG per joukkue 1,2–5,0, yksittäinen laukaus tyypillisesti 0,003–0,05), `tuning.json` `checks.shot.baseXg`. Kaksi ongelmaa:
1. Todennäköisyyden alaraja 0,02 (D-014) nostaa kaukolaukausten xG:n (alustavasti 0,005–0,01) 0,02:een. Pitäisikö laukauksella olla oma alaraja, vai hyväksytäänkö tämä?
2. Lasketaanko xG laukausyritystä kohden (myös blokatut ja ohi menneet) vai maalia kohti mennyttä laukausta kohden? Tämä ratkaisee, onko `baseXg` maalitarkistuksen p0 sellaisenaan vai jaetaanko se läpimenon ja maalia kohti -osuuden todennäköisyydellä.
**Kysymys veljelle (liittyy Q-004):** mitä laukauksia analyysityökalun xG kattaa (kaikki yritykset, blokkaamattomat vai maalia kohti menneet)?
**Ehdotettu oletus:** `baseXg` on maalin todennäköisyys maalia kohti menneestä laukauksesta, raportin xG on yrityksen kokonaistodennäköisyys (läpi × maalia kohti × maali). Alaraja 0,02 koskee myös laukauksia, kunnes Jerry päättää toisin. Mittakaava kalibroidaan maalimäärään 4–7.
**Vastaus (Jerry, 2026-10-09):** todennäköisyyden raja 0,02–0,98 koskee vain tarkistuksia (syötöt, kamppailut, aloitukset jne.), ei laukauksen maalintodennäköisyyttä eikä xG:tä. Laukauksille omat rajat `tuning.json`:iin. Kirjattu D-014:n tarkennuksena. Kohta 2 (xG:n laskentaperuste) ja kysymys veljelle siirretty kysymykseen Q-014.

### Q-005 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** tarkoittaako "aja maalille" kiekollista vai kiekotonta pelaajaa?
**Konteksti:** vision.md, kuvion neljä toimintoa. Kiekollisena se on käytännössä kuljetus maalille ja laukaus tai harhautus. Kiekottomana se on maalin eteen meno maskiin, ohjaukseen ja reboundiin.
**Ehdotettu oletus:** kiekoton. Pelaaja siirtyy vastustajan maalin eteen, antaa laukaukselle maskibonuksen (`checks.shot.modifiers.screen`) ja osallistuu reboundiin. Kiekollinen maalille ajo kirjoitetaan `skate`-toimintona kohti maalia ja sitä seuraavana laukauksena.
**Vastaus (Jerry, 2026-10-09):** ehdotettu oletus hyväksytty: `driveNet` on oletuksena kiekoton (maskiin ja reboundille). Kirjattu: D-020.
