import { Box, Card, Typography, useTheme } from '@mui/material';
import PrecisionManufacturingOutlinedIcon from '@mui/icons-material/PrecisionManufacturingOutlined';
import ScienceOutlinedIcon from '@mui/icons-material/ScienceOutlined';
import MediationOutlinedIcon from '@mui/icons-material/MediationOutlined';
import type { RuleBasedPredictionResult } from '../predictionContracts';
import type { MockAiPrediction, MockHybridPrediction } from '../providerComparisonMock';
import { formatUserFriendlyDate } from '../predictionHelpers';

interface Props {
  ruleBased: RuleBasedPredictionResult;
  ai: MockAiPrediction;
  hybrid: MockHybridPrediction;
}

function MockDataTag() {
  const theme = useTheme();
  return (
    <Box sx={{
      display: 'inline-block',
      bgcolor: theme.palette.statusWarning.bg,
      color: theme.palette.statusWarning.text,
      border: `1px solid ${theme.palette.statusWarning.border}`,
      px: '7px', py: '2px',
      borderRadius: '3px',
      fontSize: '10.5px', textTransform: 'uppercase', fontWeight: 600,
    }}>
      Örnek Veri
    </Box>
  );
}

export function ProviderComparisonCards({ ruleBased, ai, hybrid }: Props) {
  const theme = useTheme();

  const columnSx = {
    p: '16px 20px',
    height: '100%',
    display: 'flex',
    flexDirection: 'column' as const,
  };

  return (
    <Card sx={{ mb: 2, p: 0 }}>
      <Box sx={{ p: '14px 20px', borderBottom: `1px solid ${theme.palette.borderDefault}` }}>
        <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: theme.palette.textPrimary }}>
          Sağlayıcı Karşılaştırması
        </Typography>
        <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary, mt: 0.5 }}>
          AI ve Final Hybrid sağlayıcıları bu MVP sürümünde henüz devrede değil; aşağıdaki iki kart yalnızca ekran düzenini göstermek için üretilmiş örnek değerler içerir.
        </Typography>
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' } }}>
        {/* Rule-Based — real data */}
        <Box sx={{ ...columnSx, borderRight: { xs: 'none', md: `1px solid ${theme.palette.borderDefault}` }, borderBottom: { xs: `1px solid ${theme.palette.borderDefault}`, md: 'none' } }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
            <PrecisionManufacturingOutlinedIcon sx={{ fontSize: 16, color: theme.palette.textPrimary }} />
            <Typography sx={{ fontSize: '12.5px', fontWeight: 600, color: theme.palette.textPrimary }}>Rule-Based</Typography>
          </Box>
          <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: theme.palette.textSecondary, mb: 0.5 }}>Tahmini Teslim</Typography>
          <Typography sx={{ fontSize: '15px', fontWeight: 600, color: theme.palette.textBody, mb: 1.5 }}>
            {formatUserFriendlyDate(ruleBased.estimatedDelivery)}
          </Typography>
          <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary }}>
            Fallback sayısı: {ruleBased.appliedFallbackReasons.length}
          </Typography>
        </Box>

        {/* AI — mock data */}
        <Box sx={{ ...columnSx, borderRight: { xs: 'none', md: `1px solid ${theme.palette.borderDefault}` }, borderBottom: { xs: `1px solid ${theme.palette.borderDefault}`, md: 'none' } }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <ScienceOutlinedIcon sx={{ fontSize: 16, color: theme.palette.textPrimary }} />
              <Typography sx={{ fontSize: '12.5px', fontWeight: 600, color: theme.palette.textPrimary }}>AI Model</Typography>
            </Box>
            <MockDataTag />
          </Box>
          <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: theme.palette.textSecondary, mb: 0.5 }}>Tahmini Teslim</Typography>
          <Typography sx={{ fontSize: '15px', fontWeight: 600, color: theme.palette.textBody, mb: 1.5 }}>
            {formatUserFriendlyDate(ai.estimatedDelivery)}
          </Typography>
          <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary }}>
            Model: {ai.modelVersion} · Güven: {(ai.confidenceScore * 100).toFixed(0)}%
          </Typography>
        </Box>

        {/* Final Hybrid — mock data */}
        <Box sx={columnSx}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <MediationOutlinedIcon sx={{ fontSize: 16, color: theme.palette.textPrimary }} />
              <Typography sx={{ fontSize: '12.5px', fontWeight: 600, color: theme.palette.textPrimary }}>Final Hybrid</Typography>
            </Box>
            <MockDataTag />
          </Box>
          <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: theme.palette.textSecondary, mb: 0.5 }}>Tahmini Teslim</Typography>
          <Typography sx={{ fontSize: '15px', fontWeight: 600, color: theme.palette.textBody, mb: 1.5 }}>
            {formatUserFriendlyDate(hybrid.estimatedDelivery)}
          </Typography>
          <Typography sx={{ fontSize: '12px', color: theme.palette.textSecondary }}>
            Ağırlık: Rule %{hybrid.ruleWeight * 100} · AI %{hybrid.aiWeight * 100}
          </Typography>
        </Box>
      </Box>
    </Card>
  );
}
