# Frontend Mimarisi

React 19 + Vite + TypeScript + Tailwind + TanStack Query. Klasör: `web/`.

```bash
cd web
npm install
npm run dev      # http://localhost:5173  (/api → proxy)
npm run build    # tsc -b (typecheck) + vite build
```

Prod'da `web/Dockerfile`: node ile build → nginx ile serve; nginx `/api`'yi `api:8080`'e proxy'ler
(`web/nginx.conf`), SPA fallback açık.

## Atomic Design

Bileşenler karmaşıklığa göre katmanlı; çoğu paylaşımlı, sayfaya özel mantık minimum.

| Katman | `web/src/components/…` | Örnek |
|---|---|---|
| **atoms** | tek amaçlı, stateless | `Button` `Badge` `Card` `Spinner` `Inputs` (`TextInput`/`Select`/`NumberInput`) `Display` (`Money`/`Percent`) |
| **molecules** | birkaç atom + küçük davranış | `Field` `Modal` `MonthPicker` `ConfirmButton` `StatCard` `Toast` (context) `QueryBoundary` `ErrorState` |
| **organisms** | ekran parçası | `AppHeader` `DataTable` `StatGrid` `RuleForm` `BracketTierEditor` `UserForm` `CommissionDetail` `CommissionTraceTable` `ImportErrorTable` `UserMenu` |
| **templates** | sayfa iskeleti | `PageLayout` (header + içerik) `PageHeader` (başlık + açıklama + aksiyonlar) |
| **pages** | `web/src/pages` | rota başına bir dosya |

`DataTable<T>` tamamen **controlled**: `sort` / `onSortChange` / `pagination` (page, pageSize,
totalCount, onPageChange, loading) / `onRowClick` prop'ları; kendi state'i yok — sunucu-taraflı
sıralama/sayfalama sayfada yönetilir.

## Sayfalar ve roller

| Rota | Sayfa | Rol |
|---|---|---|
| `/commission` | `MyCommissionPage` — kendi primim + hesaplama izi | EMPLOYEE |
| `/rules` | `RulesPage` — kural listesi + düzenleme (yeni versiyon) | ADMIN, ACCOUNTING |
| `/periods` | `PeriodOverviewPage` — dönem özeti, sunucu-taraflı sayfalı liste, satıra tıkla → detay, "aktif kural yok" uyarısı | ADMIN, ACCOUNTING |
| `/users` | `UsersPage` — hesap listesi, yeni kullanıcı, personel senkronu | ADMIN |
| `/import` | `ImportPage` — PMS/POS/ERP yükleme, örnek CSV, "gece işini şimdi çalıştır" | ADMIN |
| `/import-log` | `ImportLogPage` — gün seç → o gün çalışan batch'ler + karantina satırları | ADMIN |
| `/login` | `LoginPage` — kayıt yok | — |

`AppHeader` nav'ı role göre filtreler; `App.tsx` role göre ana sayfaya yönlendirir
(`employeeId` varsa `/commission`, yoksa `/periods`).

## Veri katmanı

- **`api/client.ts`** — `request<T>()`: `Authorization: Bearer` ekler, FormData'da Content-Type'ı
  tarayıcıya bırakır, **401'de tek-uçuş refresh** (`tryRefresh`) dener, olmazsa storage temizler.
  `ApiError` → `status` + `title` + `detail` + `correlationId`. Export: `api.get / post / postForm`.
- **`api/queries.ts`** — tüm `useQuery` / `useMutation` hook'ları + `keys` (query key üreticileri).
  Sayfalı sorgularda `keepPreviousData`. Mutation'lar `onSuccess`'te ilgili key'leri
  `invalidateQueries` ile tazeler (ör. hesaplama sonrası `['commissions']` + `keys.periods`).
- **`api/types.ts`** — backend `record` DTO'larının TS karşılıkları (camelCase).
- **`lib/queryClient.ts`** — `retry`: 4xx'te (404/409…) tekrar deneme yok, yalnız 5xx / bağlantı
  hatası 1 kez; `refetchOnWindowFocus: false`; `staleTime: 30s`.

## Kimlik doğrulama

- **`auth/authStorage.ts`** — `localStorage` `gloria.auth`: `{ accessToken, refreshToken, user }`.
  `user`: `{ username, fullName, roleCode, employeeId, mustChangePassword }`.
- **`auth/AuthProvider.tsx`** — `login` / `logout` / `changePassword` / `user` / `isAuthenticated`.
  `isAuthenticated` yoksa `App` doğrudan `LoginPage` render eder.
- Access token JWT içinde kullanıcı kodu/isim/rol taşır; refresh token sunucuda döner (rotation).
- `mustChangePassword` → üst şeritte uyarı + kullanıcı menüsünde "Şifre değiştir".

## Hata gösterimi (`lib/errors.ts`)

`toFriendlyError(unknown)` hatayı türe göre sınıflar:

| Durum | Kullanıcıya |
|---|---|
| 400 / 403 / 409 / 422 | API'nin spesifik `detail` mesajı (aksiyon alınabilir) |
| 401 | "Oturum sona erdi" / login ekranında "Kullanıcı adı veya şifre hatalı." |
| 404 | "Bulunamadı" + detay |
| **5xx / eşleşmeyen** | "Beklenmeyen bir hata oluştu … BT departmanı ile iletişime geçin." + `Referans: <correlationId>` — iç hata metni **gösterilmez** |
| `TypeError` (sunucuya ulaşılamıyor) | "Sunucuya ulaşılamadı …" |

- `friendlyMessage()` — toast / form içi tek satırlık sürüm (5xx'te referans kodunu ekler).
- **`ErrorState`** (molecule) — ikon + başlık + mesaj + referans kodu + "Tekrar dene".
  `QueryBoundary` bunu kullanır (`query.refetch`).
- **`ErrorBoundary`** (class, `main.tsx`'i sarar) — render sırası çöküşünde boş sayfa yerine aynı kart.
- Tüm form ve mutation hata yolları `friendlyMessage(err)` / `toast.error(friendlyMessage(e))` kullanır.

## Diğer kalıplar

- **`QueryBoundary`** — `isPending` → spinner, `isError` → `ErrorState`, boş → mesaj, aksi halde
  `children(data)`.
- **`Toast`** — küçük context; `success` / `error` / `push`.
- **`Modal`** — Escape ile kapanır; `wide` seçeneği; footer slot'u.
- Tarih/para biçimi tek yerde: `lib/format.ts` (`formatMoney` `formatDate` `formatDateTime`
  `MONTHS_TR` `previousMonth`).
- Sınıf birleştirme: `lib/cn.ts`.
