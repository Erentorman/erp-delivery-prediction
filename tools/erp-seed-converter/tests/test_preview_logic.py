import pytest
import sys
import pandas as pd
import json
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))
from convert import convert

@pytest.fixture(scope="module")
def preview_result(tmp_path_factory):
    # This runs once for all preview tests
    tmp_path = tmp_path_factory.mktemp("preview")
    
    products_data = [
        {"ProductCode": "PRD-1", "ProductName": "Masa"},
        {"ProductCode": "PRD-2", "ProductName": "Sandalye"},
        {"ProductCode": "PRD-3", "ProductName": "Dolap"},
        {"ProductCode": "PRD-4", "ProductName": "Kapı"}
    ]
    product_names = ["Masa", "Sandalye", "Dolap", "Kapı"]
    orders_data = [{"SalesOrderNo": f"SO-{i:02d}", "Product": product_names[i%4], "Quantity": 1, "RequestedDeliveryDate": "2026-08-01"} for i in range(1, 11)]
    po_data = [{"İş emri No": f"PO-{i:02d}", "Satış siparişi No": f"SO-{i:02d}", "Product": product_names[i%4], "OrderDate": "2026-07-01", "StockLevel": 50, "Quantity": 1} for i in range(1, 11)]
    
    bom_data = [["" for _ in range(9)] for _ in range(35)]
    bom_data[2][0] = "MasaMat"; bom_data[2][2] = 1; bom_data[2][3] = "Adet"
    bom_data[16][0] = "SanMat"; bom_data[16][2] = 1; bom_data[16][3] = "Adet"
    bom_data[2][5] = "DolMat"; bom_data[2][7] = 1; bom_data[2][8] = "Adet"
    bom_data[21][5] = "KapMat"; bom_data[21][7] = 1; bom_data[21][8] = "Adet"
    
    path = tmp_path / "test.xlsx"
    with pd.ExcelWriter(path) as writer:
        pd.DataFrame(orders_data).to_excel(writer, sheet_name="SalesOrders(satış siparişi)", index=False)
        pd.DataFrame(products_data).to_excel(writer, sheet_name="Products(ürün kartı)", index=False)
        pd.DataFrame(bom_data).to_excel(writer, sheet_name="BOM(ürün ağacı)", index=False, header=False)
        pd.DataFrame(po_data).to_excel(writer, sheet_name="ProductionOrders(üretim emri)", index=False)
        
    config_path = tmp_path / "mock-config.json"
    with open(config_path, "w") as f:
        json.dump({"defaultProductUnit": "Adet", "priorityValueCrosswalk": {}, "capacityCalendarAssumptions": {"workCenters": []}}, f)
        
    out_dir = tmp_path / "out"
    result = convert(path, config_path, out_dir, limit_orders=5)
    
    with open(result["seed_path"]) as f:
        seed = json.load(f)
        
    return {"result": result, "seed": seed}

def test_20_preview_limit_orders_behavior(preview_result):
    assert preview_result["result"]["isValid"] is True

def test_21_preview_orders_count_5(preview_result):
    assert preview_result["result"]["counts"]["orders"] == 5
    assert len(preview_result["seed"]["orders"]) == 5

def test_22_preview_ground_truth_count_5(preview_result):
    assert preview_result["result"]["counts"]["ground_truth_records"] == 5

def test_23_preview_stocklevels_count_4(preview_result):
    assert preview_result["result"]["counts"]["stockLevels"] == 4
    assert len(preview_result["seed"]["stockLevels"]) == 4

def test_24_preview_stocklevels_from_full_production_orders(preview_result):
    stock_refs = set(s["productReference"] for s in preview_result["seed"]["stockLevels"])
    assert stock_refs == {"PRD-1", "PRD-2", "PRD-3", "PRD-4"}
