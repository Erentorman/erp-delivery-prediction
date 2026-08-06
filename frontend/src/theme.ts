import { createTheme } from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Palette {
    brand900: string;
    brand700: string;
    brand100: string;
    brand50: string;
    interactiveBlue: string;
    surfacePage: string;
    surfaceCard: string;
    surfaceSubtle: string;
    borderDefault: string;
    borderStrong: string;
    textPrimary: string;
    textBody: string;
    textSecondary: string;
    textMuted: string;
    statusCritical: { bg: string; border: string; text: string };
    statusWarning: { bg: string; border: string; text: string };
    statusSuccess: { bg: string; border: string; text: string };
    statusNeutral: { bg: string; border: string; text: string };
  }
  interface PaletteOptions {
    brand900?: string;
    brand700?: string;
    brand100?: string;
    brand50?: string;
    interactiveBlue?: string;
    surfacePage?: string;
    surfaceCard?: string;
    surfaceSubtle?: string;
    borderDefault?: string;
    borderStrong?: string;
    textPrimary?: string;
    textBody?: string;
    textSecondary?: string;
    textMuted?: string;
    statusCritical?: { bg: string; border: string; text: string };
    statusWarning?: { bg: string; border: string; text: string };
    statusSuccess?: { bg: string; border: string; text: string };
    statusNeutral?: { bg: string; border: string; text: string };
  }
}

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#0f2942', // brand900 is the new primary
    },
    secondary: {
      main: '#2563eb', // interactive blue
    },
    background: {
      default: '#eef1f4', // surfacePage
      paper: '#ffffff',   // surfaceCard
    },
    text: {
      primary: '#0f2942',
      secondary: '#5c7288',
    },
    error: {
      main: '#a13a3a',
    },
    warning: {
      main: '#6b5518',
    },
    success: {
      main: '#2f6b35',
    },
    info: {
      main: '#5c7288',
    },
    // Custom properties
    brand900: '#0f2942',
    brand700: '#1a3a5c',
    brand100: '#e8edf2',
    brand50: '#f4f7fa',
    interactiveBlue: '#2563eb',
    surfacePage: '#eef1f4',
    surfaceCard: '#ffffff',
    surfaceSubtle: '#fafbfc',
    borderDefault: '#dde3e8',
    borderStrong: '#c3ccd4',
    textPrimary: '#0f2942',
    textBody: '#1a1a1a',
    textSecondary: '#5c7288',
    textMuted: '#8fa6bc',
    statusCritical: { bg: '#fbeaea', border: '#e5b8b8', text: '#a13a3a' },
    statusWarning: { bg: '#fdf6e3', border: '#e8d9a8', text: '#6b5518' },
    statusSuccess: { bg: '#e9f4ea', border: '#b8d9bc', text: '#2f6b35' },
    statusNeutral: { bg: '#f4f7fa', border: '#dde3e8', text: '#5c7288' },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    // Using MUI typography variations for our roles, or we can use custom styles in components.
    // Since MUI has specific h1..h6, we map them closely to the design spec.
  },
  shape: {
    borderRadius: 4, // --radius-md
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: 4,
          padding: '10px 20px',
          fontSize: '13px',
          fontWeight: 600,
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 4,
          boxShadow: 'none',
          border: '1px solid #dde3e8', // borderDefault
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          borderRadius: 4,
          height: 36,
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: '#2563eb',
            borderWidth: '1px',
            boxShadow: '0 0 0 2px rgba(37,99,235,0.15)',
          },
        },
        notchedOutline: {
          borderColor: '#dde3e8',
        }
      }
    }
  },
});
