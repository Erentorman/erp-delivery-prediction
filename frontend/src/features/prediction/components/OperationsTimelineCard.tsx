import { Box, Card, Typography, useTheme } from '@mui/material';
import type { TimelineItem } from '../predictionContracts';
import { formatUserFriendlyDate } from '../predictionHelpers';
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined';

export function OperationsTimelineCard({ timeline }: { timeline: TimelineItem[] }) {
  const theme = useTheme();

  if (!timeline || timeline.length === 0) return null;

  return (
    <Card sx={{ mb: 2, p: '16px 20px' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <TimelineOutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.textPrimary }} />
        <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textPrimary }}>
          Tüm Operasyonlar
        </Typography>
      </Box>

      {/* Masaüstü Tablo Görünümü */}
      <Box sx={{ display: { xs: 'none', md: 'block' }, border: `1px solid ${theme.palette.borderDefault}`, borderRadius: '4px', overflow: 'hidden' }}>
        <Box sx={{ display: 'flex', bgcolor: theme.palette.brand50, borderBottom: `1px solid ${theme.palette.borderDefault}` }}>
          <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}` }}>
            <Typography sx={{ fontSize: '13px', fontWeight: 600, color: theme.palette.textSecondary }}>Operasyon</Typography>
          </Box>
          <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}` }}>
            <Typography sx={{ fontSize: '13px', fontWeight: 600, color: theme.palette.textSecondary }}>Başlangıç</Typography>
          </Box>
          <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}` }}>
            <Typography sx={{ fontSize: '13px', fontWeight: 600, color: theme.palette.textSecondary }}>Bitiş</Typography>
          </Box>
          <Box sx={{ width: '80px', p: '7px 10px' }}>
            <Typography sx={{ fontSize: '13px', fontWeight: 600, color: theme.palette.textSecondary }}>Durum</Typography>
          </Box>
        </Box>
        {timeline.map((op, idx) => (
          <Box key={idx} sx={{ display: 'flex', bgcolor: idx % 2 === 0 ? theme.palette.surfaceCard : theme.palette.surfaceSubtle, borderBottom: idx < timeline.length - 1 ? `1px solid ${theme.palette.borderDefault}` : 'none' }}>
            <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}`, display: 'flex', alignItems: 'center' }}>
              <Typography sx={{ fontSize: '13px', fontWeight: 500, color: theme.palette.textBody }}>{op.operationRef}</Typography>
            </Box>
            <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}`, display: 'flex', alignItems: 'center' }}>
              <Typography sx={{ fontSize: '12px', color: theme.palette.textBody }}>{formatUserFriendlyDate(op.estimatedStart)}</Typography>
            </Box>
            <Box sx={{ flex: 1, p: '7px 10px', borderRight: `1px solid ${theme.palette.borderDefault}`, display: 'flex', alignItems: 'center' }}>
              <Typography sx={{ fontSize: '12px', color: theme.palette.textBody }}>{formatUserFriendlyDate(op.estimatedEnd)}</Typography>
            </Box>
            <Box sx={{ width: '80px', p: '7px 10px', display: 'flex', alignItems: 'center' }}>
              {op.isCritical && (
                <Box sx={{ 
                  bgcolor: theme.palette.statusCritical.bg, 
                  color: theme.palette.statusCritical.text, 
                  px: '7px', py: '2px', 
                  borderRadius: '3px',
                  fontSize: '10.5px', textTransform: 'uppercase', fontWeight: 600
                }}>
                  Kritik
                </Box>
              )}
            </Box>
          </Box>
        ))}
      </Box>

      {/* Mobil Kart Görünümü */}
      <Box sx={{ display: { xs: 'flex', md: 'none' }, flexDirection: 'column', gap: 1 }}>
        {timeline.map((op, idx) => (
          <Box key={idx} sx={{ 
            display: 'flex', flexDirection: 'column', gap: 1,
            p: '10px 14px',
            border: `1px solid ${theme.palette.borderDefault}`,
            borderRadius: '4px',
            bgcolor: idx % 2 === 0 ? theme.palette.surfaceCard : theme.palette.surfaceSubtle
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography sx={{ fontSize: '13px', fontWeight: 500, color: theme.palette.textBody }}>{op.operationRef}</Typography>
              {op.isCritical && (
                <Box sx={{ 
                  bgcolor: theme.palette.statusCritical.bg, 
                  color: theme.palette.statusCritical.text, 
                  px: '7px', py: '2px', 
                  borderRadius: '3px',
                  fontSize: '10.5px', textTransform: 'uppercase', fontWeight: 600
                }}>
                  Kritik
                </Box>
              )}
            </Box>
            <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary }}>
              B: {formatUserFriendlyDate(op.estimatedStart)}<br />
              Bitiş: {formatUserFriendlyDate(op.estimatedEnd)}
            </Typography>
          </Box>
        ))}
      </Box>

    </Card>
  );
}