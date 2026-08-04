# Assumption Decision Report

Bu rapor yalnızca `mvp-assumptions.v1.json` içindeki değerlerin gerekçesini taşır. **Bu config şu an hiçbir C# koduna bağlı değildir** — `PredictionContext`/Step sınıfları (`WorkingCalendarStep`, `WorkCenterCapacityStep`, `PurchaseLeadTimeStep`, `ShippingDurationStep`) doğrulanmadığından, bu değerlerin gerçekte nasıl tüketileceği ayrı bir entegrasyon kararıdır.

**Mimari not:** Bu değerler bilerek **seed JSON'a değil**, ayrı bir config dosyasına yazıldı. Çünkü Mock ERP'nin "gerçek ERP verisi" görünümünü bozmadan (SAD §1.3 "veri kaynağından bağımsızlık" ilkesi), MVP varsayımlarının nerede devreye gireceği (muhtemelen Application/Integration katmanında bir fallback resolver) ayrı bir mimari karardır.

| Alan | Önerilen Değer | Gerekçe | Kaynak Karşılığı (SAD/task/test) | Alternatif | Etki |
|---|---|---|---|---|---|
| `netWorkingMinutesPerShift` | 480 | Tek vardiya, 9 saat - 1 saat mola = 8 saat net | SAD'de bir sayı verilmiyor; genel endüstri pratiği | 420 (7 saat) / 510 (8.5 saat) | CPM'in üretilebilir süre penceresini belirler |
| `defaultCapacityMinutesPerWorkCenterPerDay` | 480 | Tek vardiya × tek kaynak varsayımı | SAD'de karşılığı yok | Vardiya başına birden fazla paralel kaynak varsayımı (ör. 2x480) | Kapasite darboğazı hesaplarını doğrudan etkiler — **yanlış seçilirse CPM sonuçları gerçekçi olmayan gecikmeler/erken tarihler üretebilir** |
| `holidays.fixedDates2026` | Yalnızca sabit tarihli 7 resmi tatil | Dini bayramlar (değişken tarihli) bu ortamda doğrulanamadı, uydurulmadı | — | Gerçek 2026 dini bayram tarihleri eklenmeli (ERP/HR'dan teyit) | Eksik tatil = CPM'in gerçek çalışılamayan günleri çalışılabilir sayması riski |
| `defaultShippingDurationMinutes` | 1875 | Excel'deki 1000 gerçekleşmiş "Teslimat Süresi" kolonunun **medyanı** — bir lookup değeri değil, tek genel sabit | Gerçek route lookup'ı yok | Ürün/bölge bazlı farklı sabitler | Tüm siparişlere aynı sevkiyat süresi uygulanır — gerçekçi değişkenlik kaybolur |
| `procurement.defaultLeadTimeWorkingMinutes` | 960 | 2 iş günü kararı (2 * 480 dakika) | — | Malzeme bazında farklı tedarik süreleri (gerçek tedarikçi verisiyle) | Stok yetersizse tüm ürünlere aynı gecikme varsayılır |
| `defaultProductUnit` | "Adet" | Mobilya kalemleri adetle sayılır (Excel'de doğrudan yok ama iş bilgisiyle neredeyse kesin) | — | Ürün bazında override | Düşük risk |

## Karar Gerektiren (Decision Required) — Faz 4

Yalnızca **geri dönüşü zor veya mimari sonuç doğuran** iki nokta:

1. **`defaultCapacityMinutesPerWorkCenterPerDay` ve `missingCapacityStrategy`'nin nerede tüketileceği** — bu config, Mock ERP seed'ine mi (fake work-center kayıtları olarak) yoksa yalnızca Application-katmanı fallback'ine mi gidecek? Bu doküman **ikincisini** varsayıyor (seed gerçek/boş kalıyor). Bu varsayım yanlışsa, T-349'un tüm tasarımı değişir.
2. **`stockLevels` için "en son sipariş = güncel stok" kuralı** — bu, ERP'nin gerçek "güncel stok" kavramıyla örtüşmeyebilir (ör. sipariş dışı stok hareketleri, iade, sayım farkı Excel'de yok). Bu kural yalnızca **mevcut veri kümesinin bir yorumu**dur, gerçek bir "current inventory feed" değildir.

Bunların dışındaki tüm sayısal varsayımlar (satır 1-6, yukarıdaki tablo) mekanik/kalibrasyon niteliğinde — tek seferlik onay turu ile kapatılabilir, converter'ı bloklamıyor.
