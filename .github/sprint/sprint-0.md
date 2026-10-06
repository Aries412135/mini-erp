# Sprint 0: Panduan Lengkap Mini ERP

Stack: .NET 9 (Clean Architecture) + React (Vite, TypeScript) + SQL Server + JWT + REST.
Metode: Agile (sprint 1-2 minggu), Git + GitHub, CI dengan GitHub Actions.

Tujuan Sprint 0: **bikin "pipa" ujung ke ujung yang sudah jalan, walau belum ada fitur.**

```
React (browser) --> API .NET --> SQL Server
       ^
       +-- dijaga oleh: Git + PR + CI + Ruleset + Backlog
```

| Fase | Isi | Hasil |
|---|---|---|
| 0 | Cek tools | Versi sesuai |
| 1 | Repo + branch | Tempat kode hidup |
| 2 | Backend 4 layer | Solution .NET jalan |
| 3 | EF Core + SQL Server | DB terkoneksi |
| 4 | Frontend React | React jalan |
| 5 | CORS + tes end-to-end | "Backend status: healthy" |
| 6 | CI + PR pertama | Build otomatis |
| 7 | Ruleset | Aturan dikunci sistem |
| 8 | Backlog Agile | Papan kerja |
| 9 | Dokumentasi | Catatan keputusan |

> **Urutan penting:** Ruleset (Fase 7) sengaja **setelah** CI jalan. GitHub cuma nampilin status check yang **sudah pernah jalan**, jadi `backend` dan `frontend` baru bisa dipilih setelah PR pertama menjalankan CI.

Semua perintah pakai **PowerShell**. Ganti `Aries412135` kalau username GitHub beda.

---

## Fase 0: Cek Tools

```powershell
dotnet --version     # 9.0.310
node --version       # LTS (20.x atau 22.x)
npm --version
git --version
```

Tools tambahan: VS Code, SSMS (atau Azure Data Studio), Postman (atau Bruno).

---

## Fase 1: Repo, Branch, Default Branch

**1.1 Bikin repo di GitHub.** Nama `mini-erp`, centang **Add a README** (supaya repo langsung punya branch `main` dan bisa di-clone).

**1.2 Clone dan bikin struktur folder:**

```powershell
cd F:\Wardhana\Project\dotnet
git clone https://github.com/Aries412135/mini-erp.git
cd mini-erp

New-Item -ItemType Directory -Force backend, frontend, docs/adr, docs/requirements, .github/workflows
```

> Di PowerShell, `mkdir -p` ala Linux nggak bisa diandalin. Pakai `New-Item -ItemType Directory -Force`. Git **nggak nyimpen folder kosong**, jadi folder baru kelihatan di Git setelah ada file di dalamnya.

**1.3 Bikin branch `develop`:**

```powershell
git checkout -b develop
git push -u origin develop
```

**1.4 Jadikan `develop` default branch.** GitHub repo > **Settings > General > Default branch** > ikon panah dua arah > pilih `develop` > **Update** > konfirmasi.

> Kenapa wajib? (1) GitHub cuma baca **issue template** dari default branch. (2) `Closes #12` di PR **cuma otomatis nutup issue kalau PR di-merge ke default branch**. Karena PR kita selalu ke `develop`, default-nya harus `develop`.

---

## Fase 2: Backend, Solution Clean Architecture

**2.1 Bikin branch kerja.** Mulai sekarang **nggak ada commit langsung ke `develop`**:

```powershell
git checkout -b chore/initial-setup
cd backend
```

**2.2 Kunci versi SDK dan bikin `.gitignore` .NET:**

```powershell
dotnet new globaljson --sdk-version 9.0.310 --roll-forward latestFeature
dotnet new gitignore
```

> `.gitignore` harus ada **sebelum** `git add` pertama. Kalau `bin/`, `obj/`, atau `node_modules/` terlanjur ke-commit, menambahkannya ke `.gitignore` nggak cukup: Git tetap nge-track file yang sudah masuk, dan harus `git rm -r --cached <folder>` dulu.

**2.3 Bikin solution dan project:**

```powershell
dotnet new sln -n MiniErp

dotnet new classlib -n MiniErp.Domain         -o src/MiniErp.Domain
dotnet new classlib -n MiniErp.Application    -o src/MiniErp.Application
dotnet new classlib -n MiniErp.Infrastructure -o src/MiniErp.Infrastructure
dotnet new webapi   -n MiniErp.Api            -o src/MiniErp.Api --use-controllers

dotnet new xunit -n MiniErp.Domain.UnitTests      -o tests/MiniErp.Domain.UnitTests
dotnet new xunit -n MiniErp.Application.UnitTests -o tests/MiniErp.Application.UnitTests
dotnet new xunit -n MiniErp.Api.IntegrationTests  -o tests/MiniErp.Api.IntegrationTests
```

`--use-controllers` bikin template pakai Controller (bukan Minimal API), karena kita mau REST gaya enterprise.

**2.4 Daftarin ke solution:**

```powershell
dotnet sln add src/MiniErp.Domain src/MiniErp.Application src/MiniErp.Infrastructure src/MiniErp.Api
dotnet sln add tests/MiniErp.Domain.UnitTests tests/MiniErp.Application.UnitTests tests/MiniErp.Api.IntegrationTests
dotnet sln list
```

`dotnet sln list` harus nampilin **7 project**.

> Versi lama `dotnet sln add (Get-ChildItem ...)` gagal karena di PowerShell semuanya itu **objek**. `Get-ChildItem` ngirim objek file, yang kalau diubah jadi teks cuma jadi nama file tanpa folder. Path eksplisit nggak ambigu.

**2.5 Atur dependency antar layer:**

```powershell
dotnet add src/MiniErp.Application    reference src/MiniErp.Domain
dotnet add src/MiniErp.Infrastructure reference src/MiniErp.Application
dotnet add src/MiniErp.Api            reference src/MiniErp.Application
dotnet add src/MiniErp.Api            reference src/MiniErp.Infrastructure

dotnet add tests/MiniErp.Domain.UnitTests      reference src/MiniErp.Domain
dotnet add tests/MiniErp.Application.UnitTests reference src/MiniErp.Application
dotnet add tests/MiniErp.Api.IntegrationTests  reference src/MiniErp.Api
```

**Aturan emas: dependency selalu mengarah ke dalam.**

| Layer | Isi | Padanan Flutter |
|---|---|---|
| Domain | Entity + aturan bisnis murni | `models/` |
| Application | Use case, interface, DTO | `usecases/` + abstract repository |
| Infrastructure | EF Core, JWT, email (implementasi nyata) | `data/` layer |
| Api | Controller, middleware, konfigurasi | `presentation/` versi HTTP |

Domain **nggak boleh** tahu soal EF Core atau HTTP. Kalau client minta ganti ke PostgreSQL, yang berubah cuma Infrastructure.

**2.6 Install package:**

```powershell
# Infrastructure
dotnet add src/MiniErp.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version "9.0.*"
dotnet add src/MiniErp.Infrastructure package Microsoft.EntityFrameworkCore.Design    --version "9.0.*"

# Api
dotnet add src/MiniErp.Api package Microsoft.EntityFrameworkCore.Design --version "9.0.*"
dotnet add src/MiniErp.Api package Serilog.AspNetCore --version "9.*"

# Application
dotnet add src/MiniErp.Application package MediatR --version "12.*"
dotnet add src/MiniErp.Application package FluentValidation --version "11.*"
dotnet add src/MiniErp.Application package FluentValidation.DependencyInjectionExtensions --version "11.*"
```

> Tanpa `--version`, NuGet ambil **versi stabil terbaru** (EF Core 10) yang nggak cocok dengan `net9.0` (error `NU1202`). Aturannya: **package Microsoft harus sebaris dengan versi .NET-nya** (.NET 9 = EF Core 9 = ASP.NET Core 9). MediatR dipatok ke 12 karena versi 13 ke atas pakai model lisensi komersial. Setelah sukses, ganti wildcard (`9.0.*`) di `.csproj` jadi versi pasti supaya build reproducible.

**2.7 Bersihin template:** hapus `Class1.cs` di Domain, Application, Infrastructure; hapus `WeatherForecast.cs` dan `Controllers/WeatherForecastController.cs` di Api. Biarin `UnitTest1.cs` di project test (itu yang bikin `dotnet test` nggak error).

**2.8 Bikin health check.** `src/MiniErp.Api/Controllers/HealthController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace MiniErp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", time = DateTime.UtcNow });
    }
}
```

| Kode | Artinya |
|---|---|
| `[ApiController]` | Aktifin perilaku API otomatis (validasi model, binding) |
| `[Route("api/[controller]")]` | `[controller]` jadi `health`, URL `/api/health` |
| `ControllerBase` | Base class API (tanpa dukungan View/HTML) |
| `IActionResult` | Return fleksibel: `Ok()`=200, `NotFound()`=404 |
| `DateTime.UtcNow` | Simpan waktu selalu UTC, konversi ke WIB di frontend |

**2.9 Tes:**

```powershell
dotnet build
dotnet run --project src/MiniErp.Api
```

Lihat baris `Now listening on: http://localhost:xxxx` (di project ini `5205`), buka `/api/health`. Stop dengan `Ctrl+C`.

---

## Fase 3: EF Core + SQL Server

**3.1 Connection string.** `src/MiniErp.Api/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MiniErpDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Pakai instance bernama? Ganti jadi `Server=localhost\\SQLEXPRESS;...`.

**Aturan keamanan (repo public):**

- Versi di atas aman karena pakai **Windows Authentication**, jadi tidak ada password di file.
- **Jangan pernah** commit password, apalagi `sa`. Kalau password pernah ke-commit, menghapusnya dari file nggak cukup karena tetap ada di riwayat Git. Satu-satunya perbaikan pasti: **ganti passwordnya**.
- **Jangan pakai database sistem lain** buat latihan, karena migration EF Core bisa ngubah skemanya. Pakai database baru `MiniErpDb`.

**Kalau terpaksa pakai SQL login, pakai user-secrets:**

```powershell
cd src/MiniErp.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=MiniErpDb;User Id=...;Password=...;TrustServerCertificate=True;"
dotnet user-secrets list
cd ../..
```

Lalu hapus key `ConnectionStrings` dari `appsettings.Development.json`. Kode nggak perlu diubah: di mode Development, .NET otomatis baca secrets dan menimpa `appsettings`. Secrets disimpan di `%APPDATA%\Microsoft\UserSecrets\` (di luar folder repo), tapi **tidak terenkripsi**, jadi cuma untuk development.

> Saat deploy, user-secrets **tidak ikut**. Isi lewat **environment variable** di hosting: `ConnectionStrings__DefaultConnection=...` (tanda `:` jadi `__`). Frontend (Vite) bisa di-deploy ke Vercel, backend .NET perlu hosting terpisah, dan SQL Server lokal tidak bisa diakses dari internet (butuh DB cloud). Dibahas di sprint akhir.

**3.2 DbContext.** `src/MiniErp.Infrastructure/Persistence/ApplicationDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace MiniErp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

**3.3 Dependency Injection per layer.** `src/MiniErp.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniErp.Infrastructure.Persistence;

namespace MiniErp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
```

`src/MiniErp.Application/DependencyInjection.cs`:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MiniErp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

| .NET | Flutter (GetX) |
|---|---|
| `services.AddScoped<IFoo, Foo>()` | `Get.lazyPut<IFoo>(() => Foo())` |
| Minta lewat constructor, framework yang nyediain | `Get.find<IFoo>()` manual |

Tiga lifetime (sering ditanya di interview): **Transient** (baru tiap diminta), **Scoped** (satu per HTTP request, default `DbContext`), **Singleton** (satu seumur aplikasi).

**3.4 `Program.cs` versi bersih:**

```csharp
using MiniErp.Application;
using MiniErp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

> `builder.Services...` = fase **mendaftarkan** service. `app.Use...` = fase **menyusun pipeline middleware**, dan **urutan menentukan perilaku**. `UseCors` harus **sebelum** `UseAuthorization` dan `MapControllers`. `UseHttpsRedirection()` sengaja belum dipasang: redirect http ke https di development bisa bikin request CORS preflight gagal kalau sertifikat https belum dikonfigurasi.

**3.5 Migration pertama:**

```powershell
dotnet tool install --global dotnet-ef --version "9.*"
# kalau sudah terlanjur ke-install versi lain:
# dotnet tool update --global dotnet-ef --version "9.*"

dotnet ef migrations add InitialCreate -p src/MiniErp.Infrastructure -s src/MiniErp.Api -o Persistence/Migrations
dotnet ef database update -p src/MiniErp.Infrastructure -s src/MiniErp.Api
```

`-p` = project tempat migration disimpan, `-s` = startup project (yang punya `Program.cs` dan connection string).

Buka SSMS: database `MiniErpDb` harus ada (isinya baru tabel `__EFMigrationsHistory`).

> Migration itu **git-nya database**. Tiap perubahan skema = satu file migration yang bisa di-apply dan di-rollback. Di tim beneran, nggak ada yang ngubah tabel manual lewat SSMS di environment bersama.

Cek akhir fase:

```powershell
dotnet build
dotnet test
```

---

## Fase 4: Frontend React + Vite

```powershell
cd ..\frontend
npm create vite@latest . -- --template react-ts
npm install
npm install axios react-router-dom @tanstack/react-query
npm run dev
```

Buka `http://localhost:5173`. TypeScript dipilih karena terbiasa dengan Dart yang strongly-typed.

**Peta konsep Flutter ke React:**

| Flutter | React | Catatan |
|---|---|---|
| `Widget` | Component (function) | Function yang return tampilan (JSX) |
| `setState` | `useState` | State lokal |
| `initState` / `dispose` | `useEffect` | Side effect |
| `build()` | `return (...)` | |
| GetX Controller | Zustand / Context | State global |
| `pubspec.yaml` | `package.json` | |
| `main.dart` | `main.tsx` | Entry point |

**Bersihin template dengan aman.** Kosongin isinya, **jangan hapus file-nya**:

```powershell
cd src
Remove-Item -Recurse -Force assets
Set-Content App.css ""
Set-Content index.css ""
```

> `import "./index.css"` di `main.tsx` itu statement biasa. Kalau file-nya hilang, Vite error `Failed to resolve import`, persis kayak import Dart dengan path salah. **Hapus file = hapus juga baris import-nya.**

`src/App.tsx` sementara:

```tsx
function App() {
  return <h1>Mini ERP</h1>;
}

export default App;
```

Struktur folder `src/`:

```powershell
New-Item -ItemType Directory -Force api, components, features, hooks, layouts, pages, routes, types, utils
```

**Bedah `main.tsx`:**

| Kode | Fungsi | Padanan Flutter |
|---|---|---|
| `document.getElementById("root")!` | Ambil `<div id="root">` dari `index.html`. `!` = "percaya gue, ini nggak null" | View tempat Flutter di-attach |
| `createRoot(...)` | Bikin akar aplikasi React | `runApp()` |
| `.render(<App />)` | Gambar `App` ke akar | Widget root |
| `<StrictMode>` | Mode dev: render **dua kali** buat nangkep bug | Mirip assert di debug |

Karena `StrictMode`, `useEffect` jalan 2x di mode dev, jadi `/api/health` kelihatan dipanggil dua kali. Itu **normal**.

---

## Fase 5: Hubungin React ke API

**5.1 Axios client.** `src/api/axiosClient.ts`:

```ts
import axios from 'axios';

const axiosClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
});

export default axiosClient;
```

`frontend/.env.development`:

```
VITE_API_URL=http://localhost:5205
```

Port **harus sama persis** dengan yang muncul di terminal `dotnet run`.

> Env di Vite **wajib diawali `VITE_`**, dan cuma dibaca **saat startup**, jadi habis ngubah `.env`, restart `npm run dev`. Apapun di frontend bisa dilihat publik, jadi jangan taruh secret di sini.

**5.2 `src/App.tsx`:**

```tsx
import { useEffect, useState } from 'react';
import axiosClient from './api/axiosClient';

interface HealthResponse {
  status: string;
  time: string;
}

function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    axiosClient
      .get<HealthResponse>('/api/health')
      .then((res) => setHealth(res.data))
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <p>Error: {error}</p>;
  if (!health) return <p>Loading...</p>;

  return (
    <div>
      <h1>Mini ERP</h1>
      <p>Backend status: {health.status}</p>
    </div>
  );
}

export default App;
```

| Kode | Penjelasan | Padanan Flutter |
|---|---|---|
| `useState<HealthResponse \| null>(null)` | State awal `null`, return `[nilai, fungsiUbah]` | `Rx<T?>` / `setState` |
| `useEffect(() => {...}, [])` | Jalan setelah render; `[]` = cuma sekali saat component muncul | `initState` |
| `setHealth(...)` | Ubah state, React render ulang | `setState` |
| `if (!health) return ...` | Conditional rendering pakai `if` biasa | `if` di `build()` |
| `{health.status}` | Sisipin ekspresi ke JSX | `$health.status` |

Konsep inti: **UI adalah fungsi dari state.** Lo nggak ngubah tampilan langsung, lo ubah state dan React yang ngegambar ulang.

**5.3 Tes end-to-end.** Jalanin backend dan frontend di **dua terminal terpisah**, buka `http://localhost:5173`. Harus muncul **Backend status: healthy**.

**Kalau muncul error CORS di Console (F12):**

> CORS dicek oleh **browser**, bukan server. `ERR_FAILED 200 (OK)` artinya server sudah jawab sukses, tapi browser membuang jawabannya karena header `Access-Control-Allow-Origin` nggak ada. Postman jalan, React nggak.

| Penyebab | Cara cek dan perbaiki |
|---|---|
| Backend belum di-restart setelah ngubah `Program.cs` | `Ctrl+C`, `dotnet run` ulang |
| Proses lama nyangkut di port 5205 | `netstat -ano \| findstr :5205`, ambil PID paling kanan, `taskkill /PID <pid> /F` |
| `UseCors` hilang atau di bawah `MapControllers` | Samain dengan `Program.cs` di Fase 3.4 |
| Origin beda | Harus persis `http://localhost:5173`, tanpa `/` di akhir; `localhost` beda dengan `127.0.0.1` |

Verifikasi tanpa browser:

```powershell
curl.exe -i -H "Origin: http://localhost:5173" http://localhost:5205/api/health
```

Harus ada baris `Access-Control-Allow-Origin: http://localhost:5173`.

---

## Fase 6: CI dan PR Pertama

> **CI** = robot di server GitHub yang ngejawab: "Kode ini masih bisa di-build dan lolos test?" Penting karena "di laptop gue jalan" bukan bukti. CI build dari repo **bersih**, jadi ketahuan kalau ada file lokal yang nggak ke-commit atau versi SDK yang beda.

**6.1 Bikin `.github/workflows/ci.yml`** (di root repo, branch `chore/initial-setup`):

```yaml
name: CI

on:
  pull_request:
    branches: [develop, main]
  push:
    branches: [develop]

jobs:
  backend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: backend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: backend/global.json
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run lint
      - run: npm run build
```

| Bagian | Artinya |
|---|---|
| `on:` | Pemicu: tiap PR ke `develop`/`main`, tiap push ke `develop` |
| `jobs:` | Dua pekerjaan paralel; nama job (`backend`, `frontend`) dipakai ruleset nanti |
| `working-directory` | Folder tempat perintah jalan (karena monorepo) |
| `npm ci` | Install persis sesuai `package-lock.json` (lebih ketat dari `npm install`) |

> CI mengeksekusi **persis** yang tertulis. Pernah kena `npm run buildgit` (kata `git` nyangkut) dan hasilnya `Missing script`. Kebiasaan debug CI: **baca baris `Run ...` paling atas di log**.

**6.2 Jalanin dulu di lokal:**

```powershell
cd F:\Wardhana\Project\dotnet\mini-erp\backend
dotnet restore; dotnet build -c Release; dotnet test -c Release

cd ..\frontend
npm ci; npm run lint; npm run build

cd ..
```

**6.3 Cek isi commit, lalu push:**

```powershell
git status
```

Nggak boleh ada `node_modules/`, `bin/`, `obj/`, `.vs/`, atau password. Kalau bersih:

```powershell
git add .
git commit -m "chore: initial project setup (backend, frontend, health check, CI)"
git push -u origin chore/initial-setup
```

**6.4 Buka PR di GitHub:**

1. Klik banner **Compare & pull request**, atau tab **Pull requests > New pull request**
2. Cek dropdown: **base = `develop`**, **compare = `chore/initial-setup`**. GitHub kadang milih `main` secara default, dan itu salah
3. Isi title dan deskripsi:

```markdown
## Ringkasan
Setup awal: backend Clean Architecture (.NET 9), frontend React + Vite,
koneksi EF Core ke SQL Server, health check, dan CI.

## Cara tes
1. dotnet run --project src/MiniErp.Api
2. npm run dev di folder frontend
3. Buka http://localhost:5173, harus muncul "Backend status: healthy"
```

4. **Create pull request**, buka tab **Files changed**, review sendiri dengan serius
5. Tunggu job `backend` dan `frontend` jalan

**Kalau CI merah:**

| Error | Penyebab | Solusi |
|---|---|---|
| `Missing script: "..."` | Typo nama script di `ci.yml` | Samain dengan `npm run` di lokal |
| Error TypeScript saat build | Di dev cuma warning, di build jadi error | Jalanin `npm run build` di lokal |
| `npm ci` gagal | `package-lock.json` nggak ke-commit | Commit file-nya |
| SDK 9.0.310 nggak ketemu | Versi belum tersedia di runner | Ganti `global-json-file` jadi `dotnet-version: 9.0.x` |

Perbaiki di branch yang sama, commit, push, dan PR otomatis ke-update.

**6.5 Merge.** Kalau CI hijau: **Squash and merge > Confirm merge > Delete branch**. Lalu sinkronin lokal:

```powershell
git checkout develop
git pull
git branch -d chore/initial-setup
```

| Strategi merge | Hasil |
|---|---|
| Merge commit | Semua commit + satu commit merge |
| **Squash and merge** | Satu commit bersih per PR (rekomendasi) |
| Rebase and merge | Riwayat lurus, lebih rawan salah paham |

> **Editor "Merge branch 'develop' of ... into develop"** bukan error, itu editor pesan merge commit. Muncul kalau `develop` lokal dan GitHub bercabang, biasanya karena ada commit langsung di `develop` lokal. Simpan dan tutup editornya (Vim: `Esc`, `:wq`, `Enter`; VS Code: `Ctrl+S` lalu tutup tab). Pencegahan: jangan pernah commit langsung di `develop` atau `main`, dan opsional `git config --global pull.rebase true`.

---

## Fase 7: Ruleset (Branch Protection)

Pastikan PR pertama sudah ke-merge, supaya `backend` dan `frontend` muncul di pilihan.

> GitHub punya dua sistem: "Branch protection rules" klasik (Settings > Branches) dan **Rulesets** baru (Settings > **Rules > Rulesets**). Kita pakai Rulesets: bikin **satu aturan lalu nentuin target-nya**, jadi satu ruleset bisa nutup `develop` dan `main` sekaligus.

**7.1 Buka form.** Repo > **Settings > Rules > Rulesets > New ruleset > New branch ruleset**.

**7.2 Bagian atas:**

| Field | Isi |
|---|---|
| Ruleset Name | `Protect develop and main` |
| Enforcement status | **Active** |
| Bypass list | Biarin kosong |

> Default enforcement sering **Disabled**. Kalau lupa ganti ke **Active**, aturannya tersimpan tapi tidak berlaku sama sekali. Bypass kosong artinya owner pun tidak bisa nembus aturan.

**7.3 Target branches:**

1. **Add target > Include by pattern** > ketik `develop` > **Add Inclusion pattern**
2. **Add target > Include by pattern** > ketik `main` > **Add Inclusion pattern**

Di daftar target harus ada dua baris: `develop` dan `main`.

**7.4 Branch rules:**

| Aturan | Centang? | Alasan |
|---|---|---|
| Restrict deletions | Ya (biasanya default) | Cegah branch kehapus |
| Block force pushes | Ya (biasanya default) | Cegah riwayat ditimpa paksa |
| Require a pull request before merging | Ya | Wajib lewat PR |
| Require status checks to pass | Ya | Wajib CI hijau |
| Require signed commits | Tidak | Bikin ribet di awal |

Di **Require a pull request before merging**:

| Sub-opsi | Isi |
|---|---|
| Required approvals | **0** |
| Allowed merge methods | Sisakan **Squash** (opsional) |

> Karena solo, **Required approvals harus 0**. GitHub tidak mengizinkan author menyetujui PR-nya sendiri, jadi kalau diisi 1, merge tidak bisa dilakukan dan project macet total.

Di **Require status checks to pass**: **Add checks** > cari `backend` > pilih; ulangi untuk `frontend`. Opsi *Require branches to be up to date* boleh dikosongin dulu.

Kalau `backend`/`frontend` tidak muncul: GitHub cuma nampilin check yang **sudah pernah jalan**, dan namanya adalah **nama job** (bukan nama workflow `CI`). Pastikan CI pernah jalan, atau ketik manual lalu Enter.

**7.5 Simpan:** scroll ke bawah > **Create**. Ruleset muncul di daftar dengan status **Active**.

**7.6 Buktikan aturannya bekerja:**

```powershell
git checkout develop
git pull
git commit --allow-empty -m "test: direct push"
git push
```

Hasil yang **benar** adalah push **ditolak**:

```
remote: error: GH013: Repository rule violations found for refs/heads/develop.
remote: - Changes must be made through a pull request.
remote: - 2 of 2 required status checks are expected.
 ! [remote rejected] develop -> develop (push declined due to repository rule violations)
```

| Baris | Artinya |
|---|---|
| `GH013` | Kode error GitHub untuk "kena aturan ruleset" |
| `Changes must be made through a pull request` | Aturan Require PR bekerja |
| `2 of 2 required status checks are expected` | Aturan status checks bekerja; "2" itu `backend` dan `frontend` |
| `remote rejected` | Penolakan terjadi di sisi server |

Bersihin commit tes (cuma ada di lokal):

```powershell
git reset --hard origin/develop
```

Tes sisi satunya: di PR yang CI-nya masih jalan atau merah, tombol Merge harus terkunci; setelah hijau, **Squash and merge** harus aktif.

Mau ngubah: Settings > Rules > Rulesets > klik nama ruleset > edit > **Save changes**. Mau mematikan sementara: ubah enforcement ke **Disabled**.

---

## Fase 8: Backlog Agile

**Agile** = kerjain dalam potongan kecil, tunjukin hasil tiap 1-2 minggu, evaluasi, lanjut.

| Level | Ukuran | Analogi buku | Contoh |
|---|---|---|---|
| Epic | Beberapa sprint | Bab | Authentication & Authorization |
| User Story | Beberapa hari | Sub-bab | Login pakai email & password |
| Task | Hitungan jam | Poin catatan | Bikin endpoint `POST /api/auth/login` |

- **Format story:** "Sebagai [siapa], gue mau [apa], supaya [manfaat]".
- **Acceptance Criteria (AC):** checklist ya/tidak yang bikin story bisa dites. Kalau tidak bisa nulis AC, requirement belum jelas.
- **Story point** (1, 2, 3, 5, 8, 13): ukuran kerumitan **relatif**, bukan jam. Story 13 terlalu besar, pecah dulu.

**Urutan kerja:**

| # | Kerjaan | Alasan urutan |
|---|---|---|
| 1 | Label | Template dan issue butuh label yang sudah ada |
| 2 | Issue template (lewat PR) | Biar semua story seragam |
| 3 | Milestone | Issue tinggal dipasangin |
| 4 | Project board | Wadah visual semua issue |
| 5 | Epic | Bab besar dulu |
| 6 | Story Sprint 1 | Isi dari epic pertama |

### Step 1: Label

Repo > tab **Issues** > tombol **Labels** > **New label** > isi nama dan warna > **Create label**.

| Label | Fungsi |
|---|---|
| `epic` | Bab besar |
| `user-story` | Story |
| `task` | Pekerjaan teknis kecil |
| `backend` | Sisi .NET |
| `frontend` | Sisi React |

Label `documentation` dan `bug` sudah bawaan GitHub.

### Step 2: Issue template (lewat branch + PR)

```powershell
git checkout develop
git pull
git checkout -b docs/issue-template
New-Item -ItemType Directory -Force .github/ISSUE_TEMPLATE
code .github/ISSUE_TEMPLATE/user-story.md
```

Isi file (tanpa pembungkus kode, baris pertama **harus** `---`):

```text
---
name: User Story
about: Template untuk user story
title: "[US-XXX] "
labels: user-story
---

## User Story
Sebagai [role],
Saya ingin [aksi],
Supaya [tujuan].

## Acceptance Criteria
- [ ] 
- [ ] 

## Catatan Teknis
(opsional)
```

Story point tidak ditulis di sini, tapi diisi lewat field di board.

```powershell
git status
git add .github/ISSUE_TEMPLATE/user-story.md
git commit -m "docs: add user story issue template"
git push -u origin docs/issue-template
```

Di GitHub: **Compare & pull request** > pastikan **base = develop** > **Create pull request** > tunggu `backend` dan `frontend` hijau > **Squash and merge** > **Delete branch**. Lalu:

```powershell
git checkout develop
git pull
git branch -d docs/issue-template
```

Cek: Issues > **New issue** > harus muncul pilihan **User Story**. Kalau tidak muncul, cek default branch = `develop` (Fase 1.4).

### Step 3: Milestone (ini yang jadi "Sprint")

Tab **Issues** > tombol **Milestones** > **New milestone**.

| Field | Isi |
|---|---|
| Title | `Sprint 1 - Authentication` |
| Due date | 2 minggu dari hari mulai |
| Description | `Di akhir sprint, user bisa login, sesinya diperpanjang otomatis, dan akses dibatasi sesuai role.` |

Deskripsinya adalah **Sprint Goal**: satu kalimat yang bisa diverifikasi di akhir sprint.

### Step 4: Project board

Tab **Projects** di repo > **New project** (kalau tab tidak ada: profil GitHub > tab **Projects** > **New project**). Pilih template **Board**, nama `Mini ERP Board`, **Create project**.

**4a. Atur kolom (field Status).** Kolom di board adalah **opsi dari field Status**, jadi yang diedit adalah field-nya:

1. Di project, klik ikon **...** di pojok kanan atas > **Settings**
2. Menu kiri > **Custom fields** > klik **Status**
3. Di daftar **Options**:

| Opsi bawaan | Lakukan |
|---|---|
| `Todo` | Ganti nama jadi `Backlog` |
| `In Progress` | Biarin |
| `Done` | Biarin |
| (baru) | **Add option** > `Sprint Ready` |
| (baru) | **Add option** > `In Review` |

4. Urutkan dengan drag ikon titik-titik di kiri tiap opsi:

`Backlog` > `Sprint Ready` > `In Progress` > `In Review` > `Done`

5. **Save options**, lalu balik ke board

Alternatif langsung di board: **...** di header kolom > **Edit details** untuk ganti nama; tombol **+** di ujung kanan untuk kolom baru; drag header untuk urutan.

| Kolom | Artinya |
|---|---|
| Backlog | Semua yang belum dijadwalkan |
| Sprint Ready | Sudah dipilih untuk sprint ini |
| In Progress | Lagi dikerjain (maksimal 1-2 sekaligus) |
| In Review | PR terbuka, nunggu CI |
| Done | Memenuhi Definition of Done |

**4b. Field Story Points.** Pindah ke tampilan **Table** > klik **+** di ujung kanan header kolom > **New field** > Name `Story Points`, Type **Number** > **Save**.

**4c. Hubungkan ke repo** (kalau project dibuat dari profil): **...** > **Settings** > *Linked repositories* > **Link a repository** > pilih `mini-erp`.

**4d. Otomatisasi:** **...** > **Workflows**. Pastikan *Item closed* dan *Pull request merged* mengubah Status ke **Done**. Opsional: **Auto-add to project**.

### Step 5: Epic

Epic = **Issue biasa** dengan label `epic`.

1. Tab **Issues** > **New issue** > **Open a blank issue**
2. Title: `[EPIC-01] Authentication & Authorization`
3. Deskripsi:

```markdown
## Tujuan
Sistem punya login yang aman dan akses dibatasi berdasarkan role.

## Story
(diisi setelah story dibuat)
```

4. Sidebar kanan: **Labels** > `epic`, **Projects** > `Mini ERP Board`
5. **Submit new issue**

Ulangi untuk epic lain (cukup judul dan satu kalimat tujuan):

| Epic | Isi |
|---|---|
| EPIC-02 | Master Data |
| EPIC-03 | Procurement |
| EPIC-04 | Inventory |
| EPIC-05 | Sales |
| EPIC-06 | Finance (AP/AR) |
| EPIC-07 | Reporting & Dashboard |

Jangan didetailin: detail ditulis **just in time**, karena rencana yang terlalu jauh hampir pasti berubah.

### Step 6: Story Sprint 1

1. **New issue** > pilih template **User Story** > **Get started**
2. Isi judul dan isinya. Contoh US-101:

```markdown
## User Story
Sebagai user,
Saya ingin login dengan email dan password,
Supaya saya bisa mengakses sistem sesuai role saya.

## Acceptance Criteria
- [ ] Login sukses mengembalikan access token dan refresh token
- [ ] Password salah mengembalikan 401 dengan pesan generik
- [ ] Akun nonaktif tidak bisa login
- [ ] Password disimpan sebagai hash, bukan teks asli
```

3. Sidebar kanan: **Labels** (`user-story` otomatis, tambah `backend`), **Milestone** > `Sprint 1 - Authentication`, **Projects** > `Mini ERP Board`
4. **Submit new issue**
5. Di board, isi **Story Points** di kartunya, pastikan Status = `Backlog`

| ID | Story | Point | AC utama |
|---|---|---|---|
| US-101 | Login email & password | 5 | Token keluar, 401 kalau salah, akun nonaktif ditolak |
| US-102 | Sesi diperpanjang via refresh token | 8 | Refresh token valid menghasilkan access token baru, token lama dirotasi, token kedaluwarsa ditolak |
| US-103 | Logout dan token dicabut | 3 | Refresh token ditandai revoked, tidak bisa dipakai lagi |
| US-104 | Admin assign role | 5 | Hanya Admin yang boleh, user bisa punya banyak role |
| US-105 | Akses sesuai role | 5 | 401 tanpa token, 403 kalau role tidak cukup |
| US-106 | Halaman login web | 5 | Form tervalidasi, error ditampilkan, sukses diarahkan ke dashboard |
| US-107 | Halaman terlindungi + auto-refresh | 8 | Tanpa login diarahkan ke login, token kedaluwarsa diperpanjang otomatis |

Total 39 point, kemungkinan **kebanyakan** untuk sprint pertama sambil belajar dua teknologi baru. Sisanya digeser di retrospective. Bikin 2-3 story dulu, sisanya menyusul.

**Hubungkan story ke epic.** Edit issue EPIC-01, ganti bagian Story dengan daftar nomor issue:

```markdown
## Story
- [ ] #8
- [ ] #9
- [ ] #10
```

(Ganti angka dengan nomor issue sebenarnya.) Kalau ada opsi **Add sub-issue**, itu cara resmi yang lebih rapi.

### Cara pakai board sehari-hari

```
Backlog -> pilih untuk sprint -> Sprint Ready
 -> mulai: geser ke In Progress -> git checkout develop -> git pull
 -> git checkout -b feature/US-101-login -> coding + commit -> git push -u origin <branch>
 -> PR ke develop dengan "Closes #8" -> geser ke In Review
 -> CI hijau -> Squash and merge -> issue tertutup otomatis, kartu ke Done -> git pull
```

Konvensi branch: `feature/`, `fix/`, `chore/`, `docs/`.
Konvensi commit (Conventional Commits): `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `ci:`.

> `Closes #8` di deskripsi PR menyambungkan **kode** dengan **requirement**. Tapi cuma otomatis nutup issue kalau PR di-merge ke **default branch**, itu sebabnya `develop` dijadiin default di Fase 1.4.

**Task** (level terkecil) tidak perlu jadi issue sendiri. Tulis sebagai checklist di dalam issue story saat mulai dikerjain:

```markdown
- [ ] Bikin entity User
- [ ] Bikin endpoint POST /api/auth/login
- [ ] Unit test
```

### Ritual sprint versi solo

| Acara | Kapan | Isi |
|---|---|---|
| Sprint Planning | Hari pertama | Pilih story, geser ke Sprint Ready, cek Sprint Goal |
| Daily | Tiap hari | 3 baris di `docs/learning-journal.md`: kemarin, hari ini, blocker |
| Sprint Review | Hari terakhir | Demo, rekam video pendek |
| Retrospective | Setelahnya | Apa yang bagus, buruk, mau diubah |

---

## Fase 9: Dokumentasi

Semua dokumen masuk lewat branch `docs/...` + PR, sama seperti Fase 8 Step 2.

| Dokumen | Menjawab | File |
|---|---|---|
| README | Project apa, cara jalaninnya? | `README.md` |
| ERD | Struktur datanya kayak apa? | `docs/erd-auth.png` |
| ADR | Kenapa dulu milih X? | `docs/adr/000N-*.md` |
| Definition of Done | Kapan story dianggap selesai? | `docs/definition-of-done.md` |

**README** minimal: deskripsi, tech stack, cara run backend dan frontend (termasuk cara set connection string, **tanpa nilai asli**), struktur folder. Ujiannya: orang lain bisa jalanin project tanpa nanya.

**ERD Auth** (tempel di dbdiagram.io, export PNG ke `docs/`):

```text
Table Users {
  Id uniqueidentifier [pk]
  Email nvarchar(256) [unique, not null]
  PasswordHash nvarchar(500) [not null]
  FullName nvarchar(200) [not null]
  IsActive bit [not null, default: true]
  CreatedAt datetime2 [not null]
}

Table Roles {
  Id int [pk, increment]
  Name nvarchar(50) [unique, not null]
}

Table UserRoles {
  UserId uniqueidentifier [ref: > Users.Id]
  RoleId int [ref: > Roles.Id]
}

Table RefreshTokens {
  Id uniqueidentifier [pk]
  UserId uniqueidentifier [ref: > Users.Id]
  TokenHash nvarchar(500) [not null]
  ExpiresAt datetime2 [not null]
  RevokedAt datetime2
  CreatedAt datetime2 [not null]
}
```

`Users` dan `Roles` banyak-ke-banyak, makanya butuh tabel penghubung `UserRoles`. Satu user punya banyak `RefreshTokens` (HP dan laptop = 2 token).

**ADR** (10-15 baris, struktur Status, Konteks, Keputusan, Konsekuensi). Bikin tiga: `0001-clean-architecture`, `0002-monorepo`, `0003-typescript`. Contoh:

```markdown
# ADR-0001: Menggunakan Clean Architecture

## Status
Accepted

## Konteks
ERP akan tumbuh kompleks dan butuh pemisahan tanggung jawab yang jelas.

## Keputusan
4 layer: Domain, Application, Infrastructure, Api.

## Konsekuensi
+ Mudah dites, mudah ganti infrastruktur
- Lebih banyak file dan boilerplate di awal
```

**Definition of Done** (`docs/definition-of-done.md`):

```markdown
# Definition of Done
- [ ] Semua Acceptance Criteria terpenuhi
- [ ] Kode ter-merge lewat Pull Request
- [ ] CI hijau
- [ ] Ada unit test untuk logic penting
- [ ] Endpoint bisa dites lewat Postman
- [ ] Tidak ada secret/password di kode
- [ ] Dokumentasi diperbarui jika perlu
```

AC itu spesifik per story, sedangkan DoD berlaku untuk **semua** story.

---

## Checklist Akhir Sprint 0

- [ ] Repo ada, default branch `develop`
- [ ] `dotnet build` dan `dotnet test` hijau, `/api/health` merespons
- [ ] Database `MiniErpDb` terbentuk lewat migration, tanpa password di repo
- [ ] Browser nampilin "Backend status: healthy"
- [ ] CI hijau di PR
- [ ] Ruleset **Active**, push langsung ke `develop` ditolak (GH013)
- [ ] 5 label, issue template muncul di *New issue*
- [ ] Milestone `Sprint 1 - Authentication`
- [ ] Board 5 kolom + field Story Points
- [ ] 7 epic + minimal 3 story Sprint 1 (dengan AC dan point)
- [ ] README, ERD Auth, 3 ADR, Definition of Done

## Skill yang dilatih

| Skill | Dilatih lewat |
|---|---|
| Struktur solution enterprise | Clean Architecture 4 layer |
| Dependency Injection .NET | `AddApplication` / `AddInfrastructure` |
| Manajemen skema DB | EF Core migration |
| Middleware pipeline | Urutan di `Program.cs`, CORS |
| Keamanan dasar | Secrets di luar repo, `.gitignore` |
| Dasar React | `useState`, `useEffect`, axios |
| Alur kerja tim | Branch, PR, CI, merge |
| Penjagaan kualitas | CI + Ruleset |
| Debugging | Error NuGet, CORS, Vite, CI, Git |
| Agile | Epic, Story, Task, sprint, DoD |

## Tips biar ngelotok

1. **Ketik ulang, jangan copy-paste.** Tangan yang ngetik bantu otak nangkep pola.
2. **Pas error, baca pesannya dulu** sebelum nanya. Error .NET, npm, dan Git itu informatif.
3. **Satu fase, satu sesi.** Jangan dikebut semua dalam sehari.
4. **Catat "hal baru yang dipelajari"** di `docs/learning-journal.md`. Jadi bahan retrospective dan portfolio.