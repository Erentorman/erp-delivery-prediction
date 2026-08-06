import { useEffect, useState } from 'react';
import { getMockOrders, type Order } from '../orders/orderMockData';
import { calculatePrediction } from './predictionApi';
import { toPredictionApiError } from './predictionErrors';

export type OpenOrderPredictionStatus = 'loading' | 'onTime' | 'delayed' | 'error';

export interface OpenOrderPrediction {
  order: Order;
  status: OpenOrderPredictionStatus;
  estimatedDelivery?: string;
  delayDays?: number;
  errorMessage?: string;
}

// "Açık" siparişler: henüz teslim edilmemiş veya iptal edilmemiş olanlar.
const OPEN_STATUSES: Order['status'][] = ['Beklemede', 'Üretimde'];

// order.orderDate alanı, gerçek Mock ERP seed'inde requestedDeliveryDate ile aynı değeri taşır
// (bkz. src/MockErp.Api/Data/mock-erp-seed.json) — isimlendirme yanıltıcı olsa da değer istenen
// teslim tarihidir.
export function getRequestedDeliveryDate(order: Order): string {
  return order.orderDate;
}

function computeDelayDays(estimatedDelivery: string, requestedDeliveryDate: string): number {
  const estimated = new Date(estimatedDelivery).getTime();
  const requested = new Date(requestedDeliveryDate).getTime();
  return Math.ceil((estimated - requested) / (1000 * 60 * 60 * 24));
}

/**
 * Açık siparişler için backend'in gerçek Rule-Based hesaplama servisine
 * (/api/predictions/calculate) paralel çağrılar yapar ve sonucu istenen
 * teslim tarihiyle karşılaştırarak gecikme durumunu belirler. Mock olan
 * yalnızca sipariş listesidir (getMockOrders); her satırın tahmini gerçektir.
 */
export function useOpenOrderDelayRisk() {
  const [rows, setRows] = useState<OpenOrderPrediction[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      const orders = await getMockOrders();
      const openOrders = orders.filter((o) => OPEN_STATUSES.includes(o.status));

      if (cancelled) return;
      setRows(openOrders.map((order) => ({ order, status: 'loading' })));
      setLoading(false);

      await Promise.all(
        openOrders.map(async (order) => {
          try {
            const result = await calculatePrediction(order.orderReference);
            const requestedDeliveryDate = getRequestedDeliveryDate(order);
            const delayDays = computeDelayDays(result.estimatedDelivery, requestedDeliveryDate);

            if (cancelled) return;
            setRows((prev) => prev.map((r) => r.order.orderReference === order.orderReference
              ? { order, status: delayDays > 0 ? 'delayed' : 'onTime', estimatedDelivery: result.estimatedDelivery, delayDays }
              : r));
          } catch (err) {
            if (cancelled) return;
            const apiError = toPredictionApiError(err);
            setRows((prev) => prev.map((r) => r.order.orderReference === order.orderReference
              ? { order, status: 'error', errorMessage: apiError.message }
              : r));
          }
        })
      );
    }

    load();
    return () => { cancelled = true; };
  }, []);

  return { rows, loading };
}
