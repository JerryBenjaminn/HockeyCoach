# Kiekkovalmentaja – tekninen speksi

Oct 9, 2026 · @Jerry

## Tekniset päätökset

Simulaatio rakennetaan puhtaana C#-kirjastona ilman Unity-riippuvuuksia, jotta sen voi testata komentoriviltä ja liittää myöhemmin Unityyn sellaisenaan.

| Päätös | Valinta | Miksi |
| --- | --- | --- |
| Kieli | C# | Sama kieli kuin Unityssä |
| Simulaatiokirjasto | netstandard2.1, kieliversio C# 9 | Unity 6 tukee tätä tasoa, joten kirjasto siirtyy Unityyn ilman muutoksia |
| Testiympäristö ja testit | net8.0 | Nopea ajaa, ei Unity-rajoitteita |
| Testikehys | xUnit | Vakio .NET-maailmassa |
| Data | JSON-tiedostot | Pelaajat, kuviot, järjestelmät ja tasapainoarvot muokattavissa ilman koodia |
| Kieli koodissa | Englanti (nimet, kommentit) | Vakiokäytäntö, agentit toimivat parhaiten näin |
| Dokumentit | Suomi, repossa Markdownina kansiossa /docs | Nämä dokumentit viedään Markdowniksi repoon |
| Unity, grafiikka, käyttöliittymä | Ei prototyypissä | Prototyyppi on headless |

## Projektin rakenne

Yksi solution, neljä projektia. Simulaatio ja AI eivät tiedä mitään testiympäristöstä tai tiedostojärjestelmästä: ne saavat datan olioina.

```
HockeyCoach/
├── CLAUDE.md                     # Agenttien yhteiset säännöt
├── .claude/agents/
│   ├── game-designer.md
│   └── programmer.md
├── docs/
│   ├── vision.md
│   ├── stats-and-checks.md
│   ├── tech-spec.md
│   ├── decisions-log.md          # Lukitut päätökset ja muutosehdotukset
│   └── questions.md              # Agenttien avoimet kysymykset Jerrylle
├── data/
│   ├── tuning.json               # Kaikki tasapainoarvot
│   ├── rink.json                 # Kaukalon solmuverkko
│   ├── roles.json                # Roolien generointipohjat
│   ├── plays/*.json              # Pelikirjan kuviot
│   └── systems/*.json            # Puolustusjärjestelmät
├── src/
│   ├── HockeyCoach.Sim/          # netstandard2.1, ei riippuvuuksia
│   └── HockeyCoach.AI/           # netstandard2.1, viittaa Simiin
├── tools/
│   └── HockeyCoach.Harness/      # net8.0 konsolisovellus
└── tests/
    └── HockeyCoach.Sim.Tests/    # net8.0, xUnit
```

## Simulaation moduulit

Riippuvuudet kulkevat ylhäältä alas: ylempi moduuli saa käyttää alempia, ei toisin päin.

| Moduuli (namespace) | Vastuu |
| --- | --- |
| `Sim.Match` | Ottelun kulku: erät, ketjujen kierto, aikalisä, maalivahdin vetäminen, lopputulos |
| `Sim.Shift` | Yksi vaihto: aloitus, kuviotila, laukaus, järjestelmätila, katko. Tuottaa tapahtumia |
| `Sim.Checks` | Tarkistuskaava, painotetut statsit, muokkaajat. Puhtaita funktioita |
| `Sim.State` | Energia, paine, järjestäytyneisyys, tuttuus, kiekon ja pelaajien sijainnit |
| `Sim.Tactics` | Kuviot (tahdit, toiminnot), puolustusjärjestelmien säännöt, järjestelmätilan ohjeet |
| `Sim.Model` | Pelaaja, maalivahti, statsit, rooli, ketju, joukkue, kaukalon solmut |
| `Sim.Events` | Tapahtumatyypit statsidokumentin tapahtumaskeeman mukaan, tapahtumaloki |
| `Sim.Random` | Deterministinen satunnaislukugeneraattori |
| `AI` | Valmentaja-AI:t: kokoonpano, pelikirja ja järjestelmä vaihdon alussa, reagointi vastustajaan |

**Rajapinta valmentajalle.** Sekä ihmispelaaja (myöhemmin) että AI toteuttavat saman rajapinnan, esim. `ICoach`, joka palauttaa jokaiselle vaihdolle päätökset: ketju, kuviot prioriteettijärjestyksessä, puolustusjärjestelmä, siirtymäohje ja järjestelmätilan ohjeet. Asynkroninen PvP toimii myöhemmin samalla tavalla: tallennettu suunnitelma on vain yksi `ICoach`-toteutus.

## Data ja konfiguraatio

Koodissa ei ole maagisia numeroita: jokainen painokerroin, perustaso ja aikakustannus luetaan tiedostosta `tuning.json`. Näin designer-agentti ja Jerry voivat tasapainottaa koskematta koodiin.

**Kaukalon solmuverkko (`rink.json`).** Kaukalo on 9 × 5 solmun verkko (pituus × leveys). Jokaisella solmulla on koordinaatti, alue (oma pää, keskialue, hyökkäysalue), vyöhyke laukauksen perus-xG:tä varten ja tieto, onko se slotissa. Kuviot, puolustusjärjestelmät ja pelaajien sijainnit käyttävät samoja solmuja. Verkon koko on alustava ja voi tihentyä.

**Kuvio (`plays/*.json`).** Kuvion nimi, tyyppi (avaus, alueelle tulo, alueella pelaaminen, aloitus, ylivoima) ja enintään neljä tahtia. Jokainen tahti listaa pelaajien siirrot solmusta toiseen ja yhden kiekkotoiminnon (luistele, syötä, laukaise, aja maalille).

**Puolustusjärjestelmä (`systems/*.json`).** Säännöt, joilla jokaisen puolustajan kohdesolmu määräytyy kiekon sijainnista ja alueesta. Sääntöjen pitää olla deterministisiä, jotta editori voi myöhemmin näyttää puolustajien haamut.

**Pelaajat.** Prototyypissä joukkueet generoidaan `roles.json`-pohjista siemenluvulla. Tallennettuja pelaajatiedostoja ei tarvita.

## Determinismi ja satunnaisuus

Sama siemenluku ja samat päätökset tuottavat aina täsmälleen saman tapahtumalokin. Tämä on ehdoton vaatimus.

- **Oma satunnaislukugeneraattori** (esim. PCG32 tai xoshiro128), ei `System.Random`, koska sen tulokset voivat vaihdella .NET-versioiden ja Unityn välillä.
- **Kaikki satunnaisuus kulkee yhden generaattori-instanssin kautta**, joka annetaan ottelulle parametrina. Ei staattisia generaattoreita, ei `DateTime.Now`-siemeniä.
- **Ei liukulukujen järjestysriippuvuutta:** sanakirjojen läpikäynti tehdään aina järjestettynä, jotta tulokset eivät riipu hajautusjärjestyksestä.

**Miksi:** testit voidaan kirjoittaa tarkkoina, bugit voidaan toistaa siemenluvulla, ja asynkronisessa PvP:ssä riittää myöhemmin tallentaa siemen ja suunnitelmat. Toisto lasketaan uudelleen eikä sitä tarvitse tallentaa.

## Testiympäristö ja raportit

Testiympäristö on konsolisovellus, joka pelaa otteluita AI:den välillä ja vertaa tuloksia statsidokumentin tavoitearvoihin.

**Komennot**

- `match --home rulebased --away random --seed 42`: yksi ottelu, tulostaa tapahtumalokin luettavana tekstinä ja JSONina.
- `batch --matches 5000 --matchups all`: kaikki AI-parit, tulostaa raportin.
- `compare --tuning a.json --tuning b.json`: kaksi tasapainoversiota rinnakkain.

**Raportin sisältö (Markdown + CSV)**

| Osio | Mitä näyttää |
| --- | --- |
| Taito vs. tuuri | Voittoprosenttimatriisi AI-tyyppien välillä |
| Ottelun mittarit | Maalit, laukaukset, xG, hyökkäysalueaika per ottelu (keskiarvo ja hajonta) |
| Paikat | Laukaukset ja xG paikkatyypeittäin, paikkaluokat (huippu, hyvä, kohtalainen) |
| Tarkistukset | Syöttöjen onnistuminen (kaikki ja paineen alla), kamppailujen jakauma, aloitukset, alueelle tulot |
| Taktiikka | Kuvio × puolustusjärjestelmä -matriisi: onnistuminen ja xG |
| Tavoitevertailu | Jokainen mittari vs. tavoitehaarukka: OK, liian korkea tai liian matala |

Raportin muoto noudattaa otteluanalyysin jakoa, jotta simulaation ja oikean ottelun lukuja voi verrata suoraan.

## Testaus ja laatukriteerit

Jokainen ominaisuus valmistuu testeineen, ja kaikki testit menevät läpi ennen commitia.

- **Yksikkötestit:** tarkistuskaava (esim. kun H = D, onnistuminen on p0), muokkaajien suunta, energian kulutus, järjestäytyneisyyden palautuminen.
- **Determinismitesti:** sama siemen ajetaan kahdesti, tapahtumalokien pitää olla identtiset.
- **Invarianttitestit:** jokaisessa tapahtumassa on tasan yksi kiekon haltija tai irtokiekko, kentällä on oikea määrä pelaajia, aika ei kulje taaksepäin, maalit täsmäävät tapahtumalokin kanssa.
- **Savutestit:** 1 000 ottelua ilman poikkeuksia ja ilman jumittuneita vaihtoja.
- **Tasapainotestit ovat raportteja, eivät yksikkötestejä:** niiden epäonnistuminen tarkoittaa tasapainotyötä, ei rikkinäistä koodia.

**Koodin säännöt:** pienet luokat yhdellä vastuulla, puhtaat funktiot `Sim.Checks`-moduulissa, ei globaalia tilaa, julkiset rajapinnat dokumentoitu XML-kommentein.

## Agenttien työnjako

Jerry on pääsuunnittelija. Dokumentit kansiossa /docs ovat ainoa totuus, ja lukittuja päätöksiä muuttaa vain Jerry.

|  | Game designer -agentti | Programmer-agentti |
| --- | --- | --- |
| Tehtävä | Ylläpitää dokumentteja, kirjoittaa kuviot ja järjestelmät JSONina, ajaa raportteja ja analysoi tuloksia | Toteuttaa speksin mukaan, kirjoittaa testit, ylläpitää koodin laatua |
| Saa muuttaa | `data/`-kansion, `docs/questions.md`, muutosehdotukset `decisions-log.md`:hen | `src/`, `tools/`, `tests/`, teknisiä muistiinpanoja |
| Ei saa muuttaa | Koodia, lukittuja päätöksiä | Dokumenttien suunnittelusisältöä, tasapainoarvoja (paitsi Jerryn tai designerin pyynnöstä) |
| Kun speksi on epäselvä | Kirjaa kysymyksen `questions.md`:hen | Kirjaa kysymyksen `questions.md`:hen eikä keksi vastausta itse |

**Muutosehdotusten kulku:** designer kirjaa ehdotuksen `decisions-log.md`:hen tilalla "ehdotettu" ja perustelee sen raportin luvuilla. Jerry hyväksyy tai hylkää. Vasta hyväksytty muutos päivitetään dokumentteihin ja koodiin.

**CLAUDE.md sisältää:** projektin tavoitteen yhdellä kappaleella, viittaukset dokumentteihin, komennot (build, test, harness), determinismi- ja testisäännöt sekä yllä olevan työnjaon.

## Virstanpylväät syyslomalle

Jokainen virstanpylväs on valmis, kun sen testit menevät läpi ja Jerry on katsonut tuloksen.

1. **Perusta (päivä 3):** solution, datamalli, satunnaislukugeneraattori, tarkistuskaava, `tuning.json` ja `rink.json`. Testit kaavalle ja determinismille.
2. **Vaihto (päivä 3–4):** aloitus, kuviotila, laukaus, järjestelmätila ja katko. Tapahtumaloki skeeman mukaan. Yksi vaihto tulostuu luettavana.
3. **Ottelu (päivä 4–5):** erät, ketjujen kierto, energia, paine, järjestäytyneisyys, siirtymät ja tuttuus. Kokonainen ottelu tulostuu luettavana.
4. **AI ja testiympäristö (päivä 5):** satunnainen, sääntöpohjainen ja sopeutuva AI. Batch-ajo ja raportti.
5. **Kalibrointi (päivä 6–7):** raporttien perusteella tasapainotus tavoitearvoja kohti. Ensimmäinen vastaus kysymykseen: ratkaiseeko taito vai tuuri.

Päivät 1–2 käytetään dokumentteihin. Erikoistilanteet (ylivoima ja alivoima) ovat bonus, jos aikaa jää.

## Avoimet kysymykset

- [ ] Kaukalon solmuverkon tiheys: riittääkö 9 × 5 kuvioille ja puolustusjärjestelmille?
- [ ] Kuvion ja puolustusjärjestelmän JSON-skeema tarkasti: kirjoitetaan ennen virstanpylvästä 2
- [ ] Sopeutuvan AI:n logiikka: miten se lukee vastustajan taipumuksia ottelun aikana?
- [ ] Tarvitaanko yksinkertainen visualisointi (esim. HTML-toisto tapahtumalokista) jo prototyyppiin?
- [ ] Repo julkinen vai yksityinen? (Otteluanalyysien luvut vain keskiarvoina joka tapauksessa)
