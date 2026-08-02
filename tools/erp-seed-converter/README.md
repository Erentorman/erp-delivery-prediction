# erp-seed-converter

Furniture ERP Excel verisini Mock ERP seed formatına dönüştüren araç.

## Klasör Yapısı (kullanıcı önerisinden küçük sapma — gerekçeli)

```
tools/erp-seed-converter/
├── convert.py
├── README.md
├── config/
│   └── mvp-assumptions.v1.json
├── reports/
│   ├── roadmap.md
│   ├── seed-coverage-report.md
│   ├── field-mapping-report.md
│   ├── assumption-decision-report.md
│   ├── converter-algorithm-report.md
│   └── task-impact-report.md
└── output/
    ├── preview/
    │   ├── mock-erp-seed.json
    │   ├── prediction-ground-truth.json
    │   └── material-dictionary-provisional.json
    └── full/
        ├── mock-erp-seed.json
        ├── prediction-ground-truth.json
        ├── material-dictionary-provisional.json
        └── validation-report.json
```

**Sapma ve gerekçesi:** Önerilen yapıda `output/` altında `mock-erp-seed.json` ve `preview-mock-erp-seed.json` yan yana duruyordu. Bunun yerine `output/preview/` ve `output/full/` alt klasörleri kullanıldı — amaç, hangi dosyanın "önizleme" hangisinin "tam/gerçek" olduğunu dosya adına değil klasör yoluna bağlamak, böylece biri diğerinin yerine yanlışlıkla kullanılamasın (özellikle gerçek repoya kopyalama sırasında).

## Kurulum ve Kullanım

**Gereksinimler:**
- Python 3.10+ (Minimum sürüm)
- Orijinal ERP Excel dosyası (**ÖNEMLİ:** Güvenlik ve veri gizliliği kuralları gereği, orijinal Excel dosyası kesinlikle bu repository'ye eklenmemeli, repository dizini dışında veya `.gitignore` kapsamındaki bir klasörde tutulmalıdır.)

**Kurulum (Sanal Ortam):**
```bash
cd tools/erp-seed-converter
python3 -m venv .venv
source .venv/bin/activate  # Mac/Linux
# Windows için: .venv\Scripts\activate

pip install -r requirements.txt
pip install -r requirements-dev.txt
```

**Kullanım:**
Excel yolunu (`--xlsx`) her zaman kendi makinenizdeki yerel bir tam yol (veya göreceli yol) olarak vermelisiniz. Çıktılar (`--out`) varsayılan olarak `output/full` veya `output/preview` altına, config ise `config/mvp-assumptions.v1.json` olarak belirtilir.

```bash
# Tam Dönüşüm
python3 convert.py --xlsx /kendi/yerel/yolunuz/Furniture_ERP_Data_Minutes.xlsx --config config/mvp-assumptions.v1.json --out output/full

# Önizleme Dönüşümü (Sadece ilk 5 sipariş)
python3 convert.py --xlsx /kendi/yerel/yolunuz/Furniture_ERP_Data_Minutes.xlsx --config config/mvp-assumptions.v1.json --out output/preview --limit-orders 5
```

## Önemli Sınırlar
- Bu araç `mock-erp-seed.json`'ı üretir ama **gerçek `MockErpDataStore.cs` tarafından deserialize edilebildiğini bu ortamda kanıtlayamaz** (.NET runtime yok). Gerçek smoke test (T-356) ayrıca yapılmalı.
- `openPurchaseOrders`, `workOrders`, `capacityCalendar.*`, `shippingDurations` **bilinçli olarak boş** — kaynakta veri yok, uydurulmadı.
- `mvp-assumptions.v1.json` şu an hiçbir çalışan koda bağlı değil; gelecekteki bir Application-katmanı fallback mekanizması için hazırlanmış bir taslaktır.
- **Önizleme (Preview) Modu stockLevels Davranışı:** `--limit-orders N` parametresi yalnızca siparişleri (SalesOrders) ve bunlara bağlı ground-truth örneklemesini sınırlar. Stok seviyeleri (`stockLevels`) ise, ERP sisteminin tüm ürünler için anlık stok görünümünü yansıtması gerektiğinden, filtre uygulanmamış **TAM ProductionOrders** tablosundan hesaplanır. Böylece 5 sipariş limitli bir önizlemede bile stok tablosu eksiksiz (tüm ürünleri içerecek şekilde) oluşur.

## Validasyon ve Çıkış Kodları (Exit Codes)
Converter her çalıştığında veriyi otomatik olarak denetler ve çıktı klasörüne `validation-report.json` dosyasını bırakır. (Bu rapor daha önceden ad hoc üretilmekteyken artık her çalışmada dinamik ve kalıcı olarak üretilmektedir).
- **Exit Code 0:** Dönüşüm ve validasyon tamamen başarılı.
- **Exit Code 1:** Validasyon sırasında fatal hata (ör: eksik alanlar, geçersiz referanslar, duplicate ID'ler veya slug çakışmaları) oluştu. Rapor yine de yazılır ancak süreç hata ile sonlanır.
- **Warnings (Uyarılar):** Validasyon sürecini durdurmaz (Exit 0 döner), ancak rapordaki `warnings` dizisine kaydedilir.

Detaylar için `reports/` klasörüne bakınız.
