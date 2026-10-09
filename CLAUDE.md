# HockeyCoach – projektin säännöt

## Tavoite

Mobiilipeli, jossa pelaaja on jääkiekkovalmentaja: hän kokoaa ketjut, valitsee kuviot ja puolustusjärjestelmän, ja pelaajat pelaavat vaihdon automaattisesti. Tämän repon nykyinen tavoite on **headless-simulaatio ja testiympäristö**, joka vastaa kysymykseen: ratkaiseeko taito vai tuuri. Ei grafiikkaa, ei käyttöliittymää, ei Unityä.

## Dokumentit ovat ainoa totuus

Lue relevantit dokumentit ennen jokaista tehtävää. Jos koodi ja dokumentti ovat ristiriidassa, dokumentti voittaa.

| Dokumentti | Sisältö |
| --- | --- |
| `docs/vision.md` | Visio, pilarit, lukitut päätökset, prototyypin onnistumisen kriteerit |
| `docs/stats-and-checks.md` | Statsit, tarkistuskaava, tapahtumaskeema, järjestäytyneisyys, paine, tavoitearvot |
| `docs/data-schema.md` | Datatiedostojen skeemat: `rink.json`, `tuning.json`, `targets.json`, kuviot ja järjestelmät (designer omistaa) |
| `docs/tech-spec.md` | Arkkitehtuuri, projektirakenne, determinismi, testiympäristö, virstanpylväät |
| `docs/decisions-log.md` | Lukitut päätökset ja muutosehdotukset |
| `docs/questions.md` | Avoimet kysymykset Jerrylle |

Jerry on pääsuunnittelija. Lukittuja päätöksiä muuttaa vain Jerry.

## Kieli

- Koodi, nimet, kommentit, commit-viestit: **englanti**.
- Dokumentit kansiossa `docs/`, päätösloki ja kysymykset: **suomi**.

## Komennot

```bash
dotnet build
dotnet test
dotnet run --project tools/HockeyCoach.Harness -- validate [--data <dir>]
dotnet run --project tools/HockeyCoach.Harness -- match --home rulebased --away random --seed 42
dotnet run --project tools/HockeyCoach.Harness -- batch --matches 5000 --matchups all
dotnet run --project tools/HockeyCoach.Harness -- compare --tuning data/tuning.json --tuning data/tuning-b.json
```

## Rakenne

| Projekti | Kohde | Huom. |
| --- | --- | --- |
| `src/HockeyCoach.Sim` | netstandard2.1, C# 9 | Ei ajonaikaisia riippuvuuksia. Ei tiedostojärjestelmää, ei konsolia |
| `src/HockeyCoach.AI` | netstandard2.1, C# 9 | Viittaa vain Simiin |
| `tools/HockeyCoach.Harness` | net10.0 | Lataa datan, ajaa otteluita, kirjoittaa raportit |
| `tests/HockeyCoach.Sim.Tests` | net10.0, xUnit | |

Sim- ja AI-projektien pitää kääntyä Unity 6:ssa sellaisenaan. Siksi niissä **ei saa käyttää** C# 10+ ominaisuuksia: ei file-scoped namespaceja, ei global usingeja, ei `required`-jäseniä, ei record structeja, ei raw string literaaleja. Tämä on pakotettu `LangVersion`-asetuksella.

"Ei riippuvuuksia" tarkoittaa ajonaikaisia riippuvuuksia. Käännösaikaiset analysaattorit (esim. `BannedApiAnalyzers`, `PrivateAssets=all`) ovat sallittuja.

## Ehdottomat säännöt

1. **Determinismi.** Sama siemen ja samat päätökset tuottavat aina saman tapahtumalokin.
   - Kaikki satunnaisuus kulkee yhden, parametrina annetun generaattorin kautta (`Sim.Random`).
   - Ei `System.Random`, ei `Guid.NewGuid()`, ei `DateTime.Now`, ei staattisia generaattoreita.
   - Sanakirjat ja joukot käydään läpi järjestettynä.
2. **Ei maagisia numeroita.** Jokainen painokerroin, perustaso, aikakustannus ja raja luetaan `data/tuning.json`-tiedostosta. Tavoitehaarukat ovat vain tiedostossa `data/targets.json`.
3. **Tapahtumaloki noudattaa tapahtumaskeemaa** (`docs/stats-and-checks.md`). Uusi tapahtumatyyppi vaatii dokumentin päivityksen ensin.
4. **Testit ennen valmista.** Ominaisuus on valmis vasta, kun sillä on testit ja `dotnet test` menee läpi.
5. **Otteluanalyysien raakadataa ei tallenneta repoon.** Vain keskiarvot ja haarukat dokumentteihin.

## Työnjako

| | `game-designer` | `programmer` |
| --- | --- | --- |
| Tehtävä | Data, kuviot, järjestelmät, raporttien analyysi, muutosehdotukset | Koodi ja testit speksin mukaan |
| Saa muuttaa | `data/`, `docs/data-schema.md`, uudet kysymykset `docs/questions.md`:hen, ehdotukset `docs/decisions-log.md`:hen | `src/`, `tools/`, `tests/`, uudet kysymykset `docs/questions.md`:hen |
| Ei saa muuttaa | Koodia, lukittuja päätöksiä | Suunnitteludokumentteja, tasapainoarvoja omin päin |

Delegoi tehtävät näin:
- Koodin toteutus, bugit, testit, refaktorointi → `programmer`.
- Kuvioiden ja järjestelmien JSON, tasapainotus, raporttien tulkinta, dokumenttien ylläpito → `game-designer`.
- Kun tehtävä vaatii molempia (esim. uusi mekaniikka), designer tarkentaa speksin ensin ja programmer toteuttaa sen jälkeen.

## Kun jokin on epäselvää

Älä keksi vastausta. Kirjaa kysymys `docs/questions.md`:hen, jatka sellaisilla osilla, joihin kysymys ei vaikuta, ja mainitse kysymys loppuraportissa. Molemmat agentit saavat lisätä kysymyksiä, mutta vain Jerry merkitsee ne ratkaistuiksi.

## Muutosehdotukset

1. Kirjaa ehdotus `docs/decisions-log.md`:hen tilalla **ehdotettu**, perusteluna raportin luvut.
2. Jerry hyväksyy tai hylkää.
3. Vasta hyväksytty muutos päivitetään dokumentteihin, dataan ja koodiin.

## Commitit

Pienet commitit, yksi asia kerrallaan, Conventional Commits -muoto (`feat:`, `fix:`, `test:`, `docs:`, `data:`, `refactor:`). Älä committaa, jos testit eivät mene läpi.

## Ei tässä vaiheessa

Unity, grafiikka, käyttöliittymä, kuvioeditori, verkko ja PvP-palvelin, draft- ja kausijärjestelmä. Erikoistilanteet (ylivoima, alivoima) vasta, kun virstanpylväät 1–5 ovat valmiit.
