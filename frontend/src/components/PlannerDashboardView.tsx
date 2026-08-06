import { useState } from 'react';
import { 
  Typography, Box, Card, CardContent, Button, 
  Divider, CircularProgress, Alert, TextField, Chip,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper
} from '@mui/material';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import ScienceIcon from '@mui/icons-material/Science';
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing';
import MediationIcon from '@mui/icons-material/Mediation';
import { usePrediction } from '../hooks/usePrediction';

export default function PlannerDashboardView() {
  const { isLoading, error, plannerResult, fetchCalculation } = usePrediction();
  const [orderReference, setOrderReference] = useState<string>('');

  const handleCalculate = () => {
    if (!orderReference) return;
    fetchCalculation(orderReference);
  };

  const formatDate = (isoString: string) => {
    return new Date(isoString).toLocaleString('tr-TR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  };

  return (
    <Box sx={{ animation: 'fadeIn 0.5s ease-in' }}>
      <Box sx={{ display: 'flex', gap: 2, mb: 4, alignItems: 'center' }}>
        <TextField
          size="small"
          label="Sipariş Numarası"
          placeholder="Örn: SO00001"
          value={orderReference}
          onChange={(e) => setOrderReference(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && handleCalculate()}
          sx={{ width: 300 }}
        />
        <Button 
          variant="contained" 
          onClick={handleCalculate} 
          disabled={isLoading || !orderReference}
          sx={{ fontWeight: 'bold' }}
        >
          {isLoading ? <CircularProgress size={24} color="inherit" /> : 'SİPARİŞİ ANALİZ ET'}
        </Button>
      </Box>

      {error && (
        <Alert severity="error" variant="filled" sx={{ mb: 4 }}>
          {error}
        </Alert>
      )}

      {plannerResult && (
        <>
          {/* Tripartite Panel */}
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' }, gap: 3, mb: 4 }}>
            {/* 1. Rule-Based */}
            <Box>
              <Card sx={{ height: '100%', borderTop: '4px solid #0ea5e9' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <PrecisionManufacturingIcon color="primary" />
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>Rule-Based Prediction</Typography>
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">Est. Delivery:</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{formatDate(plannerResult.ruleBased.estimatedDelivery)}</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">Lead Time:</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{plannerResult.ruleBased.displayWorkingLeadTime} Saat</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                    <Typography variant="body2" color="text.secondary">Fallbacks:</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{plannerResult.ruleBased.appliedFallbackReasons.length}</Typography>
                  </Box>

                  {plannerResult.ruleBased.appliedFallbackReasons.length > 0 && (
                    <Alert severity="warning" icon={<WarningAmberIcon fontSize="small"/>} sx={{ p: 0.5, px: 1 }}>
                      <Typography variant="caption">Rules relaxed due to constraints.</Typography>
                    </Alert>
                  )}
                </CardContent>
              </Card>
            </Box>

            {/* 2. AI Model */}
            <Box>
              <Card sx={{ height: '100%', borderTop: '4px solid #a855f7' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <ScienceIcon color="secondary" />
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>AI Model Prediction</Typography>
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">Est. Delivery:</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{formatDate(plannerResult.ai.estimatedDelivery)}</Typography>
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">Model Ver:</Typography>
                    <Chip label={plannerResult.ai.modelVersion} size="small" variant="outlined" />
                  </Box>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                    <Typography variant="body2" color="text.secondary">Confidence:</Typography>
                    <Typography variant="body2" sx={{ fontWeight: 'bold', color: plannerResult.ai.confidenceScore > 0.8 ? 'success.main' : 'warning.main' }}>
                      {(plannerResult.ai.confidenceScore * 100).toFixed(0)}%
                    </Typography>
                  </Box>

                  {plannerResult.ai.warnings.map((w, idx) => (
                    <Typography key={idx} variant="caption" sx={{ color: 'error.main', display: 'block' }}>
                      • {w}
                    </Typography>
                  ))}
                </CardContent>
              </Card>
            </Box>

            {/* 3. Hybrid Final */}
            <Box>
              <Card sx={{ height: '100%', borderTop: '4px solid #22c55e' }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                    <MediationIcon color="success" />
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>Final Hybrid Strategy</Typography>
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="body2" color="text.secondary">Final Delivery:</Typography>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', color: 'success.main' }}>{formatDate(plannerResult.hybrid.estimatedDelivery)}</Typography>
                  </Box>
                  
                  <Box sx={{ mt: 3 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>Weights Applied:</Typography>
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <Chip label={`Rule: ${plannerResult.hybrid.ruleWeight * 100}%`} size="small" color="primary" />
                      <Chip label={`AI: ${plannerResult.hybrid.aiWeight * 100}%`} size="small" color="secondary" />
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
          </Box>

          {/* Explainability Panel */}
          <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 2 }}>Açıklanabilirlik ve Kritik Hat Özeti</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '7fr 5fr' }, gap: 3 }}>
            
            {/* Timeline (Gantt substitute) */}
            <Box>
              <Card sx={{ height: '100%' }}>
                <CardContent>
                  <Typography variant="subtitle2" color="text.secondary" sx={{ textTransform: 'uppercase', mb: 2 }}>
                    Critical Path (Darboğazlar)
                  </Typography>
                  
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                    {plannerResult.criticalPathSummary.map((cp, idx) => (
                      <Box key={idx} sx={{ p: 1.5, bgcolor: 'rgba(239, 68, 68, 0.1)', borderLeft: '4px solid', borderColor: 'error.main', borderRadius: 1 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{cp.operationRef}</Typography>
                          <Typography variant="caption" color="error.main">Kritik</Typography>
                        </Box>
                        <Typography variant="caption" color="text.secondary">
                          {new Date(cp.estimatedStart).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })} - {new Date(cp.estimatedEnd).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })}
                        </Typography>
                      </Box>
                    ))}
                    {plannerResult.criticalPathSummary.length === 0 && (
                      <Typography variant="body2" color="text.secondary">Kritik darboğaz tespit edilmedi.</Typography>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Box>

            {/* Prediction Factors */}
            <Box>
              <Card sx={{ height: '100%' }}>
                <CardContent>
                  <Typography variant="subtitle2" color="text.secondary" sx={{ textTransform: 'uppercase', mb: 2 }}>
                    Risk Faktörleri (Factors)
                  </Typography>
                  
                  <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider' }}>
                    <Table size="small">
                      <TableHead sx={{ bgcolor: 'grey.50' }}>
                        <TableRow>
                          <TableCell><b>Faktör</b></TableCell>
                          <TableCell><b>Etki</b></TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {plannerResult.factors.factors.map((row, idx) => (
                          <TableRow key={idx}>
                            <TableCell>{row.name}</TableCell>
                            <TableCell>{row.impact}</TableCell>
                          </TableRow>
                        ))}
                        {plannerResult.factors.factors.length === 0 && (
                          <TableRow>
                            <TableCell colSpan={2} align="center" sx={{ color: 'text.secondary' }}>Risk yok.</TableCell>
                          </TableRow>
                        )}
                      </TableBody>
                    </Table>
                  </TableContainer>
                  
                  <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="body2">Genel Risk Seviyesi:</Typography>
                    <Chip 
                      label={plannerResult.factors.riskLevel} 
                      size="small"
                      color={
                        plannerResult.factors.riskLevel === 'Low' ? 'success' :
                        plannerResult.factors.riskLevel === 'Medium' ? 'warning' : 'error'
                      } 
                    />
                  </Box>
                  
                  {plannerResult.ruleBased.shortages && plannerResult.ruleBased.shortages.length > 0 && (
                    <Box sx={{ mt: 3, p: 1.5, bgcolor: 'rgba(239, 68, 68, 0.1)', border: '1px solid', borderColor: 'error.main', borderRadius: 1 }}>
                      <Typography variant="subtitle2" color="error.main" sx={{ mb: 1, fontWeight: 'bold' }}>Hammadde Eksiklikleri (Shortages)</Typography>
                      {plannerResult.ruleBased.shortages.map((s, idx) => (
                        <Typography key={idx} variant="caption" sx={{ display: 'block' }} color="text.primary">
                          • {s.materialRef}: <Typography component="span" variant="caption" color="error.main">{s.missingQuantity} eksik</Typography>
                        </Typography>
                      ))}
                    </Box>
                  )}
                </CardContent>
              </Card>
            </Box>

          </Box>
        </>
      )}
    </Box>
  );
}
