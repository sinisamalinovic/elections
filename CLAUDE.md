# Posmatraci App — Project Guide for Claude

## Šta je ovo
Aplikacija za praćenje izbora. Posmatrači (election observers) koriste **PosmatraciApp** (Blazor WASM PWA) da popunjavaju kontrolnu listu tokom izbornog dana na biračkom mestu (BM). Popunjene podatke šalju direktno na **BirackaMestaReport** (Blazor Server) koji prikazuje izveštaje po BM.

---

## Projekti u Solution-u (`Posmatraci.sln`)

| Projekat | Tip | Status | Port |
|---|---|---|---|
| `PosmatraciApp` | Blazor WASM PWA | Radi | 5049 |
| `BirackaMestaReport` | Blazor Server + API | Radi | 64246 |
| `PosmatraciApp.Shared` | Class Library | Radi | — |

---

## PosmatraciApp.Shared (zajednički modeli)

**Putanja:** `PosmatraciApp.Shared/Models/`
**Namespace:** `PosmatraciApp.Shared.Models`

Modeli koji se dele između PosmatraciApp i BirackaMestaReport:
- `BmState.cs` — stanje popunjenosti čekliste za jedno BM
- `ChecklistModels.cs` — `AnswerState`, `CheckSeverity`, `ChecklistItemDefinition`, `ChecklistSectionDefinition`, `ChecklistDefinition`
- `IzlaznostModels.cs` — `IzlaznostEntry`, `BmIzlaznost`
- `LocationModels.cs` — `LocationData`, `Opstina`, `BirackaJedinica`, `BirackoMesto`
- `ObserverSession.cs` — sesija posmatrača
- `SubmitRequest.cs` — DTO za API: `{ string Email, BmState? BmState, BmIzlaznost? BmIzlaznost }`

---

## PosmatraciApp (Observer app)

**Stack:** Blazor WASM .NET 10, MudBlazor 9.2.0, QRCoder 1.7.0, bez backend-a, PWA.

### Ključni fajlovi
- `wwwroot/data/emails.json` — lista emailova posmatrača (bez lozinki)
- `wwwroot/data/locations.json` — hijerarhija Opstina → BirackaJedinica → BirackoMesto (sa `ShortId`)
- `wwwroot/data/checklist.json` — 92 stavke (ID 0–91), 8 sekcija, srpski ćirilica. **ID-evi se nikad ne menjaju** (ugrađeni u QR)
- `wwwroot/data/config.json` — `{ "serverUrl": "https://localhost:64246/" }` — URL servera; prazno = API pozivi se preskaču
- `wwwroot/app.js` — JS funkcije: `shareQrCode`, `downloadJson`, `downloadQrCode`
- `Services/QrEncoderService.cs` — enkodovanje BmState u QR
- `Services/ChecklistService.cs` — upravljanje stanjem čekliste, čuva u localStorage
- `Services/IzlaznostService.cs` — upravljanje izlaznošću, čuva u localStorage (ključ: `posmatraci_izlaznost_{bmId}`)
- `Services/AuthService.cs` — autentifikacija posmatrača, sesija
- `Services/LocationService.cs` — učitavanje i pretraga lokacija

### Stranice
- `/login` — prijava emailom
- `/location-select` — izbor BM (prikazuje `ShortId. Naziv — Adresa`)
- `/checklist/{BmId}` — popunjavanje čekliste; AppBar prikazuje `ShortId. Naziv`; dugme Send u headeru
- `/izlaznost/{BmId}` — unos izlaznosti birača; dugme "Пошаљи излазност" šalje i na server
- `/izlaznost-istorija/{BmId}` — hronologija unosa izlaznosti (MudTimeline)

### Slanje podataka na server
- **Čeklista:** klik na dugme Send (ikona) u AppBar → `POST /api/submit` sa `{ Email, BmState, BmIzlaznost }`
- **Izlaznost:** klik na "Пошаљи излазност" → `POST /api/submit` sa `{ Email, BmIzlaznost }` (bez BmState)
- Ako server nije dostupan ili `serverUrl` je prazan → tiho, prikazuje Snackbar

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

## BirackaMestaReport (Server / Admin app)

**Stack:** Blazor Server .NET 10, MudBlazor 9.2.0, EF Core + SQLite.
**Putanja baze:** `posmatraci.db` (SQLite, kreira se automatski pri pokretanju)

### Autentifikacija
- Cookie auth (`posmatraci_admin_auth`, 12h)
- Lozinka u `appsettings.json` → `AdminPassword: "posmatraci2026"`
- Login stranica: `/login` (Razor Page, ne Blazor)
- Sve Blazor stranice zaštićene middleware-om; `/api/*` je javno

### API endpointi
```
POST /api/submit
  Body: { "email": "...", "bmState": {...}, "bmIzlaznost": {...} }
  - bmState i bmIzlaznost su opcioni, ali bar jedan mora biti prisutan
  - Upisuje novi red u BmSubmission tabelu

POST /api/upload-json
  multipart/form-data, fajl = JSON export iz PosmatraciApp
  - Isti format kao /api/submit body
```

### Baza podataka (EF Core SQLite)
```csharp
BmSubmission {
  Id, BmId, BmShortId, BmDisplayName, OpstineName, BjName,
  Email, ReceivedAt, BmStateJson, BmIzlaznostJson
}
```
- Jedan red po submisiji (može više redova za isti BmId)
- Reportovi uvek uzimaju **najnoviji red per BmId**

### Stranice
- `/` ili `/izlaznost` — **Izlaznost report**: tabela svih BM sortirana po ShortId; kolone: BM, Naziv, Oblast, Укупно, Гласало, %, Уноса, Време, Посматрач; color coding: >50% zeleno, >20% narandžasto, ostalo crveno; auto-refresh 60s
- `/checklista` — **Čeklista report**: tabela sortirana po težini (K→V→S); filter po opstini; toggle "Само инциденти"; dugme ▼/▲ po redu otvara punu čeklistu sa svim stavkama, odgovorima i napomenama; auto-refresh 60s
- `/upload` — ručni upload JSON fajla (backup kada API nije dostupan)

### Ključni servis
- `ReportService.cs` — `GetLatestPerBmAsync()` (latest per BmId koristeći dva upita zbog EF/SQLite ograničenja), `SaveSubmissionAsync(email, BmState?, BmIzlaznost?)`

### App.razor — obavezni MudBlazor provajderi
```razor
<MudThemeProvider />
<MudPopoverProvider />   ← obavezno za dropdown-ove
<MudSnackbarProvider />
<MudDialogProvider />
```

---

## Lokacije podataka
- `data/locations.json` (root) — kopija za razvoj
- `PosmatraciApp/wwwroot/data/locations.json` — koristi PosmatraciApp
- Kada se doda nova opstina — ažurirati **oba** fajla
- Trenutne opstine: **Бајина Башта** (BB01, 53 biračka mesta BB01-01-001 do BB01-01-053)

---

## Pokretanje
```bash
# PosmatraciApp (observer)
cd PosmatraciApp
dotnet run
# → http://localhost:5049

# BirackaMestaReport (admin)
cd BirackaMestaReport
dotnet run
# → https://localhost:64246
```

## Poznati problemi / napomene
- **EF Core GroupBy + First()** ne može se prevesti u SQL za SQLite → u `ReportService` koristiti dva upita (Max pa Where)
- **MudPopoverProvider** mora biti u `App.razor` ili će pasti pri otvaranju MudSelect
- `config.json` u PosmatraciApp treba ažurirati pre deployment-a sa pravim IP serverа
