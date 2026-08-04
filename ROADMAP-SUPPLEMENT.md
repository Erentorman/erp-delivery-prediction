# Roadmap Supplement — ERP Delivery Prediction

*`ROADMAP-HANDOFF.md`'de raporlanmayan modüllerin gerçek repository durumu. Tüm bulgular bu oturumda gerçek `git`/`dotnet`/`gh`/dosya taramasıyla doğrulanmıştır. Kod değiştirilmedi, commit atılmadı, task oluşturulmadı.*

**Terminoloji (bu raporda tutarlı kullanılır):**
- **Mock ERP runtime integration** = `seed → IErpDataProvider` zincirinin tamamlanması.
- **Prediction Pipeline integration** = `IErpDataProvider → Batch Reader → PredictionContext → engine` zinciri.
- Bu iki kavram **birbirinin yerine geçmez** — biri tamam, diğeri hiç başlamadı (bkz. Bölüm 9).

---

## 1. Snapshot Date and Current Develop HEAD

- Aktif branch: **`develop`** ✓
- `develop` = `origin/develop`: **Evet**, ikisi de **`1cd75ed`**
- HEAD commit: `1cd75ed4a8a3c933d04c71e9876ef456adaa3825` (Merge PR #25)
- `git status`: temiz — yalnızca `ROADMAP-HANDOFF.md` untracked (önceki oturumdan, commit edilmedi, beklenen)
- **T-357 (Mock ERP DI/config) PR'ı develop'a merge edilmemiş** — `gh pr list --search "t-357"` sıfır sonuç döndürdü. Değişiklik hâlâ yalnızca yerel `feature/t-357-mockerp-di-config` branch'inde (commit `d53b4cc`), push edilmedi.
- **Python 3.11 CI PR'ı (#25) develop içinde: Evet** — `f7d8311`/`1cd75ed` `origin/develop` geçmişinde doğrulandı.
- **`dotnet build`:** 0 Uyarı, 0 Hata. **`dotnet test`:** **143/143 başarılı** (33+22+9+15+64).

---

## 2. Latest Merged PRs

| PR | Başlık | Hedef | Durum |
|---|---|---|---|
| #25 | T-354/T-355: Add Python 3.11 converter tests to CI | develop | MERGED |
| #24 | T-354: Integrate validated full runtime seed | develop | MERGED |
| #23 | T-354/T-355: Add ERP seed converter and validate full conversion | develop | MERGED |
| #22 | T-356: Add preview seed deserialize smoke test | develop | MERGED |
| #17 | T-201: Add JWT generation and validation infrastructure | develop | MERGED |

*(#18/#19/#21'in `main`'e doğrudan merge/revert geçmişi için `ROADMAP-HANDOFF.md` Bölüm 2/13'e bakın — burada tekrar edilmiyor.)*

---

## 3. AI Prediction State

| Bileşen | Dosya Yolu | Durum | Test Kanıtı | MVP'de gerçekten kullanılıyor mu? |
|---|---|---|---|---|
| `ai-prediction/` klasörü | `ai-prediction/` | **Placeholder** | — | Hayır |
| FastAPI uygulaması | `ai-prediction/main.py` (7 satır) | **Placeholder** | Yok | Yalnızca `/health` endpoint'i var, başka hiçbir şey yok |
| requirements | `ai-prediction/requirements.txt` | Var (`fastapi`, `uvicorn` — 2 satır) | — | — |
| pyproject.toml | — | **Not Started** | — | Yok |
| training script | — | **Not Started** | — | Bulunamadı (`find -iname "*train*"` sıfır sonuç) |
| model artifact | — | **Not Started** | — | Bulunamadı (`.pkl`/`.joblib`/`.h5` sıfır sonuç) |
| `model/__init__.py` | `ai-prediction/model/__init__.py` | **Placeholder** | — | İçeriği yalnızca: `"""Empty structure for future AI prediction model code."""` |
| `preprocessing/__init__.py` | `ai-prediction/preprocessing/__init__.py` | **Placeholder** | — | Aynı şekilde boş |
| `IAiPredictionClient` | — | **Not Started** | — | Repoda sıfır dosya |
| `FastApiPredictionClient` | — | **Not Started** | — | Repoda sıfır dosya |
| `AiFeatureBuilder` | — | **Not Started** | — | Repoda sıfır dosya |
| `AiPredictionProvider` | — | **Not Started** | — | Repoda sıfır dosya |
| `IPredictionProvider` | — | **Not Started** | — | Repoda sıfır dosya |
| `PredictionOrchestrator` | — | **Not Started** | — | Repoda sıfır dosya |
| `IFinalPredictionCombiner` | — | **Not Started** | — | Repoda sıfır dosya |
| AI testleri | — | **Not Started** | — | `ai-prediction/` altında hiçbir `test_*.py` yok |
| AI CI job'ı | `.github/workflows/ci.yml` | **Not Started** | — | `grep "ai-prediction"` sıfır sonuç — CI'da AI servisi hiç build/test edilmiyor |

**Genel:** AI Prediction katmanı, SAD'nin öngördüğü tüm C#/Python bileşenleriyle **%0 — yalnızca isim ve health-check iskeleti var.**

---

## 4. Persistence State

| Bileşen | Dosya Yolu | Durum |
|---|---|---|
| `PredictionResult` entity/table | — | **Not Started** — sıfır sonuç |
| `PredictionProviderResults` | — | **Not Started** |
| `PredictionFactors` | — | **Not Started** |
| CriticalPath persistence | — | **Not Started** |
| `SystemSettings` | `src/App.Domain/Entities/SystemSetting.cs`, `src/App.Persistence/Configurations/SystemSettingConfiguration.cs` | **Done** (T-401'den, bu oturumda tekrar doğrulandı) |
| EF Core configurations | `src/App.Persistence/Configurations/*.cs` (6 dosya: Audit/Integration/Role/SystemSetting/User/UserRole) | **Done** — yalnızca kimlik/log alanları için, prediction için yok |
| Migrations | `20260730063602_InitialPersistence`, `20260730142440_T306AddIntegrationAndAuditLogs` | **Done** (mevcut kapsam için) |
| `IPredictionRepository`/servis | — | **Not Started** — sıfır sonuç |
| Persistence testleri (prediction'a özgü) | — | **Not Started** |

**Genel:** Persistence katmanı yalnızca **kimlik/audit/integration-log** alanı için tam; **prediction'a ait hiçbir tablo/entity/repository yok** — SAD §15.4'teki `PredictionResults`, `PredictionProviderResults`, `PredictionFactors` şemaları hiç migrate edilmemiş.

---

## 5. Prediction API State

| Aranan | Bulunan |
|---|---|
| `/api/predictions/*` endpoint'leri | **Yok** |
| Controller/Minimal API | **`src/App.Api`'de sıfır controller dosyası** (`find -iname "*Controller*"` boş) |
| Command/query/use case | Yok |
| Request/response DTO | Yok |
| Auth policy (`[Authorize]`, `AddAuthorization`, `RequireRole`) | **Yok** — `grep` sıfır sonuç, T-201'in JWT altyapısı var ama hiçbir endpoint'i koruyacak policy tanımlanmamış |
| API testleri (prediction'a özgü) | Yok |

**SAD §16.2 Endpoint Tablosu karşılaştırması:** SAD'de tanımlı 12 endpoint'ten (`/api/auth/login`, `/api/erp/orders`, `/api/predictions/*`, `/api/integrations/*` vb.) **hiçbiri kodda mevcut değil.** `App.Api`, DI/middleware iskeleti (auth, exception handling, DbContext, Mock ERP provider) tamamen kurulmuş bir composition root ama **üzerine tek bir HTTP endpoint'i inşa edilmemiş** durumda.

---

## 6. Frontend and What-if State

| Bileşen | Dosya Yolu | Durum |
|---|---|---|
| React/Vite frontend iskeleti | `frontend/src/` | **Done** (routing, layout, auth context) |
| Dashboard | `frontend/src/pages/Dashboard.tsx` (54 satır) | **Placeholder** — tamamen hardcoded değerler ("Active Orders: 124"), hiçbir API çağrısı yok |
| Prediction ekranı | `frontend/src/pages/Predictions.tsx` (67 satır) | **Placeholder** — "Final Hybrid: 3.5 Days", "Rule-Based: 4 Days", "AI Model: 2.8 Days" tamamen **statik/hardcoded**, `apiClient` hiç kullanılmıyor |
| Orders ekranı | `frontend/src/pages/Orders.tsx` | İncelendi, aynı şekilde statik (yalnızca `Login.tsx` gerçek bir `apiClient.post('/api/auth/login', ...)` çağrısı yapıyor — ki bu endpoint de backend'de yok, bkz. Bölüm 5) |
| What-if simulation | — | **Not Started** — `grep -rli "what-if\|whatif\|simulate"` sıfır sonuç |
| API client | `frontend/src/api/client.ts` | **Done** — gerçek axios instance, JWT interceptor + 401 → logout event akışı çalışır durumda |
| Frontend testleri | — | **Not Started** — `package.json`'daki `"test"` script'i literal olarak `echo "Error: no test specified" && exit 1` |

**Genel:** Frontend'in altyapısı (routing, auth, HTTP client) gerçek ve çalışır durumda; **iş ekranlarının hiçbiri gerçek veriyle konuşmuyor**, hepsi demo/mockup seviyesinde sabit değerler gösteriyor.

---

## 7. Docker Compose and CI State

**SAD §14.1'in beklediği 5 servis:** `frontend`, `api`, `postgres`, `mock-erp`, `ai-prediction`.
**`docker-compose.yml`'de gerçekten bulunanlar:** `frontend`, `api`, `postgres`, `mock-erp`, `ai-prediction` — **isim/topoloji olarak SAD ile birebir eşleşiyor.**

**Ama önemli bir fark var — önceki handoff'ta raporlanmamıştı:**

| Servis | Dockerfile Durumu | Gerçekten çalışır mı? |
|---|---|---|
| `frontend` | Gerçek multi-stage build (`node:22-alpine` → build) | **Evet** |
| `ai-prediction` | Gerçek (T-105 kaynaklarına bağlı, FastAPI çalıştırıyor) | **Evet** (yalnızca `/health`) |
| **`api`** | **Hâlâ T-103/T-104 placeholder** — `CMD ["sh", "-c", "echo '[api] Placeholder container...' && sleep infinity"]` | **HAYIR — container gerçek uygulamayı hiç çalıştırmıyor, sonsuza kadar uyuyor** |
| **`mock-erp`** | **Aynı placeholder deseni** — `sleep infinity` | **HAYIR** |
| `postgres` | Resmi `postgres:18-alpine` image | Evet |

- **Reverse proxy:** SAD'de yok, kodda da yok — tutarlı, fark değil.
- **Health check:** Yalnızca `postgres` için var (1 adet). `api`/`mock-erp`/`ai-prediction` için health check tanımlı değil.
- **Migration davranışı (SAD §14.3: "EF Core Migrations, api başlangıcında otomatik uygulanır"):** `App.Api/Program.cs`'de `Migrate()`/`EnsureCreated()` çağrısı **yok** — bu gereksinim hiç implemente edilmemiş. Zaten container gerçek uygulamayı çalıştırmadığı için bu bir anlamda "moot" ama kod seviyesinde de eksik.
- **CI job'ları:** `build-and-test` (.NET, tüm çözüm) + `erp-seed-converter-tests` (Python 3.11). **AI servisi için CI job'ı yok.** Frontend için CI job'ı yok.

**Sonuç: `docker compose up` şu an gerçek bir uçtan uca demo çalıştıramaz** — `api` ve `mock-erp` container'ları placeholder olarak sonsuza dek uyur, gerçek build/publish adımı hiç yapılmaz. Bu, önceki handoff'ta (`ROADMAP-HANDOFF.md`) hiç raporlanmamış, roadmap açısından kritik bir bulgu.

---

## 8. Ground-truth Contract State

**Dosya:** `tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/prediction-ground-truth.json` (yalnızca 5 kayıtlık preview fixture — 1000 kayıtlık "full" versiyon repoda **committed değil**, yalnızca converter çalıştırıldığında yerel `output/` altında üretiliyor).

**SAD §18.4 alan bazlı karşılaştırma:**

| SAD §18.4 Alanı | Gerçek JSON'daki Karşılığı | Uyum |
|---|---|---|
| `actual_total_working_lead_time_minutes` | `totalDeliveryDurationMinutes` | **Uyumsuz** — farklı isim, "working minutes" mi yoksa takvim dakikası mı olduğu belirsiz (muhtemelen ham takvim farkı, çalışma takvimi düzeltmesi yok) |
| `actual_production_start`/`actual_production_end` | `productionStartDate`/`productionFinishDate` | **Kısmi** — kavramsal karşılığı var, isim (`actual_` öneki yok, "Finish" ≠ "End") ve format (camelCase vs snake_case) uyumsuz |
| Paketleme başlangıç/bitiş | `packagingStartDate`/`packagingFinishDate` | **SAD'de hiç yok** — converter'ın kaynak Excel'den türettiği, SAD §18.4'ün öngörmediği ekstra bir alan çifti |
| `actual_shipping_date` | **Yok** — yalnızca `shippingDurationMinutes` (süre, tarih değil) var | **Uyumsuz** |
| `actual_delivery_date` | `estimatedDeliveryDate` (isim "tahmin" çağrıştırıyor, gerçekleşen mi hedef mi belirsiz) | **Uyumsuz/belirsiz** |
| `delivered_late` (boolean) | **Yok** | **Uyumsuz** |
| Leakage ayrımı (feature'da gelecek bilgisi olmaması) | `test_25_leakage_fields_not_in_seed` testi geçiyor (T-352) | **Uyumlu** — ayrım prensibi doğru uygulanmış, yalnızca isimlendirme SAD ile hizalı değil |

**Genel değerlendirme: Kısmi uyumlu.** Kavramsal olarak SAD'nin istediği bilgi türleri (üretim başlangıç/bitiş, teslimat, gecikme) büyük ölçüde mevcut ve leakage-ayrımı prensibi doğru uygulanmış — ama **isimlendirme (`actual_` öneki, snake_case, `delivered_late` boolean) SAD ile birebir örtüşmüyor** ve SAD'nin istediği bazı alanlar (`actual_shipping_date`, `delivered_late`) hiç yok, buna karşın SAD'de olmayan `packaging*` alanları eklenmiş. Bu, SAD'nin güncellenmesi mi yoksa ground-truth şemasının SAD'ye göre yeniden adlandırılması mı gerektiğine dair bir karar noktası.

---

## 9. Corrected Runtime Integration Boundary

Bu ayrım önceki handoff'ta zaman zaman bulanıklaşmış olabilir — burada kesin çizgi:

- **Mock ERP runtime integration** (`seed → IErpDataProvider`): **TAMAMLANDI.** 1000 siparişlik gerçek seed, `MockErpDataStore`, 7 controller, `MockErpDataProvider` (retry/timeout/logging ile), `IErpDataProvider`'ın 9 metodu — hepsi gerçek `dotnet test` ile doğrulandı (`RuntimeSeedProviderIntegrationTests`). Yalnızca `App.Api`'nin composition root'unda DI kaydı eksikti (T-357 ile yerel olarak düzeltildi, merge edilmedi).
- **Prediction Pipeline integration** (`IErpDataProvider → Batch Reader → PredictionContext → engine`): **HİÇ BAŞLAMADI.** `IErpBatchReader`, `IClock`, Rule Engine'in tamamı, `PredictionOrchestrator`, AI sağlayıcı zinciri — hiçbiri yok. `PredictionContext.cs` yalnızca boş placeholder alanlar taşıyan bir kabuk.

**Bu iki zincir birbirinden tamamen bağımsız durumda** — biri (Mock ERP) production-kalitesinde tamamlanmış, diğeri (Prediction Pipeline) sıfırdan başlanacak.

---

## 10. Missing Components (bu supplement'te yeni doğrulananlar)

- AI Prediction katmanının tamamı (C# tarafı + Python model/training).
- Prediction'a ait tüm persistence (`PredictionResult`, `PredictionProviderResults`, `PredictionFactors`).
- `App.Api`'de tek bir controller/endpoint yok (`/api/predictions`, `/api/erp`, `/api/auth/login` dahil hiçbiri).
- **`App.Api` ve `MockErp.Api` Dockerfile'ları hâlâ placeholder — `docker compose up` gerçek uygulamayı hiç çalıştırmıyor.**
- EF Core migration'ların container başlangıcında otomatik uygulanması (SAD §14.3) — kod seviyesinde de yok.
- Frontend'in iş ekranlarının gerçek API'ye bağlanması (şu an tamamen statik/mockup).
- Frontend ve AI servisi için CI job'ı.
- Ground-truth şemasının SAD §18.4 ile isim/format uyumu.

---

## 11. Test Evidence

- `dotnet build` (develop, `1cd75ed`): **0 Uyarı, 0 Hata**
- `dotnet test` (develop, `1cd75ed`): **143/143 başarılı** (App.Domain.Tests 33, App.Application.Tests 22, App.Infrastructure.Tests 9, App.Api.Tests 15, App.Integration.Tests 64)
- AI/Prediction/API katmanları için test sayısı: **0** (yazılacak kod yok)
- Frontend testleri: **0** (`package.json` test script'i placeholder)
- Docker Compose gerçek çalıştırma testi: **yapılmadı** (bu oturumun kapsamında değildi; yapılsaydı `api`/`mock-erp` container'larının sleep-placeholder olduğu görülürdü)

---

## 12. Roadmap-Relevant Blockers

1. **`App.Api`/`MockErp.Api` Dockerfile'ları placeholder** — herhangi bir Docker Compose demosu şu an imkansız. Bu, T-305/Rule Engine'den bile önce, roadmap'in "çalışan bir demo" hedefi için muhtemelen daha acil bir blocker.
2. **Prediction Pipeline integration'ın tamamı (T-305 dahil) sıfırdan** — `ROADMAP-HANDOFF.md`'de zaten raporlandı, burada teyit edildi.
3. **Hiçbir API endpoint'i yok** — Rule Engine/AI tamamlansa bile, onları dışarı açacak tek bir controller mevcut değil.
4. **AI Prediction katmanı %0** — yalnızca health-check iskeleti.
5. **Ground-truth şemasının SAD §18.4 ile isim uyumsuzluğu** — AI eğitimine geçilmeden önce çözülmesi gereken, düşük maliyetli ama gözden kaçabilecek bir netleştirme.
6. **Persistence'ta prediction tablolarının hiç migrate edilmemiş olması** — Rule Engine/AI sonuçlarının kalıcı hale getirilmesi için önce bu şemanın kurulması gerekiyor.

*Bu rapor yalnızca analizdir — hiçbir kod değişikliği, branch, commit veya task oluşturulmadı.*
