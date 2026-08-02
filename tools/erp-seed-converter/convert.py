#!/usr/bin/env python3
"""
ERP (Furniture_ERP_Data_Minutes.xlsx) -> Mock ERP Seed (mock-erp-seed.json) converter.

Hedef sozlesme (repository'deki GERCEK dosyalar):
  - MockErpDataStore.cs  (tek seed dosyasi, kok anahtarlar zorunlu, bos dizi olabilir)
  - MockErpModels.cs / MockErpTransportModels.cs (record sekilleri)
  - MockErpDataProvider.cs (mapping davranisi)
  - IErpDataProvider.cs / ErpReadDtos.cs (Application sozlesmesi - yalnizca referans icin)

Ilkeler:
  - Orijinal Excel'e yazilmaz / degistirilmez (yalnizca okunur).
  - Kaynakta olmayan alan/ID/kapasite/sure SEED icine YAZILMAZ.
  - Gerceklesmis uretim/teslimat sonuclari (leakage alanlari) operasyonel
    seed'e girmez; ayri bir ground-truth dosyasinda tutulur.
  - Config (mvp-assumptions.v1.json) disaridan okunur, kodun icine gomulmez.
  - Deterministik: ayni Excel + ayni config -> ayni JSON (byte-byte).
"""
import argparse
import hashlib
import json
import re
import sys
import unicodedata
from validation import run_validation
from pathlib import Path

import pandas as pd

# ---------------------------------------------------------------------------
# Deterministik ID / slug politikasi (kullanicinin onayladigi baslangic kurali)
# ---------------------------------------------------------------------------
TR_MAP = str.maketrans({
    "ı": "i", "İ": "I", "ş": "s", "Ş": "S", "ğ": "g", "Ğ": "G",
    "ü": "u", "Ü": "U", "ö": "o", "Ö": "O", "ç": "c", "Ç": "C",
})


def slugify(name: str, prefix: str) -> str:
    s = name.strip().translate(TR_MAP)
    s = unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode("ascii")
    s = re.sub(r"[^A-Za-z0-9]+", "-", s).strip("-").upper()
    return f"{prefix}-{s}"


# ---------------------------------------------------------------------------
# BOM grid sabit offsetleri
# ---------------------------------------------------------------------------
BOM_BLOCKS = [
    ("Masa", 0, 1, 2, 3, 2, 13),
    ("Sandalye", 0, 1, 2, 3, 16, 27),
    ("Dolap", 5, 6, 7, 8, 2, 18),
    ("Kapı", 5, 6, 7, 8, 21, 34),
]


def load_config(path: Path) -> dict:
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def read_sheets(xlsx_path: Path):
    errors = []
    try:
        xl = pd.ExcelFile(xlsx_path)
    except Exception as e:
        errors.append(f"Excel dosyasi okunamadi: {e}")
        return {}, errors
    
    expected_sheets = ["SalesOrders(satış siparişi)", "Products(ürün kartı)", "BOM(ürün ağacı)", "ProductionOrders(üretim emri)"]
    missing = [s for s in expected_sheets if s not in xl.sheet_names]
    if missing:
        errors.append(f"Beklenen sheet bulunamadi: {missing}")
    
    sheets = {}
    for s in expected_sheets:
        if s in xl.sheet_names:
            sheets[s] = pd.read_excel(xl, sheet_name=s, header=None if s=="BOM(ürün ağacı)" else 0)
        else:
            sheets[s] = pd.DataFrame()
    return sheets, errors


def build_products(products_df: pd.DataFrame, config: dict):
    products = []
    name_to_code = {}
    errors = []
    if products_df.empty:
        return products, name_to_code, errors
        
    req_cols = ["ProductCode", "ProductName"]
    missing = [c for c in req_cols if c not in products_df.columns]
    if missing:
        errors.append(f"Products tablosunda zorunlu kolon eksik: {missing}")
        return products, name_to_code, errors

    for _, r in products_df.iterrows():
        code = str(r["ProductCode"]).strip()
        name = str(r["ProductName"]).strip()
        if pd.isna(r["ProductCode"]) or pd.isna(r["ProductName"]) or not code or not name or code == "nan":
            errors.append("Product satirinda bos deger atlanmadi, hata raporlandi.")
            continue
        name_to_code[name] = code
        products.append({
            "id": code,
            "name": name,
            "unit": config.get("defaultProductUnit", "Adet"),
        })
    return products, name_to_code, errors


def build_orders(orders_df: pd.DataFrame, name_to_code: dict):
    orders = []
    errors = []
    if orders_df.empty:
        return orders, errors
        
    req_cols = ["SalesOrderNo", "Product", "Quantity", "RequestedDeliveryDate"]
    missing = [c for c in req_cols if c not in orders_df.columns]
    if missing:
        errors.append(f"Orders tablosunda zorunlu kolon eksik: {missing}")
        return orders, errors

    for _, r in orders_df.iterrows():
        if pd.isna(r["SalesOrderNo"]) or pd.isna(r["Product"]):
            errors.append("Siparis satirinda bos zorunlu alan")
            continue
        product_name = str(r["Product"]).strip()
        product_id = name_to_code.get(product_name)
        if product_id is None:
            errors.append(f"Siparis {r['SalesOrderNo']} icin urun adi Products tablosunda bulunamadi: {product_name}")
            product_id = f"UNKNOWN-{product_name}"
        
        orders.append({
            "id": str(r["SalesOrderNo"]).strip(),
            "productId": product_id,
            "quantity": int(r.get("Quantity", 0)) if not pd.isna(r.get("Quantity")) else 0,
            "requestedDeliveryDate": str(r.get("RequestedDeliveryDate", ""))[:10],
        })
    return orders, errors


def build_boms(bom_raw: pd.DataFrame, name_to_code: dict):
    material_dictionary = {}
    boms = []
    errors = []
    if bom_raw.empty:
        errors.append("BOM tablosu bos veya okunamadi.")
        return boms, material_dictionary, errors

    for product_name, cM, cT, cQ, cU, r0, r1 in BOM_BLOCKS:
        product_id = name_to_code.get(product_name)
        if product_id is None:
            errors.append(f"BOM product {product_name} Products tablosunda bulunamadi")
            product_id = f"UNKNOWN-{product_name}"
        
        lines = []
        parsed_lines = 0
        for i in range(r0, min(r1, len(bom_raw))):
            name = bom_raw.iat[i, cM] if cM < bom_raw.shape[1] else None
            if pd.isna(name) or str(name).strip() == "":
                continue
            qty = bom_raw.iat[i, cQ] if cQ < bom_raw.shape[1] else 0
            unit = bom_raw.iat[i, cU] if cU < bom_raw.shape[1] else ""
            name = str(name).strip()
            if name not in material_dictionary:
                material_dictionary[name] = slugify(name, "MAT")
            code = material_dictionary[name]
            lines.append({
                "componentId": code,
                "description": name,
                "quantity": float(qty) if not pd.isna(qty) else 0.0,
                "unit": str(unit).strip(),
            })
            parsed_lines += 1
        
        if parsed_lines == 0:
            errors.append(f"BOM grid parse beklenen urun bloklarini uretmedi: {product_name} icin satir bulunamadi.")
        boms.append({"productId": product_id, "lines": lines})
    return boms, material_dictionary, errors


def build_stock_levels(po_df: pd.DataFrame, name_to_code: dict):
    stock_levels = []
    errors = []
    if po_df.empty:
        return stock_levels, errors
        
    req_cols = ["OrderDate", "Product", "StockLevel"]
    missing = [c for c in req_cols if c not in po_df.columns]
    if missing:
        errors.append(f"ProductionOrders tablosunda zorunlu kolon eksik: {missing}")
        return stock_levels, errors
        
    po = po_df.copy()
    po["OrderDate_parsed"] = pd.to_datetime(po["OrderDate"])
    po = po.sort_values("OrderDate_parsed")
    latest_by_product = po.groupby("Product").tail(1)

    for _, r in latest_by_product.iterrows():
        product_name = str(r["Product"]).strip()
        product_id = name_to_code.get(product_name)
        if product_id is None:
            errors.append(f"StockLevel productReference '{product_name}' Products tablosunda bulunamadi")
            product_id = f"UNKNOWN-{product_name}"
        
        on_hand = int(r["StockLevel"]) if not pd.isna(r["StockLevel"]) else 0
        stock_levels.append({
            "productReference": product_id,
            "locationReference": None,
            "onHandQuantity": on_hand,
            "reservedQuantity": 0,
            "availableQuantity": on_hand,
        })
    return stock_levels, errors


def build_ground_truth(po_df: pd.DataFrame):
    records = []
    errors = []
    if po_df.empty:
        return records, errors
    for _, r in po_df.iterrows():
        records.append({
            "productionOrderId": str(r.get("İş emri No", "")),
            "salesOrderId": str(r.get("Satış siparişi No", "")),
            "product": str(r.get("Product", "")),
            "quantity": int(r.get("Quantity", 0)) if not pd.isna(r.get("Quantity")) else 0,
            "orderDate": str(r.get("OrderDate", ""))[:10],
            "priority": str(r.get("Priority", "")),
            "stockLevelAtOrderTime": int(r.get("StockLevel", 0)) if not pd.isna(r.get("StockLevel")) else 0,
            "minimumStock": int(r.get("MinimumStock", 0)) if not pd.isna(r.get("MinimumStock")) else 0,
            "stockOrderRequired": str(r.get("StockOrderRequired", "")) == "Yes",
            "factoryWorkloadPercentAtOrderTime": int(r.get("FactoryWorkloadPercent", 0)) if not pd.isna(r.get("FactoryWorkloadPercent")) else 0,
            "factoryLoadAtOrderTime": str(r.get("Factory load ", "")),
            "productionStartDate": str(r.get("ProductionStartDate", ""))[:10],
            "productionFinishDate": str(r.get("ProductionFinishDate", ""))[:10],
            "packagingStartDate": str(r.get("PackagingStartDate", ""))[:10],
            "packagingFinishDate": str(r.get("PackagingFinishDate", ""))[:10],
            "estimatedDeliveryDate": str(r.get("EstimatedDeliveryDate", ""))[:10],
            "orderProcessingDurationMinutes": int(r.get("Sipariş İşlenme Süresi (Dakika)", 0)) if not pd.isna(r.get("Sipariş İşlenme Süresi (Dakika)")) else 0,
            "procurementDurationMinutes": int(r.get("Stok Tedarik Süresi (Dakika)", 0)) if not pd.isna(r.get("Stok Tedarik Süresi (Dakika)")) else 0,
            "manufacturingDurationMinutes": int(r.get("İmalat Süresi (Dakika)", 0)) if not pd.isna(r.get("İmalat Süresi (Dakika)")) else 0,
            "packagingDurationMinutes": int(r.get("Paketleme Süresi (Dakika)", 0)) if not pd.isna(r.get("Paketleme Süresi (Dakika)")) else 0,
            "shippingDurationMinutes": int(r.get("Teslimat Süresi (Dakika)", 0)) if not pd.isna(r.get("Teslimat Süresi (Dakika)")) else 0,
            "totalDeliveryDurationMinutes": int(r.get("Toplam Teslimat Süresi (Dakika)", 0)) if not pd.isna(r.get("Toplam Teslimat Süresi (Dakika)")) else 0,
        })
    return records, errors


def convert(xlsx_path: Path, config_path: Path, out_dir: Path, limit_orders: int | None = None):
    conversion_errors = []
    
    try:
        config = load_config(config_path)
    except Exception as e:
        conversion_errors.append(f"Config read error: {e}")
        config = {}
        
    sheets, read_errors = read_sheets(xlsx_path)
    conversion_errors.extend(read_errors)

    products, name_to_code, prod_errors = build_products(sheets.get("Products(ürün kartı)", pd.DataFrame()), config)
    conversion_errors.extend(prod_errors)

    boms, material_dictionary, bom_errors = build_boms(sheets.get("BOM(ürün ağacı)", pd.DataFrame()), name_to_code)
    conversion_errors.extend(bom_errors)

    orders_df = sheets.get("SalesOrders(satış siparişi)", pd.DataFrame())
    po_df = sheets.get("ProductionOrders(üretim emri)", pd.DataFrame())

    # Point 15 & 17: stockLevels from full ProductionOrders sheet BEFORE limit_orders filter
    stock_levels, stock_errors = build_stock_levels(po_df, name_to_code)
    conversion_errors.extend(stock_errors)

    if limit_orders is not None and not orders_df.empty and not po_df.empty:
        # Point 14: limit-orders yalnizca siparis ve bunlara bagli ground-truth orneklemesini sinirlar
        orders_df = orders_df.head(limit_orders)
        selected_so = set(orders_df["SalesOrderNo"])
        po_df = po_df[po_df["Satış siparişi No"].isin(selected_so)]

    orders, ord_errors = build_orders(orders_df, name_to_code)
    conversion_errors.extend(ord_errors)
    
    ground_truth, gt_errors = build_ground_truth(po_df)
    conversion_errors.extend(gt_errors)

    seed = {
        "orders": orders,
        "products": products,
        "boms": boms,
        "stockLevels": stock_levels,
        "openPurchaseOrders": [],
        "workOrders": [],
        "capacityCalendar": {
            "workCenters": [],
            "shifts": [],
            "holidays": [],
            "plannedDowntimes": [],
        },
        "shippingDurations": [],
    }

    out_dir.mkdir(parents=True, exist_ok=True)

    # ── I/O Öncesi Blokajlı Validation ──────────────────────────────
    # Hiçbir ana çıktı dosyası diske yazılmadan ÖNCE validation çalışır.
    # Yalnızca validation-report.json diske yazılır (run_validation içinde).
    val_report = run_validation(seed, ground_truth, material_dictionary, out_dir, conversion_errors)

    if not val_report["isValid"]:
        # Fatal hata: sadece validation-report.json yazıldı.
        # mock-erp-seed.json, prediction-ground-truth.json, material-dictionary
        # dosyaları diske YAZILMADI — bozuk veri downstream'e gidemez.
        return {
            "validation_report_path": str(out_dir / "validation-report.json"),
            "counts": {
                "orders": len(orders),
                "products": len(products),
                "boms": len(boms),
                "bom_lines": sum(len(b["lines"]) for b in boms),
                "stockLevels": len(stock_levels),
                "ground_truth_records": len(ground_truth),
                "unique_materials": len(material_dictionary),
            },
            "warnings": val_report["warnings"],
            "errors": val_report["errors"],
            "isValid": False,
        }

    # ── Başarılı Durum: Tüm çıktıları diske yaz ─────────────────────
    seed_path = out_dir / "mock-erp-seed.json"
    with open(seed_path, "w", encoding="utf-8") as f:
        json.dump(seed, f, ensure_ascii=False, indent=2)

    gt_path = out_dir / "prediction-ground-truth.json"
    with open(gt_path, "w", encoding="utf-8") as f:
        json.dump(ground_truth, f, ensure_ascii=False, indent=2)

    dict_path = out_dir / "material-dictionary-provisional.json"
    with open(dict_path, "w", encoding="utf-8") as f:
        json.dump(
            [{"name": n, "componentId": c} for n, c in sorted(material_dictionary.items())],
            f, ensure_ascii=False, indent=2
        )

    return {
        "seed_path": str(seed_path),
        "ground_truth_path": str(gt_path),
        "material_dictionary_path": str(dict_path),
        "validation_report_path": str(out_dir / "validation-report.json"),
        "counts": {
            "orders": len(orders),
            "products": len(products),
            "boms": len(boms),
            "bom_lines": sum(len(b["lines"]) for b in boms),
            "stockLevels": len(stock_levels),
            "ground_truth_records": len(ground_truth),
            "unique_materials": len(material_dictionary),
        },
        "warnings": val_report["warnings"],
        "errors": val_report["errors"],
        "isValid": val_report["isValid"]
    }


def sha256_of(path: Path) -> str:
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="ERP Excel -> Mock ERP Seed converter")
    parser.add_argument("--xlsx", required=True, type=Path)
    parser.add_argument("--config", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--limit-orders", type=int, default=None,
                         help="Onizleme icin ilk N siparisle sinirla (full run icin verilmez)")
    args = parser.parse_args()

    result = convert(args.xlsx, args.config, args.out, args.limit_orders)

    if not result.get("isValid", True):
        # Fatal hata: konsola hata detaylarını bas, exit(1) ile sonlandır.
        # Bu noktada yalnızca validation-report.json diske yazılmıştır.
        print(json.dumps(result, ensure_ascii=False, indent=2), file=sys.stderr)
        sys.exit(1)

    result["seed_sha256"] = sha256_of(Path(result["seed_path"]))
    print(json.dumps(result, ensure_ascii=False, indent=2))
