# Converter Algorithm Report

## Adımlar (convert.py)

1. **Config yükle** (`--config` parametresi, `mvp-assumptions.v1.json`).
2. **Excel'i oku** (4 sheet: SalesOrders, Products, BOM(ham grid), ProductionOrders) — yalnızca okuma, orijinal dosyaya yazma yok.
3. **`products[]` üret** — `Products(ürün kartı)` sheet'inden doğrudan (`ProductCode`→id, `ProductName`→name) + config'ten `unit`.
4. **`boms[]` üret** — 4 sabit grid bloğu (satır/kolon offset) parse edilir; malzeme adı ilk görüldüğünde deterministik slug (`MAT-<ASCII-UPPER-SLUG>`) üretilir ve bir sözlükte (`material_dictionary`) saklanır — aynı ad her zaman aynı kodu alır.
5. **`orders[]` üret** — SalesOrders satırları, `Product` adı → `ProductCode` lookup ile eşlenir. Eşleşmeyen varsa `warnings`'e düşer (converter durmaz, uyarı olarak raporlanır).
6. **`stockLevels[]` üret** — ProductionOrders, `OrderDate`'e göre sıralanır, ürün başına **en son** satır alınır (`groupby(...).tail(1)`), `Reserved=0`/`Available=OnHand` config varsayımı uygulanır. Bu adım `--limit-orders` filtresinden önce tam veriden beslenir. Bütün ürünler (4 ürün) için stok daima üretilir.
7. **`prediction-ground-truth.json` üret** — leakage alanları (gerçekleşmiş tarihler + 6 süre kolonu + sipariş-anı stok/yük anlık görüntüleri) buraya yazılır, **seed'e hiç dahil edilmez**.
8. **Kalan 5 alan** (`openPurchaseOrders`, `workOrders`, `capacityCalendar.*`, `shippingDurations`) **boş dizi** olarak yazılır — kaynakta veri olmadığı için.
9. JSON'lar `ensure_ascii=False, indent=2` ile yazılır (Türkçe karakterler korunur, okunabilir format).

## Deterministiklik Garantisi
- Girdi verisi (Excel + config) değişmediği sürece çıktı **byte-byte aynıdır** (SHA256 karşılaştırmasıyla doğrulandı — Faz 6).
- Rastgelelik, zaman damgası, GUID kullanılmıyor.
- Slug fonksiyonu saf/deterministik (girdiye göre çıktı sabit).

## Durdurma Koşulları (converter ne zaman durur/uyarır)
- **Durdurma (Fatal Error - Exit Code 1):** Aşağıdaki durumlardan herhangi biri oluştuğunda süreç durur:
  - Sales order veya BOM product'ı `Products` tablosunda bulunamazsa.
  - Aynı ID'den birden fazla üretiliyorsa (Duplicate ID).
  - Referans hataları (`productId` veya `componentId` bulunamaması).
  - İki farklı malzeme adı aynı `componentId` slug'ına dönüşüyorsa (Slug collision).
  - Zorunlu root alanlar null ise.
  - Beklenen sheet veya zorunlu kolon bulunamazsa.
  - BOM grid parse beklenen blokları üretmezse.
- Dönüşüm (mekanik adım) sırasında tespit edilen eksik veya hatalı alanlar artık sessizce atlanmaz (`continue`), hatalar toplanıp `validation-report.json` içine "errors" olarak yazılır ve çıkış kodu `1` döner.

## Genişletme Noktaları (gelecek task'lar için)
- `workOrders[]` üretimi: T-348 kararları netleşince (operasyon süresi kaynağı + predecessor kuralı) buraya yeni bir `build_work_orders()` fonksiyonu eklenecek.
- `openPurchaseOrders[]`: T-350 netleşince (`OpenQuantity` kaynağı belirlenince) eklenecek.
- Capacity/Shipping: Eğer Faz 4'teki mimari karar "config → seed'e de yazılsın" yönünde değişirse, `capacityCalendar`/`shippingDurations` için ayrı `build_capacity_calendar(config)` / `build_shipping_durations(config)` fonksiyonları eklenebilir (şu an bilinçli olarak yazılmadı).
