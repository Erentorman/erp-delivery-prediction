import json
from pathlib import Path

def run_validation(seed: dict, ground_truth: list, material_dict: dict, out_dir: Path, convert_warnings: list) -> dict:
    report = {
        "schemaVersion": "1.0.0",
        "isValid": True,
        "totalChecks": 0,
        "passedChecks": 0,
        "failedChecks": 0,
        "errors": [],
        "warnings": [],
        "recordCounts": {},
        "duplicateIds": {"products": [], "orders": [], "boms": []},
        "brokenReferences": {"orders": [], "boms": [], "bomLines": [], "stockLevels": []},
        "slugCollisions": [],
        "fallbackUsage": {
            "defaultProductUnitUsage": len(seed.get("products", [])),
            "capacityFallbackUsage": 0,
            "shippingFallbackUsage": 0,
            "supplierLeadTimeFallbackUsage": 0,
            "reservedQuantityAssumedZero": len(seed.get("stockLevels", [])),
            "availableQuantityAssumedOnHand": len(seed.get("stockLevels", []))
        },
        "consciouslyEmptySections": {
            "openPurchaseOrders": len(seed.get("openPurchaseOrders", [])) == 0,
            "workOrders": len(seed.get("workOrders", [])) == 0,
            "capacityCalendar.workCenters": len(seed.get("capacityCalendar", {}).get("workCenters", [])) == 0,
            "shippingDurations": len(seed.get("shippingDurations", [])) == 0
        },
        "sourceInputSummary": {}
    }

    def add_error(msg):
        report["errors"].append(msg)
        report["isValid"] = False
        report["failedChecks"] += 1

    def add_warning(msg):
        report["warnings"].append(msg)

    # 1. Root fields non-null
    root_fields = ["orders", "products", "boms", "stockLevels", "openPurchaseOrders", "workOrders", "capacityCalendar", "shippingDurations"]
    for rf in root_fields:
        report["totalChecks"] += 1
        if seed.get(rf) is None:
            add_error(f"Root field '{rf}' is null or missing.")
        else:
            report["passedChecks"] += 1

    # 2. capacityCalendar subfields non-null
    if seed.get("capacityCalendar") is not None:
        cc = seed["capacityCalendar"]
        for subf in ["workCenters", "shifts", "holidays", "plannedDowntimes"]:
            report["totalChecks"] += 1
            if cc.get(subf) is None:
                add_error(f"capacityCalendar.{subf} is null or missing.")
            else:
                report["passedChecks"] += 1

    # 3. Duplicate IDs
    product_ids = set()
    for p in seed.get("products", []):
        report["totalChecks"] += 1
        pid = p.get("id")
        if pid in product_ids:
            report["duplicateIds"]["products"].append(pid)
            add_error(f"Duplicate product ID: {pid}")
        else:
            product_ids.add(pid)
            report["passedChecks"] += 1

    order_ids = set()
    for o in seed.get("orders", []):
        report["totalChecks"] += 1
        oid = o.get("id")
        if oid in order_ids:
            report["duplicateIds"]["orders"].append(oid)
            add_error(f"Duplicate order ID: {oid}")
        else:
            order_ids.add(oid)
            report["passedChecks"] += 1

    bom_product_ids = set()
    for b in seed.get("boms", []):
        report["totalChecks"] += 1
        bpid = b.get("productId")
        if bpid in bom_product_ids:
            report["duplicateIds"]["boms"].append(bpid)
            add_error(f"Duplicate BOM for product ID: {bpid}")
        else:
            bom_product_ids.add(bpid)
            report["passedChecks"] += 1

    # 4. Broken references
    for o in seed.get("orders", []):
        report["totalChecks"] += 1
        if o.get("productId") not in product_ids:
            report["brokenReferences"]["orders"].append(o.get("id"))
            add_error(f"Order {o.get('id')} references unknown product {o.get('productId')}")
        else:
            report["passedChecks"] += 1

    mat_components = set(material_dict.values())
    for b in seed.get("boms", []):
        report["totalChecks"] += 1
        if b.get("productId") not in product_ids:
            report["brokenReferences"]["boms"].append(b.get("productId"))
            add_error(f"BOM references unknown product {b.get('productId')}")
        else:
            report["passedChecks"] += 1
            
        for l in b.get("lines", []):
            report["totalChecks"] += 1
            cid = l.get("componentId")
            if cid not in mat_components:
                report["brokenReferences"]["bomLines"].append(cid)
                add_error(f"BOM line references unknown component {cid}")
            else:
                report["passedChecks"] += 1

    for s in seed.get("stockLevels", []):
        report["totalChecks"] += 1
        if s.get("productReference") not in product_ids:
            report["brokenReferences"]["stockLevels"].append(s.get("productReference"))
            add_error(f"StockLevel references unknown product {s.get('productReference')}")
        else:
            report["passedChecks"] += 1

    # 4.5. Mandatory record fields non-null

    def check_required(condition, message):
        report["totalChecks"] += 1
        if condition:
            report["passedChecks"] += 1
        else:
            add_error(message)

    for p in seed.get("products", []):
        check_required(
            bool(p.get("id")),
            "Product id is null",
        )
        check_required(
            bool(p.get("name")),
            f"Product name is null for id {p.get('id')}",
        )

    for o in seed.get("orders", []):
        check_required(
            bool(o.get("id")),
            "Order id is null",
        )
        check_required(
            bool(o.get("productId")),
            f"Order productId is null for id {o.get('id')}",
        )
        check_required(
            o.get("quantity") is not None,
            f"Order quantity is null for id {o.get('id')}",
        )

    for b in seed.get("boms", []):
        check_required(
            bool(b.get("productId")),
            "BOM productId is null",
        )

        for line in b.get("lines", []):
            check_required(
                bool(line.get("componentId")),
                f"BOM line componentId is null for BOM {b.get('productId')}",
            )
            check_required(
                line.get("quantity") is not None,
                f"BOM line quantity is null for BOM {b.get('productId')}",
            )

    for stock_level in seed.get("stockLevels", []):
        check_required(
            bool(stock_level.get("productReference")),
            "StockLevel productReference is null",
        )
        check_required(
            stock_level.get("onHandQuantity") is not None,
            (
                "StockLevel onHandQuantity is null for "
                f"{stock_level.get('productReference')}"
            ),
        )

    # 5. Leakage fields check
    leakage_fields = {"productionStartDate", "productionFinishDate", "estimatedDeliveryDate", "totalDeliveryDurationMinutes"}
    for o in seed.get("orders", []):
        report["totalChecks"] += 1
        has_leakage = any(k in leakage_fields for k in o.keys())
        if has_leakage:
            add_error(f"Order {o.get('id')} contains leakage fields.")
        else:
            report["passedChecks"] += 1

    # 6. Slug collisions
    slug_to_names = {}
    for name, slug in material_dict.items():
        slug_to_names.setdefault(slug, []).append(name)
    
    for slug, names in slug_to_names.items():
        report["totalChecks"] += 1
        if len(names) > 1:
            report["slugCollisions"].append({"slug": slug, "names": names})
            add_error(f"Slug collision for '{slug}': mapped from {names}")
        else:
            report["passedChecks"] += 1

    # Treat unmapped warnings from generator as validation errors (fatal)
    for w in convert_warnings:
        report["totalChecks"] += 1
        add_error(f"Conversion error: {w}")

    # record counts
    report["recordCounts"] = {
        "orders": len(seed.get("orders", [])),
        "products": len(seed.get("products", [])),
        "boms": len(seed.get("boms", [])),
        "stockLevels": len(seed.get("stockLevels", [])),
        "groundTruth": len(ground_truth),
        "uniqueMaterials": len(material_dict)
    }

    # Write report
    report_path = out_dir / "validation-report.json"
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=2)

    return report
