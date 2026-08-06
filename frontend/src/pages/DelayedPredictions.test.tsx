import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ThemeProvider } from '@mui/material';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import DelayedPredictions from './DelayedPredictions';
import { calculatePrediction } from '../features/prediction/predictionApi';
import type { RuleBasedPredictionResult } from '../features/prediction/predictionContracts';
import { theme } from '../theme';

vi.mock('../features/prediction/predictionApi', () => ({ calculatePrediction: vi.fn() }));
const calculateMock = vi.mocked(calculatePrediction);

function baseResult(orderReference: string, estimatedDelivery: string): RuleBasedPredictionResult {
  return {
    orderReference,
    estimatedStart: '2026-01-01T08:00:00Z',
    estimatedEnd: '2026-01-02T08:00:00Z',
    estimatedDelivery,
    criticalPathOperations: [],
    appliedFallbackReasons: [],
    shortages: [],
    timeline: [],
  };
}

beforeEach(() => {
  calculateMock.mockReset();
});

function renderPage() {
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter>
        <DelayedPredictions />
      </MemoryRouter>
    </ThemeProvider>
  );
}

describe('DelayedPredictions', () => {
  it('marks orders whose estimated delivery is after the requested date as delayed', async () => {
    // SO00001 is requested for 2026-07-02 in mock order data; predict a later date -> delayed.
    calculateMock.mockImplementation(async (orderReference: string) => {
      if (orderReference === 'SO00001') return baseResult(orderReference, '2026-07-10T00:00:00Z');
      return baseResult(orderReference, '2020-01-01T00:00:00Z'); // safely on-time for everything else
    });

    renderPage();

    await waitFor(() => expect(screen.getByText('SO00001')).toBeInTheDocument(), { timeout: 3000 });
    expect(await screen.findByText('Gecikiyor')).toBeInTheDocument();
  });

  it('shows an error chip when a calculation fails and does not crash the page', async () => {
    calculateMock.mockRejectedValue(new Error('network down'));

    renderPage();

    await waitFor(() => expect(screen.getAllByText('Hesaplanamadı').length).toBeGreaterThan(0), { timeout: 3000 });
  });
});
