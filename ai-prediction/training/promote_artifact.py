"""Promotes a trained model artifact to the canonical runtime location (T-907).

model_training.py writes to an ephemeral staging directory (default:
training/artifacts/<model_version>/, git-ignored) — that staging output is
generated training-run output, not the runtime source of truth on its own
(training and runtime artifact lifecycles are kept deliberately separate).

This script is the explicit, human-triggered step that copies an
already-trained, already-evaluated staged artifact into the single
canonical, version-controlled location FastAPI/T-909 will load from and
commit to the repository:

    ai-prediction/model/artifacts/<model_version>/

It does not train, does not re-validate metrics, and does not choose a
model version on its own — the caller decides which staged artifact is
approved to become the tracked runtime artifact. There is exactly one
canonical artifact path per model_version; this script never writes
anywhere else.
"""

from __future__ import annotations

import argparse
import json
import shutil
from pathlib import Path

ARTIFACT_FILES = ("model.json", "model-metadata.json", "evaluation.json")

AI_PREDICTION_ROOT = Path(__file__).resolve().parents[1]
CANONICAL_ARTIFACT_ROOT = AI_PREDICTION_ROOT / "model" / "artifacts"


def promote(source_dir: Path, model_version: str | None = None) -> Path:
    """Copies model.json/model-metadata.json/evaluation.json from source_dir
    into ai-prediction/model/artifacts/<model_version>/.

    model_version defaults to the value already recorded in
    source_dir/model-metadata.json — never invented here.
    """
    missing = [name for name in ARTIFACT_FILES if not (source_dir / name).exists()]
    if missing:
        raise FileNotFoundError(f"{source_dir} is missing required artifact file(s): {missing}")

    if model_version is None:
        metadata = json.loads((source_dir / "model-metadata.json").read_text(encoding="utf-8"))
        model_version = metadata["model_version"]

    dest_dir = CANONICAL_ARTIFACT_ROOT / model_version
    dest_dir.mkdir(parents=True, exist_ok=True)
    for name in ARTIFACT_FILES:
        shutil.copyfile(source_dir / name, dest_dir / name)
    return dest_dir


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Promote a trained model artifact to the canonical runtime location (T-907)."
    )
    parser.add_argument(
        "--source", type=Path, required=True,
        help="Staged artifact directory produced by model_training.py --out.",
    )
    parser.add_argument(
        "--model-version", type=str, default=None,
        help="Defaults to the model_version recorded in the staged model-metadata.json.",
    )
    args = parser.parse_args()

    dest_dir = promote(args.source, args.model_version)
    print(f"Promoted artifact to canonical location: {dest_dir}")


if __name__ == "__main__":
    main()
