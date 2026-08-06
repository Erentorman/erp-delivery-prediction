import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { describe, expect, it } from 'vitest';
import { CriticalPathCard } from './CriticalPathCard';
import { createAppTheme } from '../../../theme';
import type { TimelineItem } from '../predictionContracts';

const theme = createAppTheme('light');

const operations: TimelineItem[] = [
  { operationRef: 'CUT-10', estimatedStart: '2026-08-05T08:00:00Z', estimatedEnd: '2026-08-05T09:00:00Z', isCritical: true },
  { operationRef: 'ASM-20', estimatedStart: '2026-08-05T09:00:00Z', estimatedEnd: '2026-08-05T10:30:00Z', isCritical: true },
  { operationRef: 'PACK-30', estimatedStart: '2026-08-05T09:00:00Z', estimatedEnd: '2026-08-05T09:30:00Z', isCritical: false },
];

function renderCard(ops: TimelineItem[]) {
  return render(
    <ThemeProvider theme={theme}>
      <CriticalPathCard operations={ops} />
    </ThemeProvider>
  );
}

describe('CriticalPathCard', () => {
  it('renders one numbered step per critical operation, in order, excluding non-critical ones', () => {
    renderCard(operations);
    expect(screen.getByText('CUT-10')).toBeInTheDocument();
    expect(screen.getByText('ASM-20')).toBeInTheDocument();
    expect(screen.queryByText('PACK-30')).not.toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('shows an empty-state message when there is no critical path', () => {
    renderCard([{ ...operations[2], isCritical: false }]);
    expect(screen.getByText('Kritik yol bilgisi bulunamadı.')).toBeInTheDocument();
  });
});
