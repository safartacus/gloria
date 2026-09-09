# Loglama — Serilog + Seq

Tüm loglama pipeline'ı **Serilog**. Çıkış: **Console** + **Seq** (`http://localhost:5341`, dev'de
girişsiz). `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger(); // config okunmadan önceki pencere
try {
    builder.Host.UseSerilog((ctx, sp, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)   // "Serilog" bölümü: seviyeler + Console + Seq sink
        .ReadFrom.Services(sp)
        .Enrich.FromLogContext());
    ...
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) { Log.Fatal(ex, "Application terminated unexpectedly"); }
finally { Log.CloseAndFlush(); }
```

## Yapılandırma

`appsettings.json` → `Serilog`:

```json
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
  "MinimumLevel": { "Default": "Information",
    "Override": { "Microsoft.AspNetCore": "Warning",
                  "Microsoft.EntityFrameworkCore.Database.Command": "Warning" } },
  "WriteTo": [ { "Name": "Console" },
               { "Name": "Seq", "Args": { "serverUrl": "http://localhost:5341" } } ],
  "Enrich": [ "FromLogContext" ],
  "Properties": { "Application": "Gloria.Api" }
}
```

Docker'da Seq URL'i compose'dan override edilir:
`Serilog__WriteTo__1__Args__serverUrl: "http://seq"` (dizin 1 = `WriteTo` dizisindeki Seq girişi;
ağ içinde Seq `http://seq` olarak erişilir).

## Middleware sırası

```
UseSerilogRequestLogging   ← en dışta: özet satırı, exception handler'ın yazdığı NİHAİ status'u görür
UseAuthentication / UseAuthorization
ExceptionHandlingMiddleware  ← auth'tan sonra: log context'e kimliği alabilir; tüm endpoint'leri sarar
MapControllers
```

- `UseSerilogRequestLogging` her HTTP isteği için tek satır özet üretir
  (`HTTP POST /api/... responded 200 in 12ms`), `EnrichDiagnosticContext` ile `CorrelationId` + `User`
  ekler. Exception handler'ın dışında olduğu için **doğru** status'u (401/409/…) loglar, uçuştaki
  exception'ı değil.
- `ExceptionHandlingMiddleware` her istek için `LogContext.PushProperty` ile
  `CorrelationId` (`Activity.Current?.Id ?? TraceIdentifier`), `RequestPath`, `RequestMethod`, `User`
  bindirir — bu istek boyunca çıkan **her** log satırı bu alanları taşır.

## Exception logları

| Exception | Seviye | HTTP | Not |
|---|---|---|---|
| `AuthenticationException` | Warning | 401 | başarısız login / süresi geçmiş token |
| `DomainException` | Warning | 409 | iş kuralı ihlali |
| `FileNotFoundException` | Warning | 400 | eksik referans dosya |
| diğer hepsi | **Error** | 500 | iç detay istemciye sızmaz |

Cevap gövdesi `{ title, detail, status, correlationId }` döner. Kullanıcı 500 alırsa arayüz
"BT departmanı ile iletişime geçin (Referans: `00-…`)" gösterir — bu id Seq'te aratılıp tam kayıt
bulunur.

## Karantina (hatalı içe aktarma satırları) logları

`import_errors` tablosuna ek olarak Seq'e de akar:

- `EndOfDayImportService` — batch bitince her karantina satırı için bir `Warning`:
  `Import row quarantined — batch 6, ERP line 58: UNPOSTED_RECORD — …`
  property'ler: `BatchId`, `ImportSource`, `ImportLine`, `ImportErrorCode`, `ImportErrorMessage`,
  `ImportRawRow` + istek bağlamı (`CorrelationId`, `RequestPath`, `User`). Ardından bir özet
  (`RowsFailed` / `RowsRead`).
- `PersonnelSyncService` — reddedilen her satır için `Warning` (`PersonnelLine`, `PersonnelNo`,
  `PersonnelRawRow`) + rol-yok durumu + özet.
- `NightlyBatchRunner` — tüm gece koşusunu bir `BeginScope("BatchRunId", guid)` ile sarar; o koşunun
  personel + import + hesaplama logları (karantina satırları dahil) tek id ile gruplanır.

## Seq'te tipik sorgular

```
ImportErrorCode = 'INVALID_AMOUNT'
select count(*) group by ImportSource
CorrelationId = '00-...'                      -- bir yüklemenin tüm satırları
@Level = 'Warning' and ImportErrorCode is not null   -- kayıtlı "karantina" sinyali
BatchRunId is not null                        -- bir gece koşusunun tamamı
```

Ayrıca admin arayüzünde **İçe Aktarma Günlüğü** ekranı gün seçerek o gün çalışan batch'leri ve
karantina satırlarını DB'den (Seq'e gitmeden) gösterir.

## Denetim kaydı ≠ hata logu

`audit_entries` tablosu **veri değişikliklerinin** iş kaydıdır (kim/ne zaman/eski→yeni, hash zinciri);
exception/log deposu değildir. İkisi ayrı amaçlara hizmet eder.
