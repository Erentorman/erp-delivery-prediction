// Sipariş detay ekranı için örnek veri.
//
// Alan şeması, App.Api'de henüz uygulanmamış /api/erp/orders/{ref} endpoint'i yerine
// gerçek Mock ERP DTO'larını (MockErpOrder, MockErpProduct, MockErpBomLine,
// MockErpStockLevel, MockErpWorkOrder/Routing) yansıtır — bkz. src/MockErp.Api/Models/MockErpModels.cs.
// Gerçek ERP'de "customerName" veya "status" alanı yoktur; bu ekran onları kullanmaz.
import { mockOrders, type Order } from './orderMockData';

export const ORDER_DETAIL_DATA_IS_MOCK = true;

export interface BomLine {
  componentId: string;
  description: string;
  quantity: number;
  unit: string;
}

export interface StockLevel {
  locationReference: string | null;
  onHandQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
}

export interface OperationSummary {
  operationReference: string;
  operationSequence: number;
  workCenterReference: string;
  standardDurationMinutes: number;
}

export interface WorkOrderSummary {
  workOrderReference: string;
  status: string;
  operations: OperationSummary[];
}

export interface OrderDetail {
  orderReference: string;
  productId: string;
  productName: string;
  productUnit: string;
  quantity: number;
  requestedDeliveryDate: string;
  bom: BomLine[];
  stock: StockLevel;
  workOrder: WorkOrderSummary | null;
}

interface ProductDefinition {
  name: string;
  unit: string;
  bom: BomLine[];
  operations: OperationSummary[];
}

const PRODUCTS: Record<string, ProductDefinition> = {
  P001: {
    name: 'Masa',
    unit: 'Adet',
    bom: [
      { componentId: 'WOOD-TOP-01', description: 'Masa tablası', quantity: 1, unit: 'Adet' },
      { componentId: 'WOOD-LEG-01', description: 'Masa ayağı', quantity: 4, unit: 'Adet' },
      { componentId: 'SCR-M6', description: 'M6 vida', quantity: 12, unit: 'Adet' },
    ],
    operations: [
      { operationReference: 'CUT-10', operationSequence: 10, workCenterReference: 'WC-CUT', standardDurationMinutes: 45 },
      { operationReference: 'ASM-20', operationSequence: 20, workCenterReference: 'WC-ASSEMBLY', standardDurationMinutes: 60 },
      { operationReference: 'FIN-30', operationSequence: 30, workCenterReference: 'WC-FINISH', standardDurationMinutes: 30 },
    ],
  },
  P002: {
    name: 'Sandalye',
    unit: 'Adet',
    bom: [
      { componentId: 'WOOD-SEAT-01', description: 'Oturak paneli', quantity: 1, unit: 'Adet' },
      { componentId: 'WOOD-LEG-02', description: 'Sandalye ayağı', quantity: 4, unit: 'Adet' },
      { componentId: 'SCR-M5', description: 'M5 vida', quantity: 8, unit: 'Adet' },
    ],
    operations: [
      { operationReference: 'CUT-10', operationSequence: 10, workCenterReference: 'WC-CUT', standardDurationMinutes: 25 },
      { operationReference: 'ASM-20', operationSequence: 20, workCenterReference: 'WC-ASSEMBLY', standardDurationMinutes: 35 },
      { operationReference: 'FIN-30', operationSequence: 30, workCenterReference: 'WC-FINISH', standardDurationMinutes: 20 },
    ],
  },
  P003: {
    name: 'Dolap',
    unit: 'Adet',
    bom: [
      { componentId: 'WOOD-PANEL-01', description: 'Gövde paneli', quantity: 5, unit: 'Adet' },
      { componentId: 'HNG-01', description: 'Menteşe', quantity: 6, unit: 'Adet' },
      { componentId: 'SCR-M6', description: 'M6 vida', quantity: 24, unit: 'Adet' },
    ],
    operations: [
      { operationReference: 'CUT-10', operationSequence: 10, workCenterReference: 'WC-CUT', standardDurationMinutes: 90 },
      { operationReference: 'ASM-20', operationSequence: 20, workCenterReference: 'WC-ASSEMBLY', standardDurationMinutes: 120 },
      { operationReference: 'FIN-30', operationSequence: 30, workCenterReference: 'WC-FINISH', standardDurationMinutes: 60 },
    ],
  },
  P004: {
    name: 'Kapı',
    unit: 'Adet',
    bom: [
      { componentId: 'WOOD-DOOR-01', description: 'Kapı paneli', quantity: 1, unit: 'Adet' },
      { componentId: 'HNG-02', description: 'Kapı menteşesi', quantity: 3, unit: 'Adet' },
      { componentId: 'LOCK-01', description: 'Kilit mekanizması', quantity: 1, unit: 'Adet' },
    ],
    operations: [
      { operationReference: 'CUT-10', operationSequence: 10, workCenterReference: 'WC-CUT', standardDurationMinutes: 40 },
      { operationReference: 'ASM-20', operationSequence: 20, workCenterReference: 'WC-ASSEMBLY', standardDurationMinutes: 50 },
      { operationReference: 'FIN-30', operationSequence: 30, workCenterReference: 'WC-FINISH', standardDurationMinutes: 25 },
    ],
  },
};

const STOCK_BY_PRODUCT: Record<string, StockLevel> = {
  P001: { locationReference: 'WH-MAIN', onHandQuantity: 18, reservedQuantity: 9, availableQuantity: 9 },
  P002: { locationReference: 'WH-MAIN', onHandQuantity: 32, reservedQuantity: 20, availableQuantity: 12 },
  P003: { locationReference: 'WH-MAIN', onHandQuantity: 6, reservedQuantity: 4, availableQuantity: 2 },
  P004: { locationReference: 'WH-MAIN', onHandQuantity: 40, reservedQuantity: 15, availableQuantity: 25 },
};

// Bir sipariş, üretime alındıysa (Üretimde/Tamamlandı) bir iş emrine sahiptir; henüz alınmadıysa yoktur.
function buildWorkOrder(orderReference: string, productId: string, status: string): WorkOrderSummary | null {
  if (status !== 'Üretimde' && status !== 'Tamamlandı') return null;

  const product = PRODUCTS[productId];
  const workOrderStatus = status === 'Tamamlandı' ? 'Completed' : 'InProgress';

  return {
    workOrderReference: `WO-${orderReference}`,
    status: workOrderStatus,
    operations: product?.operations ?? [],
  };
}

function parseProductSummary(productSummary: string): { productId: string; quantity: number } {
  const match = productSummary.match(/^(\S+)\s*\((\d+)/);
  return {
    productId: match?.[1] ?? productSummary,
    quantity: match ? Number(match[2]) : 0,
  };
}

export interface ProductStockOverview {
  productId: string;
  productName: string;
  unit: string;
  stock: StockLevel;
}

// Stok/Kapasite görünümü için ürün bazlı stok durumu. Gerçek karşılığı
// GET /api/stock-levels (bkz. src/MockErp.Api/Controllers/StockLevelsController.cs) olacaktır.
export function getProductStockOverview(): ProductStockOverview[] {
  return Object.entries(PRODUCTS).map(([productId, product]) => ({
    productId,
    productName: product.name,
    unit: product.unit,
    stock: STOCK_BY_PRODUCT[productId] ?? { locationReference: null, onHandQuantity: 0, reservedQuantity: 0, availableQuantity: 0 },
  }));
}

// Bir siparişin talep ettiği miktar, o ürün için kullanılabilir stoğu aşıyor mu?
export function hasStockShortfall(order: Order): boolean {
  const { productId, quantity } = parseProductSummary(order.productSummary);
  const stock = STOCK_BY_PRODUCT[productId];
  if (!stock) return false;
  return stock.availableQuantity < quantity;
}

export async function getMockOrderDetail(orderReference: string): Promise<OrderDetail | null> {
  return new Promise((resolve) => {
    setTimeout(() => {
      const order = mockOrders.find((o) => o.orderReference === orderReference);
      if (!order) {
        resolve(null);
        return;
      }

      const { productId, quantity } = parseProductSummary(order.productSummary);
      const product = PRODUCTS[productId];

      resolve({
        orderReference: order.orderReference,
        productId,
        productName: product?.name ?? productId,
        productUnit: product?.unit ?? 'Adet',
        quantity,
        requestedDeliveryDate: order.orderDate,
        bom: product?.bom ?? [],
        stock: STOCK_BY_PRODUCT[productId] ?? { locationReference: null, onHandQuantity: 0, reservedQuantity: 0, availableQuantity: 0 },
        workOrder: buildWorkOrder(order.orderReference, productId, order.status),
      });
    }, 400);
  });
}
