import { Box, Typography, useTheme } from '@mui/material';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';

export function DemoDataBanner({ visible }: { visible: boolean }) {
  const theme = useTheme();
  if (!visible) return null;

  return (
    <Box 
      sx={{ 
        display: 'flex',
        alignItems: 'center',
        bgcolor: theme.palette.statusWarning.bg, 
        color: theme.palette.statusWarning.text,
        border: `1px solid ${theme.palette.statusWarning.border}`,
        borderLeft: `3px solid ${theme.palette.statusWarning.text}`,
        borderRadius: '4px',
        borderTopLeftRadius: 0,
        borderBottomLeftRadius: 0,
        mb: 2,
        px: '14px', py: '10px',
        width: '100%'
      }}
    >
      <WarningAmberOutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.statusWarning.text }} />
      <Typography sx={{ fontSize: '13px', color: theme.palette.statusWarning.text }}>
        Sentetik demo veri kullanılıyor. Bu operasyonlar ERP tarafından doğrulanmamıştır.
      </Typography>
    </Box>
  );
}