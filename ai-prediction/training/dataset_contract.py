"""Training dataset contract for the AI baseline model (SAD v1.2 §9.9, §18.4).

Single source of truth for target mapping, feature availability, leakage
rules and dataset/schema versions. Nothing in this module reads data or
performs I/O — see dataset_loader.py / prepare_dataset.py for that.
"""

from __future__ import annotations

from enum import Enum


# --- Versioning (SAD §9.9: kept at the top, not scattered across code) ---

FEATURE_SCHEMA_VERSION = 1
TRAINING_DATASET_VERSION = "synthetic-v1"

# --- Target ---

# SAD §9.9 canonical target. AI predicts this; it is never a date.
CANONICAL_TARGET_FIELD = "actual_total_working_lead_time_minutes"

# Raw ground-truth field considered as the mapping source. Semantic
# equivalence with CANONICAL_TARGET_FIELD is NOT assumed from the name —
# see target_mapping.validate_target_mapping().
RAW_TARGET_FIELD = "totalDeliveryDurationMinutes"

# Raw duration fields whose sum is expected to reconcile with
# RAW_TARGET_FIELD. Used by the semantic validation, not by the feature set.
RAW_STAGE_DURATION_FIELDS = (
    "orderProcessingDurationMinutes",
    "procurementDurationMinutes",
    "manufacturingDurationMinutes",
    "packagingDurationMinutes",
    "shippingDurationMinutes",
)

# --- Identifiers (never features) ---

IDENTIFIER_FIELDS = frozenset({
    "productionOrderId",
    "salesOrderId",
})

# --- Leakage fields (never features; only used for target validation) ---

LEAKAGE_FIELDS = frozenset({
    "productionStartDate",
    "productionFinishDate",
    "packagingStartDate",
    "packagingFinishDate",
    "estimatedDeliveryDate",
    "orderProcessingDurationMinutes",
    "procurementDurationMinutes",
    "manufacturingDurationMinutes",
    "packagingDurationMinutes",
    "shippingDurationMinutes",
    "totalDeliveryDurationMinutes",
})


class FeatureAvailability(str, Enum):
    AVAILABLE = "AVAILABLE"
    OPTIONAL = "OPTIONAL"
    UNAVAILABLE = "UNAVAILABLE"


class FeatureType(str, Enum):
    NUMERIC = "numeric"
    CATEGORICAL = "categorical"


class FeatureSpec:
    __slots__ = ("name", "feature_type", "availability", "rationale")

    def __init__(
        self,
        name: str,
        feature_type: FeatureType,
        availability: FeatureAvailability,
        rationale: str,
    ) -> None:
        self.name = name
        self.feature_type = feature_type
        self.availability = availability
        self.rationale = rationale


# SAD §9.9 feature contract, in the table's declared order. This order is
# the deterministic feature ordering used downstream for AVAILABLE/OPTIONAL
# features (see prepare_dataset.resolve_feature_order).
#
# Availability reflects what tools/erp-seed-converter currently produces
# (mock-erp-seed.json + prediction-ground-truth.json preview fixture), not
# a hypothetical future ERP. Nothing here is guessed: a feature is only
# AVAILABLE if it can be computed from an order-time-known raw field
# without inventing a business default.
FEATURE_CONTRACT: tuple[FeatureSpec, ...] = (
    FeatureSpec(
        "product_ref",
        FeatureType.CATEGORICAL,
        FeatureAvailability.AVAILABLE,
        "mock-erp-seed.json orders[].productId / products[] — known at order time.",
    ),
    FeatureSpec(
        "product_category",
        FeatureType.CATEGORICAL,
        FeatureAvailability.OPTIONAL,
        "SAD §9.9 Sprint-1 decision: Mock ERP ProductReadDto.PlanningClassification "
        "is always null in the MVP. Kept in the contract, unused until Phase-2.",
    ),
    FeatureSpec(
        "quantity",
        FeatureType.NUMERIC,
        FeatureAvailability.AVAILABLE,
        "mock-erp-seed.json orders[].quantity — known at order time.",
    ),
    FeatureSpec(
        "bom_item_count",
        FeatureType.NUMERIC,
        FeatureAvailability.AVAILABLE,
        "mock-erp-seed.json boms[].lines count for the order's product — "
        "structural BOM data, not time-dependent.",
    ),
    FeatureSpec(
        "missing_material_count",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json stockLevels[] is the LATEST snapshot only "
        "(converter takes the last ProductionOrders row per product), not a "
        "per-order point-in-time snapshot. Computing this against current "
        "stock for historical orders would either leak future stock state "
        "or misrepresent the order-time state. No reliable source yet.",
    ),
    FeatureSpec(
        "total_missing_quantity",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "Same root cause as missing_material_count.",
    ),
    FeatureSpec(
        "maximum_supplier_lead_time_days",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json openPurchaseOrders[] is intentionally empty "
        "(source Excel has no open-PO data; see erp-seed-converter field "
        "mapping report).",
    ),
    FeatureSpec(
        "operation_count",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json workOrders[] is intentionally empty.",
    ),
    FeatureSpec(
        "total_standard_operation_minutes",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json workOrders[] is intentionally empty.",
    ),
    FeatureSpec(
        "work_center_load_ratio",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "SAD source is a capacityCalendar snapshot, which is intentionally "
        "empty in mock-erp-seed.json. prediction-ground-truth.json has a "
        "similarly-named factoryWorkloadPercentAtOrderTime, but per the "
        "no-blind-name-mapping rule its semantic equivalence to the SAD "
        "capacity-snapshot-based ratio has not been validated, so it is not "
        "auto-mapped.",
    ),
    FeatureSpec(
        "active_work_order_count",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json workOrders[] is intentionally empty.",
    ),
    FeatureSpec(
        "shift_capacity_minutes",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json capacityCalendar is intentionally empty.",
    ),
    FeatureSpec(
        "holiday_count",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json capacityCalendar is intentionally empty.",
    ),
    FeatureSpec(
        "planned_downtime_minutes",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "mock-erp-seed.json capacityCalendar is intentionally empty.",
    ),
    FeatureSpec(
        "shipping_duration_minutes",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "SAD source is an order-time shipping snapshot (mock-erp-seed.json "
        "shippingDurations[], intentionally empty). "
        "prediction-ground-truth.json shippingDurationMinutes is a REALIZED "
        "duration and is also a LEAKAGE_FIELDS member — see the Shipping "
        "Duration Warning in the task spec; it must never be auto-mapped "
        "into this feature by name similarity.",
    ),
    FeatureSpec(
        "requested_delivery_lead_minutes",
        FeatureType.NUMERIC,
        FeatureAvailability.UNAVAILABLE,
        "Derivable in principle from orderDate and orders[].requestedDeliveryDate "
        "(both known at order time), but converting a calendar-day gap into "
        "working minutes requires the C# WorkingCalendar service (holidays, "
        "shift hours). A naive calendar-minutes conversion in Python would be "
        "a hardcoded business default, which this contract forbids.",
    ),
)

FEATURE_CONTRACT_BY_NAME = {spec.name: spec for spec in FEATURE_CONTRACT}

# --- Train/test split ---

SPLIT_RANDOM_STATE = 42
SPLIT_TEST_SIZE = 0.20
