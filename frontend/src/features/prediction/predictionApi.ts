import { apiClient } from '../../api/client';
import type {
  CalculatePredictionRequest,
  MaterialShortage,
  ProblemDetails,
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
    && value.timeline.every(isTimelineItem);
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
