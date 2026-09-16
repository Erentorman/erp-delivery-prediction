import { apiClient } from '../../api/client';
import type {
  CalculatePredictionRequest,
  FinalPredictionResult,
  MaterialShortage,
  ProblemDetails,
  ProviderPredictionResult,
  RuleBasedPredictionResult,
  TimelineItem,
  WhatIfPredictionRequest,
} from './predictionContracts';
import { classifyProblem, PredictionApiError, toPredictionApiError } from './predictionErrors';

export const PREDICTION_ENDPOINT = '/Predictions/calculate';
export const WHAT_IF_PREDICTION_ENDPOINT = '/Predictions/simulate';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string');
}

function isShortage(value: unknown): value is MaterialShortage {
  return isRecord(value)
    && typeof value.productReference === 'string'
    && typeof value.shortageQuantity === 'number'
    && Number.isFinite(value.shortageQuantity);
}

function isTimelineItem(value: unknown): value is TimelineItem {
  return isRecord(value)
    && typeof value.operationRef === 'string'
    && typeof value.estimatedStart === 'string'
    && typeof value.estimatedEnd === 'string'
    && typeof value.isCritical === 'boolean';
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === 'string';
}

function isNullableNumber(value: unknown): value is number | null {
  return value === null || (typeof value === 'number' && Number.isFinite(value));
}

function isNullableStringArray(value: unknown): value is string[] | null {
  return value === null || isStringArray(value);
}

function isProviderPredictionResult(value: unknown): value is ProviderPredictionResult {
  return isRecord(value)
    && typeof value.providerType === 'string'
    && typeof value.status === 'string'
    && isNullableNumber(value.workingLeadTimeMinutes)
    && isNullableString(value.modelVersion)
    && isNullableString(value.featureSchemaVersion)
    && isNullableString(value.trainingDatasetVersion)
    && isNullableStringArray(value.warnings)
    && typeof value.durationMs === 'number';
}

function isFinalPredictionResult(value: unknown): value is FinalPredictionResult {
  return isRecord(value)
    && typeof value.status === 'string'
    && typeof value.fallbackReason === 'string'
    && isNullableNumber(value.workingLeadTimeMinutes)
    && isNullableString(value.estimatedStart)
    && isNullableString(value.estimatedEnd)
    && isNullableString(value.estimatedDelivery)
    && isNullableString(value.combinationStrategy)
    && isNullableNumber(value.ruleBasedWeight)
    && isNullableNumber(value.aiWeight)
    && isNullableNumber(value.absoluteDifferenceMinutes)
    && isNullableNumber(value.relativeDifferencePercent);
}

export function isRuleBasedPredictionResult(value: unknown): value is RuleBasedPredictionResult {
  return isRecord(value)
    && typeof value.orderReference === 'string'
    && typeof value.estimatedStart === 'string'
    && typeof value.estimatedEnd === 'string'
    && typeof value.estimatedDelivery === 'string'
    && isStringArray(value.criticalPathOperations)
    && isStringArray(value.appliedFallbackReasons)
    && Array.isArray(value.shortages)
    && value.shortages.every(isShortage)
    && Array.isArray(value.timeline)
    && value.timeline.every(isTimelineItem)
    && isProviderPredictionResult(value.ruleBasedPrediction)
    && isProviderPredictionResult(value.aiPrediction)
    && isFinalPredictionResult(value.finalPrediction);
}

function parseProblemDetails(value: unknown): ProblemDetails {
  if (!isRecord(value)) return {};
  return {
    status: typeof value.status === 'number' ? value.status : undefined,
    title: typeof value.title === 'string' ? value.title : undefined,
    detail: typeof value.detail === 'string' ? value.detail : undefined,
    errorCode: typeof value.errorCode === 'string' ? value.errorCode : undefined,
  };
}

function readBody(response: { data: unknown }): unknown {
  return response.data === '' ? null : response.data;
}

export async function calculatePrediction(orderReference: string): Promise<RuleBasedPredictionResult> {
  const request: CalculatePredictionRequest = { orderReference: orderReference.trim() };

  try {
    const response = await apiClient.post(PREDICTION_ENDPOINT, request, { validateStatus: () => true });
    const body = readBody(response);

    if (response.status < 200 || response.status >= 300) {
      const problem = typeof body === 'string' ? { detail: body } : parseProblemDetails(body);
      throw classifyProblem(problem, response.status);
    }
    if (!isRuleBasedPredictionResult(body)) {
      throw new PredictionApiError('The prediction service returned an unexpected response.', 'calculationFailure');
    }
    return body;
  } catch (error: unknown) {
    throw toPredictionApiError(error);
  }
}

export async function simulatePrediction(request: WhatIfPredictionRequest): Promise<RuleBasedPredictionResult> {
  try {
    const response = await apiClient.post(WHAT_IF_PREDICTION_ENDPOINT, request, { validateStatus: () => true });
    const body = readBody(response);
    if (response.status < 200 || response.status >= 300) {
      const problem = typeof body === 'string' ? { detail: body } : parseProblemDetails(body);
      throw classifyProblem(problem, response.status);
    }
    if (!isRuleBasedPredictionResult(body)) {
      throw new PredictionApiError('The prediction service returned an unexpected response.', 'calculationFailure');
    }
    return body;
  } catch (error: unknown) {
    throw toPredictionApiError(error);
  }
}
