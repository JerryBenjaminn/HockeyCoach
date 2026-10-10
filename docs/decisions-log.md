# Päätösloki

Lukittuja päätöksiä muuttaa vain Jerry. Agentit kirjaavat muutosehdotukset tilalla **ehdotettu** ja perustelevat ne raportin luvuilla.

Tilat: **lukittu** · **ehdotettu** · **hyväksytty** · **hylätty**

## Lukitut päätökset

| ID | Päivä | Päätös | Valinta | Tila |
| --- | --- | --- | --- | --- |
| D-001 | 2026-10-09 | Alusta | Mobiili, kaikki ohjaus sormella | lukittu |
| D-002 | 2026-10-09 | Ydinmuoto | Asynkroninen PvP, PvE harjoitukseksi | lukittu |
| D-003 | 2026-10-09 | Joukkue | Kausidraft, ei pysyvää kokoelmaa | lukittu |
| D-004 | 2026-10-09 | Pelaajat | Kuvitteellisia, satunnaisesti generoituja | lukittu |
| D-005 | 2026-10-09 | Kuviot | Valmis pelikirja + solmu- ja tahtipohjainen editori | lukittu |
| D-006 | 2026-10-09 | Simulaatio | Tapahtumapohjainen, kahden tilan malli (kuviotila ja järjestelmätila) | lukittu |
| D-007 | 2026-10-09 | Siirtymät | Puolustuksen järjestäytyneisyys, siirtymäohje (suorahyökkäys tai kokoaminen) | lukittu |
| D-008 | 2026-10-09 | Paikkatyypit | Suorahyökkäys, kiekonriisto, alueella pelaaminen, ylivoima, alivoima, aloitus | lukittu |
| D-009 | 2026-10-09 | Monetisointi | Vain kosmetiikka ja kausipassi | lukittu |
| D-010 | 2026-10-09 | Tekniikka | C#, Sim ja AI netstandard2.1 / C# 9, Harness ja testit net10.0 (muutettu 2026-10-09: koneella ei ole .NET 8 -ajonaikaa) | lukittu |
| D-011 | 2026-10-09 | Kieli | Koodi englanniksi, dokumentit suomeksi | lukittu |
| D-012 | 2026-10-09 | Determinismi | Determinismi samassa ajoympäristössä riittää, bittitarkkuutta .NETin ja Unityn välillä ei vaadita. Asynkronisessa PvP:ssä palvelin laskee ottelun ja lähettää tapahtumalokin, jota asiakas vain toistaa. Kaikki matematiikka `CheckMath`-luokan takana, jotta sen voi vaihtaa yhdessä paikassa | lukittu |
| D-013 | 2026-10-09 | Dataskeemat | `docs/data-schema.md` on datatiedostojen skeemojen ainoa totuus, designer omistaa ja ylläpitää | lukittu |
| D-014 | 2026-10-09 | Todennäköisyyden rajat | Tarkistuksen onnistumistodennäköisyys rajataan välille min–max. Arvot (0,02 / 0,98) ovat tasapainoarvoja `tuning.json`:ssa. Tarkennus 2026-10-09 (Q-009): raja koskee vain tarkistuksia (syötöt, kamppailut, aloitukset jne.), ei laukauksen maalintodennäköisyyttä eikä xG:tä. Laukauksilla omat rajat `tuning.json`:ssa | lukittu |
| D-015 | 2026-10-09 | Riippuvuudet | Simissä ja AI:ssa ei ajonaikaisia riippuvuuksia. Käännösaikaiset analysaattorit (BannedApiAnalyzers) sallittuja | lukittu |
| D-016 | 2026-10-09 | Tavoitehaarukat | `data/targets.json` on tavoitehaarukoiden ainoa totuus. `stats-and-checks.md` viittaa siihen eikä toista lukuja | lukittu |
| D-017 | 2026-10-09 | Datasopimukset | Tarkistuksen painot summautuvat 1:een kummallakin puolella (hyökkääjä, puolustaja). Solmun id = x × leveys + y (11 × 5 -verkossa x * 5 + y). Data kirjoitetaan oman joukkueen näkökulmasta: oma pää on pienillä x:n arvoilla (11 × 5 -verkossa oma maali x = 1, D-027), vastustajan näkökulma kierrolla 180° | lukittu |
| D-018 | 2026-10-09 | Toteutus | Klassinen `.sln`, nimiavaruus `Sim.Config`. Virstanpylväässä 1 pelaajat luodaan käsin, `roles.json` myöhemmin | lukittu |
| D-019 | 2026-10-09 | Yksipuolinen tarkistus | Erillinen tarkistustyyppi: yhden osapuolen painotettu arvo vastaan referenssiarvo (oletus 10,5, `tuning.json`:ssa). D-017 koskee vain kaksipuolisia tarkistuksia. Blokki ja rebound ovat yksipuolisia (Q-013) | lukittu |
| D-020 | 2026-10-09 | Aja maalille | `driveNet` on oletuksena kiekoton: pelaaja menee maalin eteen maskiin ja reboundille (Q-005) | lukittu |
| D-021 | 2026-10-09 | Ensimmäiset järjestelmät | 2-1-2 aggressiivinen karvaus ja 1-2-2 passiivinen / trap (Q-012) | lukittu |
| D-022 | 2026-10-09 | Moduulijärjestys | `Sim.Config` sijoittuu heti `Sim.Model`-moduulin yläpuolelle | lukittu |
| D-023 | 2026-10-09 | Ketjut | Hyökkäyskolmikot ja pakkiparit ovat erillisiä yksiköitä, jotka kiertävät eri tahtiin | lukittu |
| D-024 | 2026-10-09 | Pelipaikat | C, LW, RW, LD, RD. Pelaajalla on ensisijainen pelipaikka. Väärällä puolella pelaamisen miinus on tasapainoarvo `tuning.json`:ssa | lukittu |
| D-025 | 2026-10-09 | Kysymykset | Molemmat agentit saavat lisätä kysymyksiä `questions.md`:hen, vain Jerry merkitsee ne ratkaistuiksi | lukittu |
| D-026 | 2026-10-09 | Toteutuksen poikkeamat | Nullable pois päältä Simissä ja AI:ssa (Unity-yhteensopivuus). Usean pelaajan roolit (esim. karvaajat) käyttävät statsien keskiarvoa, laskenta latauksessa | lukittu |
| D-027 | 2026-10-09 | Kaukalon verkko | 11 × 5 solmua, kummankin maalin takana oma solmurivi (E-002) | lukittu |
| D-028 | 2026-10-09 | Solmun alue | Solmun alue (oma pää, keskialue, hyökkäysalue) johdetaan `zones`-osion x-väleistä eikä sitä tallenneta solmuun (Q-022) | lukittu |
| D-029 | 2026-10-09 | baseXg-validointi | `baseXg` hylätään, jos se on välin 0–1 ulkopuolella tai yli laukausten ylärajan (Q-023) | lukittu |
| D-030 | 2026-10-09 | tuning.json-osiot | Lataaja hylkää tuntemattomat pääosiot. Sallitut osiot ja uuden osion lisäysprosessi dokumentoidaan `data-schema.md`:hen | lukittu |
| D-031 | 2026-10-09 | Kiekkotoiminnot | Viisi toimintoa: skate, pass, shoot, driveNet ja dump (E-001) | lukittu |
| D-032 | 2026-10-09 | Kiekollisen liike | Kiekollinen pelaaja liikkuu vain tahdin toiminnolla `skate`, ei siirroilla. Muut pelaajat liikkuvat siirroilla. Jokaisessa tahdissa on tasan yksi kiekkotoiminto, ja kuljetus on aina tarkistus (Q-024) | lukittu |
| D-033 | 2026-10-09 | Maalisolmu | Kenttäpelaajan kohde ei saa olla maalisolmu. `driveNet` vie maalin eteen tai slotin solmuun, maalisolmu on vain laukauksen kohde. Poikkeus: maalivahti (Q-025) | lukittu |
| D-034 | 2026-10-09 | Kuvio- ja järjestelmäskeema | Hyväksytty ehdoin: (1) toiminnot skate, pass, shoot, driveNet, dump; (2) kuviot viittaavat pelipaikkoihin LW, C, RW, LD, RD, eivät pelaajiin; (3) saman kuvion voi pelata peilattuna kummallakin laidalla; (4) järjestelmien säännöt ovat deterministisiä ja viimeinen sääntö on aina varasääntö (Q-001) | lukittu |
| D-035 | 2026-10-09 | Pushaus | Kun `dotnet test` menee läpi ja `validate` hyväksyy datan, valmiit commitit saa pushata kysymättä | lukittu |
| D-036 | 2026-10-09 | Kuvion valinta | Kun joukkue saa kiekon, valitaan prioriteettilistan ensimmäinen kuvio, joka sopii kiekon alueeseen ja jonka kiekollinen pelipaikka on sama kuin nykyisen kiekonhaltijan. Jos sellaista ei ole, kuvio alkaa tavallisella, tarkistettavalla syötöllä kuvion kiekolliselle pelipaikalle (kiekko ei siirry ilman tarkistusta, kuten D-032). Jos alueeseen ei sovi yhtään kuviota, joukkue pitää kiekon järjestelmätilassa | lukittu |
| D-037 | 2026-10-09 | Peilauksen valinta | Simulaatio valitsee peilatun version `data-schema.md`:n säännön mukaan (kuvio peilataan, jos kiekko on eri laidalla kuin kiekollisen lähtösolmu) | lukittu |
| D-038 | 2026-10-09 | Setup | Pelaajat siirtyvät kuvion lähtöpaikoille `setupSeconds`-ajan jälkeen, ja puolustajat siirtyvät samalla järjestelmänsä paikoille. Setup-aika kuluttaa vaihdon kelloa | lukittu |
| D-039 | 2026-10-09 | Kuljetuksen tarkistus | Kuljetus hyökkäysalueelle = alueelle tulo, omalta alueelta ulos = avaus, muuten harhautus lähimmän puolustajan etäisyyden mukaan. Avaus koskee myös syöttöä ulos omalta alueelta | lukittu |
| D-040 | 2026-10-09 | Epäonnistunut tarkistus | Epäonnistunut syöttö on joko syötönkatko (puolustaja saa kiekon) tai irtokiekko syöttölinjalle lähimmän puolustajan solmuun; jako `tuning.json`-arvolla (paikkamerkki). Epäonnistunut kuljetus tai harhautus: puolustaja riistää kiekon | lukittu |
| D-041 | 2026-10-09 | Tahdin järjestys | Hyökkääjien siirrot → puolustajat liikkuvat järjestelmän mukaan → toiminto. Puolustus liikkuu jokaisessa tahdissa | lukittu |
| D-042 | 2026-10-09 | Aloitukset | Ensimmäinen aloitus keskeltä. Maalin jälkeen keskeltä, maalivahdin pidon jälkeen lähimmästä oman pään pisteestä. Joukkueella, jolla ei ole aloituskuviota pisteelle, on oletusasettelu | lukittu |
| D-043 | 2026-10-09 | Vaihdon loppu (virstanpylväs 2) | Vain katko (maali tai maalivahdin pito). Jumiutumisen estävä tapahtumaraja on kutsujan parametri, ei tasapainoarvo | lukittu |
| D-044 | 2026-10-09 | Irtokiekon paikat | Määritellään `tuning.json`:ssa: rebound slottiin `netFront`-solmuun, rebound kulmaan laukaisijan puolen kulmaan, ohilaukaus päätyriville, blokattu laukaus blokkaajan solmuun | lukittu |
| D-045 | 2026-10-09 | Tapahtumaskeeman käyttö | Ei uusia tapahtumatyyppejä. Kuljetus ja harhautus näkyvät alueelle tulona tai kiekonmenetyksenä, rebound kamppailuna. Jokaiseen tapahtumaan tallennetaan kuvion ja järjestelmän id (taktiikkaraportti) | lukittu |
| D-046 | 2026-10-09 | Virstanpylvään 2 rajaus | Järjestäytyneisyys, energia ja paine ovat vakioita virstanpylvääseen 3 asti (suorahyökkäyspaikat vasta silloin). Järjestelmätilan ohjeet (`netFrontAfterShot`, `looseChasers`, `pinch`) virstanpylväässä 3, virstanpylväässä 2 kiinteät oletukset. Taklaukset ja jäähyt myöhemmin. Avoimissa kysymyksissä Q-006, Q-007, Q-008, Q-010, Q-011, Q-014 ja Q-020 edetään ehdotetuilla oletuksilla | lukittu |
| D-047 | 2026-10-09 | Maskin xG | `netFront`-solmun laukaukset (reboundit ja ohjaukset) käyttävät maskin (`crease`) xG-arvoa (E-003) | lukittu |
| D-048 | 2026-10-09 | Määritelmät | Syöttölinjan etäisyys, poikittaissyöttö, Royal Road, blokkaaja ja irtokiekon kamppailijat kirjoitetaan `data-schema.md`:hen, ja Jerry hyväksyy ne ennen toteutusta | lukittu |
| D-049 | 2026-10-10 | Määritelmät | Määritelmät M-1–M-8 (`data-schema.md`) hyväksytty sellaisinaan virstanpylvääseen 2. D-040:n, D-044:n ja D-047:n tarkennukset hyväksytty. `pass.modifiers.pressure` nimetään `underPressure`:ksi | lukittu |
| D-050 | 2026-10-10 | Laukauksen paine | Laukaustapahtumaan lisätään kenttä `underPressure` (Q-026) | lukittu |
| D-051 | 2026-10-10 | Syöttö maalin läpi | Syöttölinja saa kulkea maalisolmun kautta, paitsi suoraan maalin läpi: validointi hylkää syötön, jossa syöttäjä ja vastaanottaja ovat molemmat keskikaistalla (y = 2) maalin eri puolilla. Vinot syötöt maalin takaa slottiin ovat sallittuja (Q-027) | lukittu |
| D-052 | 2026-10-10 | Laukausnopeus | Tyhjä virstanpylvääseen 3 asti. Myöhemmin johdetaan laukaisuvoimasta pelkäksi näyttöarvoksi ilman pelivaikutusta (Q-028) | lukittu |
| D-053 | 2026-10-10 | Maalin kierto | Puolustaja, jonka askel osuisi maalisolmuun, kiertää maalin sille puolelle, jolla kiekko on (Q-029) | lukittu |
| D-054 | 2026-10-10 | Unity-kansio | Agentit eivät koske `unity/`-kansioon. Jerry hoitaa Unity-projektin itse | lukittu |
| D-055 | 2026-10-10 | Virstanpylväs 2 | Hyväksytty valmiiksi (`shift --seed 42`). Vaihdon pituus, kuvion toisto ja aina valmis puolustus korjataan virstanpylväässä 3 nykyisten päätösten mukaan | lukittu |
| D-056 | 2026-10-10 | Paineen alla -säde | `pressure.underPressureNodes` = 0 (sama solmu). Kalibroidaan raportin perusteella Wisehockeyn 68–76 %:iin (Q-032). Toteutus virstanpylväässä 3 | lukittu |
| D-057 | 2026-10-10 | trap122:n F1 | F1:n kohde on yhden solmun päässä kiekollisesta, kiekon ja keskikaistan välissä (ohjaa laitaan, ei prässää). forecheck212:n F1 menee edelleen suoraan kiekolliseen. Toteutus virstanpylväässä 3 | lukittu |
| D-058 | 2026-10-10 | Solmun pelaajamäärä | Samassa solmussa voi olla enintään yksi pelaaja kummastakin joukkueesta. Hyökkääjä ja puolustaja samassa solmussa on sallittu. Invarianttitesti lisätään | lukittu |
| D-059 | 2026-10-10 | Toinen yritys | Kun hyökkääjä voittaa irtokiekon `netFront`-solmussa tai slotissa, se laukoo heti ilman kuvion setuppia; `netFront`-solmussa käytetään maskin xG:tä (Q-038). Virstanpylväs 3, ensimmäisten joukossa | lukittu |
| D-060 | 2026-10-10 | Aloituksen jälkeen | Aloitustarkistus ratkaisee, kumpi joukkue saa kiekon. Voittaneen joukkueen aloituskuvio määrää, kuka kiekon saa; oletuksena pakki. Ei erillistä syöttötarkistusta (Q-037) | lukittu |
| D-061 | 2026-10-10 | Oletukset Q-033–Q-036 | Ohjelmoijan oletukset hyväksytty: kuviotyyppi alueen mukaan, aloituksen oletusasettelu, järjestelmätilan askeleet, epäonnistunut kuljetus | lukittu |
| D-062 | 2026-10-10 | Virstanpylvään 3 suunnitelma | Designerin ja ohjelmoijan suunnitelmat hyväksytty suosituksin: järjestäytyneisyys joukkueen skaalaariarvona, joka palautuu pelisekuntien mukaan ja jota sijainnit seuraavat viiveellä (setup ei siirrä puolustajia suoraan kohteisiin); suorahyökkäys sisäänrakennetuilla säännöillä ilman kuviota, oletussiirtymäohje `rush`; tuplarangaistus: syöttölinjan etäisyys (M-1) ei sisällä syöttäjän omaa solmua; tasapeli sallittu; kokoonpano 4 kolmikkoa ja 3 paria; aikalisä ja maalivahdin vetäminen virstanpylväässä 4; designerin vaihtosäännöt (katkovaihto ennen aloitusasettelua, puolustava joukkue ei vaihda lennosta); eksponentiaalinen penkkipalautuminen (avain nimetään uudelleen), tarkistusmaksu kaikille osallistujille, maalivahdin energia kuluu vain paineesta; trap122:n F1 omassa päässä suoraan kiekolliseen; slotti xG:lle = (7,2) ja (8,2) (Q-015); `screenContested` 0,15 (Q-031); oletusvalmentaja `RotationCoach` AI-projektissa, rajapinta Simissä; erän raja tarkistetaan kerran jokaista kokonaisuutta kohden; aluetta vaihtava toiminto lasketaan sitä edeltävän tilan mukaan; laukausnopeus tyhjä myös virstanpylväässä 3; 1000 ottelun savutesti omassa kategoriassaan; kahden joukkuetoverin asettaminen samaan solmuun datassa on validointivirhe | lukittu |
| D-063 | 2026-10-10 | Kiekko ulos | Uusi katkon syy "kiekko ulos kaukalosta": osa ohilaukauksista (`checks.shot.missedOutOfPlayShare`, paikkamerkki 0,25) menee ulos (B5) | lukittu |
| D-064 | 2026-10-10 | Erän loppu | Uusi katkon syy "erän loppu" tapahtumalokiin (Q-B) | lukittu |
| D-065 | 2026-10-10 | Toinen yritys (tarkennus D-059:ään) | Koskee vain irtokiekkoja hyökkäysalueen slotissa eli vastustajan maalin edustalla (hyökkääjän näkökulmasta (7,2) ja (8,2)), ei omaa päätä | lukittu |
| D-066 | 2026-10-10 | Tuttuus | Joukkuekohtainen laskuri per kuvio ja ottelu (peilikuva = sama kuvio), käyttö lasketaan kuvion alkaessa (myös keskeytyneet ja aloituskuviot). Koskee kuvion syöttöjä, kuljetuksia ja blokkeja. Laskuri puolitetaan jokaisella erätauolla (arvo `tuning.json`:ssa) | lukittu |
| D-067 | 2026-10-10 | Avoimet oletukset | Q-006, Q-007, Q-008, Q-010, Q-011 ja Q-020 hyväksytty ehdotetuin oletuksin. Q-019 siirretään draftin yhteyteen, Q-021 virstanpylvään 3 jälkeen | lukittu |

## Muutosehdotukset

Kirjaa uusin ylimmäksi.

<!--
### E-001 · YYYY-MM-DD · ehdotettu
**Ehdotus:** mitä muutetaan.
**Perustelu:** raportin luvut (mittari, tulos, tavoite).
**Vaikutus:** mihin dokumentteihin, dataan ja koodiin muutos koskee.
**Päätös:** (Jerry täyttää) hyväksytty / hylätty + lyhyt syy.
-->

### E-003 · 2026-10-09 · hyväksytty
**Ehdotus:** maalin edustan tilannelaukaukset saavat `crease`-xG:n. Laukaus solmusta `netFront` (8, 2) käyttää vyöhykettä `crease` (nyt `baseXg` 0,25) eikä `slot` (0,15), kun laukaisija on reboundin saanut pelaaja tai `driveNet`-toiminnolla maskiin ajanut pelaaja, joka saa syötön. Muut laukaukset solmusta (8, 2) käyttävät `slot`-vyöhykettä.
**Perustelu:** D-033 kieltää kenttäpelaajan maalisolmussa (9, 2), joka oli ainoa `crease`-solmu, joten `crease`-xG ei ole enää käytössä yhdessäkään laukauksessa. 22 jalan verkossa solmu (8, 2) kattaa sekä maalin edustan että matalan slotin. Ilman tätä reboundit ja ohjaukset maskista saavat saman xG:n kuin slotin vapaa laukaus, vaikka oikeassa kiekossa ne ovat vaarallisimpia paikkoja, ja `driveNet` (D-020) jää palkitsematta. Raporttilukuja ei vielä ole (virstanpylväs 2), joten arvo kalibroidaan simulaatiolla.
**Vaikutus:** `docs/data-schema.md` (Maalisolmu ja `crease`, kuvioiden `shoot`), `Sim.Shift` (laukauksen xG-vyöhykkeen valinta). `tuning.json` ei muutu. Uusi mekaniikka, vaatii hyväksynnän.
**Päätös:** hyväksytty (Jerry, 2026-10-09). Kirjattu: D-047.

### E-002 · 2026-10-09 · hyväksytty
**Ehdotus:** pidennetään kaukalon solmuverkko 9 × 5:stä 11 × 5:een niin, että kummankin maalin taakse tulee oma solmurivi.
**Perustelu:** nykyisessä verkossa maaliviivat ovat päätyrivit (x = 0 ja x = 8), joten maalin takana ei ole yhtään solmua. Oikeassa kiekossa iso osa alueella pelaamisesta tapahtuu maalin takana ja päätylaidoilla: kierrätys (cycle), wraparound, maalin takaa syöttö slottiin ja pakkien kiekonhaku päätyyn ammutun kiekon jälkeen. Analyysissa ottelun 2 laukauksista suurin osa syntyi päätypelistä (joukkue A 26 / 47 ja joukkue B 8 / 20 syntytavan mukaan luokitellusta laukauksesta, `docs/stats-and-checks.md`). Ilman maalin takaisia solmuja näitä kuvioita ei voi kirjoittaa, eikä "kiekko päätyyn" (E-001) voi päätyä oikeaan paikkaan. Ratkaisu kannattaa tehdä ennen ensimmäisiä kuvioita (virstanpylväs 2), koska kuviot ja järjestelmät kirjoitetaan solmukoordinaateilla.
**Ehdotettu verkko 11 × 5 (55 solmua):**

| x | Sisältö | Alue | xgZone (hyökkäyssuunta) |
| --- | --- | --- | --- |
| 0 | Oman maalin takana (päätylaita) | defensive | longRange |
| 1 | Oma maaliviiva, maali (1, 2) | defensive | longRange |
| 2 | Omat aloituspisteet (2, 1), (2, 3) | defensive | longRange |
| 3 | Oman pään yläosa, siniviiva x ≈ 3,9 | defensive | longRange |
| 4 | Keskialue, aloituspisteet (4, 1), (4, 3) | neutral | longRange |
| 5 | Keskiviiva, `center` (5, 2) | neutral | longRange |
| 6 | Keskialue, aloituspisteet (6, 1), (6, 3) | neutral | longRange |
| 7 | Ympyröiden yläreuna, siniviiva x ≈ 6,1 | offensive | boards / point / highSlot / point / boards |
| 8 | Hyökkäyspään aloituspisteet (8, 1), (8, 3) | offensive | boards / circle / slot / circle / boards |
| 9 | Vastustajan maaliviiva, maali (9, 2) | offensive | boards / lowAngle / crease / lowAngle / boards |
| 10 | Vastustajan maalin takana | offensive | uusi `behindNet` (y = 1–3), kulmat `boards` |

Alueet: `defensive` 0–3, `neutral` 4–6, `offensive` 7–10 (symmetrinen kierrossa). Solmuväli maaliviivojen välillä on n. 22 jalkaa kuten nyt (maalin takainen alue on oikeasti vain n. 11 jalkaa syvä, mutta yksi rivi riittää kuvaamaan sen), joten nykyiset slotin ja vyöhykkeiden rajat säilyvät, ne vain siirtyvät yhdellä. Uusi laukaisuvyöhyke `behindNet` tarkoittaa, että suoraa laukausta ei ole (tai vain wraparound hyvin pienellä xG:llä), mikä on tasapainoarvo `checks.shot.baseXg.behindNet`. Leveys 5 säilyy, koska kuvioiden kaistat (laidat, aloituspisteet, keskikaista) mahtuvat siihen.
**Vaikutus:** `data/rink.json` (uusi verkko, id = x × 5 + y, ids 0–54), `tuning.json` (`baseXg` ja `attackerByXgZone` uudelle vyöhykkeelle), `docs/data-schema.md` (koko ja taulukot), tech-spec.md ("9 × 5" → "11 × 5"), D-017:n esimerkki (9 × 5 → 11 × 5, kaava ei muutu). Q-002 ratkeaa tällä pituuden osalta. Programmer: verkon koko on datavetoinen, joten koodimuutoksia ei pitäisi tarvita. Testit, joissa on kovakoodattu 9 × 5 tai id 44, päivitetään. Mahdollinen lisäsääntö: maalin solmu ja sen takana oleva solmu eivät ole luistelureitillä läpi kuljettavia (maali on esteenä), mikä on uusi mekaniikka ja vaatii erillisen päätöksen.
**Päätös:** hyväksytty (Jerry, 2026-10-09). Kirjattu: D-027. Lisäsääntö maalin läpi kulkemisesta ei sisälly hyväksyntään, vaan vaatii erillisen päätöksen.

### E-001 · 2026-10-09 · hyväksytty
**Ehdotus:** lisätään kuvioihin viides kiekkotoiminto `dump` ("kiekko päätyyn"): kiekollinen ampuu kiekon kohdesolmuun (kulma tai maalin taakse), ja siitä seuraa irtokiekkokamppailu.
**Perustelu:** "Kiekko päätyyn" on oma tapahtumansa tapahtumaskeemassa ja tarkistustaulukossa (ei tarkistusta, johtaa irtokiekkoon, maalivahdin kiekonkäsittely vaikuttaa), mutta kuvion neljällä toiminnolla (luistele, syötä, laukaise, aja maalille) sitä ei voi kirjoittaa. Oikeassa kiekossa dump and chase on peruskeino tulla alueelle järjestäytynyttä puolustusta vastaan (esim. trapia vastaan, vision.md: "suora rynnistys ei toimi"). Analyysin alueelle tulojen onnistuminen tavoittain (`docs/stats-and-checks.md`) vaatii, että tavat ovat erillisiä. Vaihtoehto: `dump` vain järjestelmätilan ja siirtymäohjeen toimintona eikä kuvioissa, mutta silloin pelaaja ei voi rakentaa dump and chase -kuviota.
**Vaikutus:** kuvion skeema (`docs/data-schema.md`: `{"type": "dump", "by": .., "to": [x, y]}`), vision.md ja tech-spec.md ("neljä toimintoa" → viisi), `tuning.json` (`time.secondsPerAction.dumpIn` ja `checks.dumpIn` ovat jo olemassa), simulaatio (`Sim.Tactics`: uusi toiminto, joka laukaisee `loosePuck`-tarkistuksen kohdesolmussa). Parhaiten toimii yhdessä E-002:n kanssa, jotta kiekon voi ampua maalin taakse.
**Päätös:** hyväksytty (Jerry, 2026-10-09). `dump` on viides toiminto. Kirjattu: D-031.

## Tasapainomuutokset

Designerin `tuning.json`-säädöt tavoitehaarukoiden sisällä. Ei vaadi erillistä hyväksyntää, mutta jokainen kirjataan.

| Päivä | Arvo | Ennen | Jälkeen | Syy | Vaikutus raporttiin |
| --- | --- | --- | --- | --- | --- |
| 2026-10-10 | `pressure.underPressureNodes` | 1 | 0 | D-056 | Ei raporttia vielä |
| 2026-10-10 | `energy.benchRecoveryPerSecond → energy.benchRecoveryRate` | 0,01 (lineaarinen) | 0,011 (eksponentiaalinen, τ ≈ 90 s) | D-062 | Ei raporttia vielä |
| 2026-10-10 | `energy.drainPerSecondOnIce` | – | 0,0055 | D-062 | Ei raporttia vielä |
| 2026-10-10 | `energy.checkModifierAtZero` | −0,8 | −1,2 | D-062 | Ei raporttia vielä |
| 2026-10-10 | `organization.dropOnTurnover def / neu / off` | 0,2 / 0,4 / 0,6 | 0,35 / 0,5 / 0,65 (taso = 1 − drop) | D-062 | Ei raporttia vielä |
| 2026-10-10 | `organization.recoveryPerEvent → organization.recoveryPerSecond` | 0,15 | 0,04 | Palautuminen pelisekuntien mukaan (D-062) | Ei raporttia vielä |
| 2026-10-10 | `organization.recoveryPerStatPoint` | 0,01 | 0,06 (kerroin) | D-062 | Ei raporttia vielä |
| 2026-10-10 | `organization.dropFactorOnShotOrDump / dropPerCommittedPlayer` | – | 0,5 / 0,1 | D-062 | Ei raporttia vielä |
| 2026-10-10 | `checks.deke / breakout / block .modifiers.organization` | – | 0,6 / 0,6 / 0,5 | D-062 | Ei raporttia vielä |
| 2026-10-10 | `checks.shot.modifiers.screenContested` | – | 0,15 | Q-031, D-062 | Ei raporttia vielä |
| 2026-10-10 | `checks.shot.missedOutOfPlayShare` | – | 0,25 | D-063 | Ei raporttia vielä |
| 2026-10-10 | `familiarity.freeUses / penaltyPerRepeat / intermissionMultiplier` | – / 0,1 / – | 2 / 0,08 / 0,5 | D-066 | Ei raporttia vielä |
| 2026-10-10 | `time.periodSeconds` | 1200 (paikkamerkki) | 1200 (vahvistettu) | Oikean kiekon mitta | Ei raporttia vielä |
| 2026-10-09 | `checks.pass.interceptionShare` | – | 0,6 (paikkamerkki) | Syötönkatkojen osuus epäonnistuneista syötöistä (D-040) | Ei raporttia vielä |
| 2026-10-09 | `checks.block.maxLaneDistance` | – | 1 (paikkamerkki) | Blokkaajaehdokkaan suurin etäisyys laukauslinjasta (M-5, luonnos) | Ei raporttia vielä |
| 2026-10-09 | `checks.block.modifiers.laneDistance` | – | [0,0, 1,0] (paikkamerkki) | Blokkaajan linjaetäisyyden muokkaaja (M-5, luonnos) | Ei raporttia vielä |
| 2026-10-09 | `checks.loosePuck.extraPlayerRadius` | – | 1 (paikkamerkki) | Lisäpelaajan säde (M-6, luonnos) | Ei raporttia vielä |
| 2026-10-09 | `checks.loosePuck.modifiers.distancePerNode` | – | 0,5 (paikkamerkki) | Kamppailijoiden etäisyysero (M-6, luonnos) | Ei raporttia vielä |
| 2026-10-09 | `pressure.underPressureNodes` | – | 1 (paikkamerkki) | Paineen alla -säde (M-7, luonnos) | Ei raporttia vielä |
| 2026-10-09 | `time.secondsPerAction.systemStep` | – | 2 (paikkamerkki) | Järjestelmätilan askel (D-036, M-6) | Ei raporttia vielä |
| 2026-10-09 | `checks.dumpIn.goalieReachNodes` | – | 1 (paikkamerkki) | Maalivahdin ulottuma kiekon päätyyn lyönnissä (D-031) | Ei raporttia vielä |
| 2026-10-09 | `checks.deke.modifiers.defenderDistance` | – | [0,0, 1,0, 3,5] (paikkamerkki, odottaa Jerryn vahvistusta) | Kuljetus on aina tarkistus (D-032): tavallinen kuljetus tarkistetaan harhautuksena puolustajan etäisyyden mukaan | Ei raporttia vielä |
| 2026-10-09 | `rink.json` solmu (9,2) `isSlot` | true | false | Maalisolmu ei ole kenttäpelaajan paikka (D-033) | Ei raporttia vielä |
| 2026-10-09 | `checks.shot.baseXg.behindNet`, `checks.shot.attackerByXgZone.behindNet.shooter` | – | 0,01 / shotAccuracy 0,8, shotPower 0,2 (paikkamerkkejä) | Uusi xG-vyöhyke maalin takana (E-002, D-027) | Ei raporttia vielä |
