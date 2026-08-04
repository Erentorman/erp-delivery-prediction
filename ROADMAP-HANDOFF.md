# Roadmap Handoff — ERP Delivery Prediction

*Bu doküman, Sonnet 5 oturumundan yeni bir Opus oturumuna devir amacıyla hazırlanmıştır. Tüm bulgular gerçek `git`/`dotnet`/`pytest`/`gh` komut çıktılarıyla doğrulanmıştır — hiçbiri tahmin veya önceki raporların körü körüne tekrarı değildir. Bu oturumda kod/dosya değişikliği yapılmamıştır (yalnızca bu handoff dosyası yazılmıştır).*

---

## 1. Güncel Branch ve Develop Durumu

- **Repo kökü:** `/Users/erentorman/Documents/Projects/erp-delivery-prediction`
- **`develop`** şu an `origin/develop` ile birebir aynı: **`1cd75ed`** (Merge PR #25)
- **`main`**, `develop`'tan hâlâ **kontrolsüz şekilde sapmış durumda** (bkz. Bölüm 13) — bu, henüz çözülmemiş bir governance sorunu, roadmap'e engel değil ama not edilmeli.
- **Açık, henüz push edilmemiş yerel çalışma var:**
  - `feature/t-357-mockerp-di-config` — **yalnızca yerelde**, commit `d53b4cc` ("fix(integration): register Mock ERP provider in App.Api"), **upstream yok, PR açılmadı, push edilmedi.** Yeni oturum bu branch'i push edip PR açmayı bir sonraki adım olarak değerlendirmeli.
  - `feature/t-354-task-13-python311-ci` — merge edildi (PR #25), artık `develop`'un parçası, temizlenebilir (lokal branch silinebilir).

---

## 2. Merge Edilmiş Son PR'lar (kronolojik, en yeniden eskiye)

| PR | Başlık | Hedef | Durum |
|---|---|---|---|
| #25 | T-354/T-355: Add Python 3.11 converter tests to CI | develop | MERGED |
| #24 | T-354: Integrate validated full runtime seed | develop | MERGED |
| #23 | T-354/T-355: Add ERP seed converter and validate full conversion | develop | MERGED |
| #22 | T-356: Add preview seed deserialize smoke test | develop | MERGED |
| #21 | Revert "T-354 Task 10..." | **main** | MERGED (revert) |
| #19 | T-354 Task 10: Add preview seed deserialize smoke test | **main** | MERGED, sonra #21 ile revert edildi |
| #18 | Feature/t-354-erp-seed-converter (klasör iskeleti) | **main** | MERGED (develop'u atlayarak) |
| #17 | T-201: Add JWT generation and validation infrastructure | develop | MERGED |
| #16 | T-304 Implement Mock ERP HttpClient data provider | develop | MERGED |

**#18 ve #19'un `main`'e doğrudan merge edilmesi, `main`'in `develop`'tan sapmasının kök nedenidir** (Bölüm 13).

---

## 3. T-345–T-357 Taskları — Gerçek Durum

| Task | Durum | Kanıt/Not |
|---|---|---|
| **T-345** Seed Contract | **Done** | Karar: kabul ölçütü schema contract, business-data coverage değil. 8 kök alan mevcut, null değil, referans bütünlüğü test ediliyor, gerçek C# deserialize + runtime provider testi geçiyor. |
| **T-346** Priority & Category | **Kısmen açık** | Priority: crosswalk hazır (`mvp-assumptions.v1.json`), model alanı yok. Category: Faz-2/Superseded kararı verildi (Bölüm 11) — henüz SAD'ye işlenmedi. |
| **T-347** Material Dictionary | **Provizyonel** | 34 benzersiz malzeme, deterministik `MAT-` kodu üretildi. ERP uzmanı onayı bekliyor. |
| **T-348** Routing & Operations | **Blocked** | `workOrders[]` boş — operasyon süresi/predecessor kaynağı Excel'de yok. |
| **T-349** Capacity & Calendar | **Blocked** | `capacityCalendar.*` tamamen boş. Fallback config'te (`mvp-assumptions.v1.json`) ama koda hiç bağlanmadı. |
| **T-350** Inventory & Open PO | **Kısmen** | `stockLevels` dolu (4 kayıt, "son sipariş = güncel stok" yorumu). `openPurchaseOrders` boş — miktar verisi yok. |
| **T-351** Shipping Lookup | **Blocked** | Karar netleşti (İstanbul-origin + 4 destinasyon lookup, Bölüm 10) ama seed/config henüz buna göre yeniden yapılandırılmadı. |
| **T-352** AI Dataset Separation | **Done** | `prediction-ground-truth.json`, leakage testi geçiyor. |
| **T-353** Excel Mapping Matrix | **Done** | 4 rapor dosyası (`tools/erp-seed-converter/reports/`). |
| **T-354** Python Converter | **Done** | Python 3.11 ile **41/41 pytest PASS**, CI'da doğrulandı (PR #25, iki job da yeşil). |
| **T-355** JSON Validation | **Done** | Aynı CI doğrulamasının parçası. |
| **T-356** Mock ERP Smoke Test | **Done** | `ErpSeedConverterPreviewSmokeTests.cs` + `RuntimeSeedProviderIntegrationTests.cs`, gerçek `dotnet test` ile doğrulandı. |
| **T-357** *(geçici numara)* Mock ERP DI/Config | **Yerel commit'te tamamlandı, merge edilmedi** | `d53b4cc`, push edilmedi. Gerçek Linear numarası atanmalı. |

---

## 4. Mock ERP Runtime Akışı (doğrulanmış zincir)

```
mock-erp-seed.json (1000 sipariş, gerçek Excel'den converter ile üretildi)
  → MockErpDataStore (ContentRootPath + "Data/mock-erp-seed.json", null-check + tekillik doğrulaması)
  → MockErp.Api Controller'ları (7 controller: Orders, Products[+bom alt-route], StockLevels,
     OpenPurchaseOrders, WorkOrders, CapacityCalendar, ShippingDurations)
  → [HTTP/JSON sınırı]
  → MockErpDataProvider (App.Integration — HttpClient, retry/timeout, IntegrationLogs)
  → IErpDataProvider (Application port, 9 metot)
  → [App.Api composition root — T-357 ile düzeltildi ama henüz merge edilmedi]
  → [Application consumer — HENÜZ YOK, bkz. Bölüm 7]
```

`App.Api/Program.cs`'de `AddMockErpDataProvider` kaydı **`develop`'ta hâlâ eksik** (düzeltme yalnızca yerel `feature/t-357-mockerp-di-config` branch'inde, push edilmedi).

---

## 5. Geçen Test Sonuçları (bu oturumda gerçek çalıştırma)

- **`.NET` (`develop`, `1cd75ed`):** `dotnet build` → 0 Uyarı/0 Hata. `dotnet test` → **143/143 başarılı** (33+22+9+15+64).
- **`.NET` (`feature/t-357-mockerp-di-config`, henüz merge değil):** 144/144 (yukarıdaki + yeni composition-root testi).
- **Python (`tools/erp-seed-converter`, Python 3.11.15):** **41 passed, 0 failed, 0 skipped** — hem yerel hem CI'da (GitHub Actions, PR #25, iki job da yeşil) doğrulandı.
- **Önceki "40/40 PASS" iddiası:** Python 3.9 ortamında **yeniden üretilemedi** (9 passed/9 failed/3 collection error) — kök neden `convert.py`'nin Python 3.10+ sözdizimiydi, ortam artık 3.11'e sabitlendiği için bu sorun kapandı.

---

## 6. Bilinçli Boş Seed Alanları (gerçek veri yokluğu, uydurulmadı)

| Alan | Durum | Gerekçe |
|---|---|---|
| `openPurchaseOrders` | 0 kayıt | Excel'de `OpenQuantity` hiç yok |
| `workOrders` | 0 kayıt | Operasyon süresi/predecessor verisi Excel'de yok |
| `capacityCalendar.workCenters/shifts/holidays/plannedDowntimes` | 4×0 | Excel'de kapasite/vardiya/tatil verisi tamamen yok |
| `shippingDurations` | 0 kayıt | Excel yalnızca gerçekleşmiş tekil süre veriyor, lookup yapısı yok |

Bu 4 kategori `MockErpDataStore`'un null-check kısıtını ihlal etmiyor (boş dizi kabul ediliyor) — yalnızca **iş verisi içeriği** eksik, şema değil (bkz. Bölüm 3, T-345 kararı).

---

## 7. T-305 ve PredictionContext — Gerçek Durum

- **T-305 (Batch Reader), `IErpBatchReader`, `ErpBatchReader`, `ErpBatchSnapshot`, `IClock`/`SystemClock`: hiçbiri repoda yok.** Önceki turlarda üzerinde anlaşılan 10 dosyalık plan hiç uygulanmadı.
- **`PredictionContext.cs`** var (`src/App.Domain/Prediction/`) ama `OrderInput`, `MaterialSnapshot`, `CapacitySnapshot`, `CalendarSnapshot`, `Operation` — **beşi de tamamen boş placeholder** sınıflar.
- **`IErpDataProvider`'ın Application katmanında hiçbir gerçek tüketicisi yok** — tek kullanıcısı kendi implementasyonu (`MockErpDataProvider`) ve testler.
- **Bu, projenin en büyük tek darboğazı:** 1000 kayıtlık, doğrulanmış, gerçek bir seed var ama onu `PredictionContext`'e dönüştürecek tek satır kod yok.

---

## 8. Rule Engine ve CPM — Gerçek Durum

**%0 — tamamen başlamamış.** Doğrulandı (`grep` sıfır sonuç):
- `ICriticalPathCalculator` — yok
- `MaterialAvailabilityStep`, `PurchaseLeadTimeStep`, `WorkingCalendarStep`, `WorkCenterCapacityStep`, `OperationDurationStep`, `ShippingDurationStep` — hiçbiri yok
- `IRuleBasedPredictionEngine` — yok
- `PredictionFactor` — yok

Yalnızca boş `IPredictionStep` sözleşmesi (T-502) ve 3 value object (`DateRange`, `Quantity`, `WorkingLeadTime`) mevcut.

---

## 9. Tarık Bey'in Kesin İş Kararları (onaylanmış, henüz dokümana işlenmedi)

1. **Capacity:** `machineCount` → Mock ERP work-center master data alanı. Operation duration → Mock ERP routing/operation data. Predecessor mevcut modelde zaten var. `setupDuration` MVP için zorunlu değil, eklenmeyecek.
2. **Procurement:** MVP varsayılan stok temin süresi **2 iş günü**. Seed'e sahte open PO yazılmayacak; gerçek `expected delivery date` varsa provider değeri, yoksa Application config/fallback resolver 2 iş günü uygulayacak. Fallback kullanımı prediction factor/audit/result metadata'da görünür olmalı. *(Not: procurement.defaultLeadTimeWorkingMinutes = 960 kararıyla 2 iş günü kesinleştirilmiştir.)*
3. **Shipping:** Sabit origin **İstanbul fabrikası**, **4 deterministik destinasyon** (İstanbul, Ankara, İzmir, Antalya), harita servisi/dış API/mesafe formülü yok, route lookup var, route bulunamazsa Application fallback var. Mevcut tek global `defaultShippingDurationMinutes` **yalnızca bilinmeyen-route fallback'i** olmalı, ana model değil.
4. **Work Center/Operation:** machineCount + operation duration **açık, görünür operational/master data** olarak temsil edilmeli — görünmez config varsayımı olarak gömülmeyecek.

---

## 10. Seed/Config/Fallback Mimari Kararı

- **Kural:** Mock ERP'nin gerçek ERP verisi görünümü bozulmayacak — MVP varsayımları **seed JSON'a değil**, ayrı `mvp-assumptions.v1.json`'a yazılıyor (zaten uygulanmış bir karar).
- **Açık nokta:** Bu config'in tüketileceği katman (Application fallback resolver) **henüz hiç yazılmadı** — `mvp-assumptions.v1.json` şu an `"consumedByCodeToday": false` diye kendi içinde işaretli.
- Procurement/Shipping/Capacity kararlarının hepsi bu "seed = gerçek veri, config = MVP fallback" ayrımına dayanıyor — resolver yazılmadan hiçbiri fiilen çalışmaz.

---

## 11. Product Category Kararı

- **Product category MVP kapsamında zorunlu değil.**
- SAD §9.9'da (satır 630, 247, 646 — AI Feature Contract) **opsiyonel/Faz-2** olarak işaretlenecek — henüz işlenmedi.
- `ProductReadDto.PlanningClassification` (nullable) **kaldırılmayacak** — sadece bu kararla kaldırma, zaten her zaman null dönüyor, zararsız.
- Converter/seed sahte category üretmeyecek (zaten üretmiyor).
- T-346, Priority kısmı ayrı kalacak şekilde revize edilecek — henüz yapılmadı.

---

## 12. ERP Uzmanlarından İstenecek Veri Başlıkları

1. Açık satın alma siparişi **miktarları** (`OpenQuantity`) — T-350/T-351 blocker'ı.
2. Operasyon bazlı **standart süre** ve **predecessor** verisi (routing/BOM operasyon detayı) — T-348 blocker'ı.
3. Gerçek kapasite/vardiya/tatil takvimi (iş merkezi bazlı) — T-349 blocker'ı.
4. **Gerçek sevkiyat süresi** — İstanbul → Ankara/İzmir/Antalya için (varsayım değil, gerçek/hedef değer) — T-351.
5. Malzeme sözlüğünün (`material-dictionary-provisional.json`, 34 kayıt) gerçek Uyumsoft kodlarıyla **çakışma kontrolü/onayı** — T-347.
6. Dini bayram (değişken tarihli) 2026 tatil günleri — `mvp-assumptions.v1.json`'da yalnızca sabit tarihli 7 tatil var, dini bayramlar dahil değil.

---

## 13. Açık Blocker'lar ve Riskler

1. **T-305/`IClock` hiç yok** — Rule Engine'in üzerine inşa edileceği temel taş eksik (kritik yol, bkz. önceki MVP progress raporu: ~%25-30 genel ilerleme).
2. **`feature/t-357-mockerp-di-config` push edilmedi** — Mock ERP DI/config düzeltmesi yerelde kilitli kaldı, `develop`'a hiç ulaşmadı.
3. **`main`/`develop` kontrolsüz sapması** (PR #18/#19 doğrudan `main`'e, #19 revert edildi) — hâlâ çözülmedi, ayrı bir governance task'ı gerektiriyor.
4. **Procurement/Shipping/Capacity fallback resolver'ı henüz yazılmadı** — kararlar netleşti ama koda hiç yansımadı.
5. **SAD güncellemeleri henüz yapılmadı** — §9.9 (product category), Mock ERP'nin kalıcı boş kategorileri, shipping modeli SAD'de hâlâ eski/eksik.
6. **T-346 (Category kısmı) ve T-351 revizyonu** dokümana işlenmedi.

---

## 14. Yeni Opus Oturumuna Verilecek Repository Analiz Görevi

Yeni oturum şu sırayla ilerlemeli:

1. **Doğrulama:** `git status`, `git log origin/develop --oneline -5`, `dotnet build && dotnet test` ile bu handoff'un hâlâ güncel olduğunu teyit et (zaman geçmiş olabilir).
2. **`feature/t-357-mockerp-di-config`'i push edip PR aç** (develop hedefli) — bu, üzerinde çalışılan en son tamamlanmış ama teslim edilmemiş iş.
3. **Karar dokümantasyonu turu:** Bölüm 9-11'deki kesinleşmiş kararları `docs/SAD-v1.1.md` (§9.9, shipping modeli, Mock ERP boş kategoriler) ve `tools/erp-seed-converter/config/mvp-assumptions.v1.json`'a işle — kod değişikliği değil, karar + config senkronizasyonu.
4. **Task rebaseline dosyasını güncelle** (Bölüm 3'teki tabloyu resmi bir task-tracking dosyasına/Linear'a yansıt).
5. **Ancak bunlardan sonra** 4 günlük roadmap taslağını hazırla — ERP uzmanı verisi bekleyen başlıkları (Bölüm 12) roadmap'te **açık bağımlılık** olarak işaretleyerek, onları bloke etmeyen paralel bir sıra kur (örn. T-305/IClock + Rule Engine iskeleti, ERP uzmanı verisini beklemeden başlanabilir).
6. Roadmap'in kritik yolu **T-305 → PredictionContext'in gerçek doldurulması → Step Pipeline + CPM → Final Hybrid → API endpoint'leri** olmalı; bunların hiçbiri atlanamaz.

*Kod yazma/dosya değiştirme bu handoff'ta yapılmadı — yalnızca bu dosya oluşturuldu.*

Önemli güncelleme: ROADMAP-HANDOFF.md oluşturulduktan sonra
feature/t-357-mockerp-di-config branch’i push/PR/merge edildiyse,
handoff içindeki “henüz push edilmedi” bilgisi artık geçersizdir.
Güncel develop durumunu esas al. 