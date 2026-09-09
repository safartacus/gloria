# Gloria Hotels & Resorts — Personel Prim Otomasyonu

> **Özel / Proprietary.** Tüm hakları saklıdır — bkz. [LICENSE](LICENSE). Bu depo yalnızca
> bir vaka çalışması (case study) kapsamında değerlendirme amacıyla paylaşılmıştır; telif hakkı
> sahibinin yazılı izni olmadan kullanılamaz, kopyalanamaz, dağıtılamaz.

Otel zincirindeki personelin **satış primlerini** kaynak sistemlerden (PMS / POS / ERP) gelen
verilerle otomatik hesaplayan uçtan uca sistem. Prim kuralları koda gömülü değildir; veritabanında
tanımlanır ve arayüzden düzenlenir.

## Ne işe yarar

- **ETL** — PMS (Fidelio), POS (Flyby) ve ERP (Oracle JDE) gün sonu CSV'lerini içeri alır;
  mükerrer satırları eler, hatalı satırları karantinaya alır, ERP↔PMS mutabakatı yapar.
- **Esnek prim motoru** — 3 kural tipi (sabit %, kademeli bracket, işlem başı sabit tutar).
  Yeni kural / oran değişikliği = kod değişikliği değil, veri değişikliği. Düzenleme = yeni versiyon
  (geçmiş yeniden üretilebilir kalır).
- **İade mahsubu** — iade işlemleri primden düşülür.
- **Hesaplama izi (trace)** — bir personelin priminin hangi işlemden, hangi kural/versiyondan,
  hangi tutarla oluştuğu adım adım görülebilir.
- **Dönem yönetimi** — aylık dönem açma/kapatma; kapalı dönem değiştirilemez (immutability).
- **Denetim kaydı** — `IAuditable` agregalarındaki her değişiklik kim/ne zaman/eski→yeni olarak,
  SHA-256 hash zinciriyle (tamper-evident) `audit_entries` tablosuna yazılır.
- **3 rol** — `ADMIN` (kural + dönem + import), `ACCOUNTING` (dönem + görüntüleme),
  `EMPLOYEE` (yalnız kendi primi).
- **Arayüz** — prim kuralı düzenleme, dönem özeti + personel bazında detay, kullanıcı yönetimi,
  içe aktarma, içe aktarma günlüğü.
- **Gece işi** — `NightlyBatchRunner`: personel senkronu + klasör importu + etkilenen dönemlerin
  yeniden hesaplanması. Aynı akış "şimdi çalıştır" butonuyla elle de tetiklenir.
- **Redis cache** — referans veri + prim/kural/dönem DTO'ları; generation-token ile invalidasyon.
- **Structured logging** — Serilog → Console + **Seq**; yakalanan her exception ve karantina satırı
  korelasyon id'siyle loglanır.

## Teknoloji

| Katman | |
|---|---|
| Backend | .NET 8 Web API · EF Core 8 · SQLite · Clean/Onion mimari · DDD |
| Cache | Redis 7 (StackExchange.Redis, Scrutor decorator'ları) |
| Log | Serilog · Seq |
| Frontend | React 19 · Vite · TypeScript · Tailwind · TanStack Query · Atomic Design |
| Çalıştırma | Docker Compose (tek komut) · GitHub Actions CI |

## Depo yapısı

```
Gloria/
├── api/                  .NET çözümü (backend)
│   ├── Gloria.sln
│   ├── src/  Gloria.Domain · Application · Infrastructure · Api
│   └── tests/ Gloria.Domain.UnitTests · Gloria.Application.Tests
├── web/                  React uygulaması (frontend)
├── Data/                 örnek PMS/POS/ERP CSV'leri + personel.csv (compose mount eder)
├── users/                admin'in oluşturduğu hesapların kredential notları (git'e girmez)
├── docker-compose.yml
├── .env / .env.example
├── docs/                 mimari & tasarım notları
└── .github/workflows/ci.yml
```

## Çalıştırma

Proje **tek komutla** ayağa kalkar — yerelde ayrı ayrı derleme/çalıştırma yoktur.
Tek gereksinim: Docker + Docker Compose.

```bash
cp .env.example .env          # gerekirse portları / JWT anahtarını düzenle
docker compose up -d --build
```

Kapatmak için `docker compose down` (verileri de silmek için `docker compose down -v`).

| Servis | Adres |
|---|---|
| Uygulama (SPA + `/api` proxy) | http://localhost:8080 |
| API + Swagger | http://localhost:5080/swagger |
| Seq (loglar) | http://localhost:5341 |
| RedisInsight (cache) | http://localhost:5540 |

Veritabanı **SQLite** — ayrı bir servis yok; `api` container'ı içindeki tek bir dosya
(`/app/db/gloria.db`), `gloria-sqlite-data` volume'unda kalıcı. Migration'lar ve seed veri
açılışta otomatik uygulanır (`GLORIA_AUTOMIGRATE=true`).

### Giriş bilgileri (seed)

| Kullanıcı | Rol | Şifre |
|---|---|---|
| `admin` | ADMIN | `Gloria2026!` |
| `accounting` | ACCOUNTING | `Gloria2026!` |
| `P1001` … `P1013` | EMPLOYEE | `Gloria2026!` |

Tüm seed hesapların `MustChangePassword` bayrağı açıktır — ilk girişte şifre değiştirilir.
Arayüzde admin'in yeni oluşturduğu her kullanıcı için rastgele şifre üretilir ve
`users/<kullanıcıadı>_<kod>.txt` dosyasına yazılır.

### İlk deneme

1. `admin` ile gir, şifre değiştir.
2. **İçe Aktarma → "Örnek CSV'lerle dene"** (veya PMS/POS/ERP dosyalarını yükle).
3. **Dönemler** → ay seç → **Hesapla**.
4. Bir personele tıkla → hesaplama izini gör.

## Konfigürasyon

Tüm ayarlar `.env` üzerinden gelir (`docker compose` `${VAR}` olarak okur) — bkz.
[.env.example](.env.example): portlar, `TZ`, `JWT_SIGNING_KEY`, registry adı. `.env` git'e girmez;
`JWT_SIGNING_KEY` boşsa compose açılışta hata verir. (SQLite'ın ayarı yoktur.)

## Testler

CI her push/PR'da çalıştırır (aşağıya bkz.). Lokal SDK kurmadan, Docker ile çalıştırmak için:

```bash
docker run --rm -v "$PWD/api:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test Gloria.sln --configuration Release
```

Kapsam: kademeli bracket hesabı, iade mahsubu, kural eşleştirme, personel senkronu
(additive + idempotent).

## CI/CD

`.github/workflows/ci.yml`:

1. **test** — her push/PR'da: `dotnet test` + `web` typecheck & build.
2. **publish** — testler geçerse (PR'da değil): `api` ve `web` imajlarını registry'ye push eder.
   Registry adı/imaj öneki `.env.example`'dan; kimlik bilgileri GitHub repo secret'larından
   (ghcr.io için ek secret gerekmez, `GITHUB_TOKEN` yeter).

## Dokümanlar

| Belge | İçerik |
|---|---|
| [docs/architecture-api.md](docs/architecture-api.md) | Backend katman mimarisi, DDD kuralları, kod standartları |
| [docs/business.md](docs/business.md) | ETL, prim hesabı, iade, dönem/immutability, denetim |
| [docs/caching.md](docs/caching.md) | Redis strateji, iki katman, generation-token invalidasyon |
| [docs/logging.md](docs/logging.md) | Serilog + Seq, korelasyon id, exception & karantina logları |
| [docs/architecture-frontend.md](docs/architecture-frontend.md) | Atomic Design, veri katmanı, auth, hata gösterimi |
