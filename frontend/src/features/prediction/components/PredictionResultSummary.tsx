import { Box, Card, Typography, useTheme } from '@mui/material';

interface Props {
  delivery: string;
  start: string;
  end: string;
  orderReference: string;
}

export function PredictionResultSummary({ delivery, start, end, orderReference }: Props) {
  const theme = useTheme();

  return (
    <Card sx={{ 
      display: 'grid', 
      gridTemplateColumns: { xs: '1fr', sm: '1.4fr 1fr 1fr' }, 
      gap: 0,
      p: 0,
      mb: 2
    }}>
      <Box sx={{ 
        p: '16px 20px', 
        bgcolor: theme.palette.brand50,
        borderRight: { xs: 'none', sm: `1px solid ${theme.palette.borderDefault}` },
        borderBottom: { xs: `1px solid ${theme.palette.borderDefault}`, sm: 'none' }
      }}>
        <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textSecondary, mb: 0.5 }}>
          Tahmini Teslim Tarihi
        </Typography>
        <Typography sx={{ fontSize: '26px', fontWeight: 600, color: theme.palette.textPrimary }}>
          {delivery}
        </Typography>
      </Box>

      <Box sx={{ 
        p: '16px 20px', 
        bgcolor: theme.palette.surfaceCard,
        borderRight: { xs: 'none', sm: `1px solid ${theme.palette.borderDefault}` },
        borderBottom: { xs: `1px solid ${theme.palette.borderDefault}`, sm: 'none' }
      }}>
        <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textSecondary, mb: 0.5 }}>
          Üretim Başlangıç
        </Typography>
        <Typography sx={{ fontSize: '15px', fontWeight: 500, color: theme.palette.textBody }}>
          {start}
        </Typography>
      </Box>

      <Box sx={{ 
        p: '16px 20px', 
        bgcolor: theme.palette.surfaceCard
      }}>
        <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textSecondary, mb: 0.5 }}>
          Üretim Bitiş
        </Typography>
        <Typography sx={{ fontSize: '15px', fontWeight: 500, color: theme.palette.textBody }}>
          {end}
        </Typography>
        
        <Typography sx={{ fontSize: '12px', color: theme.palette.textMuted, mt: 1 }}>
          Ref: {orderReference}
        </Typography>
      </Box>
    </Card>
  );
}