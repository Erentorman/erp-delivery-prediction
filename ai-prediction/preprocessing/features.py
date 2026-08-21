"""Metadata-driven feature preparation for inference (T-909).

Builds the model input vector strictly from the loaded model-metadata.json
`feature_order` / `categorical_encodings` / `unknown_category_code` — never
from a hardcoded feature list or a re-derived encoding table. SAD §9.9
features that exist in a request but are not in the loaded model's
`feature_order` are ignored; features in `feature_order` missing/null from
the request are rejected.
"""

from __future__ import annotations

import math
from typing import Any


class FeatureValidationError(Exception):
    """Raised when a request's `features` payload cannot satisfy the loaded
    model's required feature_order (missing field, wrong type, non-finite
    numeric value, or non-string categorical value)."""


def prepare_feature_vector(features: dict[str, Any], metadata: dict) -> list[float]:
    feature_order: list[str] = metadata["feature_order"]
    categorical_encodings: dict[str, dict[str, int]] = metadata.get("categorical_encodings", {})
    unknown_category_code = metadata["unknown_category_code"]

    vector: list[float] = []
    for name in feature_order:
        if name not in features or features[name] is None:
            raise FeatureValidationError(f"Missing required model feature: '{name}'.")

        value = features[name]

        if name in categorical_encodings:
            if not isinstance(value, str):
                raise FeatureValidationError(f"Feature '{name}' must be a string category value.")
            encoding_table = categorical_encodings[name]
            vector.append(float(encoding_table.get(value, unknown_category_code)))
            continue

        if isinstance(value, bool) or not isinstance(value, (int, float)):
            raise FeatureValidationError(f"Feature '{name}' must be a numeric value.")
        numeric_value = float(value)
        if not math.isfinite(numeric_value):
            raise FeatureValidationError(f"Feature '{name}' must be a finite number.")
        vector.append(numeric_value)

    return vector
