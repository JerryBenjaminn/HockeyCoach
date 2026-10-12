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

### Q-080 · 2026-10-12 · kirjaaja: programmer
**Kysymys:** miten toisto näyttää pelaajat, jotka ovat samassa solmussa? Placementit ovat ruudukon solmuja, joten esim. aloituksessa molemmat sentterit ja vastakkaiset laiturit ovat samassa solmussa, ja kiekkokiistoissa ja vaihdoissa saman joukkueen pelaajiakin voi osua samaan solmuun. Päällekkäin piirretyt kiekot rikkovat hyväksymiskriteerin 1 ("kaikki 12 pelaajaa näkyvät").
**Konteksti:** Unity-toisto, vaihe 5 (View, `PlayerDiscView`). Core ja testit käyttävät tarkkoja solmusijainteja, joten kyse on vain piirrosta.
**Ehdotettu oletus (toteutettu):** pelaajan GameObject on tarkalleen solmun kohdalla, mutta piirretty kiekko ja tunniste siirretään 0,9 m (`ReplayViewStyle.teamShift`, Inspectorissa) kohti joukkueen omaa päätyä. Vastustajat samassa solmussa näkyvät siis vierekkäin kuin aloituksessa. Saman joukkueen pelaajat samassa solmussa menevät yhä päällekkäin. Vaihtoehto: pieni hajautus solmun sisällä kaikille samassa solmussa oleville (esim. ympyrälle id-järjestyksessä).
**Vastaus:**

### Q-079 · 2026-10-12 · kirjaaja: programmer
**Kysymys:** mihin maalin kiekko jää toistossa? Maalilaukauksen `placement.puckNodeId` on ampujan solmu (siemen 42: molemmat maalit), joten polku "ampuja → maalisuu → `placement.puckNodeId`" vie kiekon verkosta takaisin ampujalle saman laukaussegmentin aikana.
**Konteksti:** Unity-toisto, vaihe 3 (Core, `TimelineBuilder`), hyväksymiskriteerit 3 ja 5. Torjutuissa laukauksissa `puckNodeId` on samoin ampujan solmu ja haltija on rebound-pelaaja, joten kiekko käy maalivahdissa ja palaa.
**Ehdotettu oletus (toteutettu):** polku päättyy aina `placement.puckNodeId`:hen (kriteeri 3), ja maali kulkee maalisuun kautta (kriteeri 5). Vaihtoehto: maalissa polku päättyy maalisuuhun ja kiekko pysyy verkossa seuraavaan aloitukseen asti.
**Vastaus:**

### Q-078 · 2026-10-12 · kirjaaja: programmer
**Kysymys:** riittääkö 8× nopeudella 7,5 minuuttia koko otteluun (hyväksymiskriteeri 9: "muutamassa minuutissa")? Toisto etenee ottelukellon mukaan, joten 3 × 20 min / 8 = 450 s. Pitäisikö samalla sekunnilla olevat tapahtumat (siemen 42: 180 / 874) näyttää vähimmäiskestolla, vai riittääkö, että ne näkyvät askeltamalla?
**Konteksti:** Unity-toisto, vaihe 3 (`ReplayCursor`). Tapahtumien kellonajat ovat kokonaisia sekunteja. Esim. syötön jälkeinen riisto tai maalin jälkeiset katko ja vaihdot tapahtuvat samalla sekunnilla, ja jatkuvassa toistossa ne ohitetaan hetkessä.
**Ehdotettu oletus (toteutettu):** puhdas ottelukello, ei kuolleen ajan ohitusta eikä vähimmäiskestoa. Nopeus on vapaa kerroin, ja 16× vie 3,75 min.
**Vastaus:**

### Q-077 · 2026-10-12 · kirjaaja: koordinaattori
**Kysymys:** mistä toiston otsakkeen kaukalon metrimitat (D-087 kohta 4: "mitat metreinä") tulevat? `rink.json`:ssa on vain 11 × 5 -verkko, ei metrejä.
**Konteksti:** Unity-toiston vaihe 2 (`match --json` -otsake). `data-schema.md` (Alueiden perustelu) johtaa verkon NHL-mitoista: maaliviivojen väli 178 ft, siniviiva 64 ft maaliviivasta.
**Ehdotettu oletus (toteutettu):** Harness kirjoittaa otsakkeeseen NHL-kaukalon mitat nimettyinä vakioina, vain toistoa varten (simulaatio ei käytä niitä): pituus 60,96 m (200 ft), leveys 25,91 m (85 ft), maaliviiva 3,35 m (11 ft) ja siniviiva 22,86 m (75 ft) päädystä. Jos mitat kuuluvat dataan, designer lisää ne `rink.json`:iin ja Harness lukee ne sieltä.
**Vastaus:**

### Q-073 · 2026-10-11 · kirjaaja: Jerry
**Kysymys veljelle:** palaavatko paluukarvauksessa hyökkääjät keskikaistaa ja pakit omia kaistojaan?
**Konteksti:** paluukarvaus (`data-schema.md`, D-085). Nyt kaikki väärällä puolella kiekkoa olevat palaavat omaa kaistaansa.
**Ehdotettu oletus:** oma kaista kaikille.

### Q-071 · 2026-10-11 · kirjaaja: Jerry (myöhemmin)
**Kysymys:** pitäisikö laitureiden sijoittua suorahyökkäyksessä kiekollisen edelle?
**Konteksti:** E-006:n analyysi (D-084): O-7:n suorahyökkäyssäännössä tukipelaajien kohde on kiekollisen x-rivi, joten alueelle tulossa hyökkääjiä on kolme 63 %:ssa tuonneista, kun Wisehockeyssa yleensä 1–2. Esim. laiturit riviä kiekollisen edellä muuttaisivat voimasuhteita ja paikkojen syntyä.
**Ehdotettu oletus:** ei muutosta nyt. Palataan, kun paluukarvaus on mitattu.
**Seuraavan kierroksen ehdotus (Jerry, 2026-10-11):** E-006:n jälkeen ylivoimaisia alueelletuonteja on 0 %, eli 2v1-hyökkäyksiä ei synny lainkaan, ja paluukarvaus vähentää niitä edelleen. Ehdotus: laiturit luistelevat suorahyökkäyksessä kaistaansa kiekollisen edelle, jolloin ylivoima syntyy, kun hyökkäävä joukkue on nopeampi (Nopeus vs puolustajan Nopeus) tai siirtymä on nopea. Tavoite: ylivoimat ovat ansaittuja, eivät ilmaisia. Ei toteuteta paluukarvauksen kierroksella.

### Q-070 · 2026-10-11 · kirjaaja: Jerry (idea myöhemmäksi, ei virstanpylväisiin)
**Kysymys:** lisätäänkö peliin ottelun jälkeinen kertomusraportti pelaajalle Wisehockeyn AI-analyysin tyyliin?
**Konteksti:** raportti kertoo, kumpi joukkue hallitsi (hyökkäysalueaika, xG), mistä paikat syntyivät (paikkatyypit, maski, Royal Road, voimasuhteet) ja miksi tulos meni niin kuin meni (esim. "loit 4,8 xG, mutta maalivahti torjui"). Data on jo tapahtumalokissa. Tukee visiota ja pilaria "jokainen tappio opettaa".
**Ehdotettu oletus:** ei toteuteta nyt. Otetaan esille käyttöliittymävaiheessa.

### Q-068 · 2026-10-11 · kirjaaja: Jerry (designerille, myöhemmin)
**Kysymys:** pitäisikö kuljetus vai päätyyn lyönti alivoimalla (N < M) olla oma taitopäätös?
**Konteksti:** Wisehockeyn data (Jokerit–HIFK, 31 hallittua alueelletuontia, D-081): kaikki hallitut alueelletuonnit säilyttivät kiekon, eli epäonnistuneet yritykset eivät ole datassa. Oikeat pelaajat kuljettavat kiekon sisään vain, kun se todennäköisesti onnistuu, ja muuten lyövät sen päätyyn. Simulaatiossa kuljetus yritetään voimasuhteesta riippumatta (M-9:n porras 51–76 %). Päätös voisi olla pelaajan tai valmentajan ohje (esim. kynnys N − M), jolloin hyvä päätös on taitoa.
**Ehdotettu oletus:** ei muutosta nyt. Designer tekee ehdotuksen, kun voimasuhdejakauma ja K4:n arvo on kalibroitu.

### Q-062 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** onko taktiikkamatriisin (D-078 K3) komentorivimuoto hyväksyttävä? E-005 sanoo "taktiikkamatriisi batch-raporttiin", mutta ei kerro, ajetaanko matriisi aina batchin mukana vai omana tilanaan, eikä mitä vastustaja pelaa matriisissa.
**Konteksti:** toteutettu näin: `batch --tactic-matrix` ajaa AI-parien sijaan pelkän matriisin ja kirjoittaa `tactic-matrix.md`:n ja `tactic-matrix.csv`:n `--out`-kansioon. Joukkue A = `rotation`, oma järjestelmä `--rotation-system` (oletus forecheck212), rivin siirtymäohje (rush, regroup). Joukkue B = `rotation`, sarakkeen järjestelmä (kaikki ladatut järjestelmät latausjärjestyksessä), siirtymäohje `--rotation-transition` (oletus rush, kuten E-005:ssä). Kokoonpanot ovat aina peilattuja. `--matches` on otteluita per solu, oletus 2000 tässä tilassa. Ilman `--tactic-matrix`-lippua `--rotation-transition` asettaa `rotation`-valmentajan siirtymäohjeen tavallisessa batchissa. Täysi batch-raportti ei sisällä matriisia (4 × 2000 lisäottelua joka ajossa). `match`-komento ei vielä tunne `--rotation-transition`-optiota.
**Ehdotettu oletus:** yllä kuvattu, kunnes Jerry päättää.
**Vastaus:** (Jerry täyttää)

### Q-061 · 2026-10-11 · kirjaaja: Jerry
**Kysymys (myöhemmin, virstanpylvään 5 jälkeen):** riittääkö tapahtumaloki Unity-toiston animointiin?
**Konteksti:** Unity-toisto. Tarkistetaan, että jokaisessa tapahtumassa on kiekon lähtö- ja tulosolmu. Reitin muotoa (suora, laidan kautta, kiertää laitaa) ei tallenneta simulaatioon, vaan Unity päättelee sen geometriasta.

### Q-060 · 2026-10-11 · kirjaaja: Jerry
**Kysymys veljelle:** minne ohilaukaukset tyypillisesti päätyvät: samaan kulmaan, laitaa kiertäen vastakkaiselle puolelle vai pisteelle?
**Konteksti:** `tuning.json` `loosePuckSpots.missedShot`. Vastauksen jälkeen osioon lisätään vaihtoehto "kiertää laitaa" omalla todennäköisyydellään. Ei fysiikkaa.

### Q-059 · 2026-10-11 · kirjaaja: game-designer
**Kysymys veljelle:** (1) Voivatko molemmat joukkueet ottaa aikalisän saman katkon aikana, ja jos voivat, onko käytäntö peräkkäiset 30 sekunnin aikalisät? (2) Katkaiseeko aikalisä käytännössä vastustajan painostusvaiheen ("momentum") enemmän kuin tavallinen katko?
**Konteksti:** `data-schema.md`, AI-4 (D-077 kohta 6). Kohta (2) olisi uusi vaikutus (esim. vastustajan painetilan lisäkerroin aikalisässä), joten se vaatii Jerryn päätöksen; nyt aikalisä vaikuttaa vain energiaan.
**Ehdotettu oletus:** (1) molemmat saavat ottaa, vaikutus sovelletaan kahdesti peräkkäin; (2) ei painevaikutusta, katkon `keepOnStoppage` riittää.

### Q-058 · 2026-10-11 · kirjaaja: game-designer
**Kysymys:** näkeekö valmentaja oman joukkueensa pelaajien todellisen energian? D-077 kohta 3 luettelee julkiseksi tiedoksi jäällä olevat yksiköt, kellon, oman jääajan ja julkisen tapahtumalokin, ei energiaa.
**Konteksti:** `data-schema.md`, AI-1 ja AI-5. Nykyinen `CoachView` antaa todellisen energian. Oikea valmentaja näkee väsymyksen, mutta ei tarkkaa lukua.
**Ehdotettu oletus:** ei näe. Sääntöpohjaiset AI:t laskevat energia-arvion jää- ja penkkiajasta (`ai.estimate.extraDrainPerSecondOnIce`), ja `CoachView.Energy` poistetaan. Jos Jerry sallii energian, arvio korvataan todellisella arvolla eikä muu speksi muutu. Myöhemmin käyttöliittymässä energia voidaan näyttää karkeana tasona.

### Q-057 · 2026-10-11 · kirjaaja: koordinaattori
**Kysymys:** virstanpylvään 4 suunnitelman kohdat, joita D-077 ei ratkaissut: (a) AI:n kynnysarvot uuteen `ai`-osioon `tuning.json`:iin; (b) `batch --matches N` on otteluiden määrä per AI-pari; (c) kokoonpanot johdetaan parin siemenestä (ei aina siemenet 1 ja 2), ja lisäksi peilikokoonpano järjestelmävertailuun.
**Konteksti:** ohjelmoijan suunnitelman kysymykset K4, K6, K7.
**Ehdotettu oletus:** suositukset (a)–(c) sellaisinaan, kunnes Jerry päättää.

### Q-050 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** sisältävätkö Wisehockeyn laukausyritykset blokatut laukaukset? Simulaation laukausyritykset sisältävät ne.
**Konteksti:** `targets.json` `shotAttempts` (D-071).
**Vastaus (Jerry, 2026-10-10, alustava):** todennäköisesti sisältävät, koska aikajanalla blokattu laukaus on Laukaus-tapahtuma. Jerry varmistaa.

### Q-049 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** miten laukaukset paineen alla (99,8 %) saadaan tavoitteeseen 68–76 %, kun syy ei ole järjestelmädatassa? Tarvitaan koodi- tai määritelmämuutos (M-7 tai D-041), ja Jerryn pitää valita suunta.
**Konteksti:** kalibrointikierroksen vaihe 2 (D-069). Oman pään box+1 ei ole mies miestä -puolustusta: molemmissa järjestelmissä vain F1 (lähin hyökkääjä, `puckOffset` [0, 0]) menee kiekolliseen, ja muut neljä seisovat kiinteissä solmuissa. Neljän ottelun tapahtumalokissa jokaisessa laukauksessa (584) laukojan solmussa oli tasan yksi puolustaja, 97 % hyökkääjä (F1). Kaikki laukaukset lähtivät kuudesta solmusta: puolustajan näkökulmasta (2, 1–3) ja (3, 1–3) eli slotti, ympyrät, korkea slotti ja pisteet. 11 × 5 -verkossa oikean box+1:n neljä paikkaa (pakit ympyröissä tai maalin edessä, laiturit pisteillä tai korkeassa slotissa) ovat juuri näitä solmuja, joten säteellä 0 laukoja on lähes aina jonkun solmussa. Kokeilu (ei repossa): kun F1 ei seuraa kiekollista slottiin, paine pysyi 99,9 %:ssa, koska boxin pelaaja oli valmiiksi samassa solmussa. Painostaja ehtii laukojan solmuun myös siksi, että puolustajat liikkuvat `maxNodesPerBeat` (2) solmua ennen jokaista toimintoa (D-041). Syöttäjiin osuu sama ongelma: omassa päässä 97 % syötöistä annetaan paineen alla.
**Vaihtoehdot (programmer toteuttaa, Jerry päättää):**
1. M-7: laukoja ei ole paineen alla, jos laukaus seuraa suoraan onnistunutta syöttöä (laukaus heti syötöstä) eikä puolustaja ollut vastaanottajan solmussa ennen tahdin liikkeitä. Ilmiö on oikea: heti syötöstä lähtevät laukaukset ovat tyypillisesti vapaita.
2. M-7: paineen antaa vain puolustaja, joka pelaa kiekolliseen eli jonka järjestelmäkohde on `puckOffset`. Muut samassa solmussa olevat puolustajat vaikuttavat blokkiin ja maskiin, eivät paineeseen. Tarvitsee skeemaan tai koodiin tiedon painostajan roolista.
3. D-041 tai O-6: kiekolliseen pelaava puolustaja liikkuu syötön jälkeen enintään 1 solmun ennen seuraavaa toimintoa (reaktioviive). Muuttaa lukittua päätöstä.
**Kysymys veljelle:** miten Wisehockey määrittelee "paineen alla" (etäisyys metreinä, aikaikkuna ennen laukausta)? Verkon solmu on noin 5–6 m leveä, joten säde 0 voi olla analyysin määritelmää väljempi.
**Ehdotettu oletus:** järjestelmädataa ei muuteta tämän vuoksi. Vaihe 3 (syötöt) voi edetä, koska syöttöjen paineosuus (67,2 %) on jo lähellä tavoitetta.
**Vastaus (Jerry, 2026-10-10):** toteutetaan vaihtoehto 1. Designer tarkentaa M-7:n `data-schema.md`:hen ensin, programmer toteuttaa, mitataan 200 ottelulla. Jos laukauksista paineen alla on edelleen yli 80 %, vaihtoehto 2 tuodaan Jerrylle päätettäväksi. Vaihtoehto 3 myöhemmin. Vaihe 3 (syötöt) odottaa tämän mittausta. Kysymys veljelle Wisehockeyn paineen alla -määritelmästä (metrit, aikaikkuna) jää auki. Kirjattu: D-070.
**Vastaus (Jerry, 2026-10-10, mittauksen jälkeen):** vaihtoehto 1 laski osuuden vain 99,8 %:sta 97,9 %:iin, joten vaihtoehto 2 hyväksytty: paineen antaa vain kiekolliseen pelaava puolustaja (järjestelmäkohde `puckOffset`), sekä laukauksissa että syötöissä. Kun järjestelmä ei ohjaa liikettä (esim. suorahyökkäys), painostaja on kiekollista lähin puolustaja. Jos laukausten tai syöttöjen paineosuus putoaa alle 68 %:n, designer saa nostaa säteen arvoon 1. Kysymys veljelle jää auki. Kirjattu: D-072.
**Vastaus (Jerry, 2026-10-11):** vaihtoehto 3 hyväksytty (D-074, painostajan reaktioviive syötön jälkeen). Säde pysyy 0:ssa. Jos laukausten paineosuus on tämän jälkeen yli 80 %, sitä ei hiota ennen veljen vastausta. Kysymys veljelle jää auki.

### Q-048 · 2026-10-10 · kirjaaja: Jerry
**Kysymys:** montako aloitusta ottelussa on? Oikeassa kiekossa tyypillisesti noin 55–65, simulaatiossa noin 47.
**Konteksti:** kalibrointikierros ennen virstanpylvästä 4 (D-069). Jerry tarkistaa luvun Wisehockeysta.

### Q-043 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** D-058:n ratkaisusäännön kaksi tarkennusta. (1) Lähimmän vapaan solmun renkaassa "lähempänä omaa maalia": millä etäisyydellä, ja mikä ratkaisee, jos kaikki designerin kriteerit ovat tasan (esim. (0, 2) ja (2, 2) ovat yhtä lähellä maalia (1, 2), samalla kaistalla ja samalla y:llä)? (2) Validointivirhe (D-062): hylätäänkö järjestelmä vain, kun kaksi roolia osuu samaan solmuun aina (sama `node` tai sama `puckOffset` samassa säännössä), vai myös, kun `puckOffset` osuu kiinteään solmuun vain joillakin kiekon sijainneilla?
**Konteksti:** designerin hyväksytty M3-suunnitelma (D-062), kohta D-058, `Sim.Tactics.OccupancyResolver` ja `SystemValidator`. Kohta (2): F1 `puckOffset` [0, 0] osuu molempien nykyisten järjestelmien kiinteisiin kohteisiin joillakin kiekon sijainneilla, joten tiukka tulkinta hylkäisi käytännössä jokaisen painostavan järjestelmän.
**Ehdotettu oletus (toteutettu):** (1) Chebyshev-etäisyys omaan maalisolmuun, sitten Manhattan-etäisyys samaan, sitten etäisyys keskikaistaan, sitten pienempi y, sitten pienempi x (joukkueen omassa näkökulmassa). (2) Vain aina osuvat kohteet hylätään. Kiekon sijainnista riippuvat osumat ratkaistaan ajon aikana resolverilla. Kuvioiden lähtö- ja tahtisolmut hylättiin jo ennestään.

### Q-042 · 2026-10-10 · kirjaaja: Jerry (virstanpylväs 4, ensimmäisen batch-raportin jälkeen, D-077)
**Kysymys:** pitkä kiekko (icing) ja sääntö, ettei sen tehnyt ketju pääse vaihtoon.
**Konteksti:** katkon syy "pitkä kiekko" on jo tapahtumaskeemassa, mutta sitä ei mallinneta.

### Q-041 · 2026-10-10 · kirjaaja: Jerry
**Kysymys:** sallitaanko toinen yritys (D-065) myös solmuista (8,1) ja (8,3) heikommalla xG:llä?
**Konteksti:** päätetään raportin perusteella.

### Q-040 · 2026-10-10 · kirjaaja: game-designer
**Kysymys veljelle:** missä aloitus on, kun hyökkääjä on laukonut kiekon ulos hyökkäysalueella?
**Ehdotettu oletus:** lähin piste samalla alueella.

### Q-039 · 2026-10-10 · kirjaaja: game-designer
**Kysymys veljelle:** luokitellaanko keskialueen riistosta alkanut suorahyökkäys riistoksi vai suorahyökkäykseksi?
**Ehdotettu oletus:** riisto ensin.

### Q-032 · 2026-10-10 · kirjaaja: Jerry
**Kysymys:** kalibroidaan paineen alla -säde (M-7, `pressure.underPressureNodes`) raportin perusteella.
**Konteksti:** laukausten osuutta paineen alla verrataan Wisehockeyn 68–76 %:iin (`stats-and-checks.md`, tavoitearvot). Virstanpylväs 5.

### Q-030 · 2026-10-10 · kirjaaja: Jerry
**Kysymys veljelle:** miten Wisehockey määrittelee Royal Road -syötön?
**Konteksti:** M-3 (`data-schema.md`). Määritelmän pitää vastata analyysityökalua, jotta mittari on vertailukelpoinen.

### Q-021 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** kertyykö kemiaa myös hyökkäyskolmikon ja pakkiparin välille, vai vain yksikön sisällä?
**Konteksti:** D-023 (kolmikot ja parit erikseen), `tuning.json` `chemistry`. Tarvitaan virstanpylväässä 3.
**Ehdotettu oletus:** vain yksikön sisällä.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.
**Ajoitus (Jerry, 2026-10-10):** siirretty virstanpylvään 3 jälkeen (D-067).

### Q-019 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saavatko muut paikkavaihdot miinuksen (esim. sentteri laidassa, hyökkääjä pakkina)?
**Konteksti:** D-024 määrittelee vain väärän puolen (LW↔RW, LD↔RD).
**Ehdotettu oletus:** ei miinusta, kunnes päätetään.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.
**Ajoitus (Jerry, 2026-10-10):** päätetään draftin yhteydessä (D-067).

### Q-014 · 2026-10-09 · kirjaaja: game-designer (siirretty Q-009:stä)
**Kysymys:** lasketaanko xG laukausyritystä kohden (myös blokatut ja ohi menneet) vai maalia kohti mennyttä laukausta kohden, ja mihin xG-mittakaava sidotaan, jotta ottelussa syntyy 4–7 maalia?
**Konteksti:** `tuning.json` `checks.shot.baseXg`. Ratkaisee, onko `baseXg` maalitarkistuksen p0 sellaisenaan. Q-009:n rajakysymys on ratkaistu (D-014:n tarkennus).
**Kysymys veljelle (liittyy Q-004):** mitä laukauksia analyysityökalun xG kattaa (kaikki yritykset, blokkaamattomat vai maalia kohti menneet)?
**Ehdotettu oletus:** `baseXg` on maalin todennäköisyys maalia kohti menneestä laukauksesta, raportin xG on yrityksen kokonaistodennäköisyys (läpi × maalia kohti × maali). Mittakaava kalibroidaan maalimäärään 4–7.

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

### Q-076 · 2026-10-11 · kirjaaja: game-designer
**Kysymys:** saako ottelun JSON-vientiin (`match --json`) lisätä otsakkeen toistoa varten: kokoonpano (pelaajan id, joukkue, pelipaikka C/LW/RW/LD/RD/G, ketju) ja erän pituus sekunteina? Tämä ei ole uusi tapahtumatyyppi eikä muuta tapahtumaskeemaa, mutta muuttaa vientimuotoa.
**Konteksti:** Q-061-tarkistus (D-086 kohta 4), ottelu `rulebased` vs `random`, siemen 42, 815 tapahtumaa. Jokaisessa tapahtumassa on `placement`: kaikki 10 kenttäpelaajaa ja 2 maalivahtia solmuina sekä kiekon solmu ja haltija, kotijoukkueen koordinaatistossa (maalivahdit solmuissa 7 ja 47 kaikissa erissä). Kiekon lähtö- ja tulosolmu saadaan siis jokaiseen tapahtumaan: lähtö = edellisen tapahtuman kiekon solmu (syötössä syöttäjän ja laukauksessa ampujan solmu), välipiste = maali tai blokkaajan solmu (`stoppedById`), tulo = tapahtuman `placement.puckNodeId`. Aloituspiste saadaan keskushyökkääjien yhteisestä solmusta (23/23 aloitusta). Ainoa puuttuva tieto on pelipaikka ja ketju: lokissa on vain id, joukkue ja maalivahtilippu, joten toisto ei voi näyttää pelaajia pelipaikan mukaan.
**Ehdotettu oletus:** lisätään otsake vientiin (programmer), tapahtumaskeemaa ei muuteta. Siihen asti toisto näyttää pelaajat joukkueen ja id:n mukaan.
**Vastaus (Jerry, 2026-10-11):** kyllä. `match --json` -vientiin otsake, jossa on formaattiversio, erän pituus, kokoonpano (id, joukkue, pelipaikka, ketju tai pari, maalivahti) ja kaukalodata (`rink.json`:sta: mitat metreinä, solmut, maalisolmut). Tapahtumaskeema ei muutu. Kirjattu: D-087.

### Q-075 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** D-036:n kuvionvalinta ja peilaus: verrataanko kiekollista kuvion kirjoitettuun vai pelattavaan (tarvittaessa peilattuun) `puckCarrier`-pelipaikkaan? Nykyinen koodi vertaa kirjoitettuun ja peilaa vasta sen jälkeen. Jos kiekollinen on `LW` ja kuvion kirjoitettu kiekollinen `LW`, mutta kuvio peilataan (`LW` → `RW`), avaussyöttöä ei tehdä. Silloin kuvion tahdit ajaa `RW`, jolla kiekkoa ei ole: esim. `skate` siirtää `RW`:n alueelle, lokiin kirjataan hänen alueelletuontinsa, mutta kiekko jää `LW`:lle.
**Konteksti:** löytyi D-085:n raporttiosion testistä (`PlaySelector.Select` ja `ShiftRun.SelectPlayStep`; `data-schema.md` Peilaus ja D-036). Batch-raportin osio "Entry strength by possession origin" kertoo, kuinka moni onnistunut kuljetus on tällainen. Siemenen 1 ottelussa 2 / 25 kuljetusta. Tämä on bugi, ja sen korjaus muuttaa kaikkia lokeja. Siksi sitä ei korjattu D-085:n mittauksen keskellä.
**Ehdotettu oletus:** verrataan pelattavaan (peilattuun) kuvioon. Valinta tehdään peilauksen jälkeen: kiekollinen = peilatun kuvion `puckCarrier` → ei syöttöä, muuten avaussyöttö peilatun kuvion kiekolliselle. Korjaus omana `fix:`-committinaan ja batch ennen/jälkeen, kun Jerry hyväksyy.
**Vastaus:**
**Vastaus (Jerry, 2026-10-11):** korjataan ehdotetulla tavalla ja lisätään testi. Kirjattu: D-086.
### Q-074 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** paluukarvauksen (D-085) reunatapaus: väärällä puolella oleva puolustaja, joka on jo riviä x_t lähempänä omaa maaliaan. Esimerkki: kiekko on puolustavan joukkueen maalin takana (hyökkäävän näkökulmasta x_k = 10), joten x_t = `opponentGoal.x` − 1 = 8, ja puolustaja on maalin vieressä x = 9. Speksin askel on aina (+1, 0) kohti omaa maalia, mutta kohde (x_t, y_p) on hänen takanaan. Liikkuuko hän taaksepäin riville x_t, pysyykö paikallaan vai liikkuuko järjestelmän mukaan?
**Konteksti:** `data-schema.md`, Paluukarvaus suorahyökkäyksessä, Liike kohdat 2–3. Tilanne on harvinainen (rushin kiekollinen maalin takana).
**Ehdotettu oletus (toteutettu):** pysyy rivillään. Kohde on (max(x_t, x_p), y_p) hyökkäävän näkökulmasta, eli askel ei koskaan vie poispäin omasta maalista. Samalla tavalla toteutettiin kohta, jota speksi ei mainitse: jos `pinch`-D1 paluukarvaa, korvattua kohdetta ei ole, joten hän ei ole sitoutunut (O-10: "`pinch`-D1 korvatun kohteen ajan").
**Vastaus:**
**Vastaus (Jerry, 2026-10-11):** oletus hyväksytty. Kirjattu: D-086.

### Q-072 · 2026-10-11 · kirjaaja: game-designer (kirjannut koordinaattori)
**Kysymys:** paluukarvauksen speksin (`data-schema.md`, Paluukarvaus suorahyökkäyksessä, D-084 kohta 2) avoimet kohdat: (1) riittääkö `transitions.backcheckNodesPerBeat` = 2 (vain suunta muuttuu), vai pitäisikö kiekottoman pelaajan olla kiekollista nopeampi (arvio: 3 ylikorjaa, alivoimaisia yli 80 %)? (2) Rakennetaanko paluukarvaus, kun E-006 yksin toi jakauman haarukoihin (66,4 / 33,6 / 0,0 %)? Hyöty olisi realismi, ei jakauma. (3) **Kysymys veljelle:** palaavatko karvaajat omaa kaistaansa vai keskikautta?
**Konteksti:** D-084, E-006:n batch (`reports/m5-strength/e006`). Designerin käsin laskettu arvio: väärällä puolella olevia puolustajia oman alueen riistojen jälkeen n. 1,64 → 0,6–0,9, jakauma muuttuu enintään muutaman prosenttiyksikön.
**Ehdotettu oletus:** (1) 2, (2) Jerry päättää, (3) oma kaista.
**Vastaus (Jerry, 2026-10-11):** (1) `backcheckNodesPerBeat` = 2. (2) Paluukarvaus toteutetaan (tapa b), koska se korjaa sijainnit, vaikka E-006 jo toi jakauman haarukoihin. (3) Oletuksena oma kaista; kysymys veljelle siirretty Q-073:een. Kirjattu: D-085.

### Q-069 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** lasketaanko A-T2:n ikkunaan (kohta 3) myös vanhat havainnot järjestelmää c vastaan ajalta ennen kuin `adaptive` viimeksi vaihtoi c:hen?
**Konteksti:** kohdan 3 sääntö on kirjaimellisesti "viimeiset `styleWindowEvents` luokiteltua havaintoa, joiden järjestelmä-id = c". Saman kohdan perustelu, kohta 4 ("aikalisän jälkeinen uusi kutsu: uuden c:n ikkuna on tyhjä, n = 0") ja testilistan kohta 4 ("vaihdon jälkeen ikkuna on tyhjä") edellyttävät kuitenkin, että ikkuna on vaihdon jälkeen tyhjä. Kirjaimellinen sääntö ei takaa sitä, kun `adaptive` palaa järjestelmään, jota se on jo pelannut: esim. trap → karvaus → trap, jolloin ensimmäisen trap-jakson havainnot (joiden perusteella vaihdettiin karvaukseen) olisivat heti ikkunassa ja voisivat vaihtaa takaisin karvaukseen jo seuraavalla katkolla tai aikalisän jälkeisellä kierroksella.
**Ehdotettu oletus (toteutettu):** ikkunaan lasketaan vain havainnot, jotka on tehty sen jälkeen, kun c viimeksi valittiin (tapahtumaindeksi ≥ lokin pituus valintahetkellä; ottelun alussa 0). Näin vaihdon jälkeen ikkuna on aina tyhjä, kuten kohdat 3 ja 4 ja testit sanovat. Tilat `protect`/`chase` eivät muuta c:tä eivätkä nollaa ikkunaa.
**Vastaus (Jerry, 2026-10-11):** ohjelmoijan tulkinta vahvistettu: vain viimeisimmän järjestelmävalinnan jälkeiset havainnot lasketaan. Kirjattu: D-083.

### Q-067 · 2026-10-11 · kirjaaja: game-designer
**Kysymys:** hyväksytkö `adaptive`n järjestelmävalinnan speksin A-T2 (D-080 kohta 3)?
**Konteksti:** `data-schema.md`, AI-2, A-T2. Tiivistelmä: (1) Skeemamuutosta ei tarvita. Vastustajan siirtymä luokitellaan sen hallinnan ensimmäisestä toimintotapahtumasta: kuvio-id tyhjä = suorahyökkäys, kuvio-id asetettu = kokoaminen (D-045, O-7). Aloituksesta alkaneet hallinnat ja hallinnat, joiden ensimmäinen toiminto on kiekonmenetys, jätetään pois. (2) Vain havainnot nykyistä omaa järjestelmää vastaan lasketaan, joten vaihdon jälkeen ikkuna alkaa tyhjästä. (3) Vaihto vain katkolla, kun vähintään `styleMinEvents` (4) havaintoa viimeisestä `styleWindowEvents` (6) havainnosta antaa suorahyökkäysosuuden ≥ 0,65 (→ trap) tai ≤ 0,35 (→ karvaus). Avaimet `controlledEntryShareFor*` poistuvat. (4) Kierre `rulebased`ia vastaan jää, koska `rulebased` vastaa yhden katkon viiveellä ja kumpikin on toisen paras vastaus (kolikkopeli). Heilunnan esto ei voi nostaa `adaptive`a yli n. 50 %:n `rulebased`ia vastaan. Arvio: 38–48 % (nyt 30,2 %), vs `randomLines` 60–72 % (nyt 52,2 %).
**Ehdotettu oletus:** ei toteutusta ennen hyväksyntää. Jos hyväksyt, mittaus 5000 ottelua per pari ja raporttiin vaihtojen määrä, vastapelattu aika ja luokittelun osuma (tavoite ≥ 90 %).
**Vastaus (Jerry, 2026-10-11):** (1) A-T2 hyväksytty ehdoin: signaali vain julkisesta tapahtumalokista, vaihto vain katkolla, vaihto vaatii selvän signaalin vähintään kynnyksen verran tapahtumia, kaikki kynnykset `tuning.json`:n `ai`-osiossa. Koordinaattori tarkisti, että speksi täyttää ehdot. (2) K4:ää ei kalibroida vielä: Wisehockeyn data (vain onnistuneet tuonnit) kalibroi voimasuhteiden jakauman (M-8), ei K4:n porrasta. Jos M-8:n korjauksen jälkeen uudelleenryhmittyminen karvausta vastaan jää selvästi alle 35 %:n (luottamusväli kokonaan rajan alla), K4:ää säädetään yhden kerran ja mitataan uudelleen. Kirjattu: D-082.

### Q-066 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** miten `adaptive`n oman järjestelmän sääntö (AI-2 A-T kohta 2) muutetaan D-078:n jälkeen? Kirjaimellisesti toteutettuna se ajaa `adaptive`n trappiin, jota `rulebased` nyt rankaisee uudelleenryhmittymisellä (D-079: `adaptive` vs `rulebased` 51,2 % → 30,2 %).
**Konteksti:** tutkittu D-079:n avoin jatko. Koodi noudattaa A-T:tä sanatarkasti: vastustajajakauma ja todennäköisin järjestelmä (kohdat 1 ja 3) lasketaan vain julkisesta lokista, tasatilanne id-järjestyksessä, N = 0 → `modes.normal.transition`. Syy on kohta 2: `rulebased` pelaa suorahyökkäystä `adaptive`n karvausta vastaan, jolloin hallittujen alueelle tulojen osuus q ≥ 0,6 → `adaptive` vaihtaa trappiin (`passiveSystem`). K2:n jälkeen `rulebased` näkee trapin ja vaihtaa uudelleenryhmittymiseen (matriisissa trapia vastaan +17,9 %-yks.). Uudelleenryhmittyminen tuottaa edelleen hallittuja tuloja (syöttötulot), joten q pysyy korkeana ja hystereesi lukitsee `adaptive`n trappiin. 1000 ottelun ajoissa (siemen 1) `rulebased` pelasi `adaptive`a vastaan n. 77 % ajasta uudelleenryhmittymistä trappia vastaan. Ennen K2:ta `rulebased` pelasi aina suorahyökkäystä, jota vastaan trap toimi. Kokeet väliaikaisella datakopiolla (ei repoon): (A) nykyinen data: vs `rulebased` 30,8 %, vs `randomLines` 52,2 %; (B) kohta 2 käytännössä pois (`controlledEntryShareForPassive` = 1,0, eli `adaptive` pysyy karvauksessa): 50,2 % ja 75,4 %; (C) `transitionBySystem` tyhjä: 45,2 % ja 70,3 %. Kyse on siis suunnittelusta, ei koodivirheestä: q mittaa alueelle tulon tapaa, ei vastustajan siirtymäohjetta, eikä sääntö huomioi, että vastustaja voi vastata järjestelmävalintaan.
**Ehdotettu oletus:** koodi pysyy speksin mukaisena, kunnes Jerry päättää. Ohjelmoijan vaihtoehdot: (a) poistetaan kohta 2, eli `adaptive` käyttää `modes.normal.system`ia (koe B: n. 50 % vs `rulebased`); (b) kohta 2 valitsee järjestelmän vastustajan havaitun siirtymätavan mukaan (esim. suorahyökkäysten osuus vastustajan alueelle tuloista: paljon suorahyökkäyksiä → trap, paljon uudelleenryhmittymisiä → karvaus), mikä vaatii julkisen merkin siirtymätavasta lokissa tai sen päättelysäännön; (c) q:ssa lasketaan vain kuljetukset (ei syöttötuloja). Vaihtoehdot (b) ja (c) vaativat designerin speksin ja mittauksen.
**Vastaus (Jerry, 2026-10-11):** vaihtoehto (b). Designer speksaa: signaali on vastustajan siirtymätyyli (suorahyökkäysten ja uudelleenryhmittymisten osuus) julkisesta tapahtumalokista; paljon suorahyökkäyksiä → trap, paljon uudelleenryhmittymisiä → karvaus; heilunnan esto: järjestelmää vaihdetaan vain katkolla ja vain, kun signaali on ollut selvä vähintään tietyn määrän tapahtumia. Kynnykset `tuning.json`:n `ai`-osioon. Speksi näytetään Jerrylle ennen toteutusta. Kirjattu: D-080.

### Q-065 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** pitääkö M-8:n "N vs M" -määritelmää tarkentaa? Kirjaimellisesti toteutettuna se tuottaa lähes aina 5v5-, 3v5- tai 4v5-lukuja, ei Wisehockeyn tyypillisiä 2v2-, 1v3- ja 3v3-lukuja.
**Konteksti:** koodi (`ShiftRun.Numbers`) noudattaa M-8:aa sanatarkasti: N = hyökkääjän kaikki kenttäpelaajat (myös puolustajat), joiden x ≥ 5, ja kiekollinen aina. M = puolustajan kaikki kenttäpelaajat, joiden x ≥ kiekon x, koko kaukalosta ilman alue- tai etäisyysrajaa. Kiekon x on lähtösolmu (kuljetus) tai syöttäjän solmu (syöttö), eli siniviivan yli tultaessa x = 4–6. Puolustava joukkue on silloin yleensä kokonaan kiekon tasalla tai sen ja oman maalinsa välissä (keskialue on vain 3 riviä), joten M = 5 lähes aina. Syötöllä tehty alueelle tulo tulee käytännössä vain kuviosta `middleDriveWidePass`: C (5,2) syöttää, LW ja RW ovat (7,·), LD ja RD (4,·) → N = 3, M = 5, siksi aina "3 vs 5". K4-matriisissa (`reports/tactic-counterplay/k4`) kuljetuksista 5v5 on 99 821, 3v5 115 212 ja 4v5 28 032, syötöistä 3v5 77 400/77 400. M-9:n muokkaaja `strengthDifferencePerPlayer` käyttää samoja lukuja, joten sen vaikutus on käytännössä vakio-offset eikä erottele tilanteita.
**Ehdotettu oletus:** koodi pysyy sanatarkkana, kunnes Jerry päättää (Wisehockeyn yhden erän voimasuhteet vertailuun). Ohjelmoijan ehdotus tarkennukseksi: N = hyökkääjän kenttäpelaajat, joiden x ≥ kiekon x (kiekon tasalla tai edellä, kiekollinen aina mukaan); M = puolustajan kenttäpelaajat, joiden x > kiekon x (tiukasti kiekon ja oman maalin välissä). Puolustajat (LD, RD) lasketaan silloin vain, jos ne ovat kiekon tasalla. Vaihtoehto: rajata molemmat hyökkäysalueeseen ja sinisen viivan riviin (x ≥ 6). Kumpikin muutos vaatii M-9:n paikkamerkin 0,25 uudelleenkalibroinnin.
**Vastaus (Jerry, 2026-10-11):** ohjelmoijan ensimmäinen ehdotus: N = kiekollinen ja kiekon tasalla tai sen edellä olevat hyökkääjät, M = vain kiekon ja oman maalinsa välissä olevat puolustajat. M-8 päivitetty. K4:n arvo kalibroidaan uudelleen, kun Jerry on kirjannut Wisehockeyn voimasuhteet. Kirjattu: D-080.

### Q-063 · 2026-10-11 · kirjaaja: game-designer
**Kysymys:** koskeeko alueelle tulon voimasuhdemuokkaaja (D-078 K4) myös syöttämällä tehtyä alueelle tuloa?
**Konteksti:** `stats-and-checks.md`:n tarkistustaulukossa muokkaaja "puolustusjärjestelmä" on vain rivillä "Kuljetus alueelle" (`zoneEntryCarry`). Syöttämällä tehty alueelle tulo (M-8, tapa "syöttö") käyttää `pass`-tarkistusta, joten trap ei rankaisisi siniviivan yli syöttämistä. Oikeassa kiekossa tiivis 1-2-2 vaikeuttaa myös syöttöä siniviivan yli, mutta syöttöä haittaavat jo syöttölinjan puolustaja (M-1) ja paine (M-7). `data-schema.md` M-9.
**Ehdotettu oletus:** vain `zoneEntryCarry`, kuten taulukossa. Alueelle tulon onnistuminen raportoidaan voimasuhteittain kuljetus ja syöttö erikseen. Jos syöttämällä tulo onnistuu K4:n jälkeen selvästi muita tasaisemmin kaikilla voimasuhteilla, designer tuo Jerrylle ehdotuksen `pass`-tarkistuksen vastaavasta muokkaajasta.
**Vastaus (Jerry, 2026-10-11):** lopullinen. Voimasuhdemuokkaaja koskee vain kuljetuksia, koska syöttölinjan tarkistus ottaa jo puolustajien läheisyyden huomioon syötöissä. Kirjattu: D-080.

### Q-064 · 2026-10-11 · kirjaaja: programmer
**Kysymys:** missä solmussa karvaaja saa kiekon, kun kokoamisen `breakout`-tarkistus epäonnistuu (D-078 K1)? O-7:n kohta 5 sanoo "Lähin karvaaja saa kiekon kiekollisen solmussa samalla polulla kuin epäonnistuneessa `skate`-kuljetuksessa", mutta epäonnistuneessa kuljetuksessa riistäjä saa kiekon omassa solmussaan, eikä kukaan siirry.
**Konteksti:** lähin karvaaja on usein 1–2 solmun päässä kiekollisesta. Jos hänet siirretään kiekollisen solmuun, D-058 (yksi pelaaja per joukkue per solmu) voi estää siirron, jos joukkuetoveri on jo siinä. Vaikuttaa Kiekonmenetys-tapahtuman solmuun ja riistäjän siirtymän lähtöpisteeseen. Järjestäytyneisyyden pudotus on sama (molemmat solmut ovat kokoavan joukkueen omalla alueella).
**Ehdotettu oletus:** toteutettu kuten `skate`: riistäjä saa kiekon omassa solmussaan, kukaan ei siirry, ja tapahtuman solmu on riistäjän solmu (commit "feat: forecheckers challenge an own-zone regroup", testi `FailedRegroupCheck_IsATakeawayByTheNearestCountedForechecker_AfterRegroupTimeOnly`).
**Vastaus (Jerry, 2026-10-11):** oletus hyväksytty.

### Q-056 · 2026-10-11 · kirjaaja: Jerry
**Kysymys:** ovatko Wisehockeyn paineluvut (syötöt 69 % / 68 %, laukaukset 68 % / 76 %) osuuksia vai onnistumisprosentteja?
**Konteksti:** kalibroinnin vaihe 3 (D-069), E-004.
**Vastaus (Jerry, 2026-10-11):** osuuksia. Jokereiden syötöistä 69 % ja HIFK:n syötöistä 68 % annettiin paineen alla. Sama koskee laukauksia: 68 % / 76 % on laukausten osuus paineen alla. Kirjattu: D-075.

### Q-055 · 2026-10-11 · kirjaaja: game-designer
**Kysymys:** hyväksytkö designerin tarkennukset D-074:n reaktioviiveeseen?
**Konteksti:** `data-schema.md`, Puolustusjärjestelmät, Painostajan reaktioviive. D-074 sanoo "kun kiekko liikkuu syötöllä" ja "seuraavan toiminnon edellä", mutta ei ratkaise alla olevia tapauksia.
**Suositukset (designer, eivät ole hyväksyttyjä, kirjoitettu speksiin oletuksina):**
1. Vain onnistunut syöttö laukaisee viiveen. Syötönkatkossa kiekko vaihtaa joukkuetta, ja irtokiekko ratkaistaan kamppailulla (M-6), jonka liikkeet eivät ole tahdin puolustajaliike.
2. Viive koskee täsmälleen syöttöä seuraavan toiminnon D-041:n vaihetta 2 ja raukeaa, jos väliin tulee järjestelmätilan askel, setup, regroup, katko tai vaihto (esim. kuvion viimeinen tahti on syöttö, tai suorahyökkäys päättyy syötön jälkeen). Perustelu: viive on yhden tahdin reaktio, ja näissä painostajalla on aikaa.
3. D-036:n aloitussyöttö (setupin jälkeen) ja avaussyöttö omalta alueelta (`breakout`) laukaisevat viiveen kuten muutkin syötöt. Aloitusvoitto ei ole syöttö.
4. Painostaja tunnistetaan M-7:n valitsimella ennen vaiheen 2 liikettä (vastaanottaja kiekollisena). Toiminnon hetken paine (M-7) arvioidaan edelleen erikseen liikkeen jälkeen, joten se voi osua eri pelaajaan, jos roolit vaihtuvat.
5. Tasapainoarvo `pressure.presserReactionNodes` (1), sallittu 0 … `plays.maxNodesPerBeat`. Ei paikkamerkki, koska arvo on Jerryn päätöksestä.
**Ehdotettu oletus:** suositukset 1–5, kunnes Jerry päättää. Ohjelmoija toteuttaa niiden mukaan.
**Vastaus:** (Jerry täyttää)
**Vastaus (Jerry, 2026-10-11):** oletukset hyväksytty, kunhan ne eivät muuta lukittuja päätöksiä. Kirjattu: D-075.

### Q-054 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** hyväksytkö designerin tarkennukset D-072:n painostajaan (M-7)?
**Konteksti:** `data-schema.md`, M-7, Painostaja. D-072 sanoo, että paineen antaa järjestelmäkohteeltaan `puckOffset`-puolustaja, ja kun järjestelmä ei ohjaa liikettä (esim. suorahyökkäys), lähin puolustaja. Päätös ei kerro, miten valitaan useasta ehdokkaasta. Lisäksi O-7 sanoo, että puolustajat liikkuvat suorahyökkäyksen aikana järjestelmän mukaan, joten suorahyökkäys ei ole speksissä tila, jossa järjestelmä ei ohjaa liikettä.
**Suositukset (designer, eivät ole hyväksyttyjä, kirjoitettu speksiin oletuksina):**
1. Mikä tahansa `puckOffset` kelpaa, ei vain [0, 0]. Seuraus: trap122:n F1 ([−1, 1]) on painostaja, mutta säteellä 0 se antaa painetta hyökkäys- ja keskialueella harvoin. Trap ohjaa eikä prässää, joten tämä on toivottu ero järjestelmien välillä.
2. Useasta ehdokkaasta valitaan pienin `puckOffset` (Chebyshev, sitten Manhattan), sitten roolijärjestys `F1`, `D1`, `D2`, `F2`, `F3`. forecheck212:n keskialueella painostaja on F1 (F2:n kohde on [−1, 1] tai [−1, 0]). `pinch`-ohjeen D1 on ehdokas pituudella 0.
3. Roolit ja sääntö arvioidaan toiminnon hetkellä nykyisistä sijainneista (puhdas funktio, ei tallennettua tilaa). Kiekollisen solmu on sama kuin liikkeessä, joten sääntö on sama.
4. Jos säännössä ei ole `puckOffset`-kohdetta, kukaan ei anna painetta. Nykyisissä järjestelmissä sellaista sääntöä ei ole.
5. D-070:n vapaan vastaanottajan ehto tarkistaa yhä kaikki vastustajat tilannekuvassa S, ei vain painostajaa (D-070:n sanamuoto, ja S:ssä painostaja määräytyi syöttäjän mukaan).
6. Suorahyökkäys: painostaja on lähin puolustaja, kuten D-072:ssa sanotaan, vaikka O-7:ssä puolustajat liikkuvat järjestelmän mukaan. Perustelu: rushissa puolustus on järjestäytymätön (O < 0,8), ja lähin taaksepäin luistelija painostaa. Vaihtoehto: järjestelmän painostaja myös rushissa, jolloin sääntö on yhtenäinen ja vain toinen yritys (O-8) käyttää lähintä.
**Ehdotettu oletus:** suositukset 1–6, kunnes Jerry päättää. Ohjelmoija toteuttaa niiden mukaan.
**Vastaus:** (Jerry täyttää)
**Vastaus (Jerry, 2026-10-11):** kaikki suositukset hyväksytty. Suorahyökkäyksen painostaja on lähin puolustaja (kohta 6), ja reaktioviive (D-074) koskee myös sitä. Kirjattu: D-074.

### Q-053 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** D-070:n ja D-071:n toteutuksessa on tarkennuksia, joita päätökset eivät kata. Hyväksytkö designerin suositukset?
**Konteksti:** `data-schema.md`, M-7 (Laukaus heti syötöstä) ja targets.json (Mittarit ja niiden laskenta, Automaattinen vertailu).
**Suositukset (designer, eivät ole hyväksyttyjä):**
1. M-7, vapaa vastaanottaja: käytetään samaa sädettä `pressure.underPressureNodes` kuin muussa paineessa. Arvolla 0 sama kuin D-070:n "ei vastaanottajan solmussa", mutta pysyy johdonmukaisena, jos säde kalibroidaan (Q-032).
2. M-7: poikkeus koskee kaikkia syöttö → laukaus -pareja: kuviot, D-036:n aloitussyöttö, aloituskuviot ja suorahyökkäys (O-7). Ei koske toista yritystä eikä aloitusvoitosta suoraan laukaisevaa.
3. M-7: poikkeus muuttaa vain laukaustapahtuman `underPressure`-kentän ja raportin. Laukaukselle ei lisätä `underPressure`-muokkaajaa (se olisi uusi mekaniikka).
4. `smartVsRandomWinRate`: voitot / ottelut, tasapeli ei ole voitto (tiukin tulkinta; tasapelien osuus näytetään erikseen). Vaihtoehto: tasapeli = puoli voittoa.
5. `shotXgTypical` (haarukka 0,003–0,05): raportti näyttää laukausten xG:n 10., 50. ja 90. persentiilin. TOO LOW, jos 10. persentiili < `min`; TOO HIGH, jos 90. persentiili > `max`; muuten OK.
6. Automaattinen vertailu tehdään AI-pareittain yhteisarvolle (molemmat joukkueet). Realismia verrataan järkevien AI:den pariin (rulebased vs. rulebased), koska satunnainen AI vääristää lukuja.
**Ehdotettu oletus:** suositukset 1–6, kunnes Jerry päättää.
**Vastaus (Jerry, 2026-10-10):** suositukset 1–3, 5 ja 6 hyväksytty. Kohta 4 muutettu: `smartVsRandomWinRate` laskee tasapelin puoleksi voitoksi, ja raportti näyttää voitot, tasapelit ja tappiot erikseen. Ohjelmoijan lisäoletukset (tasakentällinen = yhtä monta kenttäpelaajaa, hyökkäysalueaika koko ottelusta kun ei erikoistilanteita) hyväksytty. Kirjattu: D-073.

### Q-052 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** kirjataanko jatkoajalle menneiden otteluiden luvut jatkoaikoineen?
**Konteksti:** `targets.json`:n haarukkapohja (D-071).
**Vastaus (Jerry, 2026-10-10):** jatkoaikaa ei kirjata. Haarukat koskevat varsinaista peliaikaa (3 × 20 min). Kirjattu: D-071.

### Q-051 · 2026-10-10 · kirjaaja: game-designer
**Kysymys:** onko Wisehockeyn xG-jakaumassa oma aloitusluokka? Ottelun 1 osuudet summautuvat 100 %:iin ilman sitä.
**Konteksti:** `stats-and-checks.md`, tavoitearvot; `targets.json` `evenStrengthXgShare*`.
**Vastaus (Jerry, 2026-10-10):** analyysissa on aloitusluokka, mutta `stats-and-checks.md`:n osuudet laskettiin vain kolmesta luokasta (suorahyökkäys, riisto, alueella pelaaminen). Simulaation osuuksia verrataan toistaiseksi ilman aloituksia. Jerry täyttää oikeat osuudet kaikkine luokkineen. Kirjattu: D-071.

### Q-047 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** missä järjestyksessä saman katkon Vaihto-tapahtumat kirjataan: kotijoukkueen kolmikko ja pari ennen vierasjoukkuetta, vai kaikki kolmikot ennen pareja?
**Konteksti:** O-2: "kolmikko ennen paria, kotijoukkue ennen vierasta" sopii molempiin.
**Ehdotettu oletus (toteutettu):** koti kolmikko, koti pari, vieras kolmikko, vieras pari. Erän alussa vaihdetut ja säilytetyt yksiköt aloittavat uuden vuoron (vuoromäärä raportissa kasvaa).
**Vastaus (Jerry, 2026-10-10):** hyväksytty ehdotetulla oletuksella. Kirjattu: D-068.

### Q-046 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** milloin kokonaisuuden sekuntivaikutukset (O-14) näkyvät saman kokonaisuuden tarkistuksissa?
**Konteksti:** O-14 sanoo, että vaikutukset lasketaan tilasta ennen kokonaisuutta ja kerrotaan kestolla d, mutta ei sitä, näkeekö kokonaisuuden oma tarkistus jo kuluneen ajan vaikutuksen. Esim. setup (6 s) + ensimmäinen syöttö: palautuuko puolustuksen järjestäytyneisyys setupin 6 sekunnin ajalta ennen syöttötarkistusta?
**Ehdotettu oletus (toteutettu):** nopeudet otetaan talteen kokonaisuuden alussa, ja ne kertyvät sitä mukaa kuin kokonaisuuden aika kuluu (`Spend`). Setupin sekunnit ehtivät siis vaikuttaa ensimmäiseen toimintoon, mutta toiminnon omat sekunnit kirjautuvat vasta tarkistuksen jälkeen. Kiekonmenetyksen pudotus tulee aina palautumisen jälkeen.
**Vastaus (Jerry, 2026-10-10):** hyväksytty ehdotetulla oletuksella. Kirjattu: D-068.

### Q-045 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** mihin rushin tukipelaaja menee, jos sen O-7-kohde osuu maalisolmuun?
**Konteksti:** O-7: hyökkääjät kohti (min(kiekollisen x, maali x − 1), kaista). Kun kiekollinen on omalla maaliviivalla (x = 1), sentterin kohde (1, 2) on oma maalisolmu.
**Ehdotettu oletus (toteutettu):** kohde siirretään kyseisen maalin edustalle, kuten järjestelmän `puckOffset`-kohteissa (D-033).
**Vastaus (Jerry, 2026-10-10):** hyväksytty ehdotetulla oletuksella. Kirjattu: D-068.

### Q-044 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** saako maalin selitystä varten tallentaa puolustuksen järjestäytyneisyyden laukaushetkellä lokin ulkopuolelle?
**Konteksti:** hyväksytyssä suunnitelmassa ottelutulosteen maalirivi kertoo esim. "puolustus 45 % järjestäytynyt", mutta tapahtumaskeemassa ei ole järjestäytyneisyyttä, eikä tapahtumatyyppejä saa muuttaa ilman speksiä.
**Ehdotettu oletus (toteutettu):** simulaatio tallentaa jokaisesta maalista erillisen raporttimerkinnän (`GameState.GoalNotes`: paikkatyyppi, järjestäytyneisyys, alueelle tulon N vs M, Royal Road, maski, maskin xG, toinen yritys). Tapahtumaloki pysyy skeeman mukaisena. Sama koskee jakson lopun energiaa, jääaikaa ja painekäyrää (`PeriodSnapshot`, `PressureSamples`).
**Vastaus (Jerry, 2026-10-10):** hyväksytty ehdotetulla oletuksella. Kirjattu: D-068.

### Q-031 · 2026-10-10 · kirjaaja: Jerry
**Kysymys:** miten maalin edessä oleva puolustaja vaikuttaa maskiin (M-4)?
**Konteksti:** virstanpylväässä 2 puolustaja ei poista maskia. Otetaan uudelleen esille virstanpylväässä 3.
**Vastaus (Jerry, 2026-10-10):** `screenContested` 0,15, kun puolustaja on samassa maalin edustan solmussa. Kirjattu: D-062.

### Q-020 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** lasketaanko väärän puolen miinus kerran puolta kohden vai jokaisesta väärällä puolella pelaavasta pelaajasta?
**Konteksti:** D-024, `tuning.json` `positions.offSideCheckModifier`.
**Ehdotettu oletus:** kerran puolta kohden.
**Ajoitus (Jerry, 2026-10-09):** siirretty virstanpylvääseen 3.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty (kerran puolta kohden). Kirjattu: D-067.

### Q-015 · 2026-10-09 · kirjaaja: game-designer (siirretty Q-012:sta)
**Kysymys veljelle:** mitkä alueet lasketaan slotiksi?
**Konteksti:** `data/rink.json` merkitsee slotiksi (`isSlot`) hyökkäyspään keskikaistan solmut. Q-012:n järjestelmäosa on ratkaistu (D-021).
**Ehdotettu oletus:** slotti on keskikaista aloituspisteiden välissä maalin edestä ympyröiden yläreunaan.
**Vastaus (Jerry, 2026-10-10):** slotti xG:lle = (7,2) ja (8,2). Kirjattu: D-062. Jatkokysymys: Q-041.

### Q-011 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** jaetaanko puolustusjärjestelmän roolit (F1 painostaja, F2, F3, D1, D2) etäisyyden mukaan kiekkoon vai kiinteästi pelipaikan mukaan?
**Konteksti:** `docs/data-schema.md`, Puolustusjärjestelmät. Oikeassa kiekossa ensimmäinen karvaaja on se, joka on lähimpänä, ei aina sentteri.
**Ehdotettu oletus:** etäisyyden mukaan joka tapahtuman jälkeen. Hyökkääjistä lähin on F1, tasatilanteessa järjestys C, LW, RW. Pakeista lähin on D1.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-067.

### Q-010 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** missä järjestyksessä laukauksen lopputulokset (blokattu, ohi, torjuttu, maali) ratkaistaan?
**Konteksti:** tapahtumaskeeman laukauksella on neljä lopputulosta, mutta tarkistustaulukossa on vain blokki ja laukaus. Ohi menneille laukauksille ei ole sääntöä.
**Ehdotettu oletus:** blokki (`checks.block`) → maalia kohti vai ohi (`checks.shot.onTargetShare`, alustavasti 0,55, ei statseja) → maalitarkistus (`checks.shot`). Myöhemmin Laukaisutarkkuus voi vaikuttaa ohilaukauksiin, mutta se on uusi mekaniikka ja vaatii hyväksynnän.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-067.

### Q-008 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** ovatko kaikki muokkaajat (järjestäytyneisyys, paine, energia, poikittaissyöttö jne.) logit-yksiköitä, jotka lisätään kaavan summaan M?
**Konteksti:** `docs/stats-and-checks.md` sanoo vain, että M on muokkaajien summa. Yksikkö ratkaisee, miten `tuning.json`:n arvot luetaan.
**Ehdotettu oletus:** kyllä, logit. Esimerkki: syötön p0 0,85 ja paineen muokkaaja −0,9 antavat n. 70 %, mikä vastaa analyysin 13–15 %-yksikön pudotusta. Tilaan sidotut muokkaajat skaalautuvat lineaarisesti (`docs/data-schema.md`, Yleiset käytännöt). Tämä on dokumentoitu oletukseksi `data-schema.md`:hen.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty (logit). Kirjattu: D-067.

### Q-007 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** onko irtokiekon perusjakauma tasaväkisille pelaajille 39 / 22 / 39 (voitto / ei voittajaa / häviö)?
**Konteksti:** analyysin jakauma 36 / 22 / 41 on yhden joukkueen luku, eikä se sovi perustasoksi, koska tasaväkisten jakauman pitää olla symmetrinen.
**Ehdotettu oletus:** `noWinnerShare` 0,22 analyysista, loput 0,78 jaetaan logistisella tarkistuksella, jonka p0 on 0,5. Statsiero siirtää osuutta voiton ja häviön välillä, "ei voittajaa" pysyy 22 %:ssa.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty (39 / 22 / 39). Kirjattu: D-067.

### Q-006 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** kuinka paljon aikaa kuluu, kun pelaajat asettuvat kuvion lähtösolmuihin (setup) tai kokoavat hyökkäyksen siirtymässä (regroup)?
**Konteksti:** kuvion lähtöasetelma ei yleensä vastaa pelaajien sijaintia edellisen tapahtuman jälkeen. Siirtymäohje "kokoaminen" antaa puolustuksen järjestäytyä (stats-and-checks.md), mutta aikaa ei ole määritelty.
**Ehdotettu oletus:** kiinteät ajat `time.setupSeconds` 6 ja `time.regroupSeconds` 8 (paikanpitäjiä). Aika palauttaa puolustuksen järjestäytyneisyyttä normaalisti tapahtumien tapaan.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-067.

### Q-038 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** pitääkö reboundin voittajan voida laukoa heti, ennen uuden kuvion valintaa?
**Konteksti:** D-047 antaa `crease`-xG:n reboundin voittajan laukaukselle, kun se on seuraava kiekkotoiminto. D-036:n mukaan kiekon saanut joukkue valitsee kuitenkin heti kuvion, ja setup (D-038) siirtää pelaajat kuvion lähtösolmuihin. Siksi rebound-laukaus `netFront`-solmusta syntyy virstanpylväässä 2 käytännössä vain, jos valitun kuvion ensimmäinen toiminto on saman pelaajan laukaus lähtösolmusta (8, 2).
**Ehdotettu oletus:** toteutettu sääntöjen mukaan (kuvion valinta ensin). Järjestelmätilan ohje `netFrontAfterShot` (virstanpylväs 3) voisi ratkaista tämän.
**Vastaus (Jerry, 2026-10-10):** kun hyökkääjä voittaa irtokiekon `netFront`-solmussa tai slotissa, se laukoo heti ilman kuvion setuppia (toinen yritys, maskin xG `netFront`issa). Virstanpylväs 3. Kirjattu: D-059.

### Q-037 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** kenelle kiekko menee aloituksen jälkeen?
**Konteksti:** D-042 ja tarkistustaulukko kertovat vain voittajan. Kuvio `offensiveFaceoffPointShot` olettaa, että sentteri pitää kiekon (C syöttää ensimmäisessä tahdissa).
**Ehdotettu oletus:** voittaneen joukkueen sentteri saa kiekon aloituspisteessä. Jos joukkueella on aloituskuvio pisteelle, se alkaa (D-036:n syötöllä, jos kuvion kiekollinen ei ole C); muuten kuvio valitaan alueen mukaan.
**Vastaus (Jerry, 2026-10-10):** aloitustarkistus ratkaisee, kumpi joukkue saa kiekon. Voittaneen joukkueen aloituskuvio määrää, kuka kiekon saa; oletuksena pakki. Ei erillistä syöttötarkistusta. Kirjattu: D-060.

### Q-036 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** mitä tapahtuu epäonnistuneessa kuljetuksessa (`skate`)?
**Konteksti:** D-040: "puolustaja riistää kiekon". Avoinna: (1) mikä puolustaja avauksessa (`breakout`), jossa tarkistuksen puolustajapuoli on `forecheckers`; (2) missä solmussa riisto tapahtuu ja missä kuljettaja on sen jälkeen.
**Ehdotettu oletus:** riistäjä on aina reitin puolustaja (M-1, viitesolmu = kohde). Hän saa kiekon omassa solmussaan (kuten syötönkatkossa), ja kuljettaja jää lähtösolmuunsa.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-061.

### Q-035 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** mitä järjestelmätilassa tapahtuu (virstanpylväs 2)?
**Konteksti:** (1) Kiekollinen joukkue, jolle ei sovi kuviota, "pitää kiekon järjestelmätilassa" (D-036), mutta kiekollinen ei liiku, joten tila voisi jatkua loputtomiin. (2) Kun irtokiekkokamppailussa ei ole voittajaa, "kumpikin joukkue liikkuu yhden järjestelmätilan askeleen" (M-6), mutta hyökkääjäpuolella ei ole liikesääntöä ennen virstanpylvään 3 ohjeita.
**Ehdotettu oletus:** (1) jokainen järjestelmätilan askel (`time.secondsPerAction.systemStep`) liikuttaa puolustusta järjestelmän mukaan, ja sen jälkeen kuvio valitaan uudelleen kiekollisen nykyisestä alueesta. Sama tapahtuu, kun kuvio päättyy ja kiekko on tallessa. (2) Kumpikin joukkue liikkuu oman puolustusjärjestelmänsä mukaan irtokiekkoa kohden.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-061.

### Q-034 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** mikä on aloituksen oletusasettelu (D-042), kun joukkueella ei ole aloituskuviota pisteelle?
**Konteksti:** D-042 mainitsee oletusasettelun, mutta sen solmuja ei ole määritelty.
**Ehdotettu oletus:** (oma näkökulma, piste (sx, sy)) C pisteessä, LW (sx, max(0, sy − 2)), RW (sx, min(width − 1, sy + 2)), LD (max(0, sx − 2), keskikaista − 1), RD (max(0, sx − 2), keskikaista + 1).
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-061.

### Q-033 · 2026-10-10 · kirjaaja: programmer
**Kysymys:** mikä kuviotyyppi "sopii kiekon alueeseen" (D-036)?
**Konteksti:** kuvion valinta. Tyypit ja alueet ovat eri listoja, eikä niiden vastaavuutta ole kirjattu.
**Ehdotettu oletus:** `breakout` ↔ oma alue, `zoneEntry` ↔ keskialue, `offensiveZone` ↔ hyökkäysalue (kiekollisen solmu oman joukkueen näkökulmasta). `faceoff`-kuvioita käytetään vain aloituksissa, `powerPlay`-kuvioita ei tasakentällisin.
**Vastaus (Jerry, 2026-10-10):** oletus hyväksytty. Kirjattu: D-061.

### Q-029 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** mitä puolustaja tekee, kun hänen suora askeleensa y-suunnassa (sign(dx) = 0) osuisi maalisolmuun?
**Konteksti:** `docs/data-schema.md`, Puolustusjärjestelmät → Liikkuminen. Sääntö korvaa maalisolmuun osuvan askeleen askeleella (sign(dx), 0), kun sign(dy) ≠ 0. Jos myös sign(dx) = 0 (esim. puolustaja (9, 1), kohde (9, 3), välissä maalisolmu (9, 2)), korvaava askel on (0, 0), eikä puolustaja pääse kohteeseensa koskaan. Tilanne on mahdollinen `puckOffset`-kohteilla maaliviivalla.
**Ehdotettu oletus:** toteutettu kirjaimellisesti: puolustaja jää paikalleen tässä tapahtumassa. Vaihtoehto: kierretään maalin edustan kautta (askel (−1, sign(dy)) hyökkäyspäässä, eli keskiviivaa kohti).
**Vastaus (Jerry, 2026-10-10):** puolustaja kiertää maalin sille puolelle, jolla kiekko on. Kirjattu: D-053.

### Q-028 · 2026-10-09 · kirjaaja: programmer
**Kysymys:** mitä Laukaus-tapahtuman kenttä "laukausnopeus" tarkoittaa simulaatiossa?
**Konteksti:** `docs/stats-and-checks.md`, Tapahtumaskeema. Simulaatio ei mallinna laukauksen nopeutta, eikä `tuning.json`:ssa ole sille arvoja. Kenttä on nyt `ShotEvent.Speed`, ja se on aina tyhjä (null).
**Ehdotettu oletus:** kenttä pysyy tyhjänä, kunnes päätetään, johdetaanko se esim. laukaisijan Laukaisuvoimasta tai poistetaanko se skeemasta.
**Vastaus (Jerry, 2026-10-10):** laukausnopeus on tyhjä virstanpylvääseen 3 asti. Myöhemmin se johdetaan laukaisuvoimasta pelkäksi näyttöarvoksi ilman pelivaikutusta. Kirjattu: D-052.

### Q-027 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** saako syöttölinja tai luistelureitti kulkea maalisolmun kautta (esim. maalin takaa (10, 1) slottiin (8, 3), linjan solmut (10, 1), (9, 2), (8, 3))?
**Konteksti:** `docs/data-schema.md`, Määritelmät M-1 (luonnos). E-002:n hyväksyntä jätti "maali esteenä" -säännön erillisen päätöksen varaan. Oikeassa kiekossa maalin takaa syötetään slottiin maalin ohi, ei läpi, mutta 22 jalan verkossa ero ei näy solmuina.
**Ehdotettu oletus:** sallitaan. Maalisolmu kuuluu linjaan, mutta siellä ei ole puolustajaa, joten se ei vaikuta riskiin. Laukauslinjasta maalisolmu jätetään pois (M-5).
**Vastaus (Jerry, 2026-10-10):** syöttölinja saa kulkea maalisolmun kautta, paitsi suoraan maalin läpi: validointi hylkää syötön, jossa syöttäjä ja vastaanottaja ovat molemmat keskikaistalla (y = 2) maalin eri puolilla. Vinot syötöt maalin takaa slottiin ovat sallittuja. Kirjattu: D-051.

### Q-026 · 2026-10-09 · kirjaaja: game-designer
**Kysymys:** lisätäänkö laukaustapahtumaan kenttä "paineen alla (kyllä/ei)"?
**Konteksti:** tavoitearvoissa on mittari "Laukaukset paineen alla (osuus)" (analyysi 68 % / 76 %), mutta tapahtumaskeeman Laukaus-tapahtumassa ei ole paine-kenttää (vain Syötössä). Raportti ei voi laskea mittaria ilman sitä. D-045 kieltää uudet tapahtumatyypit, mutta tämä on kenttä olemassa olevaan tapahtumaan, joten se vaatii `stats-and-checks.md`:n tapahtumaskeeman päivityksen.
**Ehdotettu oletus:** kyllä, kenttä lisätään. Määritelmä sama kuin syötöllä (`data-schema.md`, M-7). Virstanpylväässä 2 se vain kirjataan, eikä se muuta laukauksen todennäköisyyksiä.
**Vastaus (Jerry, 2026-10-10):** kyllä. Laukaustapahtumaan lisätään kenttä `underPressure`, ja `stats-and-checks.md` on päivitetty. Kirjattu: D-050.

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
