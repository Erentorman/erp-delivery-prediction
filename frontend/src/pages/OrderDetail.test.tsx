import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material';
import { describe, expect, it } from 'vitest';
import OrderDetailPage from './OrderDetail';
import { theme } from '../theme';

function renderAt(orderReference: string) {
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter initialEntries={[`/orders/${orderReference}`]}>
        <Routes>
          <Route path="/orders/:orderReference" element={<OrderDetailPage />} />
        </Routes>
      </MemoryRouter>
    </ThemeProvider>
  );
}

describe('OrderDetail', () => {
  it('renders product, BOM and work order sections for a known order', async () => {
    renderAt('SO00002');

    await waitFor(() => expect(screen.getByText('Sandalye')).toBeInTheDocument());

    expect(screen.getByText('Ürün Reçetesi (BOM)')).toBeInTheDocument();
    expect(screen.getByText('Stok Durumu')).toBeInTheDocument();
    // SO00002 is 'Üretimde' in mock data -> work order should be present, not the "not yet released" message.
    expect(screen.getByText(/WO-SO00002/)).toBeInTheDocument();
  });

  it('shows a not-found message for an unknown order reference', async () => {
    renderAt('SO99999');

    await waitFor(() => expect(screen.getByText(/bulunamadı/)).toBeInTheDocument());
  });
});
