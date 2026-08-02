# Seed Coverage Report

Kaynak: `Furniture_ERP_Data_Minutes.xlsx`. Hedef sözleşme: `MockErpDataStore.cs` (tek `mock-erp-seed.json`, 8 zorunlu kök anahtar).

| Kök Alan | Durum | Gerekçe |
|---|---|---|
| `orders` | **1 — Kaynaktan doğrudan üretildi** | SalesOrders sheet, 1000/1000 satır, 0 null |
| `products` | **1 — Kaynaktan doğrudan üretildi** | Yeni `Products(ürün kartı)` sheet, 4/4 satır |
| `boms` | **2 — Kaynak veriden deterministik türetildi** | BOM sheet grid parse + malzeme adı→kod slug (34 benzersiz malzeme, T-347 onayı bekliyor) |
| `stockLevels` | **2 — Kaynak veriden deterministik türetildi (karar uygulanmış)** | Excel ürün başına tek değil, sipariş başına çoklu anlık görüntü veriyor; "en son (OrderDate) anlık görüntü = güncel stok" kararı uygulandı. `reservedQuantity=0` varsayımı config'te belgelendi |
| `openPurchaseOrders` | **4 — Bilinçli boş bırakıldı** | `StockOrderRequired`/`StockOrderDate` var ama açık PO miktarı (`OpenQuantity`) Excel'de hiç yok; uydurulmadı |
| `workOrders` | **4 — Bilinçli boş bırakıldı** | Operasyon bazlı `StandardDurationMinutes` ve `PredecessorOperationReferences` Excel'de yok |
| `capacityCalendar.workCenters/shifts/holidays/plannedDowntimes` | **4 — Bilinçli boş bırakıldı** | Excel'de kapasite/vardiya/tatil verisi tamamen yok. MVP fallback değerleri **seed'e değil**, `mvp-assumptions.v1.json`'a yazıldı (bkz. Assumption Decision Report) |
| `shippingDurations` | **4 — Bilinçli boş bırakıldı** | Excel yalnızca gerçekleşmiş tekil süre veriyor, Origin/Destination/Profile lookup yapısı yok |

**Genel değerlendirme:** 8 kök alandan 2'si tam, 1'i kısmi karar uygulanarak dolduruldu, 5'i gerçek veri yokluğu nedeniyle bilinçli boş. Boş bırakılan alanlar `MockErpDataStore`'un null-check kısıtını ihlal etmiyor (boş dizi kabul ediliyor), yalnızca içerik olarak eksik — bu T-348/T-349/T-350/T-351'in konusu.
