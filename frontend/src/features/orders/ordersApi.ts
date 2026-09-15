import type { AxiosResponse } from 'axios';
import { apiClient } from '../../api/client';
import type { OrderSummary } from './ordersContracts';

export const ORDERS_ENDPOINT = '/Orders';

export class OrdersApiError extends Error {
  constructor(message: string, readonly status?: number) {
    super(message);
    this.name = 'OrdersApiError';
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isOrderSummary(value: unknown): value is OrderSummary {
  return isRecord(value)
    && typeof value.orderReference === 'string'
    && typeof value.productReference === 'string'
    && typeof value.quantity === 'number'
    && typeof value.requestedDeliveryDateTime === 'string';
}

export async function fetchOrders(): Promise<OrderSummary[]> {
  let response: AxiosResponse;
  try {
    response = await apiClient.get(ORDERS_ENDPOINT, { validateStatus: () => true });
  } catch {
    throw new OrdersApiError('Unable to reach the orders service.');
  }

  if (response.status < 200 || response.status >= 300) {
    throw new OrdersApiError(`Orders request failed (${response.status}).`, response.status);
  }

  const body: unknown = response.data;
  if (!Array.isArray(body) || !body.every(isOrderSummary)) {
    throw new OrdersApiError('The orders service returned an unexpected response.');
  }

  return body;
}
