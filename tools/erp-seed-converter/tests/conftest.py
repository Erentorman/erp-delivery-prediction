import pytest
import pandas as pd
from pathlib import Path
import json

@pytest.fixture
def mock_config():
    return {
        "defaultProductUnit": "Adet",
        "priorityValueCrosswalk": {},
        "capacityCalendarAssumptions": {
            "workCenters": []
        }
    }

@pytest.fixture
def mock_config_path(tmp_path, mock_config):
    config_file = tmp_path / "mock-config.json"
    config_file.write_text(json.dumps(mock_config, ensure_ascii=False))
    return config_file

@pytest.fixture
def valid_sheets_data():
    return {
        "SalesOrders(satış siparişi)": pd.DataFrame([
            {"SalesOrderNo": "SO-01", "Product": "Masa", "Quantity": 10, "RequestedDeliveryDate": "2026-08-01 00:00:00"}
        ]),
        "Products(ürün kartı)": pd.DataFrame([
            {"ProductCode": "PRD-01", "ProductName": "Masa"}
        ]),
        "BOM(ürün ağacı)": pd.DataFrame([
            ["Masa", "Ahşap", "", 5.0, "m2"],
            ["", "Vida", "", 20.0, "Adet"]
        ]),
        "ProductionOrders(üretim emri)": pd.DataFrame([
            {"İş emri No": "PO-01", "Satış siparişi No": "SO-01", "Product": "Masa", "OrderDate": "2026-07-01", "StockLevel": 50, "Quantity": 10}
        ])
    }

@pytest.fixture
def create_excel(tmp_path):
    def _create(sheets_dict, filename="test.xlsx"):
        path = tmp_path / filename
        with pd.ExcelWriter(path) as writer:
            for sheet_name, df in sheets_dict.items():
                header = False if sheet_name == "BOM(ürün ağacı)" else True
                df.to_excel(writer, sheet_name=sheet_name, index=False, header=header)
        return path
    return _create
