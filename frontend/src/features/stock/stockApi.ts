import type { AxiosResponse } from 'axios';
import { apiClient } from '../../api/client';
import type { ProductStock } from './stockContracts';

export const STOCK_ENDPOINT = '/Products/stock';

export class StockApiError extends Error {
  constructor(message: string, readonly status?: number) {
    super(message);
    this.name = 'StockApiError';
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isProductStock(value: unknown): value is ProductStock {
  return isRecord(value)
    && typeof value.productReference === 'string'
    && typeof value.unitOfMeasure === 'string'
    && typeof value.availableQuantity === 'number';
}

export async function fetchStockLevels(): Promise<ProductStock[]> {
  let response: AxiosResponse;
  try {
    response = await apiClient.get(STOCK_ENDPOINT, { validateStatus: () => true });
  } catch {
    throw new StockApiError('Unable to reach the stock service.');
  }

  if (response.status < 200 || response.status >= 300) {
    throw new StockApiError(`Stock request failed (${response.status}).`, response.status);
  }

  const body: unknown = response.data;
  if (!Array.isArray(body) || !body.every(isProductStock)) {
    throw new StockApiError('The stock service returned an unexpected response.');
  }

  return body;
}
