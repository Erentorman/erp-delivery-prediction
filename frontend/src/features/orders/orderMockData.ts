export const ORDERS_DATA_IS_MOCK = true;

export type OrderStatus = 'Beklemede' | 'Üretimde' | 'Tamamlandı' | 'İptal';

export interface Order {
  orderReference: string;
  customerName: string;
  orderDate: string;
  productSummary: string;
  status: OrderStatus;
}

const mockOrders: Order[] = [
  {
    orderReference: 'SO00001',
    customerName: 'Acme Corp',
    orderDate: '2026-07-02T10:00:00Z',
    productSummary: 'P002 (16 adet)',
    status: 'Beklemede'
  },
  {
    orderReference: 'SO00002',
    customerName: 'Globex Inc',
    orderDate: '2026-07-10T11:30:00Z',
    productSummary: 'P002 (4 adet)',
    status: 'Üretimde'
  },
  {
    orderReference: 'SO00003',
    customerName: 'Stark Industries',
    orderDate: '2026-04-21T09:15:00Z',
    productSummary: 'P004 (50 adet)',
    status: 'Beklemede'
  },
  {
    orderReference: 'SO00004',
    customerName: 'Wayne Enterprises',
    orderDate: '2026-04-23T14:20:00Z',
    productSummary: 'P001 (21 adet)',
    status: 'Tamamlandı'
  },
  {
    orderReference: 'SO00015',
    customerName: 'Umbrella Corp',
    orderDate: '2026-06-09T08:45:00Z',
    productSummary: 'P002 (38 adet)',
    status: 'Beklemede'
  },
  {
    orderReference: 'SO00100',
    customerName: 'Cyberdyne Systems',
    orderDate: '2026-05-15T16:00:00Z',
    productSummary: 'P003 (10 adet)',
    status: 'Beklemede'
  },
  {
    orderReference: 'SO00011',
    customerName: 'Massive Dynamic',
    orderDate: '2026-07-14T10:00:00Z',
    productSummary: 'P002 (8 adet)',
    status: 'Üretimde'
  },
  {
    orderReference: 'SO00012',
    customerName: 'Initech',
    orderDate: '2026-03-30T13:10:00Z',
    productSummary: 'P004 (43 adet)',
    status: 'Tamamlandı'
  },
  {
    orderReference: 'SO00020',
    customerName: 'Hooli',
    orderDate: '2026-06-18T09:30:00Z',
    productSummary: 'P001 (15 adet)',
    status: 'İptal'
  },
  {
    orderReference: 'SO00021',
    customerName: 'Pied Piper',
    orderDate: '2026-06-19T11:45:00Z',
    productSummary: 'P002 (25 adet)',
    status: 'Beklemede'
  }
];

export async function getMockOrders(): Promise<Order[]> {
  // Simulate network delay
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(mockOrders);
    }, 500);
  });
}
