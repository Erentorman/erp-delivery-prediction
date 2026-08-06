import { Box, Card, Typography, useTheme } from '@mui/material';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';

export function FallbackReasonsCard({ reasons }: { reasons: string[] }) {
  const theme = useTheme();

  if (!reasons || reasons.length === 0) return null;

  return (
    <Card sx={{ mb: 2, p: '16px 20px' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <InfoOutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.textPrimary }} />
        <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textPrimary }}>
          Varsayılan Mantık Kullanımı
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {reasons.map((r, idx) => (
          <Box key={idx} sx={{ 
            display: 'flex', 
            alignItems: 'center',
            p: '10px 14px',
            border: `1px solid ${theme.palette.statusNeutral.border}`,
            borderRadius: '4px',
            bgcolor: theme.palette.statusNeutral.bg,
            color: theme.palette.statusNeutral.text
          }}>
            <Typography sx={{ fontSize: '13px' }}>{r}</Typography>
          </Box>
        ))}
      </Box>
    </Card>
  );
}