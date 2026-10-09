# Kiekkovalmentaja – statsit ja tarkistukset

Oct 9, 2026 · @Jerry

## Periaatteet

Kenttäpelaajilla on 12 statsia ja maalivahdeilla 6. Jokainen stat vaikuttaa ainakin yhteen tarkistukseen, eikä kaksi statsia tee samaa asiaa.

- **Asteikko:** sisäisesti 1–20. Riittävän tarkka tasapainotukseen, helppo generoida.
- **Näkyvyys:** pelaaja näkee ensin neljä ryhmäarvosanaa (A–E). Yksittäiset statsit näkyvät samalla asteikolla, kun kortin avaa. Raakanumeroita ei näytetä casual-näkymässä.
- **Arvosanojen rajat:** A = 17–20, B = 13–16, C = 9–12, D = 5–8, E = 1–4.
- **Ryhmäarvosana** on ryhmän statsien keskiarvo pyöristettynä.

## Kenttäpelaajien statsit

Nopeus ja ketteryys pidetään erillään, koska nopeus ratkaisee suorahyökkäykset ja ketteryys alueella pelaamisen.

| Ryhmä | Stat | Mihin vaikuttaa |
| --- | --- | --- |
| Luistelu | Nopeus | Kiekon kuljetus alueelle, kilpajuoksu irtokiekkoon, suorahyökkäykset, paluu puolustukseen |
| Luistelu | Ketteryys | 1v1-harhautus, suunnanmuutokset, irtipääsy vartijasta |
| Luistelu | Kestävyys | Energian kulutus vaihdon aikana |
| Hyökkäys | Kädet | Harhautus, syötön vastaanotto, kiekonhallinta paineen alla |
| Hyökkäys | Syöttö | Syötön onnistuminen, varsinkin riski- ja poikittaislinjoilla |
| Hyökkäys | Laukaisutarkkuus | Maalintekovaara lähietäisyydeltä ja slotista |
| Hyökkäys | Laukaisuvoima | Maalintekovaara kaukaa, siniviivalaukaukset, reboundien syntyminen |
| Puolustus | Asemapeli | Syöttölinjojen peittäminen, 1v1-puolustus, blokit |
| Puolustus | Pelinluku | Syötönkatkot, järjestäytymisen nopeus, tilaisuuksien huomaaminen siirtymissä |
| Fyysisyys | Voima | Taklaukset, irtokiekkokamppailut, maskissa pysyminen |
| Fyysisyys | Kurinalaisuus | Jäähyriski taklauksissa ja kamppailuissa |
| Fyysisyys | Aloitus | Aloitusten voittaminen, merkittävä vain sentterillä |

## Maalivahtien statsit

Luonnos, joka tarkennetaan veljen vastausten perusteella. Maalivahdin statsit näkyvät yhtenä ryhmänä.

| Stat | Mihin vaikuttaa |
| --- | --- |
| Refleksit | Läheltä tulevat ja yllättävät laukaukset, ohjaukset, reboundilaukaukset |
| Sijoittuminen | Kaikki laukaukset, varsinkin kaukaa ja hyvästä kulmasta |
| Liikkuvuus | Poikittaissyöttöjen (Royal Road) jälkeiset laukaukset ja nopeat siirtymät |
| Reboundikontrolli | Mihin kiekko jää torjunnan jälkeen |
| Kiekonkäsittely | Päätyyn ammuttujen kiekkojen pysäytys, avausten käynnistys |
| Henkinen kestävyys | Kuinka paljon paine ja väsymys heikentävät suoritusta |

## Tarkistukset

Jokainen tapahtuma on yksi tarkistus: hyökkääjän painotetut statsit vastaan puolustajan painotetut statsit, perustason ympärillä. Kaikki painot ja kertoimet ovat alustavia ja kalibroidaan simulaatiolla.

```latex
P = \frac{1}{1 + e^{-\left(a + k\,(H - D) + M\right)}}, \qquad a = \ln\frac{p_0}{1 - p_0}
```

- H ja D ovat hyökkääjän ja puolustajan statsien painotetut summat (1–20).
- p0 on perustaso: onnistumisen todennäköisyys, kun H = D. Se tulee otteluanalyyseista.
- k on herkkyys, alustavasti 0,15 per stat-piste. Se ratkaisee, kuinka paljon statsit painavat suhteessa tuuriin.
- M on muokkaajien summa: järjestäytyneisyys, energia, paine, kemia ja vire.

| Tapahtuma | Hyökkääjä (paino) | Puolustaja (paino) | Perustaso p0 | Muokkaajat |
| --- | --- | --- | --- | --- |
| Aloitus | Aloitus 0,8, Kädet 0,2 | Aloitus 0,8, Kädet 0,2 | 50 % | Kotijoukkueen etu (pieni) |
| Syöttö | Syöttäjän Syöttö 0,6, vastaanottajan Kädet 0,4 | Lähimmän puolustajan Pelinluku 0,5, Asemapeli 0,5 | 85 % (analyysi) | Etäisyys syöttölinjasta, poikittaislinja vaikeampi, järjestäytyneisyys, paine (analyysissa paineen alaiset syötöt onnistuivat n. 13–15 %-yksikköä heikommin) |
| Kuljetus alueelle | Nopeus 0,5, Kädet 0,5 | Asemapeli 0,5, Nopeus 0,5 | Täytetään analyyseista | Järjestäytyneisyys, puolustusjärjestelmä |
| Kiekko päätyyn | Ei tarkistusta | Ei tarkistusta | – | Johtaa irtokiekkoon, MV:n kiekonkäsittely |
| Harhautus 1v1 | Kädet 0,5, Ketteryys 0,5 | Asemapeli 0,6, Ketteryys 0,4 | Täytetään | Energia |
| Avaus omasta päästä | Syöttö 0,5, Pelinluku 0,5 | Karvaajien Nopeus 0,5, Pelinluku 0,5 | Täytetään | Puolustusjärjestelmä (karvaus vs. trap) |
| Laukaus | Tarkkuus ja Voima etäisyyden mukaan painotettuna | MV Sijoittuminen 0,6, Refleksit 0,4 | Laukauspaikan xG | Royal Road -syöttö, maski, paine, järjestäytyneisyys |
| Blokki | – | Asemapeli 1,0 | Täytetään | Laukauksen etäisyys |
| Rebound | – | MV Reboundikontrolli | Jakauma: pito / kulmaan / keskelle | Laukaisuvoima, MV:n energia |
| Irtokiekko | Voima 0,5, Kädet 0,3, Nopeus 0,2 | Samat | n. 36 / 22 / 41 % (voitto / ei voittajaa / häviö) | Ketkä ehtivät paikalle (Nopeus, järjestelmätilan ohjeet) |
| Taklaus | Voima 0,7, Pelinluku 0,3 | Voima 0,5, Kädet 0,5 | Täytetään | Jäähyriski Kurinalaisuuden mukaan |

Laukauksessa Tarkkuuden ja Voiman paino liukuu etäisyyden mukaan: slotista Tarkkuus 0,8, siniviivalta Voima 0,7.

## Tapahtumaskeema

Simulaation tapahtumaloki noudattaa oikean otteluanalyysin aikajanan rakennetta, jotta tuloksia voi verrata suoraan. Jokaisella tapahtumalla on aika, erä, voimasuhteet kentällä (esim. 5 vs 5) ja pelaajien sijainnit toistoa varten.

| Tapahtuma | Kentät |
| --- | --- |
| Aloitus | Osallistujat, voittaja, sijainti (oma alue, keskialue, vierasjoukkueen alue) |
| Hallittu alueelletuonti | Kiekon kuljettaja, voimasuhteet (esim. 2 vs 2), tapa (kuljetus tai syöttö), lopputulos (hallinta säilytetty tai menetetty) |
| Kiekko päätyyn | Ampuja, lopputulos kamppailusta |
| Syöttö | Syöttäjä, vastaanottaja, paineen alla (kyllä/ei), onnistui (kyllä/ei) |
| Laukaus | Laukoja, torjuja tai blokkaaja, xG, paikkaluokka, paikkatyyppi, laukausnopeus, lopputulos (maali, torjuttu, blokattu, ohi) |
| Kiekonmenetys / riisto | Menettäjä, riistäjä, sijainti |
| Kamppailu | Osallistujat, lopputulos (voitto, ei voittajaa, häviö) |
| Pelikatko | Syy (maalivahti sulki kiekon, pitkä kiekko, paitsio, jäähy, maali) |
| Vaihto | Ketju ulos, ketju sisään |

## Järjestäytyneisyys, paine ja paikkatyypit

Järjestäytyneisyys kertoo, kuinka hyvin puolustus on järjestelmänsä paikoilla. Se tekee siirtymistä vaarallisia ja kuvioista tärkeitä järjestäytynyttä puolustusta vastaan.

**Järjestäytyneisyys (0–100 %)**

- Kiekonmenetyksen jälkeen menettäneen joukkueen järjestäytyneisyys putoaa. Mitä korkeammalla kaukalossa menetys tapahtui, sitä matalammalle se putoaa.
- Jokainen tapahtuma palauttaa sitä. Palautumisen nopeus riippuu puolustajien Nopeudesta ja Pelinluvusta.
- Matala järjestäytyneisyys heikentää puolustajien tarkistuksia ja nostaa laukauspaikkojen xG:tä.
- Siirtymäohje: suorahyökkäys hyödyntää matalan järjestäytyneisyyden heti, kokoaminen antaa puolustuksen järjestäytyä mutta käynnistää pelikirjan kuvion.

**Voimasuhteet pelaajan näkymässä.** Sisäisesti järjestäytyneisyys lasketaan siitä, kuinka moni puolustaja on ehtinyt kiekon taakse ja järjestelmänsä paikalle. Pelaajalle se näytetään voimasuhteena alueelle tultaessa, kuten otteluanalyysissa (esim. 3 vs 2 tai 2 vs 2). Kiekkofani ymmärtää sen heti.

**Paine**

- Kasvaa onnistuneista alueelle tuloista, laukauksista ja ajasta hyökkäysalueella.
- Parantaa seuraavia laukauksia ja kuluttaa vastustajan maalivahdin ja puolustajien energiaa.
- Nollautuu osittain katkolla. Maalivahdin Henkinen kestävyys pienentää vaikutusta.

Paine näytetään ottelun jälkeen momentum-kaaviona, kuten otteluanalyysissa: kummankin joukkueen paine ajan funktiona, maalit ja ylivoimat merkittyinä.

**Paikkatyypit**

Jokainen laukaus merkitään syntytavan mukaan, ja testiympäristö raportoi ne kuten otteluanalyysi:

| Paikkatyyppi | Milloin |
| --- | --- |
| Suorahyökkäys | Laukaus ennen kuin puolustus on järjestäytynyt alueelle tulon jälkeen |
| Kiekonriisto | Laukaus pian hyökkäysalueella tai keskialueella tehdyn riiston jälkeen |
| Alueella pelaaminen | Laukaus järjestäytynyttä puolustusta vastaan pelikirjan kuviosta |
| Ylivoima / alivoima | Laukaus erikoistilanteessa |
| Aloitus | Laukaus suoraan aloituskuviosta |

## Muuttuvat tilat

Nämä eivät ole statseja: pelaaja ei valitse niitä draftissa, vaan ne muuttuvat pelaamalla.

| Tila | Aikajänne | Miten muuttuu | Vaikutus |
| --- | --- | --- | --- |
| Energia | Vaihto ja ottelu | Kuluu jokaisessa tapahtumassa Kestävyyden mukaan, palautuu penkillä | Matala energia heikentää kaikkia tarkistuksia |
| Vire | Viikko | Pieni satunnainen heilahdus ja viime otteluiden onnistumiset | Enintään ±2 statsipistettä |
| Ketjukemia | Kausi | Kasvaa, kun samat pelaajat pelaavat yhdessä | Bonus ketjun sisäisiin syöttöihin ja järjestäytymiseen |
| Kehitys | Kausi | Pysyvä nousu pelatuista otteluista ja onnistumisista | Nostaa statseja kauden aikana |

## Roolit generoinnin pohjina

Rooli määrää, mitkä statsit painottuvat generoinnissa. Jokainen pelaaja saa roolin painotukset ja satunnaisen hajonnan, joten saman roolin pelaajat ovat tunnistettavia mutta erilaisia.

| Rooli | Pelipaikka | Vahvat statsit | Heikot statsit |
| --- | --- | --- | --- |
| Snaipperi | Hyökkääjä | Laukaisutarkkuus, Kädet | Asemapeli, Voima |
| Pelinrakentaja | Sentteri tai hyökkääjä | Syöttö, Pelinluku, Kädet | Laukaisuvoima, Voima |
| Nopea laituri | Hyökkääjä | Nopeus, Ketteryys | Voima, Aloitus |
| Voimahyökkääjä | Hyökkääjä | Voima, Laukaisutarkkuus | Ketteryys |
| Kaksisuuntainen | Sentteri tai hyökkääjä | Asemapeli, Pelinluku, Aloitus | Laukaisuvoima |
| Taklaaja | Hyökkääjä tai pakki | Voima | Kurinalaisuus, Kädet |
| Tykkipakki | Pakki | Laukaisuvoima, Syöttö | Nopeus |
| Puolustava pakki | Pakki | Asemapeli, Voima | Kädet, Laukaisutarkkuus |
| Liikkuva pakki | Pakki | Nopeus, Syöttö, Pelinluku | Voima |

Maalivahtiroolit (esim. torjuva vs. kiekkoa pelaava) lisätään veljen vastausten jälkeen.

## Tavoitearvot

Simulaation pitää osua oikean kiekon haarukkaan. Alla on otteluanalyysien luvut molemmilta joukkueilta. Tavoitehaarukat ovat vain tiedostossa `data/targets.json` (D-016), eikä niitä toisteta tässä. Haarukat täytetään, kun 3–5 ottelun analyysit on käyty läpi. Analyysit ovat sisäistä materiaalia, joten repoon kirjataan vain keskiarvot.

| Mittari | Ottelu 1, 5.9.2026 (A / B) | Ottelu 2, 8.10.2026 (A / B) | Käyttö |
| --- | --- | --- | --- |
| Syöttöjen onnistuminen, kaikki | 84,8 % / 84,6 % | 84 % / 81 % | Kalibrointi (`targets.json`) |
| Syöttöjen onnistuminen, 5v5 | – | 83 % / 78 % | Kalibrointi (`targets.json`) |
| Syöttöjen onnistuminen paineen alla | – | 69 % / 68 % | Kalibrointi (`targets.json`) |
| Laukaukset paineen alla (osuus) | – | 68 % / 76 % | Kalibrointi (`targets.json`) |
| Kiekkokamppailut (voitto / ei voittajaa / häviö) | 36 % / 22 % / 41 % | Kirjataan | Kalibrointi (`targets.json`) |
| Laukaukset 5v5 | – | 45 / 18 | Kalibrointi (`targets.json`) |
| Laukaukset 5v5: suorahyökkäys / päätypeli / riisto | – | 12 / 26 / 9 ja 9 / 8 / 3 | Kalibrointi (`targets.json`) |
| Maalipaikat 5v5: huippu / hyvä / kohtalainen | – | 7 / 3 / 12 ja 0 / 0 / 3 | Kalibrointi (`targets.json`) |
| Maalipaikat per joukkue, kaikki | 14 / 13 | – | Kalibrointi (`targets.json`) |
| xG per joukkue | 3,75 / 3,09 | n. 5,0 / 1,2 (erien summa) | Kalibrointi (`targets.json`) |
| Tasakentällisen xG:n osuus: suorahyökkäys | n. 67 % / 45 % | – | Kalibrointi (`targets.json`) |
| Tasakentällisen xG:n osuus: kiekonriisto | n. 23 % / 33 % | – | Kalibrointi (`targets.json`) |
| Tasakentällisen xG:n osuus: alueella pelaaminen | n. 10 % / 22 % | – | Kalibrointi (`targets.json`) |
| Yksittäisen laukauksen xG, tyypillinen | – | 0,003–0,05 | Kalibrointi (`targets.json`) |
| Hyökkäysalueaika 5v5 | – | 4:56 / 2:18 | Kalibrointi (`targets.json`) |
| Aloitusten voitot | 41 % / 59 % | 47 % / 53 % | Ei kalibrointiin, kertoo hajonnasta |
| Alueelle tulojen onnistuminen tavoittain | Kirjataan | Kirjataan | Kalibrointi (`targets.json`) |
| Ykkösketjun jääaika | – | n. 21,5 min (nelosketju 0 min) | Väsymysmekaniikan vertailukohta |

Ottelun 1 xG-osuudet on laskettu tasakentällisen kokonais-xG:stä (A 2,41, B 2,33). Ottelu 2 päättyi jatkoajalla, ja sen laukausjakauma painottui päätypeliin, joten paikkojen määrä ja vaarallisuus pitää raportoida erikseen.

## Avoimet kysymykset

- [ ] Maalivahdin statsit ja roolit: vastaavatko ne veljen näkemystä?
- [ ] Paikkaluokkien xG-rajat: millä rajoilla analyysi jakaa paikat huippu-, hyviin ja kohtalaisiin? (kysytään veljeltä)
- [ ] Puolustusjärjestelmien vaikutus tarkistuksiin: mitkä järjestelmät heikentävät mitäkin hyökkäystapaa?
- [ ] Herkkyys k: kuinka paljon statsit painavat suhteessa tuuriin (ratkaistaan simulaatiolla)
- [ ] Laukauspaikkojen xG-arvot vyöhykkeittäin
- [ ] Erikoisominaisuudet (esim. ylivoimaspesialisti): otetaanko mukaan prototyyppiin vai myöhemmin?
