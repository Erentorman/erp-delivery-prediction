# Task ve Mimari Etki Raporu (T-345 → T-356)

| Task | Bu Çalışma Neyi Karşıladı | Değişen/Üretilen Dosya | Tamamlanabilir mi? | Bekleyen Bağımlılık | Eksik Test | Notlar |
|---|---|---|---|---|---|---|
| **T-345** Seed Contract | `orders/products/boms/stockLevels` için kök yapı fiilen üretildi ve doğrulandı. **Ancak** `PredictionContext.cs`/T-305 hâlâ yok — sözleşme kesinleşmiş sayılamaz | `mock-erp-seed.json` (full+preview), `convert.py` | **Kısmen** — yalnızca üretilen 3 alan için "üretim kanıtlandı" denebilir, tüm sözleşme için hayır | T-305, `PredictionContext.cs` | Gerçek `dotnet test` ile `MockErpDataStore` smoke test'i (T-356) | Bu turda "tamamlandı" ilan edilmiyor |
| **T-346** Priority & Category | Category: SAD §9.9 gereği hâlâ gerekli, kaynak (Excel'de yok, config'te `priorityValueCrosswalk` yalnızca Priority için var) — **Category için config'e madde eklenmedi, eksik kaldı**. Priority: crosswalk hazırlandı ama model alanı yok | `mvp-assumptions.v1.json` (yalnızca Priority) | **Hayır** — model değişikliği (C# kodu) gerektiriyor, converter'ın işi değil | Backend ekibi: `MockErpOrder`/`MockErpProduct` model genişletmesi | Model değişikliği sonrası mapping testi | Category crosswalk'ı bir sonraki turda eklenmeli |
| **T-347** BOM Component Dictionary | 34 benzersiz malzeme, deterministik `MAT-` kodu üretildi ve 1000 kayıt üzerinde tutarlılık doğrulandı | `material-dictionary-provisional.json` (full) | **Hayır — provizyonel.** ERP uzmanı onayı almadı | ERP uzmanı sign-off | Gerçek Uyumsoft kodlarıyla çakışma kontrolü | Önceki turdaki "33 malzeme, tamamlandı" iddiası **doğrulanamamıştı**; bu çalışma gerçek sayının **34** olduğunu kanıtladı |
| **T-348** Routing & Operations | Yalnızca gözlem: Routing sheet'ten OP/IM referans+sıra çıkarılabilir olduğu teyit edildi. `workOrders[]` **üretilmedi** (StandardDurationMinutes kaynağı yok) | — | **Hayır** | ERP uzmanı: operasyon bazlı süre verisi | — | Blocker aynen duruyor |
| **T-349** Capacity & Calendar | MVP fallback değerleri **config'e** yazıldı (seed'e değil — mimari karar bekliyor, bkz. Assumption Decision Report §Decision Required #1) | `mvp-assumptions.v1.json` | **Hayır** | Config'in nerede tüketileceğine dair mimari karar + gerçek entegrasyon kodu | Fallback mekanizması testi (henüz yok) | Önceki turdaki "blok kalktı" iddiası **yanlıştı** — hâlâ açık, yalnızca hazırlık yapıldı |
| **T-350** Inventory & Open PO | `stockLevels[]` gerçekten üretildi (karar: en son sipariş anlık görüntüsü). `openPurchaseOrders[]` bilinçli boş bırakıldı (miktar verisi yok) | `mock-erp-seed.json` içindeki `stockLevels` | **Kısmen** — yalnızca stockLevels | `OpenQuantity` kaynağı (ERP uzmanı) | Referans bütünlüğü testi zaten geçti | openPurchaseOrders hâlâ blocker |
| **T-351** Shipping Lookup | MVP fallback değeri (medyan bazlı) config'e yazıldı; seed'e yazılmadı | `mvp-assumptions.v1.json` | **Hayır** | Aynı mimari karar (T-349 ile ortak) | — | Önceki turdaki "blok kalktı" iddiası burada da **yanlıştı** |
| **T-352** AI Dataset Separation | Fiilen uygulandı: `prediction-ground-truth.json` ayrı üretildi, seed içinde leakage alanı olmadığı otomatik testle doğrulandı (31/31 PASS) | `prediction-ground-truth.json` (1000 kayıt) | **Evet — bu çalışma kapsamında karşılandı** | Train/validation ayrımı (kullanıcının önerdiği backtesting tasarımı) hâlâ ayrı bir karar | Gerçek AI servisi tarafında bu dosyanın nasıl tüketileceği testi yok | İlk gerçek "tamamlanabilir" task |
| **T-353** Excel Mapping Matrix | Bu 4 rapor (Seed Coverage/Field Mapping/Assumption Decision/Converter Algorithm) bu task'ın çıktısı sayılabilir | 4 rapor dosyası | **Evet** | — | — | Saat→dakika maddesi düştü (kaynak artık dakika) |
| **T-354** Python Converter | `convert.py` yazıldı, çalıştırıldı, hem preview hem full modda deterministik sonuç üretti | `convert.py` | **Evet — bu çalışma kapsamında karşılandı** | — | Birim testleri (pytest) henüz yazılmadı | Kod var ama otomatik test yok |
| **T-355** JSON Validation | 31 kontrol (kayıt sayısı, tekrar, null, referans bütünlüğü, leakage) çalıştırıldı ve raporlandı | `validation-report.json` | **Evet — bu çalışma kapsamında karşılandı** | Gerçek C# deserialize testi (T-356) | .NET runtime yokluğu nedeniyle gerçek smoke test yapılamadı | Python simülasyonu, gerçek testin yerini tutmaz |
| **T-356** Mock ERP Smoke Test | **Yapılamadı** — bu ortamda `.NET` yok | — | **Hayır** | .NET ortamı (gerçek repo/CI) | `dotnet test` çalıştırılmalı | Açıkça bloklu bırakıldı, "yapıldı" denmedi |

## Genel Değerlendirme
- **Gerçekten tamamlanabilir sayılan:** T-352, T-353, T-354, T-355 (dosyalar üretildi, testler bu ortamda çalıştırıldı ve geçti).
- **Kısmen ilerleyen ama tamamlanmayan:** T-345, T-350.
- **Hâlâ tamamen bloklu:** T-346 (model değişikliği gerekiyor), T-347 (ERP onayı gerekiyor), T-348, T-349, T-351, T-356.
- **Önceki turlarda "tamamlandı/blok kalktı" olarak sunulan ama bu çalışmada gerçek karşılığı bulunamayan iddialar:** T-347 (33 vs 34 malzeme farkı), T-349, T-350, T-351 ("blok kalktı" iddiaları) — bu çalışma bunların **gerçek durumunu** yeniden kurdu.

## Sıradaki Doğru Uygulama Sırası
1. Assumption Decision Report'taki 2 "Decision Required" maddesinin karara bağlanması (özellikle config'in seed'e mi Application'a mı gideceği).
2. T-347 malzeme sözlüğünün ERP uzmanına onaylatılması.
3. `PredictionContext.cs`/T-305'in tamamlanması ve bu çalışmayla çapraz doğrulanması.
4. .NET ortamında gerçek `dotnet test` ile T-356'nın kapatılması.
5. Ancak bunlardan sonra T-348 (operasyon süresi) ve T-349/T-351 (capacity/shipping) için ERP uzmanından gerçek veri talebi.
