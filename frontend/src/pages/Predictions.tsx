import { useState, useEffect } from 'react';
import { useSearchParams, Link as RouterLink } from 'react-router-dom';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { Typography, Box, TextField, Button, CircularProgress } from '@mui/material';
import { usePredictionCalculation } from '../features/prediction/hooks/usePredictionCalculation';
import { formatUserFriendlyDate, isDemoData } from '../features/prediction/predictionHelpers';
import { buildMockProviderComparison } from '../features/prediction/providerComparisonMock';
import {
  PredictionResultSummary,
  ProviderComparisonCards,
  DemoDataBanner,
  CriticalPathCard,
  MaterialShortagesCard,
  FallbackReasonsCard,
  OperationsTimelineCard,
  ValidationErrorBanner,
  CalculationFailureBanner
} from '../features/prediction/components';

export default function Predictions() {
  const [searchParams] = useSearchParams();
  const orderRefParam = searchParams.get('orderReference');
  
  const [orderReference, setOrderReference] = useState(orderRefParam || '');
  const [inputError, setInputError] = useState(false);
  
  const { state, calculate } = usePredictionCalculation();

  useEffect(() => {
    if (orderRefParam) {
      calculate(orderRefParam);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  

  const handleCalculate = () => {
    if (!orderReference.trim()) {
      setInputError(true);
      return;
    }
    setInputError(false);
    calculate(orderReference);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'Enter') {
      handleCalculate();
    }
  };

  return (
    <Box sx={{ maxWidth: '960px', mx: 'auto', width: '100%' }}>
      {orderRefParam && (
        <Button 
          component={RouterLink} 
          to="/orders" 
          startIcon={<ArrowBackIcon />}
          sx={{ mb: 2, textTransform: 'none', color: 'text.secondary' }}
        >
          Siparişler listesine dön
        </Button>
      )}
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'textPrimary', mb: 1 }}>
        Teslimat Tahmini
      </Typography>
      <Typography color="textSecondary" sx={{ mb: 4, fontSize: '13px' }}>
        Sipariş referansına göre tahmini üretim ve teslim tarihini hesaplayın.
      </Typography>

      <Box sx={{ display: 'flex', gap: 2, mb: 4, alignItems: 'flex-start', flexWrap: { xs: 'wrap', sm: 'nowrap' } }}>
        <TextField
          label="Sipariş Referansı"
          placeholder="Sipariş referansı, örn. SO00001"
          value={orderReference}
          onChange={(e) => {
            setOrderReference(e.target.value);
            if (e.target.value.trim()) setInputError(false);
          }}
          onKeyDown={handleKeyDown}
          error={inputError}
          helperText={inputError ? "Sipariş referansı girin" : ""}
          sx={{ width: { xs: '100%', sm: 300 } }}
          size="small"
        />
        <Button 
          variant="contained" 
          onClick={handleCalculate}
          disabled={state.status === 'loading'}
          sx={{ height: 40, mt: { xs: 0, sm: '2px' }, flexShrink: 0, width: { xs: '100%', sm: 'auto' } }}
        >
          {state.status === 'loading' && <CircularProgress size={14} sx={{ mr: 1, color: 'inherit' }} />}
          Tahmini Hesapla
        </Button>
      </Box>

      {/* State Render Logic */}
      <Box>
        {state.status === 'empty' && (
          <Typography sx={{ color: 'text.disabled', fontSize: '13px' }}>
            Sonuçlar burada görünecek.
          </Typography>
        )}

        {state.status === 'loading' && (
          <Typography sx={{ color: 'text.disabled', fontSize: '13px' }}>
            Hesaplanıyor, lütfen bekleyin...
          </Typography>
        )}

        {state.status === 'validationError' && (
          <ValidationErrorBanner detail={state.detail} />
        )}

        {state.status === 'calculationFailure' && (
          <CalculationFailureBanner errorCode={state.errorCode} detail={state.detail} />
        )}

        {state.status === 'success' && state.data && (() => {
          const ruleBased = state.data;
          const { ai, hybrid } = buildMockProviderComparison(ruleBased);

          return (
          <Box>
            <DemoDataBanner visible={isDemoData(ruleBased)} />

            <PredictionResultSummary
              delivery={formatUserFriendlyDate(ruleBased.estimatedDelivery)}
              start={formatUserFriendlyDate(ruleBased.estimatedStart)}
              end={formatUserFriendlyDate(ruleBased.estimatedEnd)}
              orderReference={ruleBased.orderReference}
            />

            <ProviderComparisonCards ruleBased={ruleBased} ai={ai} hybrid={hybrid} />

            <CriticalPathCard operations={ruleBased.timeline} />
            
            {ruleBased.shortages && ruleBased.shortages.length > 0 && (
              <MaterialShortagesCard shortages={ruleBased.shortages} />
            )}

            {ruleBased.appliedFallbackReasons && ruleBased.appliedFallbackReasons.length > 0 && (
              <FallbackReasonsCard reasons={ruleBased.appliedFallbackReasons} />
            )}

            <OperationsTimelineCard timeline={ruleBased.timeline} />
          </Box>
          );
        })()}
      </Box>
    </Box>
  );
}

