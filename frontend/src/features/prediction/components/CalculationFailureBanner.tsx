import { Box, Typography, useTheme, Accordion, AccordionSummary, AccordionDetails } from '@mui/material';
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';

export function CalculationFailureBanner({ detail, errorCode }: { detail?: string; errorCode?: string }) {
  const theme = useTheme();

  return (
    <Box 
      sx={{ 
        bgcolor: theme.palette.statusCritical.bg, 
        color: theme.palette.statusCritical.text,
        border: `1px solid ${theme.palette.statusCritical.border}`,
        borderLeft: `3px solid ${theme.palette.statusCritical.text}`,
        borderRadius: '4px',
        borderTopLeftRadius: 0,
        borderBottomLeftRadius: 0,
        mb: 2,
        width: '100%'
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', px: '14px', py: '10px' }}>
        <ErrorOutlineOutlinedIcon sx={{ fontSize: 16, mr: 1, color: theme.palette.statusCritical.text }} />
        <Typography sx={{ fontSize: '13px', color: theme.palette.statusCritical.text }}>
          Hesaplama başarısız oldu.
          {errorCode && <Typography component="span" sx={{ fontSize: '12px', ml: 1, opacity: 0.8 }}>({errorCode})</Typography>}
        </Typography>
      </Box>
      
      {detail && (
        <Accordion 
          elevation={0}
          disableGutters
          sx={{ 
            bgcolor: 'transparent', 
            color: 'inherit',
            '&:before': { display: 'none' }
          }}
        >
          <AccordionSummary 
            expandIcon={<ExpandMoreIcon sx={{ color: theme.palette.statusCritical.text }} />}
            sx={{ minHeight: 'auto', '& .MuiAccordionSummary-content': { my: 0 }, px: '14px', py: 0, pb: '10px' }}
          >
            <Typography sx={{ fontSize: '12px', fontWeight: 600 }}>Teknik Detay</Typography>
          </AccordionSummary>
          <AccordionDetails sx={{ px: '14px', py: '10px', pt: 0, borderTop: `1px solid ${theme.palette.statusCritical.border}` }}>
            <Typography sx={{ fontSize: '12px', fontFamily: 'monospace' }}>{detail}</Typography>
          </AccordionDetails>
        </Accordion>
      )}
    </Box>
  );
}