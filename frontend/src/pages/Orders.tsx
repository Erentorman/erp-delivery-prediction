import { useEffect, useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TableSortLabel, Button, Chip,
  Alert, AlertTitle, CircularProgress, Link, TextField, InputAdornment
} from '@mui/material';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import { getMockOrders, Order, ORDERS_DATA_IS_MOCK, OrderStatus } from '../features/orders/orderMockData';
import { buildPredictionUrl } from '../features/prediction/predictionHelpers';
import { useTableSearchSort } from '../hooks/useTableSearchSort';

function getStatusColor(status: OrderStatus) {
  switch (status) {
    case 'Tamamlandı': return 'success';
    case 'Üretimde': return 'info';
    case 'İptal': return 'error';
    case 'Beklemede':
    default:
      return 'default';
  }
}

function formatDate(isoDate: string) {
  const date = new Date(isoDate);
  return new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }).format(date);
}

const columns: { key: string; label: string; align?: 'right' }[] = [
  { key: 'orderReference', label: 'Sipariş Referansı' },
  { key: 'customerName', label: 'Müşteri' },
  { key: 'productSummary', label: 'Ürün Özeti' },
  { key: 'orderDate', label: 'Sipariş Tarihi' },
  { key: 'status', label: 'Durum' },
];

export default function Orders() {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMockOrders().then(data => {
      setOrders(data);
      setLoading(false);
    });
  }, []);

  const { query, setQuery, sortKey, direction, toggleSort, rows } = useTableSearchSort(orders, {
    searchText: (o) => `${o.orderReference} ${o.customerName} ${o.productSummary}`,
    sorters: {
      orderReference: (a, b) => a.orderReference.localeCompare(b.orderReference),
      customerName: (a, b) => a.customerName.localeCompare(b.customerName),
      productSummary: (a, b) => a.productSummary.localeCompare(b.productSummary),
      orderDate: (a, b) => new Date(a.orderDate).getTime() - new Date(b.orderDate).getTime(),
      status: (a, b) => a.status.localeCompare(b.status),
    },
  });

  return (
    <Box sx={{ maxWidth: '1200px', mx: 'auto', width: '100%' }}>
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'textPrimary', mb: 1 }}>
        Siparişler
      </Typography>
      <Typography color="textSecondary" sx={{ mb: 4, fontSize: '13px' }}>
        Aşağıdaki listeden bir sipariş seçerek teslimat tahminini hesaplayabilirsiniz.
      </Typography>

      {ORDERS_DATA_IS_MOCK && (
        <Alert severity="info" sx={{ mb: 4, borderRadius: 2 }}>
          <AlertTitle>Bilgi</AlertTitle>
          Bu liste örnek veridir. Gerçek sipariş verileri bağlandığında burası güncellenecektir.
        </Alert>
      )}

      <TextField
        size="small"
        placeholder="Sipariş referansı, müşteri veya ürün ara..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        sx={{ mb: 2, width: { xs: '100%', sm: 340 } }}
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
              <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>İşlem</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <CircularProgress size={24} />
                  <Typography variant="body2" sx={{ mt: 1 }}>Yükleniyor...</Typography>
                </TableCell>
              </TableRow>
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <Typography variant="body2" color="textSecondary">
                    {query ? 'Aramayla eşleşen sipariş bulunamadı.' : 'Listelenecek sipariş bulunamadı.'}
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              rows.map((row) => (
                <TableRow key={row.orderReference} hover>
                  <TableCell>
                    <Link component={RouterLink} to={`/orders/${encodeURIComponent(row.orderReference)}`} underline="hover" sx={{ color: 'interactiveBlue', fontWeight: 600 }}>
                      {row.orderReference}
                    </Link>
                  </TableCell>
                  <TableCell>{row.customerName}</TableCell>
                  <TableCell>{row.productSummary}</TableCell>
                  <TableCell>{formatDate(row.orderDate)}</TableCell>
                  <TableCell>
                    <Chip
                      label={row.status}
                      size="small"
                      color={getStatusColor(row.status)}
                      sx={{ fontWeight: 'bold', fontSize: '11px' }}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      variant="outlined"
                      size="small"
                      onClick={() => navigate(buildPredictionUrl(row.orderReference))}
                      sx={{ textTransform: 'none', borderRadius: 2 }}
                    >
                      Tahminle
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
