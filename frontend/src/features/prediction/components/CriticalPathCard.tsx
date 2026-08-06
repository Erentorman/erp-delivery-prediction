import { Box, Card, Typography, useTheme } from '@mui/material';
import type { TimelineItem } from '../predictionContracts';
import { formatUserFriendlyDate } from '../predictionHelpers';
import AltRouteOutlinedIcon from '@mui/icons-material/AltRouteOutlined';

export function CriticalPathCard({ operations }: { operations: TimelineItem[] }) {
  const theme = useTheme();
  
  const criticalOps = operations.filter(op => op.isCritical);

  return (
    <Card sx={{ mb: 2, p: '16px 20px' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <AltRouteOutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.textPrimary }} />
        <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textPrimary }}>
          Kritik Yol Operasyonları
        </Typography>
      </Box>

      {criticalOps.length === 0 ? (
        <Typography sx={{ fontSize: '13px', color: theme.palette.textSecondary }}>Kritik yol bilgisi bulunamadı.</Typography>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          {criticalOps.map((op, idx) => (
            <Box key={idx} sx={{ 
              display: 'flex', 
              flexDirection: { xs: 'column', sm: 'row' },
              justifyContent: 'space-between',
              alignItems: { xs: 'flex-start', sm: 'center' },
              p: '10px 14px',
              border: `1px solid ${theme.palette.borderDefault}`,
              borderRadius: '4px',
              bgcolor: idx % 2 === 0 ? theme.palette.surfaceCard : theme.palette.surfaceSubtle
            }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: { xs: 1, sm: 0 } }}>
                <Typography sx={{ fontSize: '13px', fontWeight: 500, color: theme.palette.textBody }}>{op.operationRef}</Typography>
                <Box sx={{ 
                  bgcolor: theme.palette.statusCritical.bg, 
                  color: theme.palette.statusCritical.text, 
                  px: '7px', py: '2px', 
                  borderRadius: '3px',
                  fontSize: '10.5px', textTransform: 'uppercase', fontWeight: 600
                }}>
                  Kritik
                </Box>
              </Box>
              <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary }}>
                {formatUserFriendlyDate(op.estimatedStart)} - {formatUserFriendlyDate(op.estimatedEnd)}
              </Typography>
            </Box>
          ))}
        </Box>
      )}
    </Card>
  );
}