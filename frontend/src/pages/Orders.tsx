import { useEffect, useState } from 'react';
import { useNavigate, Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Button, Chip,
  Alert, AlertTitle, CircularProgress, Link
} from '@mui/material';
import { getMockOrders, Order, ORDERS_DATA_IS_MOCK, OrderStatus } from '../features/orders/orderMockData';
import { buildPredictionUrl } from '../features/prediction/predictionHelpers';

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

  return (
    <Box sx={{ maxWidth: '1200px', mx: 'auto', width: '100%' }}>
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'brand900', mb: 1 }}>
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

      <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Table>
          <TableHead sx={{ bgcolor: 'grey.50' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 'bold' }}>Sipariş Referansı</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Müşteri</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Ürün Özeti</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Sipariş Tarihi</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Durum</TableCell>
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
            ) : orders.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <Typography variant="body2" color="textSecondary">
                    Listelenecek sipariş bulunamadı.
                  </Typography>
                </TableCell>
              </TableRow>
            ) : (
              orders.map((row) => (
                <TableRow key={row.orderReference} hover>
                  <TableCell>
                    <Link component={RouterLink} to={`/orders/${encodeURIComponent(row.orderReference)}`} underline="hover">
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
