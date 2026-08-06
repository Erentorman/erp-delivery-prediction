import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { describe, expect, it } from 'vitest';
import { RiskGauge } from './RiskGauge';
import { createAppTheme } from '../theme';

const theme = createAppTheme('light');

function renderGauge(value: number, caption?: string) {
  return render(
    <ThemeProvider theme={theme}>
      <RiskGauge value={value} caption={caption} />
    </ThemeProvider>
  );
}

describe('RiskGauge', () => {
  it('renders only the background track path when value is 0', () => {
    const { container } = renderGauge(0);
    expect(container.querySelectorAll('svg path')).toHaveLength(1);
  });

  it('renders a foreground fill path in addition to the track when value > 0', () => {
    const { container } = renderGauge(45);
    expect(container.querySelectorAll('svg path')).toHaveLength(2);
  });

  it('clamps out-of-range values instead of producing an invalid arc', () => {
    const { container } = renderGauge(150);
    expect(container.querySelectorAll('svg path')).toHaveLength(2);
  });

  it('renders the caption text when provided', () => {
    renderGauge(30, '3 / 10 açık sipariş');
    expect(screen.getByText('3 / 10 açık sipariş')).toBeInTheDocument();
  });
});
