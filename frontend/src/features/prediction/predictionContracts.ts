export interface CalculatePredictionRequest {
  orderReference: string;
}

export interface WhatIfPredictionRequest {
  productReference: string;
  quantity: number;
  locationReference: string;
}

export interface MaterialShortage {
  productReference: string;
  shortageQuantity: number;
}

export interface TimelineItem {
  operationRef: string;
  estimatedStart: string;
  estimatedEnd: string;
  isCritical: boolean;
}

export interface ProviderPredictionResult {
  providerType: string;
  status: string;
  workingLeadTimeMinutes: number | null;
  modelVersion: string | null;
  featureSchemaVersion: string | null;
  trainingDatasetVersion: string | null;
  warnings: string[] | null;
  durationMs: number;
}

export interface FinalPredictionResult {
  status: string;
  fallbackReason: string;
  workingLeadTimeMinutes: number | null;
  estimatedStart: string | null;
  estimatedEnd: string | null;
  estimatedDelivery: string | null;
  combinationStrategy: string | null;
  ruleBasedWeight: number | null;
  aiWeight: number | null;
  absoluteDifferenceMinutes: number | null;
  relativeDifferencePercent: number | null;
}

export interface RuleBasedPredictionResult {
  orderReference: string;
  estimatedStart: string;
  estimatedEnd: string;
  estimatedDelivery: string;
  criticalPathOperations: string[];
  appliedFallbackReasons: string[];
  shortages: MaterialShortage[];
  timeline: TimelineItem[];
  ruleBasedPrediction: ProviderPredictionResult;
  aiPrediction: ProviderPredictionResult;
  finalPrediction: FinalPredictionResult;
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errorCode?: string;
}
