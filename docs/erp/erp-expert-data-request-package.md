# ERP Uzmanı Veri Talep Paketi

- **İlgili görev:** T-362
- **Amaç:** Teslim tarihi tahmin akışında kullanılan ERP verilerinin mevcut yazılım sözleşmeleriyle uyumlu, doğrulanabilir ve izlenebilir biçimde temin edilmesi.
- **Kapsam:** Routing, Operations, BOM, Capacity, Calendar, Open PO, Shipping ve Material Dictionary veri kümeleri.
- **Talep edilen teslim formatı:** UTF-8 JSON veya UTF-8 CSV. İç içe koleksiyonlar için JSON tercih edilir; CSV kullanılırsa dosyalar arası anahtar ilişkileri açıkça belirtilmelidir.
- **Tarih/saat standardı:** Tarih-saat değerleri ISO 8601 biçiminde saat dilimi/ofsetiyle verilmelidir. Yalnız tarih taşıyan alanlar `YYYY-MM-DD` biçimindedir. Ortak saat dilimi kuralı kullanılacaksa yazılı olarak bildirilmelidir.
- **Süre birimi:** Dakika; süre alanları farklı birimlere çevrilmeden dakika olarak teslim edilmelidir.
- **Ondalık sayı standardı:** Ondalık miktarların kaynak hassasiyeti korunmalı; JSON'da nokta ondalık ayırıcı kullanılmalıdır.
- **Boş/null değer yaklaşımı:** Sözleşmenin null kabul ettiği alanlarda bilinmeyen değer `null`, boş koleksiyon `[]` olmalıdır. Eksik alan, `null` ve boş koleksiyon birbirinin yerine kullanılmamalıdır.
- **Referans alanlarının birebir korunması:** ERP referans değerleri gösterim adlarıyla değiştirilmemeli; büyük/küçük harf, baştaki sıfır ve anlamlı karakterler korunmalıdır. Gösterim adları yalnız desteklenen ayrı alanlarda verilebilir.
- **ERP ekip sorumluluğu:** Kaynak modül/tablo/alan eşlemesini, kapsam ve filtreleri, veri sahipliğini, güncelleme sıklığını ve saat dilimi kuralını teyit etmek; kurallara uygun örnek ve dışa aktarımı sağlamak.
- **Yazılım ekip sorumluluğu:** Teslimi bu belgedeki sözleşmelere göre doğrulamak, entegrasyon eşlemesini yapmak ve `Pending ERP Decision` maddelerini ERP ekibiyle karara bağlamak.

Bu paket gerçek kimlik bilgisi, bağlantı dizesi veya hassas kişisel veri istemez. Ulusal kimlik numarası, kişisel telefon numarası ve kişisel e-posta adresi teslimata eklenmemelidir. Bu belge gönderime hazırdır; ERP ekibine fiilî gönderim ayrı ve manuel bir işlemdir.

## 1. Routing

Routing verisi, bir iş emri kapsamındaki operasyon kümesini ve bu kümenin kimliğini taşır. `routingReference`, routing'i tanımlar; `operations` aynı routing'e ait Operation kayıtlarını içerir. Ürün/malzeme bağlantısı mevcut sözleşmede routing'in doğrudan alanı değildir; Work Order üzerindeki `productReference` ve gömülü `routing` üzerinden kurulur.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `routingReference` | string | Zorunlu, null değil | Routing'in birebir korunacak benzersiz referansı. | ERP ekibi tarafından doldurulacak | Kaynak benzersizlik kuralını belirtiniz. |
| `operations` | array&lt;Operation&gt; | Zorunlu, null değil | Aynı routing'e ait operasyon koleksiyonu; boşsa `[]`. | ERP ekibi tarafından doldurulacak | Operasyon kaynağı ve routing bağlantısını belirtiniz. |

### İlişki ve doğrulama kuralları

- `routingReference` boş olmamalı ve dışa aktarım kapsamında routing'i tekil tanımlamalıdır.
- `operations` yalnız bu routing'e ait kayıtları içermelidir.
- Ürün/malzeme ile routing sahipliği ve ana routing seçimi mevcut doğrudan Routing sözleşmesinde alan olarak yer almaz; gerekli eşleme Work Order sözleşmesi üzerinden değerlendirilir.
- Routing ve operasyon ana verisinin yetkili ERP kaynağı ve nihai alan sahipliği `Pending ERP Decision` durumundadır.

### Örnek kayıt

```json
{
  "routingReference": "R-FIC-100",
  "operations": [
    {
      "operationReference": "OP-FIC-10",
      "operationSequence": 10,
      "workCenterReference": "WC-FIC-01",
      "standardDurationMinutes": 25,
      "predecessorOperationReferences": []
    }
  ]
}
```

### ERP ekibine sorular

- `routingReference` hangi modül/tablo/alandan ve hangi şirket/tesis kapsamıyla alınacaktır? — `Pending ERP Decision`
- Work Order ile routing ve ürün/malzeme ilişkilerinin yetkili kaynağı nedir? — `Pending ERP Decision`
- Bir ürün veya iş emri için birden fazla routing varsa mevcut sözleşmeye verilecek routing hangi ERP kuralıyla seçilir? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi açıkça belirtilmelidir. Veri varsa en az bir geçerli örnek sağlanmalı; yoksa koleksiyon boş dönmeli, kayıt veya fallback üretilmemelidir. Desteklenen opsiyonel değerler eksikse `null` kullanılmalıdır. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Routing kaynağı ve kapsamı yazıldı.
- [ ] `routingReference` tekilliği doğrulandı.
- [ ] Her kaydın `operations` koleksiyonu verildi.
- [ ] Dışa aktarım zamanı ve saat dilimi belirtildi.

## 2. Operations

Operation verisi, routing içindeki iş adımını, sırasını, bağlı Work Center'ı, standart süresini ve aynı routing içindeki öncüllerini tanımlar. `workCenterReference`, Work Center ana verisindeki `workCenterRef` değerine işaret eder.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `operationReference` | string | Zorunlu, null değil | Routing içindeki operasyon referansı. | ERP ekibi tarafından doldurulacak | Aynı routing içindeki benzersizlik kuralını belirtiniz. |
| `operationSequence` | integer (Int32) | Zorunlu, null değil | Pozitif operasyon sıra değeri. | ERP ekibi tarafından doldurulacak | Kaynak sıralama alanını belirtiniz. |
| `workCenterReference` | string | Zorunlu, null değil | Work Center `workCenterRef` değerine referans. | ERP ekibi tarafından doldurulacak | Work Center eşleme alanını belirtiniz. |
| `standardDurationMinutes` | integer (Int64) | Zorunlu, null değil | Pozitif standart operasyon süresi, dakika. | ERP ekibi tarafından doldurulacak | Kaynak süre birimi ve dönüşüm kuralını belirtiniz. |
| `predecessorOperationReferences` | array&lt;string&gt; | Zorunlu, null değil | Aynı routing içindeki doğrudan öncül operasyon referansları; öncül yoksa `[]`. | ERP ekibi tarafından doldurulacak | Öncelik ilişkisinin kaynağını belirtiniz. |

### İlişki ve doğrulama kuralları

- `standardDurationMinutes` pozitif olmalıdır.
- `operationSequence` pozitif olmalıdır.
- `predecessorOperationReferences` yalnız aynı routing içindeki `operationReference` değerlerine referans vermelidir.
- `workCenterReference`, Work Center `workCenterRef` alanına işaret eder; alan adı değiştirilmez.
- Bu paket kapsamında döngü tespiti gereksinimi istenmemektedir.
- Kurulum, kalan süre, durum, kuyruk/bekleme, alternatif Work Center, cycle veya CPM verisi istenmemektedir.

### Örnek kayıt

```json
{
  "operationReference": "OP-FIC-20",
  "operationSequence": 20,
  "workCenterReference": "WC-FIC-02",
  "standardDurationMinutes": 40,
  "predecessorOperationReferences": ["OP-FIC-10"]
}
```

### ERP ekibine sorular

- Operasyon ana verisinin yetkili ERP kaynağı ve sahibi nedir? — `Pending ERP Decision`
- Standart sürenin ERP'deki kaynak birimi ve dakikaya kayıpsız dönüşüm kuralı nedir? — `Pending ERP Decision`
- Öncül ilişkisi doğrudan tutulmuyorsa mevcut beş alanla uyumlu türetme kuralı nedir? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi açıkça belirtilmelidir. Veri varsa geçerli örnek sağlanmalı; boş koleksiyonlar `[]` olmalı, fallback veya sahte kayıt üretilmemelidir. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Sıra ve süre değerlerinin pozitifliği doğrulandı.
- [ ] Work Center referansları `workCenterRef` sözlüğüyle eşleşti.
- [ ] Öncüllerin aynı routing içinde olduğu doğrulandı.
- [ ] Sürelerin dakika olduğu teyit edildi.

## 3. BOM

BOM verisi, üst ürün/malzeme ile bileşen ürün/malzeme arasındaki ihtiyaç ilişkisini, üst birim başına miktarı ve ölçü birimini taşır. Aşağıdaki alanlar uygulamanın güncel `BomItemReadDto` sözleşmesidir; Mock ERP taşıma modelindeki açıklama alanı bu uygulama sözleşmesine aktarılmadığından talep alanı değildir.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `parentProductReference` | string | Zorunlu, null değil | BOM'un üst ürün/malzeme referansı. | ERP ekibi tarafından doldurulacak | Üst malzeme anahtarını belirtiniz. |
| `componentProductReference` | string | Zorunlu, null değil | Bileşen ürün/malzeme referansı. | ERP ekibi tarafından doldurulacak | Bileşen anahtarını belirtiniz. |
| `requiredQuantityPerParentUnit` | decimal | Zorunlu, null değil | Bir üst ürün birimi için gereken bileşen miktarı. | ERP ekibi tarafından doldurulacak | Kaynak hassasiyet ve ölçeği belirtiniz. |
| `unitOfMeasure` | string | Zorunlu, null değil | Bileşen miktarının ölçü birimi. | ERP ekibi tarafından doldurulacak | Birim kodu sözlüğünü belirtiniz. |
| `lineReference` | string veya null | Opsiyonel, null olabilir | ERP'de mevcutsa BOM satır referansı. | ERP ekibi tarafından doldurulacak | Kaynakta yoksa `null`. |

### İlişki ve doğrulama kuralları

- Üst ve bileşen referansları Material Dictionary içindeki `productReference` değerleriyle eşleşmelidir.
- `requiredQuantityPerParentUnit` kaynak hassasiyetini korumalı ve pozitif bir ihtiyaç miktarını temsil etmelidir.
- `unitOfMeasure`, miktarın birimidir ve gösterim adıyla değiştirilmemelidir.
- Hurda, verim, alternatif bileşen, revizyon ve geçerlilik alanları mevcut sözleşmede bulunmadığından istenmemektedir.

### Örnek kayıt

```json
{
  "parentProductReference": "PRD-FIC-001",
  "componentProductReference": "MAT-FIC-010",
  "requiredQuantityPerParentUnit": 1.2500,
  "unitOfMeasure": "KG",
  "lineReference": "BOM-FIC-L10"
}
```

### ERP ekibine sorular

- BOM üst/bileşen ve satır verisinin yetkili kaynakları nelerdir? — `Pending ERP Decision`
- Miktarın kaynak hassasiyeti, ölçeği ve birim kod sözlüğü nedir? — `Pending ERP Decision`
- `lineReference` kaynak sistemde mevcut ve dışa aktarılabilir mi? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; tek bir dışa aktarım tarih/saat ve açık saat dilimi verilmelidir. Veri varsa geçerli örnek sağlanmalı; veri yoksa `[]` dönülmeli, kayıt/fallback üretilmemelidir. `lineReference` yoksa alan `null` olmalıdır. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Üst ve bileşen referansları sözlükte mevcut.
- [ ] Miktar hassasiyeti korundu.
- [ ] Ölçü birimleri sağlandı.
- [ ] Opsiyonel satır referansı doğru null yaklaşımıyla verildi.

## 4. Capacity

Capacity talebinin mevcut desteği; sorgulanan zaman aralığı, Work Center ana verisi ve Calendar bölümündeki çalışma/kapalı zaman kayıtlarıdır. Ayrı bir sayısal kapasite, yük veya kapasite formülü alanı mevcut sözleşmede yoktur. Work Center kimliği yalnız `workCenterRef` ve `name` alanlarından oluşur.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `rangeStart` | string (date-time, DateTimeOffset) | Zorunlu, null değil | Dönen kapasite/takvim penceresinin dahil başlangıcı. | ERP ekibi tarafından doldurulacak | Sorgu aralığından yanıt zarfına taşınır. |
| `rangeEnd` | string (date-time, DateTimeOffset) | Zorunlu, null değil | Dönen kapasite/takvim penceresinin bitiş sınırı. | ERP ekibi tarafından doldurulacak | Sınır semantiğini teyit ediniz. |
| `workCenters` | array&lt;WorkCenter&gt; | Zorunlu, null değil | İstenen Work Center ana veri koleksiyonu; boşsa `[]`. | ERP ekibi tarafından doldurulacak | Kaynak iş merkezi görünümünü belirtiniz. |
| `workCenterRef` | string | Zorunlu, null değil | Work Center'ın benzersiz ana referansı. | ERP ekibi tarafından doldurulacak | Kimlik alanıdır. |
| `name` | string | Zorunlu, null değil | Work Center gösterim adı. | ERP ekibi tarafından doldurulacak | Referans yerine kullanılmaz. |

### İlişki ve doğrulama kuralları

- `rangeEnd`, `rangeStart` değerinden önce olamaz.
- `workCenterRef` kapsam içinde benzersiz olmalı; Calendar kayıtlarındaki `workCenterReference` bu değere işaret etmelidir.
- Work Center ana verisi desteklenmeyen kapasite varsayımlarıyla birleştirilmemelidir.
- `machineCount`, varsayılan vardiya, kapasite/yük dakikaları, makine sayısı, vardiya çarpanı, kullanım oranı veya kapasite formülü ERP alanı olarak talep edilmemektedir.
- Güncelleme sıklığı mevcut sözleşmede bir alan değildir ve ERP ekibiyle kararlaştırılmalıdır.

### Örnek kayıt

```json
{
  "rangeStart": "2026-09-01T00:00:00+03:00",
  "rangeEnd": "2026-09-08T00:00:00+03:00",
  "workCenters": [
    { "workCenterRef": "WC-FIC-01", "name": "Kurgusal Montaj Merkezi" }
  ]
}
```

### ERP ekibine sorular

- Work Center ana verisinin yetkili modül/tablo/alanları nelerdir? — `Pending ERP Decision`
- `rangeEnd` dahil mi hariç mı yorumlanmalıdır? — `Pending ERP Decision`
- İstenen iş anlamındaki kapasite yalnız Calendar aralıklarından mı değerlendirilecektir; ayrı kapasite kavramı gerekiyorsa sözleşme sahipliği nasıl karara bağlanacaktır? — `Pending ERP Decision`
- Work Center ve Calendar dışa aktarım güncelleme sıklığı nedir? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi verilmelidir. Veri varsa örnek sağlanmalı; boş koleksiyonlar `[]` olmalı, varsayımsal kapasite veya fallback kayıtları üretilmemelidir. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Yalnız mevcut kapasite/takvim zarfı ve Work Center alanları kullanıldı.
- [ ] `workCenterRef` benzersizliği doğrulandı.
- [ ] Aralık ve saat dilimi açıklandı.
- [ ] Kaynak ve güncelleme sıklığı belirtildi.

## 5. Calendar

Calendar verisi Work Center çalışma aralıklarını, tatilleri ve planlı duruşları taşır. Çalışma aralığı `shifts`, tatil `holidays`, planlı duruş `plannedDowntimes` koleksiyonundadır. 480 dakikalık MVP değeri uygulama konfigürasyonudur; ERP verisi olarak talep edilmemektedir.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `shifts` | array&lt;WorkingShift&gt; | Zorunlu, null değil | Çalışma aralıkları; boşsa `[]`. | ERP ekibi tarafından doldurulacak | Vardiya/takvim kaynağını belirtiniz. |
| `workCenterReference` | string | WorkingShift için zorunlu | Çalışma aralığının Work Center `workCenterRef` referansı. | ERP ekibi tarafından doldurulacak | Referans bütünlüğünü sağlayınız. |
| `start` | string (date-time, DateTimeOffset) | WorkingShift için zorunlu | Çalışma aralığının başlangıcı. | ERP ekibi tarafından doldurulacak | Ofset/saat dilimi zorunlu. |
| `end` | string (date-time, DateTimeOffset) | WorkingShift için zorunlu | Çalışma aralığının bitişi. | ERP ekibi tarafından doldurulacak | Sınır semantiğini belirtiniz. |
| `holidays` | array&lt;Holiday&gt; | Zorunlu, null değil | Tatil kayıtları; boşsa `[]`. | ERP ekibi tarafından doldurulacak | Resmî/tesis takvimi kaynağını belirtiniz. |
| `date` | string (date, DateOnly) | Holiday için zorunlu | Tatil günü, `YYYY-MM-DD`. | ERP ekibi tarafından doldurulacak | Günün uygulanan saat dilimini belirtiniz. |
| `workCenterReference` | string veya null | Holiday için opsiyonel | Belirli Work Center'a aitse referans; genel tatilse `null`. | ERP ekibi tarafından doldurulacak | Null değerinin genel kapsam anlamını teyit ediniz. |
| `plannedDowntimes` | array&lt;PlannedDowntime&gt; | Zorunlu, null değil | Planlı duruş kayıtları; boşsa `[]`. | ERP ekibi tarafından doldurulacak | Bakım/duruş kaynağını belirtiniz. |
| `workCenterReference` | string | PlannedDowntime için zorunlu | Duruşun Work Center `workCenterRef` referansı. | ERP ekibi tarafından doldurulacak | Referans bütünlüğünü sağlayınız. |
| `start` | string (date-time, DateTimeOffset) | PlannedDowntime için zorunlu | Planlı duruş başlangıcı. | ERP ekibi tarafından doldurulacak | Ofset/saat dilimi zorunlu. |
| `end` | string (date-time, DateTimeOffset) | PlannedDowntime için zorunlu | Planlı duruş bitişi. | ERP ekibi tarafından doldurulacak | Başlangıçtan sonra olmalı. |
| `plannedDowntimeMinutes` | integer (Int64) | PlannedDowntime için zorunlu | Planlı duruş süresi, dakika. | ERP ekibi tarafından doldurulacak | Aralıkla tutarlılığı belirtiniz. |

### İlişki ve doğrulama kuralları

- Her `start`, karşılık gelen `end` değerinden önce olmalıdır; tüm tarih-saatlerde ofset bulunmalıdır.
- Work Center'a bağlı kayıtlar mevcut `workCenterRef` değerine referans vermelidir.
- Holiday `workCenterReference: null` genel takvim kapsamını ifade eder; kesin kapsam ERP ekibiyle teyit edilmelidir.
- Çakışan shift/duruşların önceliği, bitiş sınırının dahil/hariç oluşu ve tatilin çalışma aralığını nasıl etkilediği mevcut sözleşmede kodlanmamıştır.
- `plannedDowntimeMinutes`, pozitif olmalı ve `start`/`end` aralığıyla tutarlı olmalıdır.

### Örnek kayıt

```json
{
  "shifts": [
    {
      "workCenterReference": "WC-FIC-01",
      "start": "2026-09-01T08:00:00+03:00",
      "end": "2026-09-01T16:00:00+03:00"
    }
  ],
  "holidays": [
    { "date": "2026-09-02", "workCenterReference": null }
  ],
  "plannedDowntimes": [
    {
      "workCenterReference": "WC-FIC-01",
      "start": "2026-09-03T10:00:00+03:00",
      "end": "2026-09-03T10:30:00+03:00",
      "plannedDowntimeMinutes": 30
    }
  ]
}
```

### ERP ekibine sorular

- Shift, tatil ve planlı duruşların yetkili kaynakları nelerdir? — `Pending ERP Decision`
- Genel ve Work Center'a özel takvim çakışmalarında öncelik nedir? — `Pending ERP Decision`
- Aralıklar `[start, end)` olarak mı yorumlanacaktır? — `Pending ERP Decision`
- Tesis saat dilimi, yaz saati ve gece yarısını aşan vardiya kuralı nedir? — `Pending ERP Decision`
- `plannedDowntimeMinutes` kaynak alan mı, yoksa aralıktan türetilen değer mi? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; tek dışa aktarım tarih/saat, saat dilimi ve kapsanan aralık verilmelidir. Veri varsa örnek sağlanmalı; boş koleksiyonlar `[]` olmalı, çalışma süresi veya fallback kaydı üretilmemelidir. Desteklenen opsiyonel değer `null` olmalıdır. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Shift, tatil ve duruş koleksiyonları sağlandı.
- [ ] Work Center referansları doğrulandı.
- [ ] Saat dilimi ve sınır kuralları açıklandı.
- [ ] Çakışma yaklaşımı belirtildi.

## 6. Open PO

Open PO verisi açık satın alma siparişlerinin referansını, ürün/malzeme referansını, açık miktarını, beklenen kullanılabilirlik zamanını, varsa tedarikçi teslim süresini ve durumunu taşır. 960 dakikalık procurement fallback uygulama konfigürasyonudur; ERP verisi değildir ve bu bölümde talep edilmez.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `purchaseOrderReference` | string | Zorunlu, null değil | Satın alma siparişi referansı. | ERP ekibi tarafından doldurulacak | Satır ayrımı gerekiyorsa mevcut kimlik semantiğini açıklayınız. |
| `productReference` | string | Zorunlu, null değil | Sipariş edilen ürün/malzeme referansı. | ERP ekibi tarafından doldurulacak | Material Dictionary ile eşleşmelidir. |
| `openQuantity` | decimal | Zorunlu, null değil | Henüz kullanılabilir hâle gelmemiş açık miktar. | ERP ekibi tarafından doldurulacak | Birim ve hassasiyeti not ediniz. |
| `expectedAvailabilityDateTime` | string (date-time, DateTimeOffset) | Zorunlu, null değil | Malzemenin kullanılabilir olması beklenen tarih/saat. | ERP ekibi tarafından doldurulacak | Teslim tarihi mi kabul/kullanılabilirlik tarihi mi olduğunu belirtiniz. |
| `supplierLeadTimeMinutes` | integer (Int64) veya null | Opsiyonel, null olabilir | Sözleşmede mevcut tedarikçi teslim süresi, dakika. | ERP ekibi tarafından doldurulacak | Kaynakta yoksa `null`; fallback üretmeyiniz. |
| `status` | string | Zorunlu, null değil | Mevcut sözleşmedeki satın alma siparişi durumu. | ERP ekibi tarafından doldurulacak | Açık kayıt filtrelemesinde kullanılan değerleri belirtiniz. |

### İlişki ve doğrulama kuralları

- `productReference`, Material Dictionary `productReference` değeriyle eşleşmelidir.
- `openQuantity` pozitif olmalı; kaynak ondalık hassasiyeti korunmalıdır. Birim için mevcut Open PO sözleşmesinde ayrı JSON alanı yoktur; birim sahipliği `Pending ERP Decision` konusudur.
- `expectedAvailabilityDateTime` saat dilimi/ofset içermelidir.
- `supplierLeadTimeMinutes` doluysa negatif olamaz ve dakikadır; yoksa `null` olmalıdır.
- Tedarikçi ana verisi ve procurement fallback değerleri istenmemektedir.

### Örnek kayıt

```json
{
  "purchaseOrderReference": "PO-FIC-1001",
  "productReference": "MAT-FIC-010",
  "openQuantity": 75.500,
  "expectedAvailabilityDateTime": "2026-09-05T14:00:00+03:00",
  "supplierLeadTimeMinutes": null,
  "status": "OPEN-FICTIONAL"
}
```

### ERP ekibine sorular

- Açık PO ve beklenen kullanılabilirlik zamanının yetkili kaynak alanları nelerdir? — `Pending ERP Decision`
- Open PO miktarının birimi mevcut sözleşmeyle nasıl belirlenir? — `Pending ERP Decision`
- Açık kayıt kapsamına giren ERP durum değerleri ve kısmi teslim hesabı nedir? — `Pending ERP Decision`
- `supplierLeadTimeMinutes` doğrudan kaynak alan mı, türetilmiş değer mi? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi belirtilmelidir. Veri varsa örnek sağlanmalı; açık PO yoksa `[]` dönülmeli, fallback veya sahte kayıt üretilmemelidir. Opsiyonel teslim süresi yoksa `null` olmalıdır. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Açık miktar ve hassasiyet doğrulandı.
- [ ] Ürün/malzeme referansları sözlükle eşleşti.
- [ ] Kullanılabilirlik zamanı ofset içeriyor.
- [ ] Durum filtresi ve opsiyonel teslim süresi açıklandı.

## 7. Shipping

Shipping verisi, başlangıç, varış ve sevkiyat profili üçlüsüyle tanımlanan rotanın kesin pozitif taşıma süresini verir. Arama bu üç referansın birebir eşleşmesiyle yapılır.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `originReference` | string | Zorunlu, null değil | Rotanın başlangıç referansı. | ERP ekibi tarafından doldurulacak | Referans kapsamını belirtiniz. |
| `destinationReference` | string | Zorunlu, null değil | Rotanın varış referansı. | ERP ekibi tarafından doldurulacak | Referans kapsamını belirtiniz. |
| `shippingProfileReference` | string | Zorunlu, null değil | Sevkiyat profil referansı. | ERP ekibi tarafından doldurulacak | Profil kaynağını belirtiniz. |
| `shippingDurationMinutes` | integer (Int64) | Zorunlu, null değil | Kesin pozitif sevkiyat süresi, dakika. | ERP ekibi tarafından doldurulacak | Kaynak süre ve birimini belirtiniz. |

### İlişki ve doğrulama kuralları

- Rota kimliği `originReference` + `destinationReference` + `shippingProfileReference` bileşimidir ve tam eşleşmeyle aranır.
- Mevcut rota kendi kesin pozitif `shippingDurationMinutes` değerini döndürür.
- Bulunmayan rota `NotFound` kabul edilir.
- Sıfır dakikalık rota geçerli değildir.
- ERP konfigürasyonundan fallback değer istenmemektedir.
- `UnknownDestination` veya `InvalidRouteData` durumu istenmemektedir.
- Tahminî süre, alternatif rota, rota önceliği, güven değeri ve rota durumu alanı talep edilmemektedir.

### Örnek kayıt

```json
{
  "originReference": "LOC-FIC-IST",
  "destinationReference": "LOC-FIC-ANK",
  "shippingProfileReference": "SHIP-FIC-ROAD",
  "shippingDurationMinutes": 420
}
```

### ERP ekibine sorular

- Başlangıç, varış ve profil referanslarının yetkili kaynakları nelerdir? — `Pending ERP Decision`
- Taşıma süresi hangi operasyonel tanıma göre başlar ve biter? — `Pending ERP Decision`
- Rota üçlüsünün ERP kapsamındaki benzersizlik kuralı nedir? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; dört alan adı aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi belirtilmelidir. Veri varsa pozitif süreli örnek sağlanmalı; veri yoksa `[]` dönülmeli, fallback veya sahte rota üretilmemelidir. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Bileşik rota kimliği tekil.
- [ ] Süreler pozitif ve dakika cinsinde.
- [ ] Eksik rota için kayıt üretilmedi.
- [ ] Kaynak ve süre semantiği açıklandı.

## 8. Material Dictionary

Material Dictionary; Orders, BOM, Work Orders, Open Purchase Orders ve uygulanabildiği yerde Routing tarafından kullanılan ürün/malzeme referanslarını yorumlamak için ortak sözlüktür. Uygulamanın mevcut `ProductReadDto` sözleşmesi yalnız aşağıdaki alanları destekler. Mock ERP'deki ürün gösterim adı uygulama DTO'suna taşınmadığından bu pakette JSON alanı olarak istenmez.

| JSON alanı | Veri tipi | Zorunluluk | Açıklama | ERP kaynak modülü/tablosu/alanı | ERP ekip notu |
|---|---|---|---|---|---|
| `productReference` | string | Zorunlu, null değil | Ürün/malzemenin benzersiz ve birebir korunacak referansı. | ERP ekibi tarafından doldurulacak | Tüm kullanan veri kümeleriyle aynı kodu veriniz. |
| `planningClassification` | string veya null | Opsiyonel, null olabilir | Kaynakta ve mevcut sözleşme kapsamında varsa planlama sınıflandırması. | ERP ekibi tarafından doldurulacak | Yoksa `null`; yeni sınıflandırma üretmeyiniz. |
| `unitOfMeasure` | string | Zorunlu, null değil | Ürün/malzemenin mevcut sözleşmedeki ölçü birimi. | ERP ekibi tarafından doldurulacak | Birim kod sözlüğünü belirtiniz. |

### İlişki ve doğrulama kuralları

- `productReference` kapsam içinde benzersiz olmalı; Orders, BOM, Work Orders ve Open PO referansları aynı kodu kullanmalıdır.
- Referans değeri gösterim adıyla değiştirilmemelidir.
- Mevcut uygulama sözleşmesinde gösterim adı/açıklama ve aktif/pasif durumu yoktur; bu alanlar talep edilmemektedir.
- Desteklenmeyen kategori, maliyet, tedarikçi, depo ve stok alanları istenmemektedir.
- Routing ile ürün/malzeme bağlantısı doğrudan Product sözleşmesinde değildir; Work Order ilişkisi üzerinden ele alınır.

### Örnek kayıt

```json
{
  "productReference": "MAT-FIC-010",
  "planningClassification": null,
  "unitOfMeasure": "KG"
}
```

### ERP ekibine sorular

- Ürün/malzeme sözlüğünün yetkili kaynak modül/tablo/alanları nelerdir? — `Pending ERP Decision`
- Kodların şirket/tesisler arası benzersizlik kapsamı nedir? — `Pending ERP Decision`
- `planningClassification` için yetkili kaynak ve izin verilen değer sözlüğü var mıdır? — `Pending ERP Decision`
- Gösterim adı veya aktif/pasif bilgisinin gelecekte gerekli olması hâlinde sözleşme sahipliği nasıl karara bağlanacaktır? — `Pending ERP Decision`

### Teslim gereksinimleri

UTF-8 JSON veya UTF-8 CSV kullanılmalı; alan adları aynen korunmalı; dışa aktarım tarih/saat ve saat dilimi belirtilmelidir. Veri varsa örnek sağlanmalı; veri yoksa `[]` dönülmeli, sözlük veya fallback kaydı üretilmemelidir. Opsiyonel sınıflandırma yoksa `null` olmalıdır. Hassas veri eklenmemelidir.

### Teslim kontrol listesi

- [ ] Tüm kullanılan ürün/malzeme referansları sözlükte mevcut.
- [ ] Kodların benzersizlik kapsamı açıklandı.
- [ ] Ölçü birimi kodları verildi.
- [ ] Opsiyonel sınıflandırma doğru null yaklaşımıyla sağlandı.
