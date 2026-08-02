# Roadmap — ERP → Seed Dönüşümü

## Adım 1 — Repository Sözleşme Doğrulaması *(bu çalışmada yapıldı)*
- **Amaç:** Gerçek `MockErpDataStore/Models/TransportModels/DataProvider/IErpDataProvider/ErpReadDtos` dosyalarına dayanarak hedef şemayı sabitlemek.
- **Değişen/Oluşturulan:** Yok (yalnızca okuma).
- **Bağımlılık:** Yok.
- **Ön koşul:** Yok.
- **Paralel yapılabilir:** T-353 (Field Mapping Report) ile birlikte.
- **Risk:** `PredictionContext.cs`/T-305 eksikliği — sözleşme %100 kesinleşmedi.
- **Kabul kriteri:** 8 kök alanın gerçek `MockErpDataStore.cs` null-check/deserialize davranışıyla eşleştiği gösterildi.
- **Validation:** Kod okuma + Python simülasyon testleri.
- **Task:** T-345 (kısmi).
- **DoD:** PredictionContext/T-305 gelene kadar "kesin" değil, "kısmi doğrulandı" sayılır.

## Adım 2 — Converter Tasarımı ve Config Şeması *(bu çalışmada yapıldı)*
- **Amaç:** `mvp-assumptions.v1.json` şemasını ve `convert.py` algoritmasını üretmek.
- **Değişen/Oluşturulan:** `config/mvp-assumptions.v1.json`, `convert.py`.
- **Bağımlılık:** Adım 1.
- **Paralel:** Faz 3 raporlarıyla birlikte yazılabilir.
- **Risk:** Capacity/Shipping config'inin seed'e mi Application'a mı gideceği kararı henüz yok (Decision Required #1).
- **Kabul kriteri:** Config, kod içine gömülü değil, dışarıdan `--config` parametresiyle okunuyor.
- **Validation:** `convert.py --config <path>` çağrısının farklı config dosyalarıyla farklı (ama deterministik) çıktı ürettiği gösterildi.
- **Task:** T-353, T-354.
- **DoD:** Kod + config dosyası repository'de versiyonlanabilir durumda.

## Adım 3 — Preview Dönüşümü ve Doğrulama *(bu çalışmada yapıldı)*
- **Amaç:** 5 sipariş + tüm ürün/BOM ile küçük ölçekli doğrulama.
- **Oluşturulan:** `output/preview/mock-erp-seed.json`, `prediction-ground-truth.json`, `material-dictionary-provisional.json`.
- **Bağımlılık:** Adım 2.
- **Risk:** Küçük örneklemde stockLevels'ın tüm ürünleri kapsamayabileceği (gözlemlendi: 5 siparişte yalnızca 3 ürün çıktı — beklenen davranış).
- **Kabul kriteri:** 23/23 doğrulama kontrolü PASS.
- **Validation:** Python script (kayıt sayısı, referans bütünlüğü, leakage, deterministiklik).
- **Task:** T-355 (kısmi, preview kapsamında).
- **DoD:** Preview onaylandı, tam dönüşüme geçildi.

## Adım 4 — Tam Dönüşüm ve Doğrulama *(bu çalışmada yapıldı)*
- **Amaç:** 1000 siparişin tamamını dönüştürmek.
- **Oluşturulan:** `output/full/mock-erp-seed.json`, `prediction-ground-truth.json`, `validation-report.json`.
- **Bağımlılık:** Adım 3.
- **Kabul kriteri:** 31/31 doğrulama kontrolü PASS.
- **Validation:** Aynı script, tam veri kümesi üzerinde.
- **Task:** T-355.
- **DoD:** Rapor üretildi; **gerçek C# smoke test hâlâ eksik** (T-356), bu adım onu kapatmıyor.

## Adım 5 — Gerçek Smoke Test *(bu ortamda YAPILAMADI)*
- **Amaç:** `mock-erp-seed.json`'ın gerçek `MockErpDataStore.cs` tarafından hatasız yüklendiğini kanıtlamak.
- **Ön koşul:** .NET ortamı (bu konteynerde yok).
- **Task:** T-356.
- **DoD:** `dotnet test` (veya `MockErpDataStore` constructor'ının manuel çağrılması) hatasız tamamlanmalı. **Bu adım sizin/Claude Code'un .NET ortamında yapılması gerekiyor.**

## Adım 6 — Blocker Kapatma Sırası (T-347 → T-348 → T-349/T-351 → T-350)
- Bağımlılık zinciri önceki turdaki task planıyla (T-345–T-352) aynı kalıyor; bu roadmap yalnızca "hangi kısmın bu ortamda gerçekten ilerletildiği"ni ekliyor.
