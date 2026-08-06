import {
  Typography, Box, Paper, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TableSortLabel, Chip, Alert, AlertTitle,
  TextField, InputAdornment
} from '@mui/material';
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined';
import { getProductStockOverview, type ProductStockOverview } from '../features/orders/orderDetailMockData';
import { useTableSearchSort } from '../hooks/useTableSearchSort';

const columns: { key: string; label: string; align?: 'right' }[] = [
  { key: 'productName', label: 'Ürün' },
  { key: 'onHandQuantity', label: 'Eldeki Miktar', align: 'right' },
  { key: 'reservedQuantity', label: 'Rezerve', align: 'right' },
  { key: 'availableQuantity', label: 'Kullanılabilir', align: 'right' },
  { key: 'locationReference', label: 'Lokasyon' },
];

export default function Inventory() {
  const products = getProductStockOverview();

  const { query, setQuery, sortKey, direction, toggleSort, rows } = useTableSearchSort(products, {
    searchText: (p) => `${p.productName} ${p.productId}`,
    sorters: {
      productName: (a, b) => a.productName.localeCompare(b.productName),
      onHandQuantity: (a, b) => a.stock.onHandQuantity - b.stock.onHandQuantity,
      reservedQuantity: (a, b) => a.stock.reservedQuantity - b.stock.reservedQuantity,
      availableQuantity: (a, b) => a.stock.availableQuantity - b.stock.availableQuantity,
      locationReference: (a, b) => (a.stock.locationReference ?? '').localeCompare(b.stock.locationReference ?? ''),
    },
  });

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

      <TextField
        size="small"
        placeholder="Ürün ara..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        sx={{ mb: 2, width: { xs: '100%', sm: 300 } }}
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
                <TableCell key={col.key} sx={{ fontWeight: 'bold', textAlign: col.align }}>
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
            {rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                  <Typography variant="body2" color="textSecondary">Aramayla eşleşen ürün bulunamadı.</Typography>
                </TableCell>
              </TableRow>
            ) : (
              rows.map((p: ProductStockOverview) => (
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
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
