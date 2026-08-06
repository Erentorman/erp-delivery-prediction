import { Box, Card, Typography, useTheme } from '@mui/material';
import type { MaterialShortage } from '../predictionContracts';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';

export function MaterialShortagesCard({ shortages }: { shortages: MaterialShortage[] }) {
  const theme = useTheme();

  if (!shortages || shortages.length === 0) return null;

  return (
    <Card sx={{ mb: 2, p: '16px 20px' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <Inventory2OutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.textPrimary }} />
        <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textPrimary }}>
          Malzeme Eksikleri (Shortage)
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {shortages.map((s, idx) => (
          <Box key={idx} sx={{ 
            display: 'flex', 
            justifyContent: 'space-between',
            alignItems: 'center',
            p: '10px 14px',
            border: `1px solid ${theme.palette.borderDefault}`,
            borderRadius: '4px',
            bgcolor: idx % 2 === 0 ? theme.palette.surfaceCard : theme.palette.surfaceSubtle
          }}>
            <Typography sx={{ fontSize: '13px', fontWeight: 500, color: theme.palette.textBody }}>{s.productReference}</Typography>
            <Typography sx={{ fontSize: '13px', color: theme.palette.textSecondary }}>Eksik: {s.shortageQuantity}</Typography>
          </Box>
        ))}
      </Box>
    </Card>
  );
}