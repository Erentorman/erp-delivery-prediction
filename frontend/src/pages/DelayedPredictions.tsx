import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Chip, Alert, AlertTitle,
  CircularProgress, FormControlLabel, Checkbox, Link
} from '@mui/material';
import { ORDERS_DATA_IS_MOCK } from '../features/orders/orderMockData';
import { useOpenOrderDelayRisk, getRequestedDeliveryDate } from '../features/prediction/useOpenOrderDelayRisk';
import { formatUserFriendlyDate } from '../features/prediction/predictionHelpers';

export default function DelayedPredictions() {
  const { rows, loading } = useOpenOrderDelayRisk();
  const [onlyDelayed, setOnlyDelayed] = useState(true);

  // "Yalnızca gecikenleri göster" açıkken de hata/hesaplanıyor satırları gizlenmez;
  // yalnızca zamanında olduğu kesinleşen siparişler filtrelenir.
  const visibleRows = onlyDelayed ? rows.filter((r) => r.status !== 'onTime') : rows;
  const delayedCount = rows.filter((r) => r.status === 'delayed').length;
  const stillCalculating = rows.some((r) => r.status === 'loading');

  return (
    <Box sx={{ maxWidth: '1200px', mx: 'auto', width: '100%' }}>
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'brand900', mb: 1 }}>
        Tahmin Listesi / Gecikenler
      </Typography>
      <Typography color="textSecondary" sx={{ mb: 3, fontSize: '13px' }}>
        Açık siparişler için gerçek zamanlı teslimat tahmini hesaplanır ve istenen teslim tarihiyle karşılaştırılır.
      </Typography>

      {ORDERS_DATA_IS_MOCK && (
        <Alert severity="info" sx={{ mb: 3, borderRadius: 2 }}>
          <AlertTitle>Bilgi</AlertTitle>
          Sipariş listesi örnek veridir; ancak her satır için gösterilen tahmin, backend'in gerçek Rule-Based hesaplama servisine (<code>/api/predictions/calculate</code>) yapılan gerçek bir çağrının sonucudur. Backend'e ulaşılamıyorsa ilgili satır "Hesaplanamadı" olarak işaretlenir.
        </Alert>
      )}

      <FormControlLabel
        control={<Checkbox checked={onlyDelayed} onChange={(e) => setOnlyDelayed(e.target.checked)} size="small" />}
        label={`Yalnızca gecikenleri göster (${delayedCount})`}
        sx={{ mb: 2 }}
      />

      <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Table>
          <TableHead sx={{ bgcolor: 'grey.50' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 'bold' }}>Sipariş Referansı</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Ürün</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>İstenen Teslim</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Tahmini Teslim</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Gecikme</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Durum</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <CircularProgress size={24} />
                </TableCell>
              </TableRow>
            ) : visibleRows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <Typography variant="body2" color="textSecondary">
                    {onlyDelayed ? 'Gecikmesi tespit edilen sipariş bulunmuyor.' : 'Listelenecek sipariş bulunamadı.'}
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              visibleRows.map((row) => (
                <TableRow key={row.order.orderReference} hover>
                  <TableCell>
                    <Link component={RouterLink} to={`/orders/${encodeURIComponent(row.order.orderReference)}`} underline="hover">
                      {row.order.orderReference}
                    </Link>
                  </TableCell>
                  <TableCell>{row.order.productSummary}</TableCell>
                  <TableCell>{formatUserFriendlyDate(getRequestedDeliveryDate(row.order))}</TableCell>
                  <TableCell>
                    {row.status === 'loading' ? <CircularProgress size={14} /> : row.estimatedDelivery ? formatUserFriendlyDate(row.estimatedDelivery) : '—'}
                  </TableCell>
                  <TableCell>
                    {row.status === 'delayed' && row.delayDays ? `${row.delayDays} gün` : row.status === 'onTime' ? '—' : ''}
                  </TableCell>
                  <TableCell>
                    {row.status === 'loading' && <Chip label="Hesaplanıyor" size="small" />}
                    {row.status === 'onTime' && <Chip label="Zamanında" size="small" color="success" />}
                    {row.status === 'delayed' && <Chip label="Gecikiyor" size="small" color="error" />}
                    {row.status === 'error' && <Chip label="Hesaplanamadı" size="small" color="default" title={row.errorMessage} />}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {stillCalculating && !loading && (
        <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mt: 2 }}>
          Bazı satırlar için hesaplama devam ediyor...
        </Typography>
      )}
    </Box>
  );
}
