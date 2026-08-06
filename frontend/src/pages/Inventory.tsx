import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Chip, Alert, AlertTitle
} from '@mui/material';
import { getProductStockOverview } from '../features/orders/orderDetailMockData';

export default function Inventory() {
  const products = getProductStockOverview();

  return (
    <Box sx={{ maxWidth: '1000px', mx: 'auto', width: '100%' }}>
      <Typography variant="h1" gutterBottom sx={{ fontSize: '18px', color: 'textPrimary', mb: 1 }}>
        Stok Görünümü
      </Typography>
      <Typography color="textSecondary" sx={{ mb: 3, fontSize: '13px' }}>
        Ürün bazında salt-okunur stok durumu.
      </Typography>

      <Alert severity="info" sx={{ mb: 3, borderRadius: 2 }}>
        <AlertTitle>Bilgi</AlertTitle>
        Bu ekran örnek veridir; gerçek karşılığı App.Api üzerinden henüz açılmamış <code>/api/stock-levels</code> endpoint'idir. Kapasite/takvim görünümü (iş merkezi, vardiya, planlı duruş) bu MVP aşamasında kapsam dışıdır.
      </Alert>

      <TableContainer component={Paper} elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
        <Table>
          <TableHead sx={{ bgcolor: 'surfaceSubtle' }}>
            <TableRow>
              <TableCell sx={{ fontWeight: 'bold' }}>Ürün</TableCell>
              <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Eldeki Miktar</TableCell>
              <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Rezerve</TableCell>
              <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Kullanılabilir</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Lokasyon</TableCell>
              <TableCell sx={{ fontWeight: 'bold' }}>Durum</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {products.map((p) => (
              <TableRow key={p.productId} hover>
                <TableCell>
                  <Typography variant="body2" sx={{ fontWeight: 500 }}>{p.productName}</Typography>
                  <Typography variant="caption" color="textSecondary">{p.productId}</Typography>
                </TableCell>
                <TableCell sx={{ textAlign: 'right' }}>{p.stock.onHandQuantity} {p.unit}</TableCell>
                <TableCell sx={{ textAlign: 'right' }}>{p.stock.reservedQuantity} {p.unit}</TableCell>
                <TableCell sx={{ textAlign: 'right' }}>{p.stock.availableQuantity} {p.unit}</TableCell>
                <TableCell>{p.stock.locationReference ?? '—'}</TableCell>
                <TableCell>
                  {p.stock.availableQuantity <= 0 ? (
                    <Chip label="Tükendi" size="small" color="error" />
                  ) : p.stock.availableQuantity < p.stock.reservedQuantity ? (
                    <Chip label="Düşük" size="small" color="warning" />
                  ) : (
                    <Chip label="Yeterli" size="small" color="success" />
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
