# İş Mantığı — Prim Hesaplama

## Genel akış

```
Kaynak sistemler          ETL (gecelik batch)             Prim motoru
─────────────────         ───────────────────             ───────────
PMS  (Fidelio)   ┐                                        kural eşleştir
POS  (Flyby)     ├─►  CSV  ─►  adapter  ─►  SaleRecord ─►  tutar hesapla  ─►  CommissionResult
ERP  (JDE)       ┘            (map/validate)   (idempotent)   iade düş        (+ hesaplama izi)
```

Prim aylık hesaplanır, gerçek zamanlı değil. Bu yüzden **gecelik batch ETL** seçildi:
tekrarlanabilir/idempotent, tek mutabakat penceresi, basit hata modeli.

## ETL — gün sonu içe aktarma (`EndOfDayImportService`)

Tüm kaynak dosyaları **tek bir `ImportBatch`** altında işlenir. Adımlar:

1. **Map** — `ISourceAdapter` ham satırı `Sale` / `Error` / `Reconciled` sonucuna çevirir.
   Parse edilemeyen satır → `PARSE_ERROR`.
2. **Doğrulama** — personel bulunamadı (`EMPLOYEE_NOT_FOUND`), tutar/tarih geçersiz
   (`INVALID_AMOUNT`, `INVALID_DATE`), zorunlu alan boş (`REQUIRED_FIELD_MISSING`),
   ERP'de POSTED olmayan kayıt (`UNPOSTED_RECORD`) …
3. **Dedup — 3 seviye:**
   - çalışma-içi `HashSet` (aynı dosyada tekrar eden satır),
   - çalışmalar-arası mevcut anahtarlar kümesi,
   - DB'de `UNIQUE(source_system_id, source_transaction_no)`.
   Mükerrer satır sessizce atlanır, sayaç artar.
4. **Mutabakat (reconciliation)** — ERP'deki RI kaydının `Reference` alanı `PMS-` ile başlıyorsa
   bu, PMS'te zaten var olan bir postingin aynası demektir; çift sayımı önlemek için atlanır
   (`Reconciled`).
5. **Kapalı dönem koruması** — işlem tarihi kapalı bir döneme düşüyorsa satır reddedilir
   (`PERIOD_CLOSED`).
6. Hatalı satırlar **karantinaya** (`import_errors`) alınır — iyi satırlar akmaya devam eder.
   Batch sayaçları: okunan / eklenen / mükerrer / hatalı / mutabakat.

Sonuç ayrıca `AffectedPeriods` döner (içe aktarılan satışların **gerçek işlem tarihlerinden**),
gece işi bu dönemleri yeniden hesaplar.

### Girdi kaynakları

| Kanal | Nasıl |
|---|---|
| Arayüzden dosya yükleme | `POST /api/import/end-of-day` (multipart, PMS/POS/ERP) |
| Örnek CSV'ler | `POST /api/import/end-of-day/sample` — `Data/` altındaki sabit dosyalar |
| Gece işi | inbox klasörü (`InboxDirectory`, yoksa `Data/`) `pms_*/pos_*/erp_*` deseni |
| Tümü (personel + import + hesap) | `POST /api/import/end-of-day/run-nightly` |

## Prim motoru (`CommissionEngine`, saf domain servisi)

Bir personelin bir dönemdeki işlemleri + o dönemde geçerli kurallar → `CommissionTrace`
(adım adım hesaplama izi). Strateji deseni: her kural tipi bir `ICommissionRuleHandler`.

### Kural (`CommissionRule`)

- **Kapsam (`RuleScope`)** — departman kodu, ürün kodu deseni (`SPA_%` — `%` joker, `_` düz),
  kaynak sistem kodu. Bir işlem birden çok kuralla eşleşirse **`Priority`** belirler.
- **Versiyonlama** — düzenleme = `CreateNewVersion(...)`. Eski versiyon `EffectiveTo` ile kapanır
  ama kendi penceresinde hâlâ geçerlidir; geçmiş bir dönem yeniden hesaplandığında o günkü kural
  kullanılır — sonuç tekrar üretilebilir.
- **`IsEffectiveOn(date)`** — kapalı versiyon penceresi içindeyse uygulanır; açık uçlu versiyon için
  ayrıca `IsActive` gerekir.

### 3 kural tipi (kod değişikliği olmadan eklenir)

| Kod | Nasıl hesaplar |
|---|---|
| `FIXED_PERCENTAGE` | işlem tutarı × `Rate` |
| `FIXED_AMOUNT` | işlem başına sabit `AmountPerTransaction` |
| `TIERED_BRACKET` | aylık kümülatif net taban **tek bir kademeyi** seçer, o kademenin oranı ayın tüm işlemlerine uygulanır (whole-month modeli) |

Yeni bir tip = yeni bir `ICommissionRuleHandler` + `rule_types` tablosuna bir satır.

### İade mahsubu

İade işlemi (`IsRefund`) negatif katkı üretir; ilgili kural aynı mantıkla ters işaretle uygulanır.
Personelin dönem toplamı, iadeler düşülmüş nettir. Kademeli bracket'te kümülatif taban da nettir
(iadeler tabanı düşürür → daha düşük kademe seçilebilir).

### Eşleşmeyen işlem

Hiçbir kurala uymayan işlem "kapsam dışı" sayılır, prim üretmez; dönem özetinde
`UnmatchedCount` olarak görünür. Taze kurulumda hiç kural yoksa her satış kapsam dışıdır — bu yüzden
5 başlangıç kuralı (`SeedStarterCommissionRules`) seed edilir ve arayüzde "aktif kural yok" uyarısı
gösterilir.

## Dönem yönetimi (`Period`, `PeriodService`)

- Bir ay = bir `Period`. İlk hesaplamada otomatik açılır (`GetOrOpenAsync`).
- **Kapatma** — `CloseAsync`: dönem `Closed` olur; bu tarihten sonra o döneme veri girişi ve
  yeniden hesaplama reddedilir (`DomainException`). Gece işi kapalı dönemi "atlandı: kapalı" olarak
  raporlar, özeti yine gösterir.
- **Yeniden açma** — `OpenAsync` (yalnız ADMIN/ACCOUNTING).
- Hesaplama sonuçları (`CommissionResult`) döneme FK ile bağlıdır; kapanınca dondurulmuş kabul edilir.

## Denetim kaydı (`audit_entries`)

- `IAuditable` agregalarındaki (`SaleRecord`, `CommissionRule`, `Period`) her INSERT/UPDATE/DELETE.
- Alanlar: entity adı + id, aksiyon, `ChangesJson` (eski→yeni), kullanıcı + rol, UTC zaman.
- **Hash zinciri** — her kayıt `PreviousHash` + kendi `Hash`'ini (SHA-256) tutar; zincir kırılırsa
  sonradan müdahale tespit edilir. Append-only; güncelleme/silme yok.

## Roller ve görünürlük

| Rol | Görebildiği |
|---|---|
| `ADMIN` | her şey — kural düzenleme, dönem, import, kullanıcı yönetimi |
| `ACCOUNTING` | dönem hesaplama/kapatma, tüm personel primleri, kurallar (okuma) |
| `EMPLOYEE` | yalnız kendi primi ve hesaplama izi (`GET /api/commissions/me`) |

## Personel senkronu

`personel.csv` seed'i sabit kalır; `PersonnelSyncService` dosyayı tarar, **DB'de olmayan** her
personeli (ve EMPLOYEE-rol login'ini, rastgele şifre + kredential dosyası ile) ekler. Additive ve
idempotent — var olan satırlara dokunmaz. Hem gece işinde hem "Personelleri senkronize et"
butonuyla çalışır.
