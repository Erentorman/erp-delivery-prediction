# Frontend UI Design System — ERP Delivery Prediction

> **Kaynak:** Bu doküman `feature/aliUI` dalında geliştirilen frontend'in tasarım dilini, bileşen kurallarını ve şemalarını tam olarak yansıtır (son commit: `9c27c23`). Başka bir dalda çalışan bir agent, buradaki kurallara uyarak yeni ekran/bileşen eklerse, `feature/aliUI` ile birleştirildiğinde **çakışma çıkarmaz** ve **görsel olarak tutarlı** kalır.
>
> Mimari/iş kuralları için tek otorite `docs/SAD-v1.2.md` ve `CLAUDE.md`'dir — bu doküman yalnızca **UI/görsel tasarım** katmanını kapsar, iş mantığını değiştirmez.

---

## 1. Temel prensip

Yeni bir bağımlılık (grafik kütüphanesi, animasyon kütüphanesi, UI kit) **eklenmeden**, salt MUI (`@mui/material`, `@mui/icons-material`) + tema özelleştirmesi + düz CSS/SVG ile üretildi. SAD §17.1 açıkça karmaşık Gantt/interaktif timeline/gelişmiş grafiği Faz-2'ye erteliyor — bu doküman bu kısıtı miras alır. **Yeni bir npm paketi eklemeden önce mutlaka onay isteyin.**

---

## 2. Tema mimarisi (aç/kapa koyu mod)

Dosya: `frontend/src/theme.ts`. Statik `theme` export'u yerine `createAppTheme(mode: 'light' | 'dark')` fonksiyonu kullanılır.

- `frontend/src/context/ThemeModeContext.tsx` — mod state'ini tutar, `localStorage['themeMode']` içinde saklar, ilk açılışta `prefers-color-scheme` medya sorgusuna bakar. `useThemeMode()` hook'u `{ mode, toggleMode }` döner.
- `App.tsx` içinde `ThemeModeProvider` en dışta, `ThemedApp` bileşeni `useThemeMode()` ile modu okuyup `useMemo(() => createAppTheme(mode), [mode])` ile temayı üretir ve MUI `ThemeProvider`'a verir.
- **Aç/kapa anahtarı iki yerde bulunmalı:** `Layout.tsx`'in AppBar'ında (uygulama içi tüm sayfalar için) ve `Login.tsx`'te sağ üst köşede mutlak konumlu (Login, Layout dışında olduğu için ayrı eklenir). İkonlar: `DarkModeOutlined` / `LightModeOutlined`, mod'a göre karşılıklı değişir. Her ikisi de `Tooltip` ile etiketlenir ("Koyu moda geç" / "Aydınlık moda geç").
- Yeni bir sayfa eklerken bu anahtarı tekrar icat etmeyin — sayfa `Layout` altında render oluyorsa zaten AppBar'daki anahtar kapsar.

### 2.1 Renk token'ları (özel palet — `theme.palette` üzerine eklenmiş)

Bu anahtarlar `declare module '@mui/material/styles'` ile `Palette`/`PaletteOptions`'a eklendi; `sx={{ color: 'tokenAdı' }}` şeklinde doğrudan string olarak kullanılabilir (MUI otomatik çözer).

| Token | Açık mod | Koyu mod | Anlamı / kullanım yeri |
|---|---|---|---|
| `brand900` | `#0f2942` | `#15294a` | **Yalnızca arka plan/rozet** rengi (ör. ikon rozeti bg, Login sol panel, AppBar temeli). **Asla metin rengi olarak kullanılmaz** — bkz. §2.3 kritik kural. |
| `brand700` | `#1a3a5c` | `#1e3a63` | brand900'ün açık tonu (gradyanlarda ikinci durak). |
| `brand100` | `#e8edf2` | `#1b2438` | Çok hafif marka tonu. |
| `brand50` | `#f4f7fa` | `#131c2c` | CTA kart gradyanı gibi çok hafif zemin geçişleri. |
| `interactiveBlue` | `#2563eb` | `#4d8eff` | Vurgu/etkileşim rengi — odak halkası, tıklanabilir link rengi (`secondary.main` olarak da atanır). |
| `surfacePage` | `#eef1f4` | `#0a0f1a` | `background.default` — sayfa zemini. |
| `surfaceCard` | `#ffffff` | `#121b2d` | `background.paper` — kart/tablo zemini. |
| `surfaceSubtle` | `#fafbfc` | `#0e1522` | Tablo başlığı (`TableHead`) arka planı. **`grey.50`/`grey.100` yerine bunu kullanın** — MUI'nin `grey.*` kısayolları koyu modda uyum sağlamaz. |
| `borderDefault` | `#dde3e8` | `rgba(255,255,255,0.08)` | Standart kenarlık (`divider` de bu değere eşitlenir). |
| `borderStrong` | `#c3ccd4` | `rgba(255,255,255,0.16)` | Daha belirgin kenarlık. |
| `textPrimary` | `#0f2942` | `#eef2f7` | **Başlık/güçlü metin rengi.** Sayfa H1'leri, kart başlıkları burayı kullanır. |
| `textBody` | `#1a1a1a` | `#e2e8f0` | Gövde metni. |
| `textSecondary` | `#5c7288` | `#93a5c2` | İkincil/açıklama metni (`text.secondary` ile eşleşir). |
| `textMuted` | `#8fa6bc` | `#64789a` | En düşük vurgulu metin (caption, ok ikonları). |
| `statusCritical.{bg,border,text}` | `#fbeaea/#e5b8b8/#a13a3a` | `rgba(248,113,113,.12)/.35/#fca5a5` | Kritik/gecikme rozetleri (ör. "Kritik" yol operasyonu etiketi). |
| `statusWarning.{bg,border,text}` | `#fdf6e3/#e8d9a8/#6b5518` | `rgba(251,191,36,.12)/.35/#fcd34d` | Uyarı/fallback/örnek-veri rozetleri. |
| `statusSuccess.{bg,border,text}` | `#e9f4ea/#b8d9bc/#2f6b35` | `rgba(74,222,128,.12)/.35/#86efac` | Başarı/zamanında rozetleri. |
| `statusNeutral.{bg,border,text}` | `#f4f7fa/#dde3e8/#5c7288` | `rgba(148,163,184,.10)/.28/#a8b7cc` | Nötr bilgi kutucukları (ör. "Varsayılan Mantık Kullanımı"). |

MUI standart paleti de mod'a göre ayarlı: `primary.main` = koyu modda `interactiveBlue` (`#4d8eff`), açık modda `brand900`; `error/warning/success/info.main` her iki modda da kontrastı yeterli parlak/koyu varyantlarla tanımlı (bkz. `theme.ts` `lightTokens`/`darkTokens`).

### 2.2 Şekil / gölge / tipografi sistemi

- `shape.borderRadius = 8` (temel birim). MUI'nin `sx={{ borderRadius: N }}` kısayolu bunu çarpan olarak kullanır (ör. `borderRadius: 2` → `16px`) — **sayfalardaki mevcut `borderRadius: 2` kalıntıları bilerek bırakıldı, tema ile otomatik uyumlu render olurlar, silmeye gerek yok.**
- `MuiCard`: `borderRadius: 12`, kenarlık + gölge tema'dan gelir (`cardBorder`/`cardShadow`, mod'a göre). Kart bileşenlerinde `elevation={0}` kullanın, tema zaten gölgeyi veriyor.
- `MuiButton`: `textTransform: 'none'`, `borderRadius: 8`, `fontSize: 13px`, `fontWeight: 600`. `contained` varyant hover'da yukarı kayma (`translateY(-1px)`) + parlayan gölge (koyu modda mavi glow) alır.
- `MuiChip`: `borderRadius: 999` (tam pill), `fontWeight: 600`. Durum chip'leri için MUI'nin standart `color` prop'unu kullanın (`success`/`warning`/`error`/`info`/`default`), özel hex vermeyin.
- `MuiAppBar`: her zaman koyu lacivert gradyan arka plan (`linear-gradient(135deg, ...)`), mod'dan bağımsız olarak **her zaman koyu** — bu kasıtlı, marka şeridi hep koyu kalır.
- `MuiOutlinedInput`: `borderRadius: 8`, odaklanınca `interactiveBlue` kenarlık + hafif glow.
- Yazı tipi: `"Inter", "Roboto", "Helvetica", "Arial", sans-serif` (tema genelinde tek font ailesi).
- Sayfa başlıkları (H1) tema `variant="h1"` yerine **elle boyutlandırılmış `Typography`** kullanır: liste/detay sayfalarında `fontSize: '18px'`, Dashboard'da `fontSize: '24px'`, her ikisi de `fontWeight: 700` ve `color: 'textPrimary'`. Alt açıklama satırı `fontSize: '13px'–'13.5px'`, `color: 'textSecondary'`.

### 2.3 Kritik kurallar (daha önce hata yapılan noktalar — tekrarlamayın)

1. **`color: 'brand900'` metinde asla kullanılmaz.** Koyu modda `brand900` hâlâ koyu lacivert olduğu için metin bu renkte yazılırsa okunmaz hale gelir. Başlık/metin rengi için her zaman `color: 'textPrimary'` kullanın; `brand900` yalnızca `bgcolor` (rozet/panel arka planı) için geçerlidir.
2. **`bgcolor: 'grey.50'` / `'grey.100'` kullanmayın** — MUI'nin bu kısayolları mod'a göre değişmez, koyu modda açık gri kalır. Yerine `bgcolor: 'surfaceSubtle'` kullanın.
3. **Vurgu rengi gereken yeni bir bileşende (ikon, üst çizgi, rozet) sabit hex vermeyin** — `theme.palette.primary.main` / `.warning.main` / `.error.main` / `.success.main` / `.info.main`'den okuyun ki koyu modda otomatik doğru renge dönüşsün (`useTheme()` hook'u ile).
4. **Tıklanabilir satır içi metin linkleri** (ör. tablo hücresindeki sipariş referansı) MUI `Link` bileşeninin varsayılan rengiyle bırakılırsa gövde metninden ayırt edilemez. Her zaman `sx={{ color: 'interactiveBlue', fontWeight: 600 }}` + `underline="hover"` ekleyin.
5. Login'in sol marka paneli ve AppBar **kasıtlı olarak her zaman koyu** — bu ikisi içindeki sabit `#fff` / `rgba(255,255,255,…)` değerlerini mod'a göre değiştirmeyin, tasarım gereği öyle.

---

## 3. Sayfa/route envanteri (mevcut yapı — çakışmayı önlemek için)

`App.tsx`'teki route tablosu:

| Route | Sayfa | Durum |
|---|---|---|
| `/login` | `Login.tsx` | Bölünmüş panel: sol marka anlatımı (her zaman koyu), sağ form (mod'a duyarlı). |
| `/` | `Dashboard.tsx` | 5 istatistik kartı (`StatCard` iç bileşeni) + CTA kartı. |
| `/orders` | `Orders.tsx` | Sipariş tablosu, referans hücresi link. |
| `/orders/:orderReference` | `OrderDetail.tsx` | Ürün/BOM/stok/iş emri detay kartları. |
| `/predictions` | `Predictions.tsx` | Tahmin formu + sonuç kartları + `ProviderComparisonCards`. |
| `/predictions/delayed` | `DelayedPredictions.tsx` | Gecikme listesi, gerçek backend çağrısı kullanır. |
| `/inventory` | `Inventory.tsx` | Salt-okunur stok tablosu. |

**Kullanılmayan/ölü dosyalar (route'a bağlı değil, silinmedi ama referans alınmamalı):** `components/PlannerDashboardView.tsx`, `components/CustomerSimulationView.tsx`, `hooks/usePrediction.ts`. Bunlar eski tasarım denemeleri; yeni tasarım dilini bunlardan **kopyalamayın**, `theme.ts` + yukarıdaki güncel sayfalardan referans alın.

**Nav öğeleri** (`Layout.tsx` → `navItems`): Panel / Siparişler / Teslimat Tahmini / Gecikenler / Stok. Yeni bir sayfa eklerseniz bu diziye ekleyin; aktif-link mantığı (`isActive`) en uzun eşleşen `to` prefix'ini seçer (ör. `/predictions/delayed` iken hem "Teslimat Tahmini" hem "Gecikenler" aynı anda aktif görünmesin diye).

---

## 4. Sayfa düzeni (layout) kuralları

- Her sayfanın kök `Box`'u: `sx={{ maxWidth: 'Npx', mx: 'auto', width: '100%' }}`. Genişlik kalıbı: form benzeri dar sayfalar (`Predictions`) → `960px`; orta genişlik (`Inventory`) → `1000px`; liste/dashboard sayfaları → `1200px`.
- Sayfa başlığı kalıbı: ikon (opsiyonel, Dashboard'da rozetli kutu içinde) + `Typography variant="h1"` (bkz. §2.2 boyutlar) + altında `textSecondary` açıklama satırı.
- **Mock/örnek veri kullanan her ekran** (`ORDERS_DATA_IS_MOCK`, `ORDER_DETAIL_DATA_IS_MOCK` gibi bayraklarla işaretli) başlığın hemen altında **zorunlu** bir `<Alert severity="info"><AlertTitle>Bilgi</AlertTitle>…</Alert>` bandı gösterir. Metin, verinin neden mock olduğunu ve gerçek karşılığının hangi endpoint olacağını açıklar (biliniyorsa endpoint yolu `<code>` içinde verilir). **Bu, sessizce veri uydurmamak için tasarım kuralıdır — atlanmaz.**
- Sahte/örnek hesaplanmış bir değer (ör. AI/Hybrid tahmini) kart içinde gösteriliyorsa, o kart üzerinde kalıcı bir "Örnek Veri" pill rozeti bulunur (`ProviderComparisonCards.tsx`'teki `MockDataTag` deseni: `statusWarning` token'larıyla küçük, büyük harf, `borderRadius: 3px` pill).

---

## 5. Bileşen envanteri ve kalıpları

### 5.1 `frontend/src/features/prediction/components/`
Tahmin sonucu ekranının parçaları — hepsi `useTheme()` ile token okuyup kendi kartını çizer: `PredictionResultSummary`, `ProviderComparisonCards` (Rule-Based/AI/Hybrid üç sütun, gerçek olmayanlar işaretli), `CriticalPathCard`, `MaterialShortagesCard`, `FallbackReasonsCard`, `OperationsTimelineCard`, `DemoDataBanner` (DEMO-* operasyon tespit edilince gösterilir), `ValidationErrorBanner`, `CalculationFailureBanner`. Ortak desen: `Card sx={{ mb: 2, p: '16px 20px' }}`, başlık satırı `ikon (16px) + 11–13px uppercase, letter-spacing 0.04em, fontWeight 600, color textPrimary`.

### 5.2 Dashboard `StatCard` deseni (`Dashboard.tsx` içinde tanımlı, dosya-yerel)
```
<Card sx={{ borderTop: `3px solid ${accentColor}`, ... }}>
  <CardContent>
    <Box sx={{ width:34, height:34, borderRadius:2, bgcolor: `${accentColor}14` }}>{icon}</Box>
    <Typography ...>{label}</Typography>
    <Typography sx={{ fontSize:'30px', fontWeight:700 }}>{value}</Typography>
    {to && <ArrowForwardIcon />}
  </CardContent>
</Card>
```
Tıklanabilir kartlar `component={RouterLink} to={...}` alır, hover'da `translateY(-2px)` + gölge. Yeni bir özet kartı eklerken bu deseni tekrar kullanın (kopyalayıp `accentColor`'ı `theme.palette.<semantic>.main`'den verin).

### 5.3 Tablo deseni
`TableContainer component={Paper} elevation={0} sx={{ border:'1px solid', borderColor:'divider', borderRadius:2 }}` → `Table` → `TableHead sx={{ bgcolor:'surfaceSubtle' }}` (kalın başlık hücreleri) → `TableBody` satırları `hover` prop'u ile. Yükleniyor/boş durumları `colSpan` ile ortalanmış `CircularProgress`/`Typography` mesajı.

### 5.4 İkon kullanımı
`@mui/icons-material`'in **Outlined** varyantları tercih edilir (ör. `WarningAmberOutlined`, `PrecisionManufacturingOutlined`, `Inventory2Outlined`) — daha ince/modern görünüm için. Boyutlar: satır içi `16–18px`, rozet içi `18–22px`.

---

## 6. Dil ve içerik kuralları

- Tüm arayüz metni **Türkçe**.
- Gerçek olmayan/hesaplanmamış hiçbir değer, gerçekmiş gibi sunulmaz — bkz. §4 zorunlu "Bilgi" bandı ve §4 "Örnek Veri" rozeti kuralları.
- Buton metinleri kısa, emir kipi: "Tahmini Hesapla", "Siparişlere Gözat", "Teslimat Tahminini Hesapla".

---

## 7. Bilinen eksikler (bu tasarım sistemi bunları varsaymaz)

Rol bazlı arayüz farklılaşması, What-if formu ekranı, Kapasite/takvim görünümü **henüz yok** (backend desteği olmadığı için bilinçli olarak atlandı — bkz. proje hafızası `project_scope_and_status`). Yeni bir agent bu doküman + mevcut route tablosuna bakıp bunları "eksik" sanıp rastgele eklemeye kalkmasın; önce backend'in gerçekten destekleyip desteklemediğini kontrol etsin (§1'deki ilkeyle aynı: sessizce uydurmayın).
