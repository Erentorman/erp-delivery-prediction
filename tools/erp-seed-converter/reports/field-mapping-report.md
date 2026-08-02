# Field Mapping Report

## orders[]
| Excel Kaynağı | Seed Alanı | Dönüşüm |
|---|---|---|
| `SalesOrderNo` | `id` | Doğrudan |
| `Product` (ad) → `Products.ProductName` join | `productId` | Lookup: Product adı → `ProductCode` |
| `Quantity` | `quantity` | Doğrudan (int) |
| `RequestedDeliveryDate` | `requestedDeliveryDate` | Format normalizasyonu (ilk 10 karakter, ISO tarih) |
| `Priority` (Acil/Normal) | *(yok)* | **Üretilmiyor** — `MockErpOrder` record'unda Priority alanı yok (T-346). Değer kaybolmuyor, `mvp-assumptions.v1.json` içindeki `priorityValueCrosswalk`'ta referans olarak duruyor |
| `Customer` | *(yok)* | Mimaride hiç karşılığı yok (önceki turlarda MVP kapsamı dışı bırakıldı) |

## products[]
| Excel Kaynağı | Seed Alanı | Dönüşüm |
|---|---|---|
| `ProductCode` (Products sheet) | `id` | Doğrudan |
| `ProductName` (Products sheet) | `name` | Doğrudan |
| *(yok)* | `unit` | Config: `defaultProductUnit = "Adet"` |
| `RoutingID` (R001-R004) | *(yok)* | `MockErpProduct` record'unda RoutingID alanı yok. Bilgi kaybolmuyor — bu turun ham veri incelemesinde not edildi, T-348 (workOrders) tasarımı için referans |
| `BOM ID` | *(yok)* | Kaynakta 4/4 satırda tamamen null; zaten kullanılamaz durumda |

## boms[]
| Excel Kaynağı | Seed Alanı | Dönüşüm |
|---|---|---|
| Blok başlığı (Masa/Sandalye/Dolap/Kapı) | `productId` | Sabit grid offset + ürün adı → `ProductCode` lookup |
| `Malzeme` | `lines[].description` | Doğrudan |
| `Malzeme` (ad) | `lines[].componentId` | Deterministik slug (`MAT-` prefix) — **provizyonel, T-347 onayı bekliyor** |
| `Miktar` | `lines[].quantity` | Doğrudan (float) |
| `Birim` | `lines[].unit` | Doğrudan |
| `Tip` (Hammadde/Yarı Mamul/Satın Alınan Parça) | *(yok)* | `MockErpBomLine` record'unda tip alanı yok |

## stockLevels[]
| Excel Kaynağı | Seed Alanı | Dönüşüm |
|---|---|---|
| `StockLevel` (ProductionOrders, ürün başına en son OrderDate satırı) | `onHandQuantity` | **Karar uygulandı:** en son sipariş tarihli satır = güncel stok. Önizleme (limit-orders) modunda dahi, bu değerler sipariş limitinden bağımsız, TAM `ProductionOrders` veri kümesinden hesaplanır; böylece stok tablo eksiksiz kalır. |
| *(yok)* | `reservedQuantity` | Config varsayımı: `0` |
| `onHandQuantity` (kopya) | `availableQuantity` | Config varsayımı: `Available = OnHand` (Reserved veri yok) |

## openPurchaseOrders[] / workOrders[] / capacityCalendar / shippingDurations[]
Bilinçli boş — kaynakta gerekli alanlar (açık PO miktarı, operasyon süresi, kapasite/vardiya/tatil, route lookup) hiç yok. Detay: Seed Coverage Report.

## Ayrı dosyada tutulan (seed'e girmeyen) alanlar — `prediction-ground-truth.json`
`ProductionStartDate/FinishDate`, `PackagingStartDate/FinishDate`, `EstimatedDeliveryDate`, 5 alt süre + toplam süre (dakika), sipariş anı `StockLevel`/`FactoryWorkloadPercent`/`FactoryLoad` anlık görüntüleri. Bunlar AI eğitim/karşılaştırma amaçlı, operasyonel seed'in dışında.

## Risk Notu
BOM ve Routing sheet'lerindeki sabit satır/kolon offset'leri Excel'in fiziksel düzenine bağımlı. Excel şablonu değişirse (satır eklenir/silinirse) converter'daki `BOM_BLOCKS` sabitleri elle güncellenmelidir — bu bir kod riski değil, bir veri-yönetişimi riskidir.
