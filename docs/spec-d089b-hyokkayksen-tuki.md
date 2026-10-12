# D-089 B: hyökkäyksen tuki ja paitsio – speksi ja vaikutusarvio

2026-10-12 · game-designer · **luonnos, odottaa Jerryn hyväksyntää**

Tämä dokumentti on D-089:n kohdan B speksi ja vaikutusarvio yhtenä kokonaisuutena. Mitään ei ole toteutettu, eikä dataa ole muutettu. Kun Jerry hyväksyy, osat siirretään `data-schema.md`:hen (O-3, O-7, O-10, O-13, M-8, M-9, Kuviot) ja `stats-and-checks.md`:hen (tapahtumaskeema), ja programmer toteuttaa ne yksi kerrallaan suositellussa järjestyksessä. Koordinaatit ja näkökulma kuten `data-schema.md`:n Määritelmissä: hyökkäävän joukkueen näkökulma, vastustajan maali (9, 2).

## 1. Lähtötilanne

**Jerryn havainto (siemen 42, `rulebased` vs `random`, P1 2:26).** Lokista rekonstruoitu:

| Hetki | Kiekollinen | Laiturit | Pakit | Puolustajat (trap122) |
| --- | --- | --- | --- | --- |
| Riisto 2:20 | C (3, 2) | (3, 1), (3, 3) | (2, 1), (2, 2) | – |
| Alueelle tulo 2:26, ennen kuljetusta | C (5, 2) | (5, 1), (5, 3) | (4, 1), (4, 3) | x = 6: kolme, x = 7: kaksi |
| Kuljetuksen jälkeen (tapahtuman `placement`) | C (7, 2) | (5, 1), (5, 3) | (4, 1), (4, 3) | ennallaan |

Syyt koodissa ja speksissä (O-7):

1. Tukipelaajat liikkuvat tahdin vaiheessa 1, eli **ennen** kiekollisen toimintoa, kohteeseen "kiekollisen nykyinen rivi". Kun kiekollinen sitten kuljettaa kaksi riviä, tukijat jäävät aina kaksi riviä jälkeen. Siksi lokissa C on alueella yksin.
2. Pakkien kohde on enintään keskiviiva (x ≤ 5), joten he eivät koskaan nouse siniviivalle.
3. Paitsiota ei ole. Mitään ei estä hyökkääjää olemasta alueella ennen kiekkoa.
4. Kuljetus yritetään aina, myös 1v5:ssä trapia vastaan. Taktiikkamatriisissa yleisin rushin kuljetus trapia vastaan on 1v5 (45 670 yritystä, onnistuu 44,5 %).

**Nykyiset luvut** (mitattu: batch 2000 ottelua per AI-pari, siemen 1, commit fd2eeb3, sama `tuning.json` SHA 7349f9ce):

| Mittari | Arvo | Tavoite |
| --- | --- | --- |
| Voimasuhdejakauma N < M / N = M / N > M (`rulebased` vs `rulebased`) | 66,2 / 33,8 / 0,0 % | 50–75 / 20–40 / 0–15 |
| Kuljetuksen onnistuminen (`zoneEntrySuccessCarry`) | 63,0 % | – |
| Aloitukset per ottelu / keskimääräinen katkoväli | 58,3 / 62,4 s | – (placeholder) |
| Maalit per ottelu (`rulebased` vs `rulebased`) | 6,39 | 4–7 |
| `rulebased` vs `random` | 79,2 % [77,6–80,8] | 75–85 % |
| Kiekko päätyyn per ottelu (molemmat) | 6,2 | – |
| Taktiikkamatriisi: rush − regroup karvausta vastaan | +12,6 %-yks. | ≥ +5 |
| Taktiikkamatriisi: regroup − rush trapia vastaan | +18,9 %-yks. | ≥ +5 |
| Taktiikkamatriisi: regroup karvausta vastaan | 37,4 % | ≥ 35 % |

**Offline-analyysi** (mitattu, mutta osin hypoteettinen): 300 `rulebased` vs `rulebased`- ja 300 `rulebased` vs `random` -ottelun JSON-lokit (siemenet 1–300). Jokaiselle hallitulle alueelletuonnille kiekon lähtörivi rekonstruoitiin lokin "N vs M" -luvusta (39 030 / 39 030 ratkesi). Raakadataa ei tallennettu repoon.

- Rushin kuljetuksista 21,8 %:ssa (`rulebased` vs `rulebased`) ja 11,0 %:ssa (`rulebased` vs `random`) joku kiekoton joukkuetoveri on jo hyökkäysalueella (x ≥ 7) kuljetuksen hetkellä. Kaikki ovat hyökkääjiä, jotka palaavat syvältä karvauksesta (forecheck212:n F2 (9, 3)). Paitsiosäännöllä nämä tuonnit olisivat paitsioita.
- Kuvioiden alueelletuonneista 100 %:ssa tukija on alueella toiminnon hetkellä, koska kuvio siirtää tukijat saman tahdin siirroissa (esim. `middleDriveWidePass`: LW ja RW (5, ·) → (7, ·) ja samalla syöttö). Tahdin alussa kaikki ovat siniviivan takana. Siksi paitsio arvioidaan tahdin alun sijainneista (osa A).

## 2. Suositeltu toteutusjärjestys

Yksi osa kerrallaan, ja jokaisen jälkeen batch 2000 per AI-pari (`rulebased` vs `rulebased`, `rulebased` vs `random`). Taktiikkamatriisi 2000 per solu ajetaan osien 3 ja 6 jälkeen.

| # | Osa | Miksi tässä kohdassa | Mittaus |
| --- | --- | --- | --- |
| 1 | **A1 Paitsio** (sääntö, tag-up, katko, aloitus) | Sääntö, jonka kaikki muut osat olettavat. Ei muuta voimasuhteen määritelmää | Paitsiot ja tag-up-viiveet per ottelu, aloitukset, maalit, voimasuhteet, rushin laukaukset |
| 2 | **B1 Tuki suorahyökkäyksessä** (tukijat seuraavat kiekkoa) | Korjaa Jerryn havainnon. Tukijoiden sijainti alueelle tultaessa muuttuu vähän, mutta sen jälkeen paljon | Kuten 1. Lisäksi raportissa sekä nykyinen M-8 että ehdotettu M-8 (diagnostiikka) |
| 3 | **F M-8 / E-006:n uusi määritelmä** | Mitataan vasta, kun B1:n sijainnit ovat oikeat. Ilman B1:tä uusi määritelmä antaa 79,5 / 19,8 / 0,7 (haarukan ulkopuolella) | Voimasuhteet, onnistuminen voimasuhteittain, K4, taktiikkamatriisi |
| 4 | **C Hyökkäysalueen muodostelma** (pakit pisteille) | Vaikuttaa vastustajan vastahyökkäyksiin, joten mitataan F:n jälkeen | Ylivoimaisten osuus keskialueen riistojen jälkeen, laukaukset siniviivalta, maalit |
| 5 | **E Aloitusmuodostelma** | Riippumaton, pieni | Aloituksen jälkeiset laukaukset, aloitusluokan xG-osuus |
| 6 | **D Kuljetus vai päätyyn** (Q-068) | Kynnys kalibroidaan uudella määritelmällä | Päätyyn lyönnit, voimasuhteet, maalit, `rulebased` vs `random`, taktiikkamatriisi |
| 7 | **B2/B3 Ylivoimat Nopeudesta** (Q-071) | Säätää ylivoimaisten osuutta (0–15 %) sen jälkeen, kun muu on paikallaan | Ylivoimaisten osuus, onnistuminen voimasuhteittain |
| 8 | **A2 Ajoitusvirhe** (valinnainen) | Ilman sitä paitsioita ei käytännössä synny, koska tekoäly odottaa aina tag-upin | Paitsiot per ottelu tavoitetta vasten (Jerry antaa luvun) |

K4:n (`strengthDifferencePerPlayer`) kertasäätö sallitaan vasta osan 6 jälkeen, D-084:n periaatteen mukaisesti.

## 3. Osa A: paitsio

### A1 Sääntö

**Siniviiva verkossa.** Siniviiva on rivien 6 ja 7 välissä (x ≈ 6,1, `data-schema.md`, Alueiden perustelu). **Paitsioraja** on hyökkäysalueen ensimmäinen rivi, x_b = `zones.offensive.xMin` (7). **Paitsiorivi** on sitä edeltävä rivi, x_b − 1 (6). Molemmat johdetaan `rink.json`:sta, joten uusia lukuja ei tarvita.

**Alueelle vievä toiminto.** Hyökkäävän joukkueen toiminto, joka vie kiekon hyökkäysalueen ulkopuolelta (kiekon solmu x < x_b) hyökkäysalueelle:

| Toiminto | Ehto |
| --- | --- |
| `skate` (kuvio tai rush) | lähtö x < x_b ja kohde x ≥ x_b |
| `pass` | syöttäjä x < x_b ja vastaanottaja x ≥ x_b (sijainti tahdin siirtojen jälkeen, kuten syötössä nyt) |
| `dump` (kuvio tai osan D päätyyn lyönti) | lyöjä x < x_b (kohde on aina hyökkäysalueella) |
| `shoot` | laukoja x < x_b (harvinainen, mutta sääntö on sama) |

Irtokiekko, syötönkatko, vastustajan menettämä kiekko ja aloitus eivät ole alueelle vieviä toimintoja. Paitsio koskee vain hyökkäävän joukkueen itse alueelle viemää kiekkoa.

**Paitsio.** Toiminto on paitsio, jos jokin hyökkäävän joukkueen kenttäpelaaja on hyökkäysalueella (x ≥ x_b) **tahdin alussa** S0, eli ennen tahdin vaiheen 1 siirtoja. Poikkeus on kiekollinen itse (`skate`, `dump` ja `shoot`) tai syöttäjä (`pass`). Syötön vastaanottaja ei ole poikkeus: syöttö alueella jo olevalle on paitsio.

- **Miksi tahdin alku.** Tahdin siirrot ja toiminto tapahtuvat oikeassa ajassa samanaikaisesti, ja järjestys siirrot → toiminto on vain mallin järjestys (D-041). Saman tahdin siirroilla siniviivan ylittävä tukija ylittää sen siis kiekon kanssa samaan aikaan, eli on paitsiossa korkeintaan "tasalla", ja se on sallittua. Alueella jo tahdin alussa ollut oli siellä ennen kiekkoa.
- **Vaikutus nykyisiin kuvioihin:** kaikki kymmenen kuviota pysyvät valideina. `middleDriveWidePass`, `wideCarryDrive` ja `dumpAndChase` siirtävät tukijat alueelle saman tahdin siirroissa, ja tahdin alussa kaikki ovat x ≤ 6:ssa.
- **Rushissa S0** on toiminnon alun sijainti ennen kiekottomien liikettä (osa B1:n jälkeen kiekottomat liikkuvat toiminnon jälkeen, ks. B1).
- **Viivästettyä paitsiota ei mallinneta.** Katko tulee heti. Tämä on yksinkertaistus: oikeassa kiekossa paitsiossa oleva voi palata ennen kuin koskee kiekkoon.

**Seuraus.**

1. Paitsio todetaan ennen toiminnon tarkistusta. Tarkistusta ei ratkaista eikä satunnaislukua käytetä, joten determinismi säilyy, ja nykyiset lokit eivät muutu tilanteissa, joissa paitsiota ei ole.
2. Kello: toiminnon aika (`secondsPerAction`) kuluu (O-1, sama kokonaisuus), koska peli eteni siniviivalle asti.
3. Ei Hallittu alueelletuonti- eikä Kiekko päätyyn -tapahtumaa. Lokiin kirjataan Pelikatko, syy "paitsio", ja uusi kenttä paitsiossa olleista (ks. Tapahtumaskeema).
4. O-13:n katkon seuraukset: paine × `keepOnStoppage`, katkovaihdot (O-2:n katkoluetteloon lisätään paitsio), aloitus, O = 1 molemmille, sitoutumiset päättyvät. AI-4:n aikalisäkatkoihin lisätään paitsio (lista mainitsee sen jo "myöhemmin").
5. **Aloituspiste:** paitsion tehneen joukkueen näkökulmasta keskialueen piste sen siniviivan edessä, jonka yli kiekko oli menossa, kiekon puolella: kiekon y < keskikaista → `neutralOffensiveLeft` (6, 1), y > keskikaista → `neutralOffensiveRight` (6, 3), keskikaista → `neutralOffensiveLeft` (sama tasasääntö kuin O-3). Kiekon y luetaan toiminnon alusta (kiekollisen tai syöttäjän solmu). Lisätään O-3:n taulukkoon. Kiekkoperuste: IIHF:n ja NHL:n sääntöjen mukaan aloitus on paitsion lähimmässä keskialueen pisteessä. Tahallista paitsiota (aloitus omaan päähän) ei mallinneta.

**Tag-up rushissa (tekoäly välttää paitsion).** Rush ei saa viedä kiekkoa alueelle, kun joku joukkuetoveri on alueella. Muutokset O-7:n kiekollisen toimintoon:

- Sääntö 2 (syöttö): ehdokkaaksi ei kelpaa joukkuetoveri, jonka syöttö olisi paitsio.
- Sääntö 3 (kuljetus): jos kuljetus olisi paitsio, kohteen x rajataan paitsioriviin (x ≤ x_b − 1). Jos kiekollinen on jo paitsiorivillä, hän **pitää kiekon**: toiminto ilman tarkistusta ja ilman tapahtumaa, aika `secondsPerAction.systemStep`. Se on rushin toiminto (laskuri `rushMaxActions` kasvaa), ja puolustajat liikkuvat vaiheessa 2 normaalisti.
- Alueella olevat joukkuetoverit liikkuvat kohti paitsioriviä (B1:n raja), joten tag-up kestää tyypillisesti 1–2 toimintoa. Rush päättyy normaalisti, kun O ≥ `organizedThreshold`.
- Sama raja koskee osan D päätyyn lyöntiä.

**Tekoälyn paitsiot.** Tag-upin jälkeen rushissa ei synny paitsioita, ja kuviot ovat validointisäännön ansiosta aina sääntöjen mukaisia. A1:n jälkeen paitsioita on siis käytännössä 0 per ottelu. Jos Jerry haluaa oikean kiekon määrän paitsioita, tarvitaan A2.

**Kuvion validointi (uusi sääntö).** Lataaja hylkää kuvion, jos tahti, jonka toiminto on alueelle vievä, alkaa tilanteesta, jossa joku muu kuin kiekollinen tai syöttäjä on x ≥ x_b. Tahdin alku on edellisen tahdin siirtojen ja toiminnon jälkeen (1. tahdissa `start.positions`). Peilaus ei vaikuta x:ään, joten peilattu kuvio on validi, jos alkuperäinen on. Ajossa sama tarkistus tehdään silti (puolustava koodi), ja testi varmistaa, että kuvioissa ei synny paitsioita.

### A2 Ajoitusvirhe (valinnainen, uusi mekaniikka)

Oikeassa kiekossa paitsio on ajoitusvirhe: tukija ylittää siniviivan hetkeä liian aikaisin.

- **Tarkistus:** jokainen hyökkäävän joukkueen kenttäpelaaja (ei kiekollinen eikä syöttäjä), joka ylittää paitsiorajan samassa toiminnossa kuin kiekko (tahdin siirroissa tai B1:n seurausliikkeessä), tekee yksipuolisen tarkistuksen `checks.offsideTiming`.
- **Tarkistuksen arvot:** hyökkääjäpuolella Pelinluku 1,0, puolustajapuolella referenssiarvo. Onnistuminen = ajoissa.
- **Epäonnistuminen:** paitsio, kuten A1:ssä. Tarkistukset tehdään ennen toiminnon omaa tarkistusta, kiinteässä pelipaikkajärjestyksessä `C`, `LW`, `RW`, `LD`, `RD`.
- **Arvo `p0` = 1:** ei arvontaa lainkaan. Näin A1:n lokit pysyvät ennallaan, kun A2 on pois päältä.
- **Taitoperuste:** hyvä Pelinluku vähentää paitsioita (pilari "taito voittaa tuurin").
- **Arvio (käsin):** n. 150 ylitystä per ottelu (`rulebased` vs `rulebased`). `p0` 0,985 → n. 2 paitsiota per ottelu.

### A:n tasapainoarvot

| Polku | Oletus | Hylätään, jos | Huom. |
| --- | --- | --- | --- |
| – (A1) | – | – | Raja ja aloituspiste johdetaan `rink.json`:sta, ei uusia lukuja |
| `checks.offsideTiming.p0` (A2) | 0,985 (paikkamerkki) | ei välillä (0, 1] | 1 = pois päältä, ei arvontaa |
| `checks.offsideTiming.attacker.skater.awareness` (A2) | 1,0 | yleiset tarkistussäännöt | `kind: oneSided`, `modifiers: {}` |

### A:n testit

1. `skate`, `pass`, `dump` ja `shoot` paitsiorajan yli, joukkuetoveri x = 7 tahdin alussa → Pelikatko (paitsio), ei tarkistusta, ei satunnaislukua, aika kuluu.
2. Sama, kun joukkuetoveri ylittää rajan saman tahdin siirroissa → ei paitsiota.
3. Syöttö vastaanottajalle, joka oli alueella tahdin alussa → paitsio. Syöttäjä itse alueen ulkopuolella ei ole paitsiossa.
4. Aloituspiste: kiekon y 0, 1 → `neutralOffensiveLeft`, y 2 → `neutralOffensiveLeft`, y 3, 4 → `neutralOffensiveRight` paitsion tehneen näkökulmasta. Koti ja vieras ovat toistensa kiertokuvat.
5. Tag-up rushissa: alueella oleva joukkuetoveri → kuljetus rajataan riville 6. Rivillä 6 kiekollinen pitää kiekon `systemStep`-ajan, ja rushin laskuri kasvaa. Syöttöehdokas, jonka syöttö olisi paitsio, ohitetaan.
6. Kuvion validointi: hylkää kuvion, jossa tukija on x = 7 tahdin alussa ja toiminto vie kiekon alueelle. Hyväksyy kaikki nykyiset kymmenen kuviota.
7. Katkon seuraukset kuten O-13 (paine, vaihdot, O = 1). Katkovaihto ja aikalisä ovat mahdollisia.
8. Kuvion ja rushin ajossa ei synny paitsioita (invarianttitesti 200 ottelulla, A2 pois päältä).
9. A2: `p0` = 1 → loki identtinen A1:n kanssa samalla siemenellä. Tarkistusjärjestys on kiinteä. Pelinluku 20 vs 1 näkyy todennäköisyydessä.
10. Determinismi: sama siemen → sama loki.

## 4. Osa B: tuki suorahyökkäyksessä

### B1 Tukijat seuraavat kiekkoa

**Muutos O-7:ään: kiekottomat liikkuvat toiminnon jälkeen, eivät ennen.** Rushin toiminto etenee näin:

1. Puolustajien järjestelmäliike (ennallaan).
2. Kiekollisen toiminto.
3. **Seurausliike.** Jos kiekko on edelleen joukkueella (onnistunut kuljetus tai syöttö tai pito), kiekottomat liikkuvat enintään `plays.maxNodesPerBeat` askelta kohti tukikohteitaan. Kohteet lasketaan kiekon uudesta solmusta. Ei aikaa, koska liike tapahtuu kiekollisen kanssa samaan aikaan. Päätteet D-058:n mukaan.

Rushin ensimmäistä toimintoa edeltävä liike jää pois. Kiekon saannin hetken sijainnit ovat lähtökohta, kuten kuviossakin.

**Tukikohteet** (hyökkäävän joukkueen näkökulma, x_k = kiekon x seurausliikkeen hetkellä, y_k = kiekon kaista):

| Pelaaja | Kiekko alueen ulkopuolella (x_k < x_b) | Kiekko alueella |
| --- | --- | --- |
| Laituri, joka ei ole kiekollinen ("kaistan ajaja") | (min(x_k, x_b − 1), oma kaista) | (min(x_k, `opponentGoal.x` − 1), oma kaista), kuten nyt |
| C, kun kiekollinen on laituri ("jälkimmäinen") | (min(x_k − `transitions.rushTrailRows`, x_b − 1), 2) | (max(x_k − `rushTrailRows`, x_b), 2), eli korkea slotti (7, 2) |
| C, kun kiekollinen on pakki | kuten laituri, kaista 2 | kuten laituri, kaista 2 |
| Pakki, joka ei ole kiekollinen | (max(0, min(x_k − `transitions.rushDefenceTrailRows`, x_b − 1)), kaista 1 / 3) | piste (x_b, 1) `LD` / (x_b, `width` − 2) `RD` (osa C) |

- **Kaistat** ovat ennallaan: `LW` y = 1, `C` y = 2, `RW` y = 3, `LD` y = 1, `RD` y = 3. Jos tukijan kaista on kiekollisen kaista, tukija ottaa kiekollisen oman pelipaikan kaistan (vaihto). Näin kolme kaistaa pysyvät täytettyinä.
- **Paitsioraja:** kun kiekko on alueen ulkopuolella, kukaan tukija ei mene paitsioriviä pidemmälle. Alueella oleva tukija liikkuu tällöin kohti paitsioriviä (tag-up, osa A).
- **Kun C kantaa kiekkoa,** molemmat laiturit ovat kaistoillaan tasalla, eikä jälkimmäistä hyökkääjää ole. Pakit ovat silloin jälkimmäisiä. Kiekkoperuste: kolmen kaistan hyökkäys, jossa sentteri keskellä ja laiturit leveällä, ja pakki seuraa korkealla.
- **Seurausliike koskee myös tuontia.** Kun C kuljettaa (5, 2) → (7, 2), laiturit seuraavat (5, ·) → (7, ·) samassa toiminnossa ja pakit (4, ·) → (6, ·). Tapahtuman `placement` kirjataan seurausliikkeen jälkeen, joten toisto näyttää tukijat tulemassa alueelle kiekon tasalla. "N vs M" lasketaan edelleen ennen tarkistusta (M-8), joten luvut ja kuva ovat eri hetkiltä. Tämä kirjataan M-8:aan.
- **Rushin päätyttyä** (O ≥ raja tai toiminnot käytetty) peli jatkuu D-036:n mukaan setupin kanssa kuten nyt.

**Rushin laukaus- ja syöttösäännöt** (O-7, säännöt 1–2) lukevat sijainnit toiminnon hetkellä. Ne ovat nyt edellisen seurausliikkeen jälkeiset sijainnit, eli samat kuin nykyisessä vaiheen 1 liikkeessä samasta kiekon solmusta. Erona on, että kiekon alueelle tulon jälkeen tukijat ovat jo alueella seuraavassa toiminnossa: jälkimmäinen on korkeassa slotissa (7, 2), joka on laukaussolmu S.

### B2 Kaistan ajajan irtiotto Nopeudella (Q-071, uusi mekaniikka)

Seurausliikkeessä kaistan ajaja (laituri, joka ei ole kiekollinen) vertaa Nopeuttaan häntä lähimpään puolustavan joukkueen kenttäpelaajaan (yleinen lähimmän pelaajan sääntö).

- Jos Nopeus_ajaja − Nopeus_puolustaja ≥ `transitions.rushSpeedMargin`, ajajan kohde on yhden rivin kiekon edellä: x = x_k + `transitions.rushJumpRows`. Paitsioraja ja `opponentGoal.x` − 1 rajaavat kohdetta kuten B1:ssä.
- Ei satunnaisuutta. Nopeampi joukkue saa irtioton aina, ja se on taitoa, ei tuuria.
- **Vaikutus:** alueella ajaja on x_k + 1:ssä, esim. (8, 1) tai (8, 3), jotka ovat laukaussolmuja S. Rushin syöttösääntö 2 löytää hänet ennen kuin puolustus järjestäytyy, eli 2v1-tyyppinen syöttö ympyrään.
- Uudella M-8:lla (osa F) tasalla oleva hyökkääjä lasketaan jo N:ään, joten irtiotto ei muuta voimasuhdetta. Se muuttaa paikkojen syntyä.

### B3 Hitaampi puolustaja jää jälkeen (Q-071, uusi mekaniikka)

Vastustajan rushissa puolustava kenttäpelaaja, jonka järjestelmäliike vie kohti omaa maalia (x kasvaa hyökkäävän joukkueen näkökulmassa) ja jonka Nopeus ≤ kiekollisen Nopeus − `transitions.rushSpeedMargin`, liikkuu `plays.maxNodesPerBeat` − `transitions.rushSlowDefenderNodes` askelta.

- Tämä tuottaa ansaitut ylivoimat voimasuhteessa: hidas puolustaja putoaa tasalta kiekon taakse, eikä häntä lasketa M:ään.
- Sääntö on paluukarvauksen (D-085, nyt pois, arvo 0) Nopeus-versio. Jos Jerry haluaa myöhemmin paluukarvauksen takaisin, tämä sääntö lasketaan sen päälle.

### B:n tasapainoarvot (`transitions`-osio, olemassa olevien rinnalle, `_placeholders`-listaan)

| Polku | Oletus | Hylätään, jos | Merkitys |
| --- | --- | --- | --- |
| `transitions.rushTrailRows` | 1 | ei kokonaisluku, < 0 tai > 2 | Jälkimmäisen (C) etäisyys riveinä kiekon takana |
| `transitions.rushDefenceTrailRows` | 1 | ei kokonaisluku, < 0 tai > 3 | Pakkien etäisyys kiekon takana keskialueella. Nykyinen sääntö vastaa arvoa 1, mutta sen yläraja on keskiviiva |
| `transitions.rushSpeedMargin` | 4 | ei kokonaisluku, < 1 tai > `stats.max` − `stats.min` | B2 ja B3: Nopeusero, josta irtiotto tai jälkeen jääminen syntyy |
| `transitions.rushJumpRows` | 1 | ei 0 tai 1 | B2: irtioton rivit. 0 = pois |
| `transitions.rushSlowDefenderNodes` | 1 | ei kokonaisluku, < 0 tai > `plays.maxNodesPerBeat` | B3: hitaan puolustajan menettämät askeleet. 0 = pois |

`_notes`-lisäys: "rushTrailRows / rushDefenceTrailRows: rush support follows the carrier after his action (wingers level in their lanes, C trails when a winger carries, D trail), never past the onside row while the puck is outside the offensive zone (D-089 B). rushSpeedMargin, rushJumpRows, rushSlowDefenderNodes: speed-earned odd-man rushes (Q-071)."

### B:n testit

1. Seurausliike: C kuljettaa (5, 2) → (7, 2), ja laiturit ovat lopuksi (7, 1) ja (7, 3) ja pakit (6, 1) ja (6, 3) (`rushDefenceTrailRows` 1). Tapahtuman `placement` näyttää nämä sijainnit.
2. Kiekon ollessa alueen ulkopuolella kukaan tukija ei ole x > 6. Alueella oleva tukija liikkuu kohti riviä 6.
3. Laituri kantaa (oma kaista) → toinen laituri tasalla omalla kaistallaan, C rivin x_k − 1 keskikaistalla. C kantaa → molemmat laiturit tasalla. Kaistavaihto, kun tukija on kiekollisen kaistalla.
4. Epäonnistunut kuljetus tai syöttö → ei seurausliikettä.
5. B2: Nopeusero täsmälleen marginaali → irtiotto, marginaali − 1 → ei. Kohde rajataan paitsioriviin. Arvolla `rushJumpRows` 0 tulos on sama kuin ilman sääntöä.
6. B3: hidas puolustaja, joka liikkuu kohti omaa maalia, ottaa 1 askeleen. Sivuttain tai poispäin liikkuva ei menetä askelta. Arvolla 0 tulos on sama kuin ilman sääntöä.
7. D-058 pätee kaikissa liikkeissä (invarianttitesti). Peilisymmetria: koti ja vieras, vasen ja oikea kaista.
8. Determinismi.

## 5. Osa C: hyökkäysalueen muodostelma

**Pisteet** ovat (x_b, 1) `LD` ja (x_b, `width` − 2) `RD`, eli nykyverkossa (7, 1) ja (7, 3). Ne johdetaan `rink.json`:sta, ja ne ovat samat kuin kuvioiden pakkien solmut (`pointShotScreen`, `offensiveFaceoffPointShot`). Siniviiva on x ≈ 6,1, joten rivi 7 on "juuri siniviivan sisäpuolella" (Q-015).

1. **Rush:** B1:n taulukko. Kun kiekko on alueella, pakkien kohde on piste. Kun kiekko poistuu alueelta (esim. kiekollinen kuljettaa ulos), kohde on taas paitsiorivillä tai sen takana.
2. **Järjestelmätila kiekko hallussa** (D-036, `SystemPossessionStep`). Nyt hyökkääjät eivät liiku lainkaan. Uusi sääntö: ennen puolustajien liikettä kiekottomat liikkuvat enintään `plays.maxNodesPerBeat` askelta kohti kohdettaan.
   - Kiekko alueella: pakit pisteille. Hyökkääjä, joka on jo alueella, pysyy paikallaan. Alueen ulkopuolella oleva hyökkääjä menee kohteeseen (8, 1) `LW`, (7, 2) `C` tai (8, 3) `RW`.
   - Kiekko alueen ulkopuolella: B1:n keskialueen kohteet, eli paitsioraja pätee.
   - Ei tarkistusta, eikä aikaa `systemStep`-ajan lisäksi.
3. **Kuviot** määräävät setupin lähtösolmut kuten nyt. Hyökkäysalueen kuvioissa pakit ovat jo pisteillä.
4. **Vetäytyminen kiekon menetyksessä:** uutta sääntöä ei tarvita.
   - Kiekon menettänyt joukkue on puolustaja, ja sen järjestelmä ohjaa kaikkia viittä (D-034).
   - forecheck212 pitää pakit siniviivalla (7, 1) / (6, 3), kun kiekko on sen hyökkäysalueella, ja vetää ne riville 3, kun kiekko on keskialueella. trap122 vetää pakit heti riville 4.
   - Pisteiltä vetäytyminen kestää 2–3 liikettä (2 solmua per liike). Siksi keskialueen riiston jälkeen pakki voi jäädä kiekon taakse, ja vastustaja saa ansaitun ylivoiman. Kiekkoperuste: pakkien korkea asema on riski, joka maksaa vastahyökkäyksessä.
   - Jos mittaus näyttää liikaa ylivoimia, säätönuppi on paluukarvaus (`backcheckNodesPerBeat`, D-085) pelkästään pakeille. Se on erillinen ehdotus, ei osa tätä speksiä.

**Testit:** rushin pakit pisteillä kahden toiminnon jälkeen kiekon tultua alueelle. Järjestelmätilan askel liikuttaa pakit pisteille ja hyökkääjät alueelle. Kiekko alueen ulkopuolella → kukaan ei ole x > 6. Kiekon menetyksen jälkeen pakit liikkuvat järjestelmänsä kohteisiin (ennallaan, regressiotesti). D-058. Determinismi.

**Tasapainoarvot:** ei uusia.

## 6. Osa D: kuljetus vai päätyyn (Q-068)

**Sääntö (rush).** Kun rushin kiekollisen toiminto olisi alueelle vievä kuljetus (O-7:n sääntö 3), lasketaan M-8:n N ja M samasta hetkestä kuin kuljetuksen tarkistus laskisi ne. Jos N − M ≤ `transitions.rushDumpMaxStrengthDifference`, kiekollinen lyö kiekon päätyyn (`dump`) eikä kuljeta.

- **Kohde:** kulma kiekollisen puolella, (`opponentGoal.x`, 0), jos kiekon y < keskikaista, muuten (`opponentGoal.x`, `width` − 1). Keskikaistalta vasen puoli (sama tasasääntö kuin O-3).
- Kulmat ovat maalivahdin ulottuman ulkopuolella (Chebyshev 2 > `goalieReachNodes` 1). Ristikulma on oikeassa kiekossa yhtä yleinen. Valitaan lähempi, koska B1:n kaistan ajaja on samalla puolella. Tämä on kysymys veljelle (alla).
- **Seuraus kuten kuvion `dump`:** Kiekko päätyyn -tapahtuma, irtokiekko kohteessa (M-6) ja rushin päättyminen. Paitsioraja koskee lyöntiä (osa A).
- **Kuvioissa** sääntö ei päde: kuvio on valmentajan valinta, ja `dumpAndChase` on jo olemassa.
- Arvolla −5 sääntö on pois päältä, koska N ≥ 1 ja M ≤ 5, eli N − M ≥ −4.

**Taitopäätös vai yhteinen sääntö.** Speksissä kynnys on yhteinen `tuning.json`-arvo, joten se ei erota tekoälyjä toisistaan. Q-068:n idea ("hyvä päätös on taitoa") vaatisi kynnyksen valmentajan ohjeeksi O-10:n tapaan. Se olisi `Plan`-kenttä, ja `rulebased` ja `random` valitsisivat sen eri tavoin. Tämä on Jerryn päätös (kysymys 4). Suositus: ensin yhteinen sääntö ja mittaus, sitten valmentajan ohje, jos `rulebased` vs `random` putoaa.

| Polku | Oletus | Hylätään, jos | Merkitys |
| --- | --- | --- | --- |
| `transitions.rushDumpMaxStrengthDifference` | −3 | ei kokonaisluku, < −5 tai > 0 | Rush lyö päätyyn, kun N − M ≤ arvo. −5 = pois |

Oletus on −3 eikä Jerryn esimerkin −2 (1v3), koska offline-arvion mukaan −2 vie tasavoimaisten osuuden yli 40 %:n (alla). Arvo on designerin säätönuppi haarukoiden sisällä.

**Testit:** kynnys täsmälleen → `dump`, kynnys + 1 → kuljetus. Arvolla −5 ei koskaan päätyyn. Kohdekulma vasen, oikea ja keskikaista. Paitsioraja: tag-up ennen lyöntiä. Rush päättyy, ja tapahtuma on Kiekko päätyyn. Kuvioiden kuljetukset eivät muutu. Determinismi.

## 7. Osa E: aloitusmuodostelma

Korvaa D-061:n (Q-034) oletusasettelun, jota käytetään, kun joukkueella ei ole aloituskuviota pisteelle (myös hävinneen joukkueen asettelu). Nykyinen koodi käyttää kiinteää arvoa 2 (`FaceoffFormation.Default`), joten luvut siirretään `tuning.json`:iin (CLAUDE.md, sääntö 2).

Oma näkökulma, piste (sx, sy):

| Pelipaikka | Nyt (D-061) | Ehdotus |
| --- | --- | --- |
| C | (sx, sy) | (sx, sy) |
| LW | (sx, max(0, sy − 2)) | (sx, max(0, sy − w)) |
| RW | (sx, min(width − 1, sy + 2)) | (sx, min(width − 1, sy + w)) |
| LD | (max(0, sx − 2), 1) | (max(0, sx − d), 1) |
| RD | (max(0, sx − 2), 3) | (max(0, sx − d), 3) |

w = `positions.faceoffWingerLanes` (1) ja d = `positions.faceoffDefenceRows` (1).

| Piste | Nyt: laiturit / pakit | Ehdotus: laiturit / pakit |
| --- | --- | --- |
| `center` (5, 2) | (5, 0), (5, 4) / (3, 1), (3, 3), eli omalla alueella | (5, 1), (5, 3) / (4, 1), (4, 3), oman siniviivan edessä |
| `offensiveLeft` (8, 1) | (8, 0), (8, 3) / (6, 1), (6, 3), eli alueen ulkopuolella | (8, 0), (8, 2) / (7, 1), (7, 3), pisteillä |
| `defensiveLeft` (2, 1) | (2, 0), (2, 3) / (0, 1), (0, 3), eli maaliviivan takana | (2, 0), (2, 2) / (1, 1), (1, 3), tolppien vieressä |

- Kiekkoperuste: laiturit ympyrän reunoilla (ympyrän säde 15 ft ≈ 0,7 solmua, eli yksi kaista) ja pakit sentterin takana. Hyökkäyspään aloituksessa pakit ovat pisteillä.
- Pakkien kaistat 1 ja 3 eivät koskaan osu maalisolmuun (1, 2). Laituri osuu korkeintaan maalin edustalle (2, 2), joka ei ole maalisolmu.
- Validointi: w ∈ {1, 2} ja d ∈ {1, 2, 3}, kokonaislukuja. Arvoilla 2 / 2 tulos on täsmälleen nykyinen (regressiotesti).
- **Testit:** kaikki yhdeksän pistettä kummallakin joukkueella. Ei maalisolmua. Ei kahta saman joukkueen pelaajaa samassa solmussa. Arvot 2 / 2 → nykyinen loki samalla siemenellä. Kiertosymmetria.

## 8. Osa F: M-8:n ja E-006:n määritelmä paitsion ja tuen kanssa

**Ongelma.** Paitsion kanssa kukaan tukija ei voi olla alueella ennen kiekkoa, eli tukija on enintään rivillä 6. Kun kiekko lähtee riviltä 6 (39 % tuonneista), N on D-084:n määritelmällä aina 1. B1:n tukijat ovat tasalla, eikä tasalla olevaa lasketa. Ideaalisilla B1-sijainneilla D-084 antaa 76,7 / 23,3 / 0,0 % (yli haarukan), ja yleisimmät voimasuhteet ovat 1v3, 1v1, 1v2 ja 1v4. Wisehockeyssa yleisimmät ovat 2v3, 1v2 ja 3v4, eli tukija kiekon tasalla lasketaan mukaan.

**Ehdotus (muuttaa D-084:n kohtaa 1, lukittu → Jerryn päätös).** Lasketaan toiminnon hetkellä ennen tarkistusta. Kiekon solmu (x_k, y_k) on kiekollisen solmu (kuljetus) tai syöttäjän solmu (syöttö).

- **N** = kiekollinen tai syöttäjä + hyökkäävän joukkueen kenttäpelaajat, joilla x ≥ x_k (kiekon tasalla tai edellä).
- **M** = puolustavan joukkueen kenttäpelaajat, joilla x > x_k (kiekon ja oman maalin välissä), + puolustajat kiekon tasalla (x = x_k), joilla |y − y_k| ≤ `transitions.entryLevelDefenderLanes` (1).
- **Peruste:** tasalla kulkeva hyökkääjä on mukana hyökkäyksessä (kaistan ajaja, vauhti eteenpäin). Tasalla oleva puolustaja on mukana vain, jos hän on kiekollisen ulottuvilla (kiekollisen kylki tai viereinen kaista). Kaukana laidalla oleva tasalla oleva puolustaja ei ole pelissä.
- **Yksi nuppi kattaa vaihtoehdot:** `entryLevelDefenderLanes` 4 = symmetrinen "tasalla molemmille" (E-006:n hylätty vaihtoehto). 0 = vain samalla kaistalla.
- **E-006:n paluukarvaus (D-085/D-086)** käyttää peilikuvaa "väärä puoli = x_p < x_k". Se ei muutu, koska se on pois päältä (D-087).
- **M-9 (K4)** käyttää samoja lukuja, joten raportin N vs M ja vaikutus pysyvät yhtenäisinä.

| Polku | Oletus | Hylätään, jos |
| --- | --- | --- |
| `transitions.entryLevelDefenderLanes` | 1 | ei kokonaisluku, < 0 tai > `width` − 1 |

**Offline-arvio määritelmistä** (mitattu-hypoteettinen: 300 `rulebased` vs `rulebased` -ottelua, onnistuneet hallitut tuonnit, puolustajien sijainnit lokista). "Nykyiset sijainnit" = lokin sijainnit sellaisenaan. "B1-sijainnit" = rushin tukijat siirretty B1:n kohteisiin (laiturit tasalla, C ja pakit rivin jäljessä) olettaen, että he ehtivät. Tag-upin viivästämiä tuonteja ei ole poistettu.

| Määritelmä | Nykyiset sijainnit | B1-sijainnit | Keskim. N − M (B1) |
| --- | --- | --- | --- |
| D-084 (nyt): tasalla ei kummallekaan | 66,2 / 33,7 / 0,0 | 76,7 / 23,3 / 0,0 | −1,43 |
| D-080: tasalla vain hyökkääjille | 25,3 / 26,6 / 48,1 | 38,0 / 13,1 / 48,9 | +0,27 |
| Symmetrinen: tasalla molemmille (`entryLevelDefenderLanes` 4) | 86,5 / 12,8 / 0,7 | 82,5 / 15,0 / 2,5 | −1,52 |
| **Ehdotus: hyökkääjät tasalla, puolustajat tasalla kaistan säteellä 1** | 79,5 / 19,8 / 0,7 | **65,7 / 31,8 / 2,5** | −1,30 |

- Vain rushin kuljetukset, B1-sijainnit, ehdotus: `rulebased` vs `rulebased` 61,5 / 35,7 / 2,8 ja `rulebased` vs `random` 64,7 / 31,8 / 3,5. Kaikki haarukoissa.
- Yleisimmät voimasuhteet ehdotuksella: 2v5 24 %, 4v4 22 %, 3v5 15 %, 2v3 13 % ja 3v3 10 %. 4v4 syntyy, kun pakki kantaa ja kolme hyökkääjää on tasalla. Muoto ei ole tavoite (D-084), mutta 2v5 / 3v5 näyttää, että puolustus on usein kokonaan kiekon takana. Juuri nämä osa D muuttaa päätyyn lyönneiksi.

**Osan D kynnys samalla offline-aineistolla** (rushin kuljetukset, B1-sijainnit, ehdotettu M-8):

| `rushDumpMaxStrengthDifference` | `rulebased` vs `rulebased` | Päätyyn | `rulebased` vs `random` | Päätyyn |
| --- | --- | --- | --- | --- |
| −5 (pois) / −4 | 61,5 / 35,7 / 2,8 | 0 % | 64,7 / 31,8 / 3,5 | 0 % |
| **−3** | 46,9 / 49,2 / 3,9 | 28 % | 56,4 / 39,3 / 4,3 | 19 % |
| −2 (Jerryn esimerkki 1v3) | 41,0 / 54,7 / 4,3 | 35 % | 44,5 / 50,0 / 5,5 | 36 % |
| Symmetrinen määritelmä ja −2 (kaikki tuonnit) | 68,7 / 26,9 / 4,4 | 44 % | – | – |

- Päätyyn lyönti poistaa toivottomimmat kuljetukset onnistuneista, joten alivoimaisten osuus laskee. Wisehockeyn 65 % sisältää jo pelaajien omat päätyyn lyönnit (D-081: data sisältää vain kiekon säilyttäneet tuonnit).
- Ideaaliset B1-sijainnit ovat yläraja N:lle. Oikeasti osa tukijoista ei ehdi tasalle (kiekon saanti syvältä, vaihto), joten N on pienempi ja alivoimaisten osuus suurempi kuin taulukossa. Siksi −3 on lähtöarvo, ja se säädetään mittauksella välillä −4…−2.
- Sivuhavainto: simulaatiossa on n. 6 päätyyn lyöntiä ja n. 130 hallittua tuontiyritystä per ottelu. Oikeassa kiekossa päätyyn lyöntejä on arviolta 40–50 % alueelle tuonneista. Arvio on yleistietoa, ei analyysista, ks. kysymys 6. Osa D vie suuntaan, joka on oikea.

## 9. Tapahtumaskeeman muutokset (`stats-and-checks.md` ensin, CLAUDE.md sääntö 3)

Uutta tapahtumatyyppiä ei tarvita. "Pelikatko, syy paitsio" on jo skeemassa ja koodissa (`StoppageReason.Offside`). Ehdotetut kenttämuutokset, tarkka teksti `stats-and-checks.md`:n Tapahtumaskeema-taulukkoon:

| Tapahtuma | Nyt | Ehdotus |
| --- | --- | --- |
| Pelikatko | Syy (maalivahti sulki kiekon, pitkä kiekko, paitsio, jäähy, maali, kiekko ulos kaukalosta (D-063), erän loppu (D-064)) | Syy (maalivahti sulki kiekon, pitkä kiekko, paitsio, jäähy, maali, kiekko ulos kaukalosta (D-063), erän loppu (D-064)); paitsiossa olleet pelaajat (vain syyllä paitsio, D-089) |
| Kiekko päätyyn | Ampuja, lopputulos kamppailusta | Ampuja, lopputulos kamppailusta, voimasuhteet (M-8; vain kun kiekko lyödään hyökkäysalueen ulkopuolelta, D-089) |
| Hallittu alueelletuonti | ennallaan | Ei kenttämuutosta. "Voimasuhteet" lasketaan uuden M-8:n mukaan. Lisätään huomautus: sijainnit (`placement`) ovat tukijoiden seurausliikkeen jälkeen (B1) |

- Kiekko päätyyn -tapahtuman voimasuhde on valinnainen. Sitä tarvitaan, jotta Q-068:n päätöksen (kuljetus vai päätyyn samassa voimasuhteessa) voi mitata. Ilman sitä Harness ei näe, mistä tilanteesta päätyyn lyötiin.
- JSON-vienti: `offsidePlayerIds` (lista) Stoppage-tapahtumaan ja `numbers` DumpIn-tapahtumaan, molemmat valinnaisia. Toiston tekstiin "Offside" (programmer, `ReplayText`).

## 10. Harness-raportti (programmer, ei tavoitteita ilman Jerryä)

- Paitsiot per ottelu ja tag-up-pitojen määrä per ottelu.
- Rushin päätyyn lyönnit per ottelu sekä kuljetusten ja päätyyn lyöntien jakauma voimasuhteittain.
- Siirtymävaiheessa (osat 2–3) voimasuhdejakauma sekä nykyisellä että ehdotetulla M-8:lla (diagnostiikka).
- Ylivoimaisten tuontien osuus hallinnan alkualueen mukaan (keskialueen riistot, osa C).
- **Uusi tavoiteavain** `offsidesPerMatch` `targets.json`:iin `placeholder`-tilassa. Se on tavoitetiedoston muutos, joten se vaatii Jerryn hyväksynnän (kysymys 5).

## 11. Vaikutusarvio

Merkinnät: **M** = mitattu nykyisestä batchista, **H** = mitattu-hypoteettinen (offline-uudelleenlaskenta lokeista idealisoiduilla sijainneilla), **K** = käsin laskettu arvio.

| Mittari | Nyt (M) | Kaikkien osien jälkeen | Tyyppi | Perustelu |
| --- | --- | --- | --- | --- |
| Voimasuhteet N < M / N = M / N > M | 66,2 / 33,8 / 0,0 | n. 50–62 / 34–45 / 3–8 | H + K | F + B1: 61,5 / 35,7 / 2,8 (H). D −3: 47–56 / 39–49 / 4 (H). Todelliset tukijat ovat hitaampia kuin ideaaliset, joten alivoimaisia on enemmän (K). B2/B3 ja C nostavat ylivoimaisia +2–5 %-yks. (K). Riski: tasavoimaiset > 40 % kynnyksellä −3. Säätö kynnyksellä ja `entryLevelDefenderLanes`-arvolla |
| K4: keskimääräinen N − M | −0,98 | −1,30 ennen osaa D | H | × 0,25 = −0,08 logit → kuljetuksen onnistuminen −1,5…−2 %-yks. (K). Osa D poistaa heikoimmat (N − M ≤ −3), joten onnistuneiden keskiarvo nousee: `zoneEntrySuccessCarry` 63,0 → n. 64–67 % (K) |
| Paitsiot per ottelu | 0 | A1: n. 0. A2: n. 2 | K | Tag-up estää rushin paitsiot. Kuviot ovat validoituja. A2: 150 ylitystä × 1,5 % |
| Tag-up-viiveet | – | n. 25 per ottelu `rulebased` vs `rulebased` (21,8 % rushin kuljetuksista), n. 5 `rulebased` vs `random` (11 %) | M (osuus) + K (määrä) | Lähes kaikki johtuvat forecheck212:n syvästä F2:sta. Viive 1–2 toimintoa (3–6 s) nostaa puolustuksen O:ta 0,12–0,24 |
| Aloitukset per ottelu / katkoväli | 58,3 / 62,4 s | 58,5–61 / 59–61,5 s | K | A2 +2. Osa C: siniviivalaukaukset +n. 2 per ottelu × ohi n. 40 % × ulos 25 % ≈ +0,2. Osa D ei tuo katkoja (kulmat eivät ole maalivahdin ulottuvilla) |
| Kiekko päätyyn per ottelu | 6,2 | n. 25–40 (`rulebased` vs `rulebased`) | H + K | 19–28 % rushin kuljetuksista kynnyksellä −3 |
| Maalit per ottelu (`rulebased` vs `rulebased`) | 6,39 | n. 5,7–6,3 | K | Tag-up ja päätyyn lyönnit vähentävät rushin ja riistojen paikkoja (xG-osuudet 14 % ja 50 %). Tag-up siirtää osan riistolaukauksista yli 8 s:n ikkunan: −4…−8 %. Osa C (pisteet, korkea slotti) ja B2 tuovat +1…+3 %. Pysyy haarukassa 4–7 |
| `rulebased` vs `random` | 79,2 % | n. 76–79 % | K | Tag-up rankaisee eniten forecheck212:n ja rushin yhdistelmää (`rulebased`-tilan `normal`). `random` käyttää sitä vain osan ajasta. Yhteinen päätyyn lyönnin kynnys ei erota tekoälyjä. Riski: lähellä 75 %:n rajaa, korjaus valmentajan ohjeella (kysymys 4) |
| Taktiikkamatriisi: rush − regroup karvausta vastaan | +12,6 | n. +7…+11 | K | Joukkue A pelaa itse forecheck212:ta, joten sen rushit odottavat tag-upia. Raja ≥ +5 pysynee |
| Taktiikkamatriisi: regroup − rush trapia vastaan | +18,9 | n. +12…+19 | K | Trapia vastaan 1v5- ja 2v5-kuljetukset muuttuvat päätyyn lyönneiksi, mikä parantaa rushia trapia vastaan. Raja ≥ +5 pysyy |
| Taktiikkamatriisi: regroup karvausta vastaan | 37,4 % | n. 37–40 % | K | Regroup ei kärsi tag-upista (8 s riittää). Raja ≥ 35 % pysyy |

Mittaus vahvistaa tai kumoaa jokaisen K-arvion (luku 2). Jos mikä tahansa D-078:n raja, voimasuhdehaarukka tai `rulebased` vs `random` ≥ 75 % rikkoutuu, ensimmäiset säätönupit ovat `rushDumpMaxStrengthDifference` ja `entryLevelDefenderLanes`. K4 säädetään vain kerran ja vasta osan 6 jälkeen.

## 12. Päätökset ja kysymykset Jerrylle

Mikään kohta ei estä speksin hyväksymistä, joten kysymyksiä ei ole kirjattu `questions.md`:hen. Kohdat 1–5 ovat speksin hyväksymispäätöksiä.

1. **M-8:n määritelmä (muuttaa lukittua D-084:ää).** Hyväksytkö ehdotuksen "hyökkääjät tasalla mukaan, puolustajat tasalla vain kaistan säteellä 1" (H: 61,5–65,7 / 31,8–35,7 / 2,5–3,5)? Vaihtoehto on symmetrinen "tasalla molemmille" ja päätyyn lyönti kynnyksellä −2 (H: 68,7 / 26,9 / 4,4).
2. **Paitsion ajoitus tahdin alusta.** Sama tahti = kiekon kanssa samaan aikaan = sallittu. Tämä pitää nykyiset kuviot valideina.
3. **A2 ajoitusvirhe** (uusi tarkistus, Pelinluku). Ilman sitä tekoäly ei koskaan ole paitsiossa. Montako paitsiota per ottelu haluat? Onko Wisehockeyssa katkojen syitä?
4. **Kuljetus vai päätyyn:** yhteinen kynnys `tuning.json`:ssa (speksi, oletus −3) vai valmentajan ohje (Q-068: taitopäätös, uusi `Plan`-kenttä)? Huom.: Jerryn esimerkki 1v3 (−2) vie offline-arviossa tasavoimaiset 50–55 %:iin.
5. **Uudet mekaniikat B2 ja B3** (Nopeus-irtiotto ja hitaan puolustajan jälkeen jääminen) sekä uusi tavoiteavain `offsidesPerMatch` (`placeholder`).
6. **Kysymys veljelle:**
   - Mihin kulmaan suorahyökkäyksen päätyyn lyönti tyypillisesti menee, kiekollisen puolelle vai ristiin (maalivahdin kiekonkäsittely)?
   - Tuleeko paitsion aloitus Liigassa aina hyökkäysalueen viereiseen keskialueen pisteeseen?
   - Kuinka suuri osa alueelle tuloista on päätyyn lyöntejä (Wisehockeyn data, jos sellainen on)?
