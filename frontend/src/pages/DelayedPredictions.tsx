import { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TableSortLabel, Chip, Alert, AlertTitle,
  CircularProgress, FormControlLabel, Checkbox, Link, TextField, InputAdornment
} from '@mui/material';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import { ORDERS_DATA_IS_MOCK } from '../features/orders/orderMockData';
import { useOpenOrderDelayRisk, getRequestedDeliveryDate, type OpenOrderPrediction } from '../features/prediction/useOpenOrderDelayRisk';
import { formatUserFriendlyDate } from '../features/prediction/predictionHelpers';
import { useTableSearchSort } from '../hooks/useTableSearchSort';

const columns: { key: string; label: string }[] = [
  { key: 'orderReference', label: 'Sipariş Referansı' },
  { key: 'product', label: 'Ürün' },
  { key: 'requestedDelivery', label: 'İstenen Teslim' },
  { key: 'estimatedDelivery', label: 'Tahmini Teslim' },
  { key: 'delayDays', label: 'Gecikme' },
];

export default function DelayedPredictions() {
  const { rows: allRows, loading } = useOpenOrderDelayRisk();
  const [onlyDelayed, setOnlyDelayed] = useState(true);

  // "Yalnızca gecikenleri göster" açıkken de hata/hesaplanıyor satırları gizlenmez;
  // yalnızca zamanında olduğu kesinleşen siparişler filtrelenir.
  const filteredByToggle = onlyDelayed ? allRows.filter((r) => r.status !== 'onTime') : allRows;
  const delayedCount = allRows.filter((r) => r.status === 'delayed').length;
  const stillCalculating = allRows.some((r) => r.status === 'loading');

  const { query, setQuery, sortKey, direction, toggleSort, rows: visibleRows } = useTableSearchSort<OpenOrderPrediction>(filteredByToggle, {
    searchText: (r) => `${r.order.orderReference} ${r.order.productSummary}`,
    sorters: {
      orderReference: (a, b) => a.order.orderReference.localeCompare(b.order.orderReference),
      product: (a, b) => a.order.productSummary.localeCompare(b.order.productSummary),
      requestedDelivery: (a, b) => new Date(getRequestedDeliveryDate(a.order)).getTime() - new Date(getRequestedDeliveryDate(b.order)).getTime(),
      estimatedDelivery: (a, b) => new Date(a.estimatedDelivery ?? 0).getTime() - new Date(b.estimatedDelivery ?? 0).getTime(),
      delayDays: (a, b) => (a.delayDays ?? -Infinity) - (b.delayDays ?? -Infinity),
    },
  });

  return (
    <Box sx={{ maxWidth: '1200px', mx: 'auto', width: '100%' }}>
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'textPrimary', mb: 1 }}>
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

      <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 2, mb: 2 }}>
        <TextField
          size="small"
          placeholder="Sipariş referansı veya ürün ara..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          sx={{ width: { xs: '100%', sm: 300 } }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchOutlinedIcon sx={{ fontSize: 18, color: 'textMuted' }} />
                </InputAdornment>
              ),
            },
          }}
        />
        <FormControlLabel
          control={<Checkbox checked={onlyDelayed} onChange={(e) => setOnlyDelayed(e.target.checked)} size="small" />}
          label={`Yalnızca gecikenleri göster (${delayedCount})`}
        />
      </Box>

      <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Table>
          <TableHead sx={{ bgcolor: 'surfaceSubtle' }}>
            <TableRow>
              {columns.map((col) => (
                <TableCell key={col.key} sx={{ fontWeight: 'bold' }}>
                  <TableSortLabel
                    active={sortKey === col.key}
                    direction={sortKey === col.key ? direction : 'asc'}
                    onClick={() => toggleSort(col.key)}
                  >
                    {col.label}
                  </TableSortLabel>
                </TableCell>
              ))}
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
                    {query ? 'Aramayla eşleşen sipariş bulunamadı.' : onlyDelayed ? 'Gecikmesi tespit edilen sipariş bulunmuyor.' : 'Listelenecek sipariş bulunamadı.'}
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              visibleRows.map((row) => (
                <TableRow key={row.order.orderReference} hover>
                  <TableCell>
                    <Link component={RouterLink} to={`/orders/${encodeURIComponent(row.order.orderReference)}`} underline="hover" sx={{ color: 'interactiveBlue', fontWeight: 600 }}>
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
