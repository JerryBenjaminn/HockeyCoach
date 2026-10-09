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
| D-014 | 2026-10-09 | Todennäköisyyden rajat | Tarkistuksen onnistumistodennäköisyys rajataan välille min–max. Arvot (0,02 / 0,98) ovat tasapainoarvoja `tuning.json`:ssa | lukittu |
| D-015 | 2026-10-09 | Riippuvuudet | Simissä ja AI:ssa ei ajonaikaisia riippuvuuksia. Käännösaikaiset analysaattorit (BannedApiAnalyzers) sallittuja | lukittu |
| D-016 | 2026-10-09 | Tavoitehaarukat | `data/targets.json` on tavoitehaarukoiden ainoa totuus. `stats-and-checks.md` viittaa siihen eikä toista lukuja | lukittu |
| D-017 | 2026-10-09 | Datasopimukset | Tarkistuksen painot summautuvat 1:een kummallakin puolella (hyökkääjä, puolustaja). Solmun id = x × leveys + y (9 × 5 -verkossa x * 5 + y). Data kirjoitetaan oman joukkueen näkökulmasta: oma maali x = 0, vastustajan näkökulma kierrolla 180° | lukittu |
| D-018 | 2026-10-09 | Toteutus | Klassinen `.sln`, nimiavaruus `Sim.Config`. Virstanpylväässä 1 pelaajat luodaan käsin, `roles.json` myöhemmin | lukittu |

## Muutosehdotukset

Kirjaa uusin ylimmäksi.

<!--
### E-001 · YYYY-MM-DD · ehdotettu
**Ehdotus:** mitä muutetaan.
**Perustelu:** raportin luvut (mittari, tulos, tavoite).
**Vaikutus:** mihin dokumentteihin, dataan ja koodiin muutos koskee.
**Päätös:** (Jerry täyttää) hyväksytty / hylätty + lyhyt syy.
-->

## Tasapainomuutokset

Designerin `tuning.json`-säädöt tavoitehaarukoiden sisällä. Ei vaadi erillistä hyväksyntää, mutta jokainen kirjataan.

| Päivä | Arvo | Ennen | Jälkeen | Syy | Vaikutus raporttiin |
| --- | --- | --- | --- | --- | --- |
