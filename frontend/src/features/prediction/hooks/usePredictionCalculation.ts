import { useState, useCallback } from 'react';
import type { RuleBasedPredictionResult } from '../predictionContracts';
import { calculatePrediction } from '../predictionApi';
import { classifyError } from '../predictionHelpers';

export type PredictionState =
  | { status: "empty" }
  | { status: "loading" }
  | { status: "success"; data: RuleBasedPredictionResult }
  | { status: "validationError"; detail?: string; errorCode?: string }
  | { status: "calculationFailure"; detail?: string; errorCode?: string };

export function usePredictionCalculation() {
  const [state, setState] = useState<PredictionState>({ status: "empty" });

  const calculate = useCallback(async (orderReference: string) => {
    if (!orderReference.trim()) return;

    setState({ status: "loading" });

    try {
      const data = await calculatePrediction(orderReference);
      setState({ status: "success", data });
    } catch (error) {
      const classified = classifyError(error);
      
      if (classified.type === "validation") {
        setState({ status: "validationError", detail: classified.detail, errorCode: classified.errorCode });
      } else {
        // "calculation" or "unknown" both mapped to calculationFailure according to rules
        setState({ status: "calculationFailure", detail: classified.detail, errorCode: classified.errorCode });
      }
    }
  }, []);

  return { state, calculate };
}
