import { fireEvent, render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Dashboard from './Dashboard';
import { theme } from '../theme';
import { getProducts } from '../features/products/productsApi';
import { fetchStockLevels, StockApiError } from '../features/stock/stockApi';
import { fetchOrders } from '../features/orders/ordersApi';

function renderDashboard() {
  return render(
    <ThemeProvider theme={theme}>
      <Dashboard />
    </ThemeProvider>,
  );
}

const navigateMock = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => navigateMock };
});

vi.mock('../features/products/productsApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/products/productsApi')>();
  return { ...actual, getProducts: vi.fn() };
});
vi.mock('../features/stock/stockApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/stock/stockApi')>();
  return { ...actual, fetchStockLevels: vi.fn() };
});
vi.mock('../features/orders/ordersApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../features/orders/ordersApi')>();
  return { ...actual, fetchOrders: vi.fn() };
});

const getProductsMock = vi.mocked(getProducts);
const fetchStockMock = vi.mocked(fetchStockLevels);
const fetchOrdersMock = vi.mocked(fetchOrders);

const fourProducts = [
  { productReference: 'P001', unitOfMeasure: 'Adet' },
  { productReference: 'P002', unitOfMeasure: 'Adet' },
  { productReference: 'P003', unitOfMeasure: 'Adet' },
  { productReference: 'P004', unitOfMeasure: 'Adet' },
];

describe('Dashboard (product wizard)', () => {
  beforeEach(() => {
    navigateMock.mockReset();
    getProductsMock.mockReset();
    fetchStockMock.mockReset();
    fetchOrdersMock.mockReset();
    // Stock/orders are decorative on this screen; default them to a benign
    // empty resolution so tests that only care about products don't need
    // to stub every concurrent request.
    fetchStockMock.mockResolvedValue([]);
    fetchOrdersMock.mockResolvedValue([]);
  });

  it('shows a loading state, then renders a card per real product', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    renderDashboard();
    expect(screen.getByText('Ürünler yükleniyor...')).toBeVisible();

    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
    expect(screen.getByText('P003')).toBeVisible();
    expect(screen.getByText('P004')).toBeVisible();
  });

  it('shows the ERP-provided product name on the card, falling back to the reference', async () => {
    getProductsMock.mockResolvedValue([
      { productReference: 'P001', name: 'Masa', unitOfMeasure: 'Adet' },
      { productReference: 'P002', unitOfMeasure: 'Adet' },
    ]);
    renderDashboard();

    expect(await screen.findByText('Masa')).toBeVisible();
    expect(screen.getByText('P002')).toBeVisible();
  });

  it('shows empty and error product states', async () => {
    getProductsMock.mockResolvedValueOnce([]);
    const { unmount } = renderDashboard();
    expect(await screen.findByText('Görüntülenecek ürün bulunamadı.')).toBeVisible();
    unmount();

    getProductsMock.mockRejectedValueOnce(new Error('Products request failed (500).'));
    renderDashboard();
    expect(await screen.findByText(/Products request failed/)).toBeVisible();
  });

  it('reveals the quantity/location step only after a product card is selected', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    renderDashboard();
    await screen.findByText('P001');
    expect(screen.queryByLabelText('Adet')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('P001'));
    expect(await screen.findByLabelText('Adet')).toBeVisible();
    expect(screen.getByText('Ürün: P001')).toBeVisible();
  });

  it('shows a stock status chip on each card when stock data is available', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    fetchStockMock.mockResolvedValue([
      { productReference: 'P001', unitOfMeasure: 'Adet', availableQuantity: 309 },
      { productReference: 'P002', unitOfMeasure: 'Adet', availableQuantity: 0 },
    ]);
    renderDashboard();
    await screen.findByText('P001');

    expect(await screen.findByText('Yeterli')).toBeVisible();
    expect(screen.getByText('309 adet')).toBeVisible();
    expect(screen.getByText('Tükendi')).toBeVisible();
    expect(screen.getByText('0 adet')).toBeVisible();
  });

  it('renders product cards without a stock gauge when the stock request fails', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    fetchStockMock.mockRejectedValue(new StockApiError('Stock request failed (500).', 500));
    renderDashboard();

    expect(await screen.findByText('P001')).toBeVisible();
    expect(screen.queryByText(/adet$/)).not.toBeInTheDocument();
  });

  it('validates quantity and location before navigating to the result screen', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    renderDashboard();
    fireEvent.click(await screen.findByText('P001'));

    fireEvent.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(await screen.findByText(/sıfırdan büyük bir adet girin/)).toBeVisible();
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it('navigates to /predictions with the simulate payload on a valid submission', async () => {
    getProductsMock.mockResolvedValue(fourProducts);
    renderDashboard();
    fireEvent.click(await screen.findByText('P002'));

    fireEvent.change(screen.getByLabelText('Adet'), { target: { value: '10' } });
    fireEvent.mouseDown(screen.getByLabelText('İl'));
    fireEvent.click(screen.getByText('İstanbul'));
    fireEvent.click(screen.getByRole('button', { name: 'Teslimat Tahminini Hesapla' }));

    expect(navigateMock).toHaveBeenCalledWith('/predictions', {
      state: { simulate: { productReference: 'P002', quantity: 10, locationReference: 'istanbul' } },
    });
  });
});
