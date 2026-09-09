# Redis Cache Stratejisi

Gece batch'i verinin çoğunu günde bir kez değiştirir; okuma ise sürekli. Bu yüzden sık okunan,
seyrek değişen veri Redis'te tutulur. Redis **zorunludur** — bağlantı stringi boşsa uygulama açılışta
hata verir.

## Bileşenler

| Parça | Yer | Görev |
|---|---|---|
| `ICacheStore` | `Application/Abstractions` | get / set / remove + `GetGenerationAsync` / `BumpGenerationAsync` |
| `RedisCacheStore` | `Infrastructure/Caching` | StackExchange.Redis; fiziksel key öneki `gloria:v1:` |
| `CacheJson` | `Infrastructure/Caching` | `DefaultJsonTypeInfoResolver` modifier'ları ile private ctor + private setter round-trip (domain'de attribute yok) |
| `CachedReferenceRepositories` | `Infrastructure/Caching` | read-through decorator'lar (Scrutor `Decorate<>`) |
| `CacheKeys` | `Application/Abstractions` | tüm key/generation-key üreticileri |

## İki katman

### Katman 1 — referans veri (TTL-only)

Yalnızca **skaler lookup için** (id / kod) okunan, sadece migration ile değişen veri.

| Key | TTL | Decorator |
|---|---|---|
| `ref:source-systems` | 30 dk | `CachedSourceSystemRepository` |
| `ref:departments` | 30 dk | `CachedDepartmentRepository` |
| `ref:employees` | 30 dk | `CachedEmployeeRepository` |

Tek-kayıt sorguları cache'lenmiş `GetAllAsync` listesinden türetilir. `AddAsync` çağrıldığında
decorator ilgili key'i **siler** (`KeyDeleteAsync`) — personel senkronu yeni personel eklediğinde
`ref:employees` düşer, sonraki okuma DB'den taze gelir.

**Role ve RuleType bilerek cache'lenmez.** Bunlar yeni `User` / `CommissionRule` agregalarına
navigation olarak bağlanır; cache'ten gelen detached bir instance EF'i re-insert etmeye iter
(PK violation). Bu entity'ler her zaman DbContext'ten taze okunur.

### Katman 2 — DTO'lar (generation-token)

Prim sonuçları, kural listesi, dönem listesi — DTO olarak cache'lenir, wildcard `DEL` gerektirmeyen
**generation-token** deseniyle invalidate edilir.

| Alan | Generation key | Veri key'i | Bump eden |
|---|---|---|---|
| Kural listesi | `gen:rules` | `rules:list:g{gen}` | kural create / yeni versiyon / deaktive |
| Prim (liste/özet/personel) | `gen:commissions:{yıl}:{ay}` | `commissions:{yıl}:{ay}:g{gen}:…` | hesaplama, dönem kapatma |
| Dönem listesi | `gen:periods` | `periods:list:g{gen}` | dönem aç / kapat, hesaplama sırasında yeni dönem açılması |

**Nasıl çalışır:**

1. Okuma: `gen = GET gen:...` (yoksa **0**), sonra `GetOrCreate("...:g{gen}:...")`.
2. Yazma (kaydettikten sonra): `INCR gen:...` → generation 1 artar.
3. Sonraki okuma yeni `g{gen}` ile yeni key'e yazar; eski `g{gen-1}` key'i **öksüz** kalır ve
   TTL'iyle (prim 15 dk, kural/dönem 30 dk) kendiliğinden düşer. Aktif hiçbir okuyucu eski key'i
   istemez çünkü herkes güncel generation'ı hesaplar.

> `RedisInsight`'ta (`http://localhost:5540`) `gloria:v1:...:g1:summary` ve `...:g2:summary`
> anahtarlarını yan yana görebilirsin — `g1` ölü, TTL bekliyor.

### Neden token, neden `DEL` değil

`DEL gloria:v1:commissions:2026:8:*` bir sunucu-tarama komutu gerektirir; generation-token ise tek
`INCR` ile tüm ilgili key'leri (liste sayfaları + özet + personel detayları) anında geçersiz kılar,
O(1) ve cluster-güvenli.

## İnvalidasyon noktaları (kod)

| Servis / metod | Yaptığı |
|---|---|
| `CommissionRuleService.Create / CreateNewVersion / Deactivate` | `SaveChangesAsync` → `BumpGenerationAsync(RulesGen)` |
| `CommissionCalculationService.CalculateAsync` | `BumpGenerationAsync(CommissionGen(y,m))`; hesaplama sırasında **yeni dönem açıldıysa** ayrıca `BumpGenerationAsync(PeriodsGen)` |
| `PeriodService.CloseAsync` | `BumpGenerationAsync(PeriodsGen)` + `BumpGenerationAsync(CommissionGen(y,m))` (liste satırları dönem durumunu gösterir) |
| `PeriodService.OpenAsync` | `BumpGenerationAsync(PeriodsGen)` |
| `PersonnelSyncService` | değişiklik varsa `RemoveAsync(Employees)` + `RemoveAsync(Departments)` |

Kural eklemek/değiştirmek **mevcut prim sonuçlarını otomatik yeniden hesaplamaz** — kural listesi
cache'i tazelenir, prim rakamları ancak "Hesapla" ile değişir. Motor kuralları cache'ten değil
doğrudan DB'den okur (`ICommissionRuleRepository` decorator'sız), yani her hesaplama en güncel
kuralları kullanır.

## Konfigürasyon

`appsettings.json` → `Redis:ConnectionString`; Docker'da `Redis__ConnectionString: redis:6379`.
`ConfigurationOptions.AbortOnConnectFail = false` (Redis geç açılırsa API çökmesin).
