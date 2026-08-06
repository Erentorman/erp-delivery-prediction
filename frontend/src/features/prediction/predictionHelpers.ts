import type { RuleBasedPredictionResult } from './predictionContracts';
import { PredictionApiError } from './predictionErrors';

export function formatUserFriendlyDate(isoString: string): string {
  if (!isoString) return '';
  
  const date = new Date(isoString);
  if (isNaN(date.getTime())) {
    return isoString;
  }

  const formatterDate = new Intl.DateTimeFormat('tr-TR', {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  });

  const formatterTime = new Intl.DateTimeFormat('tr-TR', {
    hour: '2-digit',
    minute: '2-digit'
  });

  const datePart = formatterDate.format(date);
  
  if (date.getHours() === 0 && date.getMinutes() === 0) {
    return datePart;
  }

  const timePart = formatterTime.format(date);
  return `${datePart}, ${timePart}`;
}

export function isDemoData(result: RuleBasedPredictionResult): boolean {
  if (!result) return false;
  
  if (result.criticalPathOperations?.some(op => op.startsWith('DEMO-'))) {
    return true;
  }

  if (result.timeline?.some(item => item.operationRef?.startsWith('DEMO-'))) {
    return true;
  }

  return false;
}

export interface ClassifiedError {
  type: "validation" | "calculation" | "unknown";
  detail?: string;
  errorCode?: string;
}

export function classifyError(error: unknown): ClassifiedError {
  let errorCode: string | undefined;
  let detail: string | undefined;
  let status: number | undefined;

  if (error instanceof PredictionApiError) {
    errorCode = error.errorCode;
    detail = error.message;
    status = error.status;
  } else if (error instanceof Error) {
    detail = error.message;
  }

  const validationCodes = ['Data.Insufficient', 'RuleEngine.Failed', 'invalid_input'];
  const calculationCodes = ['CPM.CycleDetected', 'CPM.MissingPredecessorReference'];

  if (errorCode && validationCodes.includes(errorCode)) {
    return { type: 'validation', detail, errorCode };
  }
  
  if (errorCode && calculationCodes.includes(errorCode)) {
    return { type: 'calculation', detail, errorCode };
  }

  if (status === 400) {
    return { type: 'validation', detail, errorCode };
  }

  if (status === 404 || (status && status >= 500)) {
    return { type: 'calculation', detail, errorCode };
  }

  if (detail && (detail.toLowerCase().includes('fetch') || detail.toLowerCase().includes('network') || detail.toLowerCase().includes('timeout') || detail.toLowerCase().includes('reach the prediction service'))) {
     return { type: 'calculation', detail, errorCode };
  }

  return { type: 'unknown', detail, errorCode };
}

export function buildPredictionUrl(orderReference: string): string {
  return `/predictions?orderReference=${encodeURIComponent(orderReference)}`;
}

