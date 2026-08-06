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
        <Box sx={{ display: 'flex', alignItems: 'flex-start', overflowX: 'auto', pb: 0.5 }}>
          {criticalOps.map((op, idx) => (
            <Box key={idx} sx={{ display: 'flex', alignItems: 'flex-start', flexShrink: 0 }}>
              {idx > 0 && (
                <Box sx={{
                  width: 36, height: 2, mt: '15px', mx: 0.5, flexShrink: 0,
                  bgcolor: theme.palette.statusCritical.border,
                }} />
              )}
              <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 116, px: 0.5 }}>
                <Box sx={{
                  width: 30, height: 30, borderRadius: '50%', flexShrink: 0,
                  display: 'flex', alignItems: 'center', justifyContent: 'center',
                  bgcolor: theme.palette.statusCritical.bg,
                  border: `2px solid ${theme.palette.statusCritical.text}`,
                }}>
                  <Typography sx={{ fontSize: '11px', fontWeight: 700, color: theme.palette.statusCritical.text }}>
                    {idx + 1}
                  </Typography>
                </Box>
                <Typography sx={{ fontSize: '12px', fontWeight: 600, color: theme.palette.textBody, mt: 1, textAlign: 'center' }}>
                  {op.operationRef}
                </Typography>
                <Typography sx={{ fontSize: '10.5px', color: theme.palette.textMuted, textAlign: 'center', lineHeight: 1.5 }}>
                  {formatUserFriendlyDate(op.estimatedStart)}
                  <br />
                  → {formatUserFriendlyDate(op.estimatedEnd)}
                </Typography>
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </Card>
  );
}
