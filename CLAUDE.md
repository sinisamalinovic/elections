# Posmatraci App — Project Guide for Claude

## Šta je ovo
Aplikacija za praćenje izbora. Posmatrači (election observers) koriste **PosmatraciApp** (Blazor WASM PWA) da popunjavaju kontrolnu listu tokom izbornog dana na biračkom mestu (BM). Popunjene podatke šalju direktno na **PosmatraciReport** (Blazor Server) koji prikazuje izveštaj po BM sa pregledom kritičnih situacija.

---

## Projekti u Solution-u (`Posmatraci.sln`)

| Projekat | Tip | Status | Port |
|---|---|---|---|
| `PosmatraciApp` | Blazor WASM PWA | Radi | 5049 |
| `PosmatraciReport` | Blazor Server + API | U razvoju | TBD |
| `PosmatraciApp.Shared` | Class Library | Planiran (prazan) | — |

---

## PosmatraciApp (Observer app)

**Stack:** Blazor WASM .NET 10, MudBlazor 9.2.0, QRCoder 1.7.0, bez backend-a, PWA.

### Ključni fajlovi
- `wwwroot/data/emails.json` — lista emailova posmatrača (bez lozinki)
- `wwwroot/data/locations.json` — hijerarhija Opstina → BirackaJedinica → BirackoMesto (sa `shortId`)
- `wwwroot/data/checklist.json` — 92 stavke (ID 0–91), 8 sekcija, srpski ćirilica. **ID-evi se nikad ne menjaju** (ugrađeni u QR)
- `Services/QrEncoderService.cs` — enkodovanje BmState u QR
- `Models/ChecklistModels.cs` — enum `CheckSeverity` (NE `Severity` — konflikt sa MudBlazor)
- `Models/BmState.cs` — stanje popunjenosti za jedno BM
- `Models/LocationModels.cs` — modeli lokacija

### Severity nivoi (CheckSeverity enum)
```
None = 0  — nije postavljeno
N    = 1  — normalno
S    = 2  — srednje
V    = 3  — visoko
K    = 4  — kritično
```

### QR binarni format (QrEncoderService)
```
HEADER (12 bajta):
  [0]    = 0x50 ('P') — magic byte
  [1]    = 0x01       — verzija
  [2-3]  = BmShortId  — uint16 big-endian
  [4-7]  = Unix timestamp — uint32 big-endian
  [8-11] = 4 bajta email hash

ANSWERS (21 bajta):
  2 bita po stavci × 83 stavke (0=Unanswered, 1=Da, 2=Ne, 3=NA)

SEVERITIES (21 bajta):
  2 bita po stavci (None/N=0, S=1, V=2, K=3)

NOTES (varijabilno):
  [0]    = broj napomena (byte)
  za svaku: [id (byte)][dužina (byte)][UTF-8 tekst]

Celo se Deflate komprimuje pa Base64Url enkoduje.
```

### Napomena o MudBlazor
- Koristiti `CheckSeverity` (ne `Severity`) u modelima — konflikt sa `MudBlazor.Severity`
- MudBlazor alert/snackbar severity: `MudBlazor.Severity.X`

---

## PosmatraciReport (Server / Admin app) — U RAZVOJU

**Stack:** Blazor Server .NET 10, MudBlazor (planiran), bez baze podataka (in-memory za početak).

### Šta treba da radi
1. **API endpoint** — prima payload od PosmatraciApp direktno (HTTP POST)
2. **Izveštaj po BM** — lista svih biračkih mesta sa statusom
3. **Pregled kritičnih situacija** — vizualni prikaz gde ima `CheckSeverity.K` ili `V` odgovora
4. **Real-time update** — Blazor Server može da osvežava UI čim stignu novi podaci

### API kontrakt (planirano)
```
POST /api/report
Content-Type: application/json

{
  "payload": "<base64url string iz QrEncoderService>",
  "email": "posmatrac@example.com"
}
```
Server dekoduje payload koristeći `QrDecoderService` (treba kreirati, mirror od QrEncoderService).

### Izveštaj — šta prikazati
- Lista BM (iz locations.json) sa kolonama:
  - Naziv BM i adresa
  - Broj popunjenih stavki / ukupno
  - Broj `Ne` odgovora
  - Najviši severity nivo (K/V/S/N)
  - Vreme poslednjeg ažuriranja
  - Email posmatrača
- Filter po opstini / biračkoj jedinici
- Highlight kritičnih BM (K severity = crveno, V = narandžasto)

---

## Zajednički modeli (PosmatraciApp.Shared — planiran)

Modeli koji treba da se prebace u Shared:
- `BmState`
- `ChecklistModels` (AnswerState, CheckSeverity, ChecklistItemDefinition...)
- `LocationModels`
- `QrDecoderService` (novi — mirror od QrEncoderService)

---

## Lokacije podataka
- `data/locations.json` (root) — kopija za razvoj
- `PosmatraciApp/wwwroot/data/locations.json` — koristi PosmatraciApp
- Kada se doda Bajina Bašta ili druga opstina — ažurirati OBA fajla

---

## Pokretanje
```bash
# PosmatraciApp (observer)
cd PosmatraciApp
dotnet run
# → http://localhost:5049

# PosmatraciReport (admin, kad se kreira)
cd PosmatraciReport
dotnet run
# → TBD port
```
