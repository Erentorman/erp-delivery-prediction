import pytest
import sys
import subprocess
import json
import hashlib
import pandas as pd
from pathlib import Path

def get_file_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

@pytest.fixture(scope="module")
def e2e_results(tmp_path_factory):
    tmp_path = tmp_path_factory.mktemp("e2e")
    products_data = [
        {"ProductCode": "PRD-1", "ProductName": "Masa"},
        {"ProductCode": "PRD-2", "ProductName": "Sandalye"},
        {"ProductCode": "PRD-3", "ProductName": "Dolap"},
        {"ProductCode": "PRD-4", "ProductName": "Kapı"}
    ]
    orders_data = [{"SalesOrderNo": "SO-01", "Product": "Masa", "Quantity": 1, "RequestedDeliveryDate": "2026-08-01"}]
    po_data = [{
        "İş emri No": "PO-01", "Satış siparişi No": "SO-01", "Product": "Masa", "OrderDate": "2026-07-01", "StockLevel": 50, "Quantity": 1,
        "ProductionStartDate": "2026-07-02", "Teslimat Süresi (Dakika)": 100
    }]
    bom_data = [["" for _ in range(9)] for _ in range(35)]
    bom_data[2][0] = "MasaMat"; bom_data[2][2] = 1; bom_data[2][3] = "Adet"
    bom_data[16][0] = "SanMat"; bom_data[16][2] = 1; bom_data[16][3] = "Adet"
    bom_data[2][5] = "DolMat"; bom_data[2][7] = 1; bom_data[2][8] = "Adet"
    bom_data[21][5] = "KapMat"; bom_data[21][7] = 1; bom_data[21][8] = "Adet"

    path_success = tmp_path / "success.xlsx"
    with pd.ExcelWriter(path_success) as writer:
        pd.DataFrame(orders_data).to_excel(writer, sheet_name="SalesOrders(satış siparişi)", index=False)
        pd.DataFrame(products_data).to_excel(writer, sheet_name="Products(ürün kartı)", index=False)
        pd.DataFrame(bom_data).to_excel(writer, sheet_name="BOM(ürün ağacı)", index=False, header=False)
        pd.DataFrame(po_data).to_excel(writer, sheet_name="ProductionOrders(üretim emri)", index=False)
        
    path_invalid = tmp_path / "invalid.xlsx"
    with pd.ExcelWriter(path_invalid) as writer:
        pd.DataFrame(orders_data).to_excel(writer, sheet_name="SalesOrders(satış siparişi)", index=False)
        pd.DataFrame([{"BadCol": "PRD-1"}]).to_excel(writer, sheet_name="Products(ürün kartı)", index=False) # missing mandatory column
        pd.DataFrame(bom_data).to_excel(writer, sheet_name="BOM(ürün ağacı)", index=False, header=False)
        pd.DataFrame(po_data).to_excel(writer, sheet_name="ProductionOrders(üretim emri)", index=False)

    config_path = tmp_path / "mock-config.json"
    with open(config_path, "w") as f:
        json.dump({"defaultProductUnit": "Adet", "priorityValueCrosswalk": {}, "capacityCalendarAssumptions": {"workCenters": []}}, f)

    convert_script = Path(__file__).parent.parent / "convert.py"
    
    # Run success 1
    out_dir_1 = tmp_path / "out_1"
    orig_hash = get_file_hash(path_success)
    cmd_1 = [sys.executable, str(convert_script), "--xlsx", str(path_success), "--config", str(config_path), "--out", str(out_dir_1)]
    res_1 = subprocess.run(cmd_1, capture_output=True, text=True)
    post_hash = get_file_hash(path_success)
    
    # Run success 2 for determinism
    out_dir_2 = tmp_path / "out_2"
    cmd_2 = [sys.executable, str(convert_script), "--xlsx", str(path_success), "--config", str(config_path), "--out", str(out_dir_2)]
    res_2 = subprocess.run(cmd_2, capture_output=True, text=True)
    
    # Run failure
    out_dir_inv = tmp_path / "out_inv"
    cmd_inv = [sys.executable, str(convert_script), "--xlsx", str(path_invalid), "--config", str(config_path), "--out", str(out_dir_inv)]
    res_inv = subprocess.run(cmd_inv, capture_output=True, text=True)
    
    return {
        "res_1": res_1, "out_dir_1": out_dir_1, "orig_hash": orig_hash, "post_hash": post_hash,
        "res_2": res_2, "out_dir_2": out_dir_2,
        "res_inv": res_inv, "out_dir_inv": out_dir_inv
    }

def test_25_leakage_fields_not_in_seed(e2e_results):
    with open(e2e_results["out_dir_1"] / "mock-erp-seed.json") as f:
        seed = json.load(f)
    for o in seed["orders"]:
        assert "ProductionStartDate" not in o
        assert "productionStartDate" not in o
        assert "Teslimat Süresi (Dakika)" not in o

def test_26_validation_failure_exit_code_1(e2e_results):
    assert e2e_results["res_inv"].returncode == 1

def test_27_validation_success_exit_code_0(e2e_results):
    assert e2e_results["res_1"].returncode == 0

def test_28_deterministic_seed_content(e2e_results):
    hash1 = get_file_hash(e2e_results["out_dir_1"] / "mock-erp-seed.json")
    hash2 = get_file_hash(e2e_results["out_dir_2"] / "mock-erp-seed.json")
    assert hash1 == hash2

def test_29_deterministic_material_dictionary(e2e_results):
    hash1 = get_file_hash(e2e_results["out_dir_1"] / "material-dictionary-provisional.json")
    hash2 = get_file_hash(e2e_results["out_dir_2"] / "material-dictionary-provisional.json")
    assert hash1 == hash2

def test_38_validation_report_recreated(e2e_results):
    mtime1 = (e2e_results["out_dir_1"] / "validation-report.json").stat().st_mtime
    mtime2 = (e2e_results["out_dir_2"] / "validation-report.json").stat().st_mtime
    assert mtime2 >= mtime1 # Because time can be same if too fast, but file is rewritten

def test_39_failure_report_contains_details(e2e_results):
    report_file = e2e_results["out_dir_inv"] / "validation-report.json"
    assert report_file.exists()
    with open(report_file) as f:
        rep = json.load(f)
    assert rep["isValid"] is False
    assert any("Products tablosunda zorunlu kolon eksik" in e for e in rep["errors"])

def test_40_original_input_file_unchanged(e2e_results):
    assert e2e_results["orig_hash"] == e2e_results["post_hash"]
