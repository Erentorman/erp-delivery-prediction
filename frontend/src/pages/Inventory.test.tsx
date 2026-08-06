import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { describe, expect, it } from 'vitest';
import Inventory from './Inventory';
import { theme } from '../theme';

describe('Inventory', () => {
  it('renders a stock row for every known product', () => {
    render(
      <ThemeProvider theme={theme}>
        <Inventory />
      </ThemeProvider>
    );

    expect(screen.getByText('Masa')).toBeInTheDocument();
    expect(screen.getByText('Sandalye')).toBeInTheDocument();
    expect(screen.getByText('Dolap')).toBeInTheDocument();
    expect(screen.getByText('Kapı')).toBeInTheDocument();
  });
});
