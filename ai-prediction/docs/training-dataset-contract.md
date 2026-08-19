# AI Training Dataset Contract (T-905)

Status: dataset preparation only. **No model is trained by this task or this
document.** Consumed by T-907.

Source of truth for architectural decisions remains `docs/SAD-v1.2.md` §9.9
(AI Feature Contract) and §18.4 (AI Eğitim ve Yeniden Eğitim Stratejisi).
This document records how that contract was applied to the data actually
available in this repository today, and why several SAD-listed features are
not yet populated.

## Pipeline

```
Raw ERP Dataset
  -> Dataset Loader           (training/dataset_loader.py)
  -> Schema Validation        (row count, required columns, null/type checks)
  -> Ground-Truth Validation  (numeric types, target sanity: not null/negative/zero)
  -> Target Mapping Validation(training/target_mapping.py)
  -> Leakage Guard            (training/prepare_dataset.py: leakage_guard)
  -> Feature Availability Resolution
  -> Canonical Dataset        (deterministic feature order + preprocessing)
  -> Deterministic Split      (train.json / test.json)
  -> T-907 Model Training
```

Entry point: `python -m training.prepare_dataset` (run from `ai-prediction/`).
Output goes to `ai-prediction/training/output/` (git-ignored — regenerated
from source, never committed).

## Raw source

No "full" dataset is committed to this repository — `tools/erp-seed-converter`
intentionally does not commit the source Excel or its generated `output/`
(see `tools/erp-seed-converter/README.md`). The only raw data available
in-repo is the **preview fixture** (5 rows), which this pipeline defaults to:

- `tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/prediction-ground-truth.json`
- `tests/App.Integration.Tests/Fixtures/ErpSeedConverterPreview/mock-erp-seed.json`

Both paths are overridable via `--ground-truth` / `--seed` CLI flags so a
future full dataset (once converted) can be prepared with the same contract
without code changes.

## Canonical target

SAD §9.9 canonical target: **`actual_total_working_lead_time_minutes`**.

The raw ground-truth field `totalDeliveryDurationMinutes` is **not** assumed
to be the same thing just because a target-shaped field exists. Before
mapping, `target_mapping.validate_target_mapping()` runs two independent
checks on every row and raises `TargetMappingSemanticError` (pipeline stops,
no training) if either fails:

1. **Component-sum check** — `totalDeliveryDurationMinutes` must equal
   `orderProcessingDurationMinutes + procurementDurationMinutes +
   manufacturingDurationMinutes + packagingDurationMinutes +
   shippingDurationMinutes`. Confirms the field is a total of realized stage
   durations, not an unrelated number. Holds for all 5 preview rows
   (e.g. `144 + 0 + 429 + 122 + 1236 = 1931`).
2. **Working-minutes plausibility check** — the raw target must not exceed
   the calendar-minute span between `orderDate` and `packagingFinishDate`.
   Working minutes are a subset of calendar minutes; if the raw target were
   calendar-elapsed time it would often exceed or equal that span across
   multi-day orders. In the preview fixture it is consistently well below
   the calendar span, which is the expected shape for a working-minutes
   quantity.

Once validated, the mapping is applied by `target_mapping.map_raw_target_to_canonical()`:

```
totalDeliveryDurationMinutes  ->  actual_total_working_lead_time_minutes
```

This mapping is defined in exactly one place
(`training/dataset_contract.py: RAW_TARGET_FIELD`) and appears in the
generated `metadata.json` (`raw_target_field` / `canonical_target_field`)
for every prepared dataset — not scattered across the codebase.

## Leakage rules

`training/dataset_contract.py: LEAKAGE_FIELDS` and `IDENTIFIER_FIELDS` list
every field that must never reach the feature vector `X`:

- Realized dates: `productionStartDate`, `productionFinishDate`,
  `packagingStartDate`, `packagingFinishDate`, `estimatedDeliveryDate`.
- Realized durations: all 5 stage-duration fields plus the raw target itself
  (`totalDeliveryDurationMinutes`) — usable only for target-mapping
  validation, never as a feature.
- Identifiers: `productionOrderId`, `salesOrderId`.

`prepare_dataset.leakage_guard()` asserts none of these appear in the
resolved feature set before any row is built, and `build_canonical_rows()`
only ever populates the fields explicitly declared `AVAILABLE` in the
feature contract — so leakage fields structurally cannot enter `X`.

### Shipping duration warning

SAD's `shipping_duration_minutes` feature is an **order-time snapshot**
(sourced from `capacityCalendar`/`shippingDurations`, which
`erp-seed-converter` leaves intentionally empty). The ground-truth field
`shippingDurationMinutes` is a **realized** duration and a `LEAKAGE_FIELDS`
member. The two are not auto-mapped by name similarity — that would leak the
outcome into the input. `shipping_duration_minutes` is therefore
`UNAVAILABLE` until a genuine order-time shipping snapshot source exists.

## Feature availability matrix (SAD §9.9)

Declared in `training/dataset_contract.py: FEATURE_CONTRACT`, in this order
(the same order used for the resolved, deterministic feature vector):

| Feature | Status | Why |
|---|---|---|
| `product_ref` | **AVAILABLE** | `mock-erp-seed.json orders[].productId`, known at order time. |
| `product_category` | OPTIONAL (Phase-2) | SAD Sprint-1 decision: `PlanningClassification` is always `null` in the MVP. Declared, unused. |
| `quantity` | **AVAILABLE** | `mock-erp-seed.json orders[].quantity`, known at order time. |
| `bom_item_count` | **AVAILABLE** | `mock-erp-seed.json boms[].lines` count for the order's product — structural, not time-dependent. |
| `missing_material_count` | UNAVAILABLE | `stockLevels[]` is a latest-only snapshot, not per-order point-in-time; using it would misrepresent or leak stock state. |
| `total_missing_quantity` | UNAVAILABLE | Same root cause as above. |
| `maximum_supplier_lead_time_days` | UNAVAILABLE | `openPurchaseOrders[]` intentionally empty. |
| `operation_count` | UNAVAILABLE | `workOrders[]` intentionally empty. |
| `total_standard_operation_minutes` | UNAVAILABLE | `workOrders[]` intentionally empty. |
| `work_center_load_ratio` | UNAVAILABLE | SAD source (`capacityCalendar`) is empty; `factoryWorkloadPercentAtOrderTime` is similarly named but not validated as semantically equivalent — not auto-mapped. |
| `active_work_order_count` | UNAVAILABLE | `workOrders[]` intentionally empty. |
| `shift_capacity_minutes` | UNAVAILABLE | `capacityCalendar` intentionally empty. |
| `holiday_count` | UNAVAILABLE | `capacityCalendar` intentionally empty. |
| `planned_downtime_minutes` | UNAVAILABLE | `capacityCalendar` intentionally empty. |
| `shipping_duration_minutes` | UNAVAILABLE | See Shipping Duration Warning above. |
| `requested_delivery_lead_minutes` | UNAVAILABLE | Derivable in principle from `orderDate` + `requestedDeliveryDate`, but converting a calendar-day gap to working minutes needs the C# `WorkingCalendar` service; a naive Python conversion would be a hardcoded business default. |

**Resolved feature set for the current source data: `product_ref`, `quantity`,
`bom_item_count`.** This is intentionally small — no fake, random, or
hardcoded-default values are produced for `UNAVAILABLE` features. As richer
seed data becomes available (workOrders, capacityCalendar, open POs), the
corresponding rows in the matrix change to `AVAILABLE` and the resolved
feature vector grows; `feature_schema_version` is bumped whenever that
happens.

## Preprocessing contract

- **Numeric** (`quantity`, `bom_item_count`): validated finite and
  non-negative by `dataset_loader.validate_numeric_types`, then passed
  through as `float`. No imputation — a missing/invalid numeric value is a
  hard validation failure (`DatasetValidationError`), not silently defaulted.
- **Categorical** (`product_ref`): raw strings are never passed to the model.
  `prepare_dataset.build_categorical_encoding()` sorts the unique observed
  values and assigns `0..N-1` deterministically (no hashing, no
  training-order dependence). Unknown categories at inference time map to
  the reserved code `UNKNOWN_CATEGORY_CODE = -1` rather than raising —
  training and inference must use the same semantics, and this is the
  same encoding map training and inference load from `metadata.json`.
- Breaking changes to either strategy require bumping `feature_schema_version`.

## Versioning

Both version fields are defined once, in `training/dataset_contract.py`, and
echoed into every `metadata.json` output — never redefined elsewhere:

- `feature_schema_version = 1`
- `training_dataset_version = "synthetic-v1"`

**`training_dataset_version` names the schema/contract, not the row count or
completeness of a given run.** Because only the 5-row preview fixture exists
in-repo today (see Known limitation below), every generated `metadata.json`
also carries a `source` block (`ground_truth_path`, `seed_path`,
`is_preview_source`) so a `synthetic-v1` artifact built from the preview
fixture can never be mistaken for a finished, full synthetic dataset. The
CLI prints an explicit warning when `is_preview_source` is `true`. When a
full synthetic dataset is produced later, it is prepared under the same
`synthetic-v1` contract with `is_preview_source: false` — the version bumps
only on a genuine schema/contract change, not when the preview fixture is
swapped for the full export.

## Train/test split

Deterministic: `random_state = 42`, `test_size = 0.20`
(`training/dataset_contract.py`). `prepare_dataset.deterministic_split()`
sorts rows by `order_ref` before shuffling with a seeded `random.Random`, so
the same dataset version + split config always produces the same train/test
partition — verified by
`tests/test_dataset_contract.py: test_deterministic_split_reproducible`.

## Known limitation

The only in-repo raw dataset is the 5-row preview fixture. The pipeline,
contract, and tests are written against the full `mock-erp-seed.json` /
`prediction-ground-truth.json` shape (via `--ground-truth` / `--seed`), so a
future full export can be prepared without code changes — but today's
prepared dataset is necessarily small (4 train / 1 test rows).
