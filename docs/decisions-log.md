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
| D-041 | 2026-10-09 | Tahdin järjestys | Hyökkääjien siirrot → puolustajat liikkuvat järjestelmän mukaan → toiminto. Puolustus liikkuu jokaisessa tahdissa **Muutettu 2026-10-11 (D-074):** kun kiekko liikkuu syötöllä, painostaja liikkuu seuraavan toiminnon edellä vain `pressure.presserReactionNodes`-arvon verran solmuja (oletus 1) kahden sijasta | lukittu |
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
| D-062 | 2026-10-10 | Virstanpylvään 3 suunnitelma | Designerin ja ohjelmoijan suunnitelmat hyväksytty suosituksin: järjestäytyneisyys joukkueen skaalaariarvona, joka palautuu pelisekuntien mukaan ja jota sijainnit seuraavat viiveellä (setup ei siirrä puolustajia suoraan kohteisiin); suorahyökkäys sisäänrakennetuilla säännöillä ilman kuviota, oletussiirtymäohje `rush`; tuplarangaistus: syöttölinjan etäisyys (M-1) ei sisällä syöttäjän omaa solmua; tasapeli sallittu; kokoonpano 4 kolmikkoa ja 3 paria; aikalisä ja maalivahdin vetäminen virstanpylväässä 4; designerin vaihtosäännöt (katkovaihto ennen aloitusasettelua, puolustava joukkue ei vaihda lennosta); eksponentiaalinen penkkipalautuminen (avain nimetään uudelleen), tarkistusmaksu kaikille osallistujille, maalivahdin energia kuluu vain paineesta; trap122:n F1 omassa päässä suoraan kiekolliseen; slotti xG:lle = (7,2) ja (8,2) (Q-015); `screenContested` 0,15 (Q-031); oletusvalmentaja `RotationCoach` AI-projektissa, rajapinta Simissä; erän raja tarkistetaan kerran jokaista kokonaisuutta kohden; aluetta vaihtava toiminto lasketaan sitä edeltävän tilan mukaan; laukausnopeus tyhjä myös virstanpylväässä 3; 1000 ottelun savutesti omassa kategoriassaan; kahden joukkuetoverin asettaminen samaan solmuun datassa on validointivirhe **Muutettu 2026-10-11 (D-077):** aikalisä toteutetaan virstanpylväässä 4, maalivahdin vetäminen siirretään erikoistilanteiden kanssa myöhemmäksi | lukittu |
| D-063 | 2026-10-10 | Kiekko ulos | Uusi katkon syy "kiekko ulos kaukalosta": osa ohilaukauksista (`checks.shot.missedOutOfPlayShare`, paikkamerkki 0,25) menee ulos (B5) | lukittu |
| D-064 | 2026-10-10 | Erän loppu | Uusi katkon syy "erän loppu" tapahtumalokiin (Q-B) | lukittu |
| D-065 | 2026-10-10 | Toinen yritys (tarkennus D-059:ään) | Koskee vain irtokiekkoja hyökkäysalueen slotissa eli vastustajan maalin edustalla (hyökkääjän näkökulmasta (7,2) ja (8,2)), ei omaa päätä | lukittu |
| D-066 | 2026-10-10 | Tuttuus | Joukkuekohtainen laskuri per kuvio ja ottelu (peilikuva = sama kuvio), käyttö lasketaan kuvion alkaessa (myös keskeytyneet ja aloituskuviot). Koskee kuvion syöttöjä, kuljetuksia ja blokkeja. Laskuri puolitetaan jokaisella erätauolla (arvo `tuning.json`:ssa) | lukittu |
| D-067 | 2026-10-10 | Avoimet oletukset | Q-006, Q-007, Q-008, Q-010, Q-011 ja Q-020 hyväksytty ehdotetuin oletuksin. Q-019 siirretään draftin yhteyteen, Q-021 virstanpylvään 3 jälkeen | lukittu |
| D-068 | 2026-10-10 | Virstanpylväs 3 | Hyväksytty valmiiksi (`match --seed 42`, 200 ottelua). Q-044–Q-047 hyväksytty ohjelmoijan oletuksin: raporttimerkinnät lokin rinnalla, rushin tukipelaajan maalisolmukohde maalin eteen, sekuntivaikutukset kertyvät kokonaisuuden ajan mukana, vaihtotapahtumien järjestys koti kolmikko, koti pari, vieras kolmikko, vieras pari | lukittu |
| D-069 | 2026-10-10 | Kalibrointikierros ennen virstanpylvästä 4 | Ennen sääntöpohjaista ja sopeutuvaa AI:ta lyhyt kalibrointi, yksi muutos kerrallaan, raportti (200 ottelua) jokaisen jälkeen: (1) `match --count` ja `batch` pelaavat ottelut pareina (samat kokoonpanot ja siemen, joukkueet vaihtavat puolta), kotietu raportoidaan erikseen; (2) oman pään box+1: vain kiekon puoleinen puolustaja painostaa, muut peittävät slottia ja syöttölinjoja, tavoite laukaukset ja syötöt paineen alla 68–76 %; (3) vasta sen jälkeen syöttöjen onnistuminen 81–85 % (paineen alla 68–69 %) ja xG-jakauman muutos (suorahyökkäys suurin, riisto 23–33 %); (4) laukauksia verrataan koko ottelun lukuihin: yritykset n. 33–60 ja maalia kohti 24–39 per joukkue (Wisehockeyn erätaulukko), ei 5v5-lukuun 18–45. Jerry täyttää `targets.json`:n haarukat Wisehockeyn otteluista designerin pohjalla **Korjattu 2026-10-11 (D-075):** kohdan (2) 68–76 % koskee vain laukausten paineosuutta (Wisehockey, odottaa määritelmää Q-049). Syötöillä tavoite on paineosuus 68–69 %, ei onnistumisprosentti paineen alla | lukittu |
| D-070 | 2026-10-10 | Paine syötön jälkeen (Q-049) | M-7 tarkennetaan: syötöstä heti lähtevä laukaus ei ole paineen alla, jos puolustaja ei ollut vastaanottajan solmussa ennen tahdin liikkeitä. Designer tarkentaa `data-schema.md`:n, programmer toteuttaa, mitataan 200 ottelulla. Jos laukauksista paineen alla on yhä yli 80 %, vaihtoehto 2 (vain painostaja antaa paineen) tuodaan Jerrylle. Vaihtoehto 3 (reaktioviive) myöhemmin. Kalibroinnin vaihe 3 (syötöt) odottaa tämän mittausta | lukittu |
| D-071 | 2026-10-10 | targets.json-skeema | Hyväksytty: uudet avaimet (mm. `shotAttempts`, `shotsOnTarget`, `faceoffsPerMatch`, `evenStrengthXgShareFaceoff` ja valinnaiset), uusi mittausalue `perShift`, `xg` koko ottelusta ja valinnainen `xg5v5`, Harness vertaa jokaista täytettyä haarukkaa automaattisesti. Haarukat koskevat varsinaista peliaikaa (3 × 20, ei jatkoaikaa, Q-052). xG:n paikkatyyppiosuuksia verrataan toistaiseksi ilman aloitusluokkaa (Q-051) | lukittu |
| D-072 | 2026-10-10 | Paineen antaja (Q-049, vaihtoehto 2) | M-7: paineen antaa vain kiekolliseen pelaava puolustaja (järjestelmäkohde `puckOffset`), sekä laukauksissa että syötöissä (yksi määritelmä). Samassa solmussa oleva alueen peittäjä vaikuttaa vain blokkiin ja maskiin. Kun järjestelmä ei ohjaa liikettä (esim. suorahyökkäys), painostaja on kiekollista lähin puolustaja. D-070:n poikkeus jää voimaan. Jos laukausten tai syöttöjen paineosuus putoaa alle 68 %:n, designer saa nostaa `pressure.underPressureNodes`-arvon 1:een. Mitataan 200 ottelulla ennen kalibroinnin vaihetta 3 | lukittu |
| D-073 | 2026-10-10 | Tavoitevertailun tarkennukset (Q-053) | `smartVsRandomWinRate`: tasapeli = puoli voittoa, raportti näyttää voitot, tasapelit ja tappiot erikseen. `shotXgTypical` verrataan 10. ja 90. persentiilillä. Realismivertailu kahdella sääntöpohjaisella AI:lla. Tasakentällinen = yhtä monta kenttäpelaajaa; hyökkäysalueaika koko ottelusta, kun erikoistilanteita ei ole | lukittu |
| D-074 | 2026-10-11 | Painostajan reaktioviive (muuttaa D-041:tä, Q-049 vaihtoehto 3) | Kun kiekko liikkuu syötöllä, painostaja (järjestelmän painostaja, esim. F1; suorahyökkäyksessä ja toisessa yrityksessä kiekollista lähin puolustaja) liikkuu seuraavan toiminnon edellä vain yhden solmun kahden sijasta. Muut puolustajat liikkuvat kuten ennen. Viive on tasapainoarvo `tuning.json`:ssa. Perustelu: kiekko liikkuu nopeammin kuin pelaaja, joten syöttöpelillä pitää päästä paineesta irti. Säde `underPressureNodes` pysyy 0:ssa. Q-054: lähin puolustaja säilyy suorahyökkäyksen painostajana, muut suositukset kuitattu. Aikaraja: jos laukausten paineosuus on tämän jälkeen yhä yli 80 %, sitä ei hiota ennen Wisehockeyn määritelmää (Q-049), vaan siirrytään kalibroinnin vaiheeseen 3 ja sitten virstanpylvääseen 4 | lukittu |
| D-075 | 2026-10-11 | Syöttöjen painetulkinta (E-004 hylätty) | Wisehockeyn paineluvut ovat osuuksia (Jerry varmisti, Q-056): syötöistä 69 % / 68 % ja laukauksista 68 % / 76 % annetaan paineen alla. Syöttöjen paineosuuden tavoite 68–69 % (nykyinen 67,6 % on tavoitteessa), kokonaisonnistumisen tavoite 81–85 % säilyy. Paineen miinus (`checks.pass.modifiers.underPressure` −0,9) kalibroidaan niin, että kokonaisonnistuminen on 81–85 % nykyisellä paineosuudella (paineen alla n. 80 %, ilman painetta n. 88–90 %); yksi muutos, 200 ottelua. Riiston xG-osuus siirretään virstanpylvääseen 5 (raportoidaan silti). Designer lisää kuvion, jossa syöttö kulkee omalta alueelta keskialueelle, jotta `checks.breakout` on käytössä. Q-055:n oletukset hyväksytty, kunhan ne eivät muuta lukittuja päätöksiä | lukittu |
| D-076 | 2026-10-11 | Kalibroinnin vaihe 3 | Hyväksytty (syötöt 81,8 %, paineen alla 81,6 %, uusi kuvio `breakoutWallToCenter`). Syöttöjen jatkokalibrointi (paine-ero paineen alla vs ilman) siirretään virstanpylvääseen 5. Q-055: D-074:n "seuraava toiminto" tarkoittaa kiekkotoimintoa (D-031) | lukittu |
| D-077 | 2026-10-11 | Virstanpylväs 4 | (1) Valmentaja saa oman satunnaislukuvirran, joka johdetaan deterministisesti ottelun siemenestä; CLAUDE.md:n sääntö 1 tarkennettu. (2) `random` arpoo kaiken (ketjut, kuviot, järjestelmä, siirtymäohje, järjestelmätilan ohjeet, lennosta vaihto); diagnostiikkavariantit `randomTactics` ja `randomLines` hyväksytty. (3) Valmentaja näkee vain julkisen tiedon: jäällä olevat yksiköt, kello, oma jääaika, julkinen tapahtumaloki. (4) Ensisijainen mittaus `rulebased` vs `random`, `adaptive` raportoidaan rinnalla. (5) Päätökset samanaikaisia, viimeinen vaihto myöhemmin tarvittaessa. (6) Aikalisä nyt, uusi tapahtumatyyppi (stats-and-checks päivitetty ensin); maalivahdin vetäminen erikoistilanteiden kanssa (D-062 muutettu). (7) Pitkä kiekko (`clear` ja vaihtosäännön poikkeus) ensimmäisen batch-raportin jälkeen. (8) Taito vai tuuri: ei säätöä etukäteen; ensin batch ja raportti. Jos `rulebased` vs `random` jää alle 75 %:n, Jerrylle tuodaan hajonnan jako paikkojen luontiin ja viimeistelytuuriin | lukittu |
| D-078 | 2026-10-11 | Taktiikan vastapeli (E-005) | K1, K2, K3 ja K4 hyväksytty. K4 toteutetaan nyt: alueelle tulon voimasuhdemuokkaaja on jo dokumentoitu ja hyväksytty `stats-and-checks.md`:ssä, joten kyse on hyväksytyn speksin toteuttamisesta; alueelle tulo, joka onnistuu 68–70 % voimasuhteista riippumatta, rikkoo siirtymäsuunnittelun (järjestäytyneisyys, N vs M). Järjestys, yksi muutos ja mittaus kerrallaan (taktiikkamatriisi, 2000 ottelua per solu): (1) K3 taktiikkamatriisi ja `--rotation-transition`; (2) K1, mittaus; (3) K4 `stats-and-checks.md`:n mukaiset muokkaajat, mittaus ja alueelle tulon onnistuminen voimasuhteittain (esim. 3v2, 2v2, 2v3, 2v5); (4) K2 ja uusi batch. Mittausrajat: karvausta vastaan rush − regroup ≥ +5 %-yks., trapia vastaan regroup − rush ≥ +5 %-yks., regroup karvausta vastaan ≥ 35 %, `rulebased` vs `randomTactics` ≥ 55 %, maalit per ottelu 4–7 | lukittu |
| D-079 | 2026-10-11 | Virstanpylvään 4 päätulos (D-078:n jälkeen) | Taktiikan vastapeli toimii ja taitotavoite täyttyy. Batch 5000 ottelua per AI-pari, siemen 1 (commit 4c1f051): `rulebased` vs `random` 80,2 % (95 %:n luottamusväli 79,3–81,2; ennen 64,9 %), tavoite ≥ 75 % täyttyy. `rulebased` vs `randomTactics` 62,0 % (ennen 45,4 %), `rulebased` vs `randomLines` 74,1 %. Taktiikkamatriisi (2000 ottelua per solu): karvausta vastaan suorahyökkäys − uudelleenryhmittyminen +14,5 %-yks., trapia vastaan uudelleenryhmittyminen − suorahyökkäys +17,9 %-yks. Maalit per ottelu 6,37 (rb vs rb). Viimeistelytuuri selittää edelleen n. 90 % maalieron varianssista, paikkojen luonti n. 27 %. Avoimet jatkot: N vs M -laskennan tarkistus (M-8), `adaptive`n romahdus (vs `rulebased` 30,2 %) | lukittu |
| D-080 | 2026-10-11 | Voimasuhde, muokkaajan rajaus ja `adaptive`n järjestelmävalinta (Q-063, Q-065, Q-066) | (1) M-8 korjataan: N = kiekollinen ja hyökkäävän joukkueen kenttäpelaajat kiekon tasalla tai sen edellä (x ≥ kiekon x); M = puolustavan joukkueen kenttäpelaajat kiekon ja oman maalinsa välissä (x > kiekon x). Raportoidaan voimasuhteiden jakauma (kuljetukset ja syötöt erikseen) ja K4:n porras. `strengthDifferencePerPlayer` kalibroidaan uudelleen Wisehockeyn voimasuhteiden jälkeen. (2) Voimasuhdemuokkaaja koskee vain kuljetuksia; syöttölinjan tarkistus huomioi jo puolustajien läheisyyden. (3) `adaptive`n oma järjestelmä valitaan vastustajan siirtymätyylin mukaan julkisesta lokista (paljon suorahyökkäyksiä → trap, paljon uudelleenryhmittymisiä → karvaus); vaihto vain katkolla ja vain, kun signaali on ollut selvä vähintään kynnyksen verran tapahtumia; kynnykset `ai`-osioon. Designer speksaa, Jerry hyväksyy speksin ennen toteutusta. Järjestys: ensin (1), sitten (3) | lukittu |
| D-081 | 2026-10-11 | Alueelletuontien voimasuhdejakauma (Wisehockey) | Uusi mittari `targets.json`:iin: onnistuneiden hallittujen alueelletuontien voimasuhdejakauma M-8:n mukaan, alivoima (N < M) 50–75 %, tasavoima 20–40 %, ylivoima 0–15 %. Haarukat väljiä, koska otos on yksi ottelu (Jokerit–HIFK, 3 erää 5v5, 31 hallittua alueelletuontia: 65 / 29 / 6 %; yleisimmät 2v3, 1v2, 3v4, 1v3, 2v2, 3v3, 4v4; 5v5 ei kertaakaan). Data sisältää vain kiekon säilyttäneet alueelletuonnit, joten mittari lasketaan onnistuneista. `strengthDifferencePerPlayer` kalibroidaan tämän jälkeen | lukittu |
| D-082 | 2026-10-11 | `adaptive`n järjestelmävalinta A-T2 ja K4:n kalibrointisääntö (Q-067) | (1) A-T2 hyväksytty: vastustajan siirtymätyyli luokitellaan vain julkisesta tapahtumalokista (hallinnan ensimmäisen toiminnon kuvio-id), järjestelmä vaihdetaan vain katkolla, vaihto vaatii vähintään `ai.adaptive.styleMinEvents` havaintoa nykyistä järjestelmää vastaan ja selvän osuuden (`rushShareForPassive` / `rushShareForAggressive`), kaikki kynnykset `ai.adaptive`-osiossa. Hyväksytään, että `adaptive` jää `rulebased`ia vastaan n. 50 %:iin tai alle (viiveellinen vastapeli). (2) K4:ää ei kalibroida Wisehockeyn datalla (vain onnistuneet tuonnit kalibroivat M-8:n jakauman). Jos uudelleenryhmittyminen karvausta vastaan jää M-8:n korjauksen jälkeen selvästi alle 35 %:n (95 %:n luottamusväli kokonaan rajan alla), K4:ää säädetään yhden kerran ja mitataan uudelleen | lukittu |

## Muutosehdotukset

Kirjaa uusin ylimmäksi.

<!--
### E-001 · YYYY-MM-DD · ehdotettu
**Ehdotus:** mitä muutetaan.
**Perustelu:** raportin luvut (mittari, tulos, tavoite).
**Vaikutus:** mihin dokumentteihin, dataan ja koodiin muutos koskee.
**Päätös:** (Jerry täyttää) hyväksytty / hylätty + lyhyt syy.
-->

### E-005 · 2026-10-11 · hyväksytty
**Ehdotus:** taktiikan vastapeli (handoff, seuraava askel 1). Muutoksia on neljä:

- **K1 (koodi + yksi tasapainoarvo).** Karvaus haastaa kokoamisen omassa päässä. Täydennys O-7:ään:
  1. Kun joukkue kokoaa (`regroup`) ja kiekko on sen omalla alueella, puolustajat liikkuvat ensin kokoamisen askelmäärän kuten nyt.
  2. Sen jälkeen lasketaan karvaajat f (M-1 `forecheckers`).
  3. Jos f > `transitions.regroupForecheckersAllowed` (ehdotus 1), kiekollinen ratkaisee yhden olemassa olevan `breakout`-tarkistuksen nykyisin roolein ja muokkaajin.
  4. Epäonnistuminen on riisto: lähin karvaaja saa kiekon kiekollisen solmussa (nykyinen Takeaway-polku, O-6:n pudotus `defensive`).
  5. Onnistuminen jatkaa kokoamista nykyiseen tapaan (D-036 ja setup).
  6. `regroupSeconds` kuluu kummassakin tapauksessa.
  7. Keskialueen kokoaminen pysyy ennallaan.
- **K2 (tekoäly, koodi + data).** Uusi avain `ai.rulebased.transitionBySystem` (esim. `forecheck212` → `rush`, `trap122` → `regroup`). Sitä käytetään tilassa `normal`, kun vastustajan järjestelmä tiedetään (S-T:n kohta 2). `adaptive` käyttää todennäköisintä järjestelmää. Muuten käytetään `modes.normal.transition`ia.
- **K3 (harness).**
  - Taktiikkamatriisi batch-raporttiin: siirtymäohje × vastustajan järjestelmä kiinteillä valmentajilla, peilatuilla kokoonpanoilla ja 2000 ottelulla per solu. Lisäksi optio `--rotation-transition`.
  - "Rush vs regroup" -taulukko jaetaan vastustajan järjestelmän mukaan, ja siihen lisätään sarake "xG-ero / 60".
- **K4 (varalla, ei nyt).** `zoneEntryCarry`-muokkaaja voimasuhteesta N − M (M-8). Se on stats-and-checks.md:n dokumentoitu mutta toteuttamaton muokkaaja "puolustusjärjestelmä". Otetaan käyttöön vain, jos trapia vastaan ero jää K1:n jälkeen tavoitteen alle.

**Perustelu:** kontrolloitu 2 × 2 -koe, 2000 ottelua per solu.

- Asetelma:
  - Joukkue A: kiinteä siirtymäohje, oma järjestelmä forecheck212.
  - Joukkue B: kiinteä järjestelmä, `rush`.
  - Molemmilla RotationCoachin kierto, peilatut kokoonpanot ja siemenet 1…1000.
  - Väliaikainen analyysityökalu, ei koodi- eikä datamuutoksia.
- Batch-raportin luku 2,21 vs 1,12 xG / 60 on pelkkä luotu xG, eikä sitä ole eritelty järjestelmittäin. Nettona kokoaminen on dominoiva. Solut ovat A:n pisteet sekä xGF / xGA / ero per 60 min:

| A:n siirtymäohje | B pelaa forecheck212 | B pelaa trap122 |
| --- | --- | --- |
| rush | 50,0 % · 2,91 / 2,91 / 0,00 | 48,0 % · 2,71 / 2,81 / −0,10 |
| regroup | 61,0 % · 1,83 / 1,36 / +0,47 | 68,3 % · 2,03 / 1,19 / +0,84 |

- Kun B kokoaa, tulos on sama. Suorahyökkäys saa forecheck212:ta vastaan 39,1 % ja trap122:ta vastaan 47,2 %. Kokoaminen saa trap122:ta vastaan 60,6 %.
- **Trapia vastaan tavoite täyttyy jo:** kokoaminen saa 68 % ja suorahyökkäys 48 %.
  - Trap pysäyttää suorahyökkäyksen hieman paremmin kuin karvaus.
  - Kuviot tuottavat trapia vastaan 17 % enemmän: 1,69 vs 1,45 xG per ottelu.
  - xG per hallinta (riisto tai irtokiekko omalla alueella tai keskialueella):

| | forecheck212 | trap122 |
| --- | --- | --- |
| rush | 0,0162 | 0,0156 |
| regroup | 0,0126 | 0,0151 |

- **Karvausta vastaan järjestys on väärin päin.** Syyt:
  1. Kokoaminen on omassa päässä 8 sekuntia ilman tarkistuksia, vaikka forecheck212:n karvaajia on kokoamisen aikana keskimäärin 2,88 ja 95 %:ssa tilanteista 3. trap122:n karvaajia on 1,18, ja 83 %:ssa tilanteista enintään 1.
  2. Suorahyökkäys on harhautusvoittoinen: 114 harhautusta per ottelu, joista 51 % onnistuu. Kokoamisen kuviot syöttävät: 179 syöttöä, joista 78–82 % onnistuu.
  3. Kiekonmenetyksiä on suorahyökkäyksessä 104 per ottelu ja kokoamisessa 62. B:n Rush- ja Turnover-xG putoaa 1,62:sta 0,50:een.
- **Trap ei nyt rankaise suoraa sisääntuloa mekaanisesti.** Kuljetus alueelle onnistuu 68–70 %:sti kaikilla voimasuhteilla. Avaus onnistuu 72,0 % karvausta vastaan (2,6 karvaajaa) ja 72,1 % trapia vastaan (1,6 karvaajaa).
- **K1:n arvioitu vaikutus** (laskettu kokeen luvuista, ei mitattu):
  - Karvausta vastaan omassa päässä tapahtuvia kokoamisia on n. 49 per ottelu, ja niistä n. 38–40 % epäonnistuu.
  - Kokoaminen karvausta vastaan: n. 44–47 % (xG-ero n. −0,2…−0,3). Suorahyökkäys karvausta vastaan pysyy 50 %:ssa.
  - Kokoaminen trapia vastaan: n. 65–67 %.
  - K2 tarvitaan, jotta `rulebased` hyödyntää eroa. Nyt se pelaa normaalitilassa aina suorahyökkäystä, mikä selittää tuloksen `rulebased` vs `randomTactics` 45,4 %.
- Oikea kiekko: 2-1-2-karvaus on rakennettu estämään rauhallinen kokoaminen omassa päässä (vision.md: "Karvausjoukkue: nopeat avaukset"). Trapia vastaan kiekkoa kierrätetään ja kootaan keskialueella.

**Mittaus ja ehdotetut haarukat** (Jerry päättää, `targets.json`):

| Mittari | Nyt | Ehdotettu tavoite |
| --- | --- | --- |
| Karvausta vastaan: pisteet(rush) − pisteet(regroup) | −11,0 %-yks. | ≥ +5 %-yks. |
| Trapia vastaan: pisteet(regroup) − pisteet(rush) | +20,3 %-yks. | ≥ +5 %-yks. |
| Suojaraja: regroup vs forecheck212 | 61,0 % | ≥ 35 % |
| `rulebased` vs `randomTactics` (K1 + K2) | 45,4 % | ≥ 55 % |
| `rulebased` vs `random` | 64,9 % | nousee kohti 75 %:a |
| Maalit per ottelu | 5,84 | 4,0–7,0 |

**Säätönuppi:** jos q ≈ 0,4 ei riitä, `breakout`-tarkistukseen lisätään muokkaaja `transitions.regroupForecheckModifier` (logit per karvaaja yli rajan). Sitä ei lisätä ennen mittausta.

**Ei ehdoteta:** `modes.normal.transition` = `regroup` -datamuutosta. Se lukitsisi nykyisen tasapainovirheen tekoälyyn.

**Vaikutus:**

- Dokumentit:
  - `data-schema.md`: O-7, O-15/O-16 (uusi avain), AI-2 S-T ja A-T, AI-3.
  - `stats-and-checks.md`: avauksen muokkaaja.
- Data: `tuning.json`, eli `transitions.regroupForecheckersAllowed` ja `ai.rulebased.transitionBySystem`.
- Koodi:
  - Sim: `RegroupStep`.
  - AI: `RuleTactics` ja `AdaptiveTactics`.
  - Harness: taktiikkamatriisi ja `--rotation-transition`.
- Järjestys: K1 + K3, savu ja matriisi; sitten K2 ja täysi batch.

**Päätös:** hyväksytty muutoksella (Jerry, 2026-10-11): K1, K2 ja K3 hyväksytty, K4 otetaan mukaan nyt eikä varalle. Mittausrajat hyväksytty. Kirjattu: D-078.

### E-004 · 2026-10-11 · hylätty (Jerry 2026-10-11, D-075)
**Ehdotus:** syöttöjen paineosuus (`passesUnderPressureShare`) sidotaan samaan Wisehockeyn määritelmään kuin laukausten (Q-049, D-074:n aikaraja), eikä syöttöjen kokonaisonnistumista (81–85 %) hiota tasapainoarvoilla ennen sitä. Tavoitehaarukaksi `passesUnderPressureShare` ehdotetaan aritmeettisesti johdettua n. 25–40 %:a, kunnes Wisehockeyn luku on tiedossa. Kun määritelmä on päätetty, designer tarkentaa M-7:n syötöille (vaihtoehdot alla) ja programmer toteuttaa.
**Perustelu:** kalibroinnin vaihe 3 (D-069). 200 ottelua (`match --count 200 --seed 1`) uusilla syöttöarvoilla (Tasapainomuutokset 2026-10-11): syötöt paineen alla onnistuvat 69,0 % (tavoite 68–69 %, OK), ilman painetta n. 84 % (analyysin p0 85 %), mutta kaikki syötöt vain 73,9 % (tavoite 81–85 %), koska 67,6 % syötöistä on paineen alla. Kokonaisluku = osuus × paineen alla + (1 − osuus) × ilman painetta. Paineen alla 69 %:lla ja ilman painetta 90 %:lla 83 % vaatii, että paineen alla on enintään n. 33 % syötöistä (92 %:lla enintään n. 39 %). Analyysin 84 % / 69 % ei siis ole mahdollinen yli n. 50 %:n paineosuudella edes 100 %:n onnistumisella ilman painetta. Molempia tavoitteita ei voi saavuttaa tasapainoarvoilla ilman, että paineen alaisten syöttöjen tavoite rikkoutuu. Paineosuus on käytännössä binäärinen järjestelmän ja alueen mukaan (16 ottelun JSON-erittely): forecheck212:ta vastaan 96–100 % kaikilla alueilla (F1:n kohde on kiekon solmu), trap122:ta vastaan omalla ja keskialueella 0–1 % (F1 ohjaa solmun päästä, D-057) ja trapin omassa päässä (box+1) 99 %. Syöttö heti vastaanotetun syötön jälkeen on silti paineen alla 56 %:ssa tapauksista, eli `presserReactionNodes` 1 ei riitä, kun painostaja on valmiiksi viereisessä solmussa.
**Vaihtoehdot M-7:n tarkennukseksi (Jerry valitsee Q-049:n vastauksen jälkeen):**
1. Syöttäjä on paineen alla vain, jos painostaja oli syöttäjän solmussa jo ennen tahdin puolustajaliikettä (sama ajatus kuin D-070:n laukauspoikkeus). Karkea arvio JSON-sijainneista: paineosuus n. 13 %, todennäköisesti liian matala.
2. `pressure.presserReactionNodes` 1 → 0 (painostaja ei liiku syötön jälkeisessä tahdissa). Jerryn asettama arvo, siksi ei designerin säätö. Karkea arvio: paineosuus n. 51 %, ei vielä riitä yksinään.
3. Paineen alla -tila arvotaan, kun painostaja on samassa solmussa: todennäköisyys uusi tasapainoarvo (esim. 0,5). Uusi mekaniikka ja satunnaisuus, viimeinen vaihtoehto.
**Vaikutus:** `docs/data-schema.md` (M-7, mahdollisesti Painostajan reaktioviive), `data/targets.json` (`passesUnderPressureShare`-haarukka, Jerry), mahdollisesti `pressure.presserReactionNodes`, `Sim.Shift` (paineen arviointi syötössä). Syöttöarvot (`laneDefenderDistance`, `crossIce`, `underPressure`) pysyvät nykyisinä: kun paineosuus laskee n. 30 %:iin, nykyisillä arvoilla kokonaisluku on arviolta 0,3 × 69 + 0,7 × 84 ≈ 79–80 %, ja loppu hiotaan tasapainoarvoilla.
**Päätös:** (Jerry täyttää)

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
| 2026-10-11 | `checks.pass.modifiers.underPressure` | −0,9 | −0,2 | D-075: analyysin 69 % / 68 % on paineosuus, ei onnistumisprosentti. Kokonaisonnistumisen 81–85 % saavuttamiseksi nykyisellä paineosuudella (n. 68 %) paineen alaisten syöttöjen pitää onnistua n. 80 %:ssa. −0,3 antoi kokonaisonnistumiseksi 81,3 % (liian lähellä alarajaa), −0,2 82,0 %. Vain tämä arvo muutettiin: ilman painetta -onnistuminen (85,0 %) jää alle D-075:n suuntaa-antavan 88–90 %:n, mutta sitä ei tarvita kokonaistavoitteeseen (ehdotus loppuraportissa) | `match --count 200 --seed 1`: syötöt 73,9 → 82,0 % (A 77,2 → 82,5, B 69,7 → 81,4); paineen alla 69,0 → 80,9 % (A 68,5 → 80,2, B 69,3 → 81,3); ilman painetta 84,4 → 85,0 % (200 ottelun JSON-erittely); paineosuus 67,6 → 68,3 % (A 44,0 → 43,3, B 97,6 → 97,9); maalit 6,73 → 6,62; yritykset 73,1 → 74,1, maalia kohti 33,6 → 34,4; xG-osuudet ilman aloituksia suorahyökkäys 15,0 → 13,4 %, riisto 51,8 → 48,2 %, alueella 33,2 → 38,4 % |
| 2026-10-11 | `checks.pass.modifiers.laneDefenderDistance` | [−1,2, −0,5, 0,0] | [−0,4, 0,2, 0,5] | Kalibroinnin vaihe 3 (D-069). 11 × 5 -verkossa vain 7 % syötöistä saa etäisyyden 2+, 59 % etäisyyden 1 ja 34 % etäisyyden 0, joten vanha taulukko oli lähes aina miinusta (keskimäärin n. −0,7 logit) ja p0 85 % ei toteutunut koskaan. Uusi taulukko on syöttöjen jakaumalla keskimäärin n. 0, jolloin syöttö ilman painetta onnistuu n. analyysin 85 %:n tasolla. Sama muutos kuin alla (yksi kokonaisuus) | `match --count 200 --seed 1`: syötöt 58,3 → 73,9 % (A 62,4 → 77,2, B 52,7 → 69,7); paineen alla 51,2 → 69,0 % (A 49,8 → 68,5, B 52,1 → 69,3); ilman painetta n. 71 → 84 %; paineen alla -osuus 66,5 → 67,6 %; maalit 6,57 → 6,73; yritykset 72,5 → 73,1, maalia kohti 33,5 → 33,6 |
| 2026-10-11 | `checks.pass.modifiers.crossIce` | −0,4 | −0,2 | Sama kokonaisuus. 29 % syötöistä on poikittaissyöttöjä, ja ne kulkevat useimmiten myös puolustajan ohi (etäisyys 0 tai 1), joten riski näkyi jo linjataulukossa. `underPressure` −0,9 (analyysi 85 → 70 %) ja p0 0,85 ennallaan | Ks. yllä. Poikittaissyötöt 47,7 → 69,7 %, muut 61,9 → 75,4 % (16 ottelun JSON-erittely) |
| 2026-10-11 | `pressure.presserReactionNodes` | – | 1 | Painostajan reaktioviive (D-074). Lisätään dataan lataajan tuen kanssa (D-030, O-16) | Ei raporttia vielä |
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
