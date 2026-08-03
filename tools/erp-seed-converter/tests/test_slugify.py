import pytest
import sys
from pathlib import Path

# Add parent dir to sys.path so we can import convert
sys.path.insert(0, str(Path(__file__).parent.parent))

from convert import slugify

def test_01_turkish_characters():
    assert slugify("MasaıİşŞğĞüÜöÖçÇ", "MAT") == "MAT-MASAIISSGGUUOOCC"

def test_02_case_standardization():
    assert slugify("TeST mEtNi", "PRD") == "PRD-TEST-METNI"

def test_03_whitespace_cleaning():
    assert slugify("  test   metni  ", "BOM") == "BOM-TEST-METNI"

def test_04_special_character_cleaning():
    assert slugify("test@metni#deneme!", "MAT") == "MAT-TEST-METNI-DENEME"

def test_05_consecutive_separators():
    assert slugify("test   - @ - metni", "MAT") == "MAT-TEST-METNI"

def test_06_deterministic_id():
    assert slugify("Ahşap Masa", "MAT") == slugify("Ahşap Masa", "MAT")
    assert slugify("Ahşap Masa", "MAT") == "MAT-AHSAP-MASA"
