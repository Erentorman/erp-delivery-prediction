import pytest
import sys
import pandas as pd
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))
from convert import build_products, build_orders, build_boms
from validation import run_validation

def test_08_product_lookup_success(mock_config, valid_sheets_data):
    products_df = valid_sheets_data["Products(ürün kartı)"]
    products, name_to_code, errors = build_products(products_df, mock_config)
    assert not errors
    assert len(products) == 1
    assert name_to_code["Masa"] == "PRD-01"
    
    orders_df = valid_sheets_data["SalesOrders(satış siparişi)"]
    orders, ord_errors = build_orders(orders_df, name_to_code)
    assert not ord_errors
    assert orders[0]["productId"] == "PRD-01"

def test_18_bom_grid_parse_4_groups():
    data = [["" for _ in range(9)] for _ in range(35)]
    data[2][0] = "Masa Malzemesi"; data[2][2] = 5; data[2][3] = "Adet"
    data[16][0] = "Sandalye Malzemesi"; data[16][2] = 4; data[16][3] = "Adet"
    data[2][5] = "Dolap Malzemesi"; data[2][7] = 3; data[2][8] = "Adet"
    data[21][5] = "Kapi Malzemesi"; data[21][7] = 1; data[21][8] = "Adet"

    bom_df = pd.DataFrame(data)
    name_to_code = {"Masa": "PRD-M", "Sandalye": "PRD-S", "Dolap": "PRD-D", "Kapı": "PRD-K"}
    boms, mat_dict, errors = build_boms(bom_df, name_to_code)
    
    assert not errors
    assert len(boms) == 4
    product_ids = [b["productId"] for b in boms]
    for p in ["PRD-M", "PRD-S", "PRD-D", "PRD-K"]:
        assert p in product_ids

def test_19_bom_line_count_51():
    data = [["" for _ in range(9)] for _ in range(35)]
    for i in range(2, 13):
        data[i][0] = f"MasaMat{i}"; data[i][2] = 1; data[i][3] = "Adet"
    for i in range(16, 27):
        data[i][0] = f"SanMat{i}"; data[i][2] = 1; data[i][3] = "Adet"
    for i in range(2, 18):
        data[i][5] = f"DolMat{i}"; data[i][7] = 1; data[i][8] = "Adet"
    for i in range(21, 34):
        data[i][5] = f"KapMat{i}"; data[i][7] = 1; data[i][8] = "Adet"
        
    bom_df = pd.DataFrame(data)
    name_to_code = {"Masa": "PRD-M", "Sandalye": "PRD-S", "Dolap": "PRD-D", "Kapı": "PRD-K"}
    boms, _, errors = build_boms(bom_df, name_to_code)
    
    assert not errors
    total_lines = sum(len(b["lines"]) for b in boms)
    assert total_lines == 51

@pytest.fixture
def empty_seed_validation_report(tmp_path):
    seed = {
        "products": [{"id": "P1", "name": "Prod1", "unit": "Adet"}, {"id": "P2", "name": "Prod2", "unit": "Adet"}],
        "stockLevels": [{"productReference": "P1", "onHandQuantity": 10}, {"productReference": "P2", "onHandQuantity": 20}],
        "orders": [], "boms": [], "openPurchaseOrders": [], "workOrders": [],
        "capacityCalendar": {"workCenters": [], "shifts": [], "holidays": [], "plannedDowntimes": []},
        "shippingDurations": []
    }
    return run_validation(seed, [], {}, tmp_path, [])

def test_30_capacity_calendar_lists_non_null(empty_seed_validation_report):
    assert empty_seed_validation_report["isValid"] is True

def test_31_empty_root_sections_reported(empty_seed_validation_report):
    empty_sections = empty_seed_validation_report["consciouslyEmptySections"]
    assert empty_sections["openPurchaseOrders"] is True
    assert empty_sections["workOrders"] is True

def test_32_capacity_fallback_zero(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["capacityFallbackUsage"] == 0

def test_33_shipping_fallback_zero(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["shippingFallbackUsage"] == 0

def test_34_supplier_leadtime_fallback_zero(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["supplierLeadTimeFallbackUsage"] == 0

def test_35_default_product_unit_usage(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["defaultProductUnitUsage"] == 2

def test_36_reserved_qty_zero_usage(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["reservedQuantityAssumedZero"] == 2

def test_37_available_qty_onhand_usage(empty_seed_validation_report):
    assert empty_seed_validation_report["fallbackUsage"]["availableQuantityAssumedOnHand"] == 2


def test_38_validation_check_counts_are_consistent(
    empty_seed_validation_report,
):
    report = empty_seed_validation_report

    assert report["totalChecks"] == (
        report["passedChecks"] + report["failedChecks"]
    )