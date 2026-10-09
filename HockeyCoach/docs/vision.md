# Kiekkovalmentaja – visio ja pilarit

Oct 9, 2026 · @Jerry

## Tiivistelmä

Mobiilipeli, jossa pelaaja on jääkiekkovalmentaja: hän kokoaa ketjut, valitsee tai rakentaa kuviot ja puolustusjärjestelmän, ja pelaajat pelaavat vaihdon automaattisesti. Peli yhdistää auto battler -genren suunnittele–katso-rytmin ja oikean jääkiekon taktiikan.

**Pitch:** "Valmentajan pelikirja taskussa. Suunnittele vaihto, katso sen toimivan, voita lukemalla vastustajaa paremmin."

Ydin on asynkroninen PvP lyhyissä kausissa. Jokainen kausi alkaa draftilla, joten kaikki lähtevät samalta viivalta, eikä parasta joukkuetta voi ostaa.

## Kohdeyleisö

Ensisijainen pelaaja on casual-urheilupelaaja ja kiekkofani, ei hardcore auto battler -pelaaja. Hän tuntee lajin, mutta ei halua opetella monimutkaista pelijärjestelmää.

|  | Haluaa | Ei halua |
| --- | --- | --- |
| Ymmärrettävyys | Lajin omat käsitteet: ketjut, ylivoima, väsymys, aikalisä | Pelaajaslangia (synergiat, unit-tierit), numeroseinät |
| Sessio | 5–8 minuutin otteluita, pelattavissa yhdellä kädellä | Pitkiä sessioita tai pakkoa olla paikalla samaan aikaan vastustajan kanssa |
| Reiluus | Ymmärtää, miksi voitti tai hävisi | Häviöitä, jotka tuntuvat pelkältä tuurilta |
| Eteneminen | Oma joukkue, johon kiintyy | Grindiä tai maksumuuria parhaiden pelaajien edessä |

Toissijainen yleisö on taktiikasta innostuva pelaaja, joka haluaa hallita kuvioeditorin ja ketjujen vastakkainasettelun. Peli palvelee häntä syvyydellä, ei monimutkaisuudella pinnalla.

## Suunnittelun pilarit

Jokainen suunnittelupäätös testataan näitä viittä periaatetta vasten. Ristiriitatilanteessa järjestys ratkaisee.

1. **Taito voittaa tuurin.** Satunnaisuus luo tilanteita ennen päätöstä, mutta ei ratkaise lopputulosta päätöksen jälkeen. Hyvä suunnitelma voittaa todennäköisesti, ja mittaamme sen simulaatiolla.
2. **Helppo pinta, syvä pohja.** Ensimmäinen ottelu onnistuu kahdella päätöksellä (ketju ja kuvio). Syvyys paljastuu kerroksittain: vastakkainasettelu, väsymys, oma kuvioeditori.
3. **Kiekon kieli.** Mekaniikat nimetään ja toimivat kuten oikeassa lajissa. Kiekkofanin pitää katsoa toistoa ja ajatella "juuri noin tuo kuvio toimii". Simulaation luvut verrataan oikeisiin otteluanalyyseihin.
4. **Jokainen tappio opettaa.** Pelaaja näkee aina, miksi maali syntyi tai miksi kuvio epäonnistui. Selitys on yksi lause, ei taulukko.
5. **Reilu kilpailu.** Kaikki aloittavat kauden samalta viivalta. Rahalla ei saa etua otteluissa.

Realismi tarkoittaa uskottavia tilanteita ja oikeita taktiikoita, ei fysiikkaa tai oikeita tilastojakaumia. Kun hauskuus ja realismi ovat ristiriidassa, hauskuus voittaa: esimerkiksi otteluissa on enemmän maaleja kuin oikeassa kiekossa.

## Ydinsilmukka

Peli rakentuu kolmesta sisäkkäisestä silmukasta: vaihto on minuutteja, ottelu on sessio ja kausi on viikkoja.

| Silmukka | Kesto | Pelaajan päätökset | Tarkempi speksi |
| --- | --- | --- | --- |
| Vaihto | n. 45 pelisekuntia, muutama sekunti katsottavaa | Ketju, pelikirjan kuviot, puolustusjärjestelmä, järjestelmätilan ohjeet | Ydinsilmukka ja vaihdon simulaatio |
| Ottelu | 3 erää, 5–8 minuuttia | Ketjujen kierto, väsymyksen hallinta, yksi aikalisä, maalivahdin vetäminen | Ydinsilmukka ja vaihdon simulaatio |
| Kausi | 4–6 viikkoa | Draft, viikoittainen siirtoikkuna, ketjujen kehittäminen | Joukkueen rakentaminen (alla) |

**Vaihto lyhyesti:** penkillä valinnat, aloitus, kuviotila (tahti kerrallaan tarkistuksia), laukaus, järjestelmätila (irtokiekot ja maalille ajo), kunnes katko palauttaa penkille. Kiekonmenetys siirtää hallinnan vastustajalle ja aktivoi oman puolustusjärjestelmän.

Simulaatio on tapahtumapohjainen, ei jatkuva: jokainen tapahtuma kuluttaa aikaa ja energiaa. Visualisointi on toisto tapahtumalistasta.

**Siirtymät ovat yhtä tärkeitä kuin kuviot.** Oikeissa otteluanalyyseissa suurin osa vaarallisista tasakentällisen paikoista syntyy suorahyökkäyksistä ja kiekonriistoista, ei alueella pelaamisesta. Siksi puolustavalla joukkueella on järjestäytyneisyys, joka on matala kiekonmenetyksen jälkeen ja palautuu tapahtumien myötä. Nopea hyökkäys pelataan järjestäytymätöntä puolustusta vastaan, ja kuviot ratkaisevat järjestäytynyttä puolustusta vastaan.

## Pelimuodot

Peli on ytimeltään asynkroninen PvP. PvE on harjoittelua ja opetusta varten.

**PvP (ydin).** Pelaaja pelaa vastustajan tallennettua suunnitelmaa vastaan, joten kummankaan ei tarvitse olla paikalla yhtä aikaa. Vastustajan reaktiot (puolustusjärjestelmä, pelikirja) on valittu etukäteen.

**PvE (harjoitus).** Vastustajajoukkueilla on selkeät tyylit, ja jokainen opettaa yhden taktisen asian:

- Trap-joukkue: suora rynnistys ei toimi, kiekkoa pitää kierrättää.
- Karvausjoukkue: nopeat avaukset ja nopeat laiturit.
- Fyysinen joukkue: kerää jäähyjä, opettaa ylivoiman arvon.
- Tähtijoukkue: nojaa yhteen ketjuun, opettaa vastakkainasettelun ja väsymyksen.
- Boss-joukkueet loppupäässä yhdistelevät tyylejä.

Samat AI-profiilit täyttävät PvP-jonoa, kun oikeita vastustajia ei ole tarpeeksi.

**Ranking.** Pysyvä seuran taso avauksille ja kosmetiikalle, plus viikoittaiset sarjat (20–30 pelaajaa), joista kärki nousee ja häntäpää putoaa. Kauden lopussa pudotuspelit divisioonan kärjelle.

## Joukkueen rakentaminen

Jokainen kausi alkaa draftilla, ja pelaajat ovat kuvitteellisia, satunnaisesti generoituja hahmoja. Pysyvää korttikokoelmaa ei ole.

- **Draft:** n. 15 kierrosta, joissa valitaan 1 kolmesta. Kierrokset painotetaan rooleittain (pakki-, maalivahti- ja hyökkääjäkierrokset), jotta jokainen saa toimivan joukkueen. Automaattinen draft casual-pelaajalle.
- **Pelaajat:** arvosanat tai tähdet raakanumeroiden sijaan. Selkeät roolit, kuten snaipperi, pelinrakentaja, taklaaja, kaksisuuntainen hyökkääjä, tykkipakki ja maalivahtityylit.
- **Kehitys kauden aikana:** pelaajat kehittyvät pelatuista otteluista ja onnistumisista. Tämä korvaa auto chessin yhdistämismekaniikan.
- **Ketjukemia:** kasvaa, kun samat pelaajat pelaavat yhdessä.
- **Siirtoikkuna:** rajattu määrä vaihtoja kerran viikossa, jotta roster elää.

Pelaajadata on dataa, ei kovakoodia. Näin oikean liigadatan voi kytkeä myöhemmin, jos lisenssi joskus järjestyy.

## Kuviot ja editori

Pelaaja käyttää valmiita kuvioita pelikirjasta ja voi myöhemmin rakentaa omia rakenteellisella editorilla. Vapaata piirtämistä ei tehdä.

- **Pelikirja (taso 1):** valmiit kuviot, rakennettu samalla editorilla, jota pelaajat käyttävät. Editori on siis myös sisällöntuotantotyökalu.
- **Editori (taso 2):** kaukalo on jaettu solmuihin, ja sormella vedetty viiva napsahtaa niihin. Kuviossa on enintään 3–4 tahtia, ja toimintoja on neljä: luistele, syötä, laukaise, aja maalille.
- **Ensikosketus:** valmiin kuvion muokkaaminen, ei tyhjä kaukalo.
- **Puolustajien haamut:** vastustajan järjestelmä noudattaa selkeitä sääntöjä, joten editori näyttää, missä puolustajat todennäköisesti ovat kunakin tahtina. Tämä tekee kuvioshakista ennakoitavaa.
- **Riskin väri:** syöttölinja on vihreä, keltainen tai punainen sen mukaan, kuinka lähellä puolustajaa se kulkee. Ei numeroita.
- **Esikatselu:** näyttää liikkeet ja riskit, ei lopputulosta.
- **Tuttuus:** sama kuvio monta kertaa ottelussa heikkenee, koska puolustus oppii sen.

## Monetisointi

Raha tulee kosmetiikasta ja kausipassista, ei pelaajista tai pelietuuksista.

| Tehdään | Ei tehdä |
| --- | --- |
| Pelipaidat, kaukalot, maalilaulut, juhlinnat | Pelaajien tai korttien myynti |
| Kausipassi kosmeettisilla palkinnoilla | Gacha tai lootboxit |
|  | Draftin uudelleenarvonta rahalla |
|  | Statsibonukset tai energiat rahalla |

## Mitä peli ei ole

- Ei reaaliaikainen moninpeli eikä toimintapeli: pelaaja ei ohjaa luistelijoita ottelun aikana.
- Ei Football Managerin kaltainen taulukkopeli: tilastot näkyvät arvosanoina, ja syvyys on taktiikassa.
- Ei fysiikkasimulaatio: tapahtumat ovat tilastotarkistuksia, jotka visualisoidaan uskottavasti.
- Ei keräilykorttipeli: pysyvää kokoelmaa tai maksullista pelaajaetua ei ole.
- Ei lisensoitu liigapeli alkuvaiheessa: pelaajat, joukkueet ja liiga ovat kuvitteellisia.

## Lukitut päätökset ja avoimet kysymykset

Lukittuja päätöksiä muutetaan vain Jerryn hyväksynnällä ja muutoslokin kautta. Agentit eivät muuta niitä itsenäisesti.

| Päätös | Valinta |
| --- | --- |
| Alusta | Mobiili, kaikki ohjaus sormella |
| Ydinmuoto | Asynkroninen PvP, PvE harjoitukseksi |
| Joukkue | Kausidraft, ei pysyvää kokoelmaa |
| Pelaajat | Kuvitteellisia, satunnaisesti generoituja |
| Kuviot | Valmis pelikirja + solmu- ja tahtipohjainen editori |
| Simulaatio | Tapahtumapohjainen, kahden tilan malli (kuviotila ja järjestelmätila) |
| Siirtymät | Puolustuksen järjestäytyneisyys, pelaaja valitsee siirtymäohjeen (suorahyökkäys tai kokoaminen) |
| Paikkatyypit | Suorahyökkäys, kiekonriisto, alueella pelaaminen, ylivoima, alivoima, aloitus |
| Monetisointi | Vain kosmetiikka ja kausipassi |

**Avoimet kysymykset**

- [ ] Puolustusjärjestelmien säännöt alueella pelaamisessa (veli: tietty puolustuskuvio vastaa tiettyä hyökkäyskuviota, tarkennetaan)
- [ ] Maalivahdin ominaisuudet ja reboundimekaniikka (odottaa veljen vastauksia)
- [ ] Tavoitearvot muutamasta otteluanalyysista: paikkojen jakauma syntytavoittain, syöttöjen ja alueelle tulojen onnistuminen, kamppailujen jakauma
- [ ] Kauden tarkka pituus: 4 vai 6 viikkoa?
- [ ] Puuttuminen kesken ottelun: riittääkö yksi aikalisä ja pikavaihto?
- [ ] Pelin nimi

## Syysloman prototyyppi

Tavoite on headless-simulaatio, joka vastaa pelin tärkeimpään kysymykseen: ratkaiseeko taito vai tuuri. Ei grafiikkaa, ei käyttöliittymää, ei editoria.

**Toteutetaan:** simulaatioydin puhtaana C#-kirjastona ilman Unity-riippuvuuksia, vaihdon ja ottelun simulointi tapahtumalistana sekä testiympäristö, joka ajaa tuhansia otteluita AI-tyyppien välillä ja raportoi tulokset.

**Onnistumisen kriteerit:**

- [ ] Fiksu AI voittaa satunnaisesti valitsevan AI:n 75–85 % otteluista.
- [ ] Ottelussa syntyy keskimäärin 4–7 maalia.
- [ ] Jokainen puolustusjärjestelmä häviää ainakin yhdelle hyökkäystyylille selvästi.
- [ ] Yksikään kuvio ei voita kaikkia järjestelmiä.
- [ ] Suorahyökkäykset tuottavat suuremman osan vaarallisista paikoista kuin alueella pelaaminen, kuten oikeissa analyyseissa.
- [ ] Syöttöjen onnistuminen ja kamppailujen jakauma osuvat otteluanalyysien haarukkaan.
- [ ] Testiympäristö raportoi paikat syntytavoittain samassa muodossa kuin otteluanalyysi.
- [ ] Ottelun tapahtumaloki noudattaa otteluanalyysin aikajanan rakennetta ja on luettavissa ihmiselle.

**Roolit:** Jerry on pääsuunnittelija ja hyväksyy kaikki suunnittelumuutokset. Designer-agentti ylläpitää dokumentteja ja analysoi simulaatiotuloksia. Ohjelmoija-agentti toteuttaa speksin mukaan ja kysyy, kun speksi on epäselvä.
