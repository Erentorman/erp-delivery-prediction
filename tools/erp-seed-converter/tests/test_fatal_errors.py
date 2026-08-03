import pytest
import sys
import subprocess
import pandas as pd
from pathlib import Path

def run_cli_with_data(create_excel, mock_config_path, tmp_path, products, orders, boms, po):
    path = create_excel({
        "SalesOrders(satış siparişi)": pd.DataFrame(orders),
        "Products(ürün kartı)": pd.DataFrame(products),
        "BOM(ürün ağacı)": pd.DataFrame(boms),
        "ProductionOrders(üretim emri)": pd.DataFrame(po)
    })
    out_dir = tmp_path / "out"
    convert_script = Path(__file__).parent.parent / "convert.py"
    cmd = [sys.executable, str(convert_script), "--xlsx", str(path), "--config", str(mock_config_path), "--out", str(out_dir)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    return res, out_dir

@pytest.fixture
def base_data():
    return {
        "products": [
            {"ProductCode": "PRD-1", "ProductName": "Masa"},
            {"ProductCode": "PRD-2", "ProductName": "Sandalye"},
            {"ProductCode": "PRD-3", "ProductName": "Dolap"},
            {"ProductCode": "PRD-4", "ProductName": "Kapı"}
        ],
        "orders": [{"SalesOrderNo": "SO-01", "Product": "Masa", "Quantity": 1, "RequestedDeliveryDate": "2026-08-01"}],
        "po": [{"İş emri No": "PO-01", "Satış siparişi No": "SO-01", "Product": "Masa", "OrderDate": "2026-07-01", "StockLevel": 50, "Quantity": 1}],
        "boms": []
    }

@pytest.fixture
def valid_bom_data():
    b = [["" for _ in range(9)] for _ in range(35)]
    b[2][0] = "MasaMat"; b[2][2] = 1; b[2][3] = "Adet"
    b[16][0] = "SanMat"; b[16][2] = 1; b[16][3] = "Adet"
    b[2][5] = "DolMat"; b[2][7] = 1; b[2][8] = "Adet"
    b[21][5] = "KapMat"; b[21][7] = 1; b[21][8] = "Adet"
    return b

def test_07_slug_collision_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    # MasaMat already slugs to MAT-MASAMAT. Let's add another product that slugs to same
    b = valid_bom_data
    b[3][0] = "Masa Mat" # Same slug: MAT-MASA-MAT (wait, MasaMat -> MAT-MASAMAT. Masa Mat -> MAT-MASA-MAT. We need exact collision)
    # Ahsap Masa vs Ahşap Masa
    b[2][0] = "Ahsap Masa"
    b[3][0] = "Ahşap Masa"
    b[3][2] = 1; b[3][3] = "Adet"
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, base_data["products"], base_data["orders"], b, base_data["po"])
    assert res.returncode == 1
    assert "Slug collision" in res.stderr

def test_09_unknown_so_product_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    orders = [{"SalesOrderNo": "SO-01", "Product": "Bilinmeyen", "Quantity": 1, "RequestedDeliveryDate": ""}]
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, base_data["products"], orders, valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_10_unknown_bom_product_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    # Remove one product from products list
    products = base_data["products"][1:] # removed Masa
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, products, base_data["orders"], valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_11_duplicate_order_id_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    orders = base_data["orders"] * 2 # duplicate
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, base_data["products"], orders, valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_12_duplicate_product_id_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    products = base_data["products"] + [base_data["products"][0]]
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, products, base_data["orders"], valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_13_broken_order_reference_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    # Similar to 09, product not in products list creates broken ref in Validation
    orders = [{"SalesOrderNo": "SO-01", "Product": "Broken", "Quantity": 1, "RequestedDeliveryDate": ""}]
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, base_data["products"], orders, valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_14_broken_bom_reference_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    # Similar to 10
    products = base_data["products"][1:]
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, products, base_data["orders"], valid_bom_data, base_data["po"])
    assert res.returncode == 1

def test_15_broken_stock_reference_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    po = [{"İş emri No": "PO-01", "Satış siparişi No": "SO-01", "Product": "BilinmeyenStock", "OrderDate": "2026-07-01", "StockLevel": 50, "Quantity": 1}]
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, base_data["products"], base_data["orders"], valid_bom_data, po)
    assert res.returncode == 1

def test_16_missing_sheet_fatal_error(create_excel, mock_config_path, tmp_path):
    path = create_excel({"Products(ürün kartı)": pd.DataFrame()}) # missing other 3
    out_dir = tmp_path / "out"
    cmd = [sys.executable, str(Path(__file__).parent.parent / "convert.py"), "--xlsx", str(path), "--config", str(mock_config_path), "--out", str(out_dir)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    assert res.returncode == 1
    assert "Beklenen sheet bulunamadi" in res.stderr

def test_17_missing_mandatory_column_fatal_error(create_excel, mock_config_path, tmp_path, base_data, valid_bom_data):
    # Drop ProductName
    products = pd.DataFrame(base_data["products"]).drop(columns=["ProductName"]).to_dict("records")
    res, _ = run_cli_with_data(create_excel, mock_config_path, tmp_path, products, base_data["orders"], valid_bom_data, base_data["po"])
    assert res.returncode == 1
    assert "zorunlu kolon eksik" in res.stderr
