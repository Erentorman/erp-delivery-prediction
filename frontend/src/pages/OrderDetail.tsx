import { useEffect, useState } from 'react';
import { useParams, Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Card, CardContent, Button,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
  Alert, AlertTitle, CircularProgress, Chip
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import TimelineIcon from '@mui/icons-material/Timeline';
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined';
import { getMockOrderDetail, ORDER_DETAIL_DATA_IS_MOCK, type OrderDetail } from '../features/orders/orderDetailMockData';
import { buildPredictionUrl, formatUserFriendlyDate } from '../features/prediction/predictionHelpers';
import { usePredictionCalculation } from '../features/prediction/hooks/usePredictionCalculation';
import { computeDelayDays } from '../features/prediction/useOpenOrderDelayRisk';
import { ValidationErrorBanner, CalculationFailureBanner } from '../features/prediction/components';

export default function OrderDetailPage() {
  const { orderReference } = useParams<{ orderReference: string }>();
  const [detail, setDetail] = useState<OrderDetail | null | undefined>(undefined);
  const { state: predictionState, calculate } = usePredictionCalculation();

  useEffect(() => {
    if (!orderReference) return;
    setDetail(undefined);
    getMockOrderDetail(orderReference).then(setDetail);
  }, [orderReference]);

  return (
    <Box sx={{ maxWidth: '960px', mx: 'auto', width: '100%' }}>
      <Button
        component={RouterLink}
        to="/orders"
        startIcon={<ArrowBackIcon />}
        sx={{ mb: 2, textTransform: 'none', color: 'text.secondary' }}
      >
        Siparişler listesine dön
      </Button>

      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'textPrimary', mb: 1 }}>
        Sipariş Detayı — {orderReference}
      </Typography>

      {ORDER_DETAIL_DATA_IS_MOCK && (
        <Alert severity="info" sx={{ mb: 3, borderRadius: 2 }}>
          <AlertTitle>Bilgi</AlertTitle>
          Bu ekran örnek veridir. Gerçek ERP entegrasyonu (/api/erp/orders/{'{ref}'}) devreye alındığında burası güncellenecektir.
        </Alert>
      )}

      {detail === undefined ? (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <CircularProgress size={24} />
          <Typography>Sipariş detayı yükleniyor...</Typography>
        </Box>
      ) : detail === null ? (
        <Alert severity="warning" sx={{ borderRadius: 2 }}>
          "{orderReference}" referanslı sipariş bulunamadı.
        </Alert>
      ) : (
        <>
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, mb: 3 }}>
            <CardContent>
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 3 }}>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>Ürün</Typography>
                  <Typography variant="h6">{detail.productName}</Typography>
                  <Typography variant="caption" color="textSecondary">{detail.productId}</Typography>
                </Box>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>Miktar</Typography>
                  <Typography variant="h6">{detail.quantity} {detail.productUnit}</Typography>
                </Box>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>İstenen Teslim Tarihi</Typography>
                  <Typography variant="h6">{formatUserFriendlyDate(detail.requestedDeliveryDate)}</Typography>
                </Box>
              </Box>

              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, mt: 3 }}>
                <Button
                  variant="contained"
                  sx={{ textTransform: 'none', borderRadius: 2 }}
                  startIcon={predictionState.status === 'loading' ? <CircularProgress size={16} sx={{ color: 'inherit' }} /> : <TimelineIcon />}
                  disabled={predictionState.status === 'loading'}
                  onClick={() => calculate(detail.orderReference)}
                >
                  Teslimat Tahminini Hesapla
                </Button>
                {predictionState.status === 'success' && (
                  <Button
                    component={RouterLink}
                    to={buildPredictionUrl(detail.orderReference)}
                    variant="text"
                    endIcon={<OpenInNewOutlinedIcon sx={{ fontSize: 15 }} />}
                    sx={{ textTransform: 'none', borderRadius: 2 }}
                  >
                    Detaylı tahmin sayfasında aç
                  </Button>
                )}
              </Box>

              {predictionState.status === 'validationError' && (
                <Box sx={{ mt: 2 }}>
                  <ValidationErrorBanner detail={predictionState.detail} />
                </Box>
              )}
              {predictionState.status === 'calculationFailure' && (
                <Box sx={{ mt: 2 }}>
                  <CalculationFailureBanner errorCode={predictionState.errorCode} detail={predictionState.detail} />
                </Box>
              )}
              {predictionState.status === 'success' && (() => {
                const result = predictionState.data;
                const delayDays = computeDelayDays(result.estimatedDelivery, detail.requestedDeliveryDate);
                return (
                  <Box sx={{
                    mt: 2, p: '14px 16px', borderRadius: 2,
                    border: '1px solid', borderColor: 'divider', bgcolor: 'surfaceSubtle',
                    display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 2,
                  }}>
                    <Box>
                      <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: 'textSecondary', fontWeight: 600, mb: 0.5 }}>Tahmini Teslim</Typography>
                      <Typography sx={{ fontSize: '14px', fontWeight: 600 }}>{formatUserFriendlyDate(result.estimatedDelivery)}</Typography>
                    </Box>
                    <Box>
                      <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: 'textSecondary', fontWeight: 600, mb: 0.5 }}>Kritik Yol Operasyonu</Typography>
                      <Typography sx={{ fontSize: '14px', fontWeight: 600 }}>{result.criticalPathOperations.length} operasyon</Typography>
                    </Box>
                    <Box>
                      <Typography sx={{ fontSize: '11px', textTransform: 'uppercase', color: 'textSecondary', fontWeight: 600, mb: 0.5 }}>İstenen Tarihe Göre</Typography>
                      <Typography sx={{ fontSize: '14px', fontWeight: 700, color: delayDays > 0 ? 'error.main' : 'success.main' }}>
                        {delayDays > 0 ? `${delayDays} gün gecikiyor` : 'Zamanında'}
                      </Typography>
                    </Box>
                  </Box>
                );
              })()}
            </CardContent>
          </Card>

          <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1.5 }}>Ürün Reçetesi (BOM)</Typography>
          <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, mb: 3 }}>
            <Table size="small">
              <TableHead sx={{ bgcolor: 'surfaceSubtle' }}>
                <TableRow>
                  <TableCell sx={{ fontWeight: 'bold' }}>Bileşen</TableCell>
                  <TableCell sx={{ fontWeight: 'bold' }}>Açıklama</TableCell>
                  <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Miktar</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {detail.bom.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={3} align="center" sx={{ py: 3, color: 'text.secondary' }}>Reçete bilgisi bulunamadı.</TableCell>
                  </TableRow>
                ) : (
                  detail.bom.map((line) => (
                    <TableRow key={line.componentId}>
                      <TableCell>{line.componentId}</TableCell>
                      <TableCell>{line.description}</TableCell>
                      <TableCell sx={{ textAlign: 'right' }}>{line.quantity} {line.unit}</TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>

          <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1.5 }}>Stok Durumu</Typography>
          <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, mb: 3 }}>
            <CardContent>
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 3 }}>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>Eldeki Miktar</Typography>
                  <Typography variant="h6">{detail.stock.onHandQuantity}</Typography>
                </Box>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>Rezerve</Typography>
                  <Typography variant="h6">{detail.stock.reservedQuantity}</Typography>
                </Box>
                <Box>
                  <Typography color="textSecondary" variant="body2" gutterBottom>Kullanılabilir</Typography>
                  <Typography variant="h6" color={detail.stock.availableQuantity < detail.quantity ? 'warning.main' : 'success.main'}>
                    {detail.stock.availableQuantity}
                  </Typography>
                </Box>
              </Box>
              {detail.stock.locationReference && (
                <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 2 }}>
                  Lokasyon: {detail.stock.locationReference}
                </Typography>
              )}
            </CardContent>
          </Card>

          <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1.5 }}>Üretim / İş Emri</Typography>
          {detail.workOrder === null ? (
            <Alert severity="info" sx={{ borderRadius: 2 }}>Bu sipariş henüz üretime alınmadı; iş emri bulunmuyor.</Alert>
          ) : (
            <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
              <Box sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>{detail.workOrder.workOrderReference}</Typography>
                <Chip label={detail.workOrder.status} size="small" color={detail.workOrder.status === 'Completed' ? 'success' : 'info'} />
              </Box>
              <Table size="small">
                <TableHead sx={{ bgcolor: 'surfaceSubtle' }}>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 'bold' }}>Sıra</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Operasyon</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>İş Merkezi</TableCell>
                    <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Standart Süre</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {detail.workOrder.operations.map((op) => (
                    <TableRow key={op.operationReference}>
                      <TableCell>{op.operationSequence}</TableCell>
                      <TableCell>{op.operationReference}</TableCell>
                      <TableCell>{op.workCenterReference}</TableCell>
                      <TableCell sx={{ textAlign: 'right' }}>{op.standardDurationMinutes} dk</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </>
      )}
    </Box>
  );
}
