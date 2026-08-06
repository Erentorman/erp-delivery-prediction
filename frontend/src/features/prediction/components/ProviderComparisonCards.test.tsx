import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { describe, expect, it } from 'vitest';
import { ProviderComparisonCards } from './ProviderComparisonCards';
import { buildMockProviderComparison } from '../providerComparisonMock';
import type { RuleBasedPredictionResult } from '../predictionContracts';
import { theme } from '../../../theme';

const ruleBased: RuleBasedPredictionResult = {
  orderReference: 'SO00001',
  estimatedStart: '2026-08-05T08:00:00Z',
  estimatedEnd: '2026-08-06T08:00:00Z',
  estimatedDelivery: '2026-08-07T08:00:00Z',
  criticalPathOperations: ['CUT-10'],
  appliedFallbackReasons: ['Default shipping duration'],
  shortages: [],
  timeline: [{ operationRef: 'CUT-10', estimatedStart: '2026-08-05T08:00:00Z', estimatedEnd: '2026-08-06T08:00:00Z', isCritical: true }],
};

describe('ProviderComparisonCards', () => {
  it('renders all three provider columns and flags the mock ones', () => {
    const { ai, hybrid } = buildMockProviderComparison(ruleBased);
    render(
      <ThemeProvider theme={theme}>
        <ProviderComparisonCards ruleBased={ruleBased} ai={ai} hybrid={hybrid} />
      </ThemeProvider>
    );

    expect(screen.getByText('Rule-Based')).toBeInTheDocument();
    expect(screen.getByText('AI Model')).toBeInTheDocument();
    expect(screen.getByText('Final Hybrid')).toBeInTheDocument();

    // Only AI and Hybrid are mocked — exactly two tags expected.
    expect(screen.getAllByText('Örnek Veri')).toHaveLength(2);
  });
});
