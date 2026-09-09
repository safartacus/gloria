# Backend Mimarisi & Kod Standartları

## Katmanlar (Clean / Onion)

```
Gloria.Api            HTTP sınırı: controller'lar, auth, middleware, DI kökü, Swagger
   │  (yalnız Application + Infrastructure'a bakar)
Gloria.Application    use-case servisleri, DTO'lar, abstraction'lar (arayüzler)
   │  (yalnız Domain'e bakar)
Gloria.Infrastructure EF Core, repository implementasyonları, Redis, Serilog sink, hosted service'ler
   │  (Domain + Application arayüzlerini implemente eder)
Gloria.Domain         entity / aggregate / value object / domain service — bağımlılığı yok
```

Bağımlılık oku **içeri** doğrudur. `Domain` hiçbir şeye referans vermez; `Application` sadece
`Domain`'e; `Infrastructure` ile `Api` dıştaki halkalardır. Uygulama servisi bir altyapı detayına
(EF, Redis, dosya sistemi) doğrudan değil, `Application/Abstractions` altındaki arayüz üzerinden erişir.

| Arayüz | Nerede implemente | Ne için |
|---|---|---|
| `IUnitOfWork` | `GloriaDbContext` | tek `SaveChangesAsync` |
| `ICacheStore` | `RedisCacheStore` | get/set/remove + generation token |
| `ICsvFileReader` | `SemicolonCsvFileReader` | `;` ayraçlı, BOM toleranslı |
| `IClock` | `SystemClock` | test edilebilir zaman |
| `IPasswordHasher` | `Pbkdf2PasswordHasher` | PBKDF2-SHA256, 210k iter |
| `ITokenService` | `JwtTokenService` | HS256 access token |
| `ICredentialFileWriter` | `TextCredentialFileWriter` | `users/*.txt` kredential notu |
| `ICurrentUser` | `CurrentUser` (Api) | JWT claim'lerinden aktif kullanıcı |

## Domain kuralları

- **`Entity<TId>`** — kimlikle karşılaştırma, `IsTransient` (Id == default).
- **`AggregateRoot<TId>`** — transaction sınırı; repository yalnızca aggregate root ile çalışır.
- **`ValueObject`** — `Money`, `RuleScope` gibi; değerle karşılaştırma, immutable.
- **`IAuditable`** — marker; bu tipteki her değişiklik `AuditSaveChangesInterceptor` tarafından
  denetim kaydına dönüşür (`SaleRecord`, `CommissionRule`, `Period`).
- **`DomainException`** — iş kuralı ihlali; middleware bunu `409 Conflict`'e çevirir.
- Constructor'lar `private`, oluşturma `static factory` metoduyla (`Employee.Create(...)`),
  setter'lar `private`. Nesne her zaman geçerli durumda doğar; geçersiz girdi `DomainException` atar.
- Domain sınıflarında **attribute yok** (EF/JSON attribute'ları dahil). Eşleme `IEntityTypeConfiguration`
  ile Infrastructure'da yapılır; cache serileştirmesi `CacheJson` resolver'ıyla private ctor/setter'ı
  round-trip eder.
- Kod içi **enum yok** — rol/kaynak/kural tipi birer tablo (`RoleCodes`, `SourceSystem`, `RuleType`);
  karşılaştırma sabit string'lerle.

## Persistence

- **EF Core 8 + SQLite.** Tek dosya (`gloria.db`), ayrı DB servisi yok; container'da
  `gloria-sqlite-data` volume'unda kalıcı. `snake_case` tablo adları, owned type'lar
  (`Money`, `RuleScope`), JSON kolonlar `TEXT` (bracket tier'lar, ham CSV kaydı, denetim değişiklikleri).
- **`decimal` → `REAL`** — SQLite'ta native decimal yok; `ConfigureConventions` içinde
  `Properties<decimal>().HaveConversion<double>()` ile REAL saklanır (sayısal sıralama/karşılaştırma
  çalışsın diye). Para, kalıcılaştırılmadan önce domain'de 2 haneye yuvarlanır.
- **Migration'lar** `Gloria.Infrastructure/Migrations`: tek `InitialCreate` (şema + `HasData` referans
  seed + 5 başlangıç prim kuralı + 3 tier'ın raw SQL seed'i).
- Seed veri `HasData` + migration ile gelir (dosyadan değil — "proje manipüle edilmesin").
  SQLite'ta explicit id'li seed'ten sonra rowid otomatik `max+1`'den devam eder — sequence ayarı gerekmez.
- `AuditSaveChangesInterceptor` — append-only; DB tarafından üretilen key'ler için iki fazlı
  (`SavedChanges`), SHA-256 hash zinciri (`PreviousHash` → `Hash`).

## Application servisleri

Her servis tek bir use-case ailesini yönetir, `AddApplication()` ile `Scoped` kaydedilir:

| Servis | Sorumluluk |
|---|---|
| `AuthService` | login / refresh (rotation + reuse tespiti) / logout / şifre değiştirme |
| `UserAdminService` | hesap listeleme / oluşturma (rastgele şifre + kredential dosyası) / aktif-pasif |
| `PersonnelSyncService` | `personel.csv` → eksik personel + login ekleme (additive, idempotent) |
| `EndOfDayImportService` | tek `ImportBatch` altında PMS/POS/ERP satırlarını işleme |
| `CommissionCalculationService` | dönem hesaplama + sayfalı liste / özet / personel detay (cache'li) |
| `CommissionRuleService` | kural oluşturma / yeni versiyon / deaktive (cache invalidasyon) |
| `PeriodService` | dönem listeleme / açma / kapatma |

Kaynak adaptörleri (`ISourceAdapter`): `PmsFidelioAdapter`, `PosFlybyAdapter`, `ErpJdeAdapter` —
ham CSV satırını `Sale` / `Error` / `Reconciled` sonucuna dönüştürür.

## API katmanı

- **JWT Bearer (HS256).** `MapInboundClaims=false`; `ClaimTypes.Name` / `ClaimTypes.Role` eşlemesi.
  Access token kısa ömürlü, refresh token sunucuda hash'li saklanır ve her kullanımda döner.
- **Yetkilendirme** controller/action üzerinde `[Authorize(Roles = RoleCodes.X)]` ile.
- **`ExceptionHandlingMiddleware`** — tek hata sınırı: `AuthenticationException`→401,
  `DomainException`→409, `FileNotFoundException`→400, diğer→500. Her yakalanan hata korelasyon id +
  path/method/kullanıcı ile Serilog'a; aynı id cevap gövdesinde döner. `UseAuthentication`'dan
  **sonra** durur (kullanıcı claim'i log context'e girsin diye), `UseSerilogRequestLogging`'in içinde.
- **Controller'lar ince** — iş yok, servise delege eder, DTO döner. Anonymous obje yerine tipli
  `record` DTO tercih edilir.
- **Configuration** — `Gloria:*` (AutoMigrate, Credentials, SampleImport, NightlyImport, PersonnelSync),
  `Jwt:*`, `Redis:*`, `Serilog:*`. Docker'da `.env` → `${VAR}` → compose env → .NET config.

## Arka plan işleri

- `NightlyImportHostedService : BackgroundService` — sadece zamanlayıcı; `Enabled` ise her gün
  `RunAtLocalTime`'da bir scope açıp `NightlyBatchRunner`'ı çalıştırır.
- `NightlyBatchRunner` (scoped) — asıl akış, hem gece işi hem `POST /api/import/end-of-day/run-nightly`
  tarafından kullanılır: (1) personel senkronu, (2) klasör importu, (3) etkilenen dönemlerin yeniden
  hesaplanması + özet. Her adım idempotent.

## Test yaklaşımı

- **Domain unit test'leri** — motor davranışı (kademeli bracket, iade), kural eşleştirme; bağımlılık yok.
- **Application test'leri** — hafif in-memory fake repo'larla servis davranışı
  (ör. `PersonnelSyncServiceTests`: yalnız eksikleri ekler + ikinci koşu no-op).
- `IClock` sahte, `SystemClock` yerine sabit zaman; DB'ye gerek yok.

## Kod standartları — özet

- İngilizce identifier; dosya başına bir asıl tip; klasör = namespace.
- Nullable reference types açık; `is null` / `is not null`.
- Public API'de `record` DTO; primary constructor kısa servislerde.
- Repository = koleksiyon soyutlaması (`GetByXAsync`, `AddAsync`); LINQ sorgusu Infrastructure'da kalır.
- Yorum yoğunluğu çevredeki kodla aynı; "ne" değil "neden" yazılır.
- Cache decorator'ları yalnız **skaler lookup için okunan** referans veriyi sarar; yeni bir
  aggregate'e navigation olarak bağlanan entity (Role, RuleType) **cache'lenmez** (EF re-insert riski).
