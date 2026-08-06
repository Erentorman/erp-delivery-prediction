import { useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Card, CardContent, Button,
  Alert, AlertTitle, CircularProgress, Divider
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ListAltIcon from '@mui/icons-material/ListAlt';
import { getMockOrders, Order, ORDERS_DATA_IS_MOCK } from '../features/orders/orderMockData';
import { hasStockShortfall } from '../features/orders/orderDetailMockData';
import { useOpenOrderDelayRisk } from '../features/prediction/useOpenOrderDelayRisk';

export default function Dashboard() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const { rows: delayRiskRows, loading: delayRiskLoading } = useOpenOrderDelayRisk();

  useEffect(() => {
    getMockOrders().then(data => {
      setOrders(data);
      setLoading(false);
    });
  }, []);

  const totalOrders = orders.length;
  const pendingOrders = orders.filter(o => o.status === 'Beklemede').length;
  const inProductionOrders = orders.filter(o => o.status === 'Üretimde').length;
  const stockShortfallOrders = orders.filter(o => o.status === 'Beklemede' && hasStockShortfall(o)).length;

  const delayedCount = delayRiskRows.filter(r => r.status === 'delayed').length;
  const delayRiskStillCalculating = delayRiskRows.some(r => r.status === 'loading');

  return (
    <Box sx={{ maxWidth: '1200px', mx: 'auto', width: '100%' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 4 }}>
        <DashboardIcon sx={{ fontSize: 32, mr: 2, color: 'brand900' }} />
        <Typography variant="h1" sx={{ fontSize: '24px', color: 'brand900' }}>
          Kontrol Paneli
        </Typography>
      </Box>

      {ORDERS_DATA_IS_MOCK && (
        <Alert severity="info" sx={{ mb: 4, borderRadius: 2 }}>
          <AlertTitle>Bilgi</AlertTitle>
          Sipariş listesi örnek verilerden türetilmiştir; "Gecikme Riski" kartı ise backend'in gerçek tahmin servisine yapılan çağrıların sonucudur.
        </Alert>
      )}

      {loading ? (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <CircularProgress size={24} />
          <Typography>Özet yükleniyor...</Typography>
        </Box>
      ) : (
        <>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(5, 1fr)' }, gap: 3, mb: 4 }}>
            <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>Toplam Sipariş</Typography>
                <Typography variant="h3" sx={{ color: 'brand900' }}>{totalOrders}</Typography>
              </CardContent>
            </Card>
            <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>Bekleyen Siparişler</Typography>
                <Typography variant="h3" color="warning.main">{pendingOrders}</Typography>
              </CardContent>
            </Card>
            <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
              <CardContent>
                <Typography color="textSecondary" gutterBottom>Üretimdekiler</Typography>
                <Typography variant="h3" color="info.main">{inProductionOrders}</Typography>
              </CardContent>
            </Card>
            <Card
              component={RouterLink}
              to="/predictions/delayed"
              elevation={0}
              sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, textDecoration: 'none', display: 'block', '&:hover': { borderColor: 'error.main' } }}
            >
              <CardContent>
                <Typography color="textSecondary" gutterBottom>Gecikme Riski</Typography>
                {delayRiskLoading ? (
                  <CircularProgress size={20} />
                ) : (
                  <Typography variant="h3" color="error.main">
                    {delayedCount}{delayRiskStillCalculating ? '+' : ''}
                  </Typography>
                )}
              </CardContent>
            </Card>
            <Card
              component={RouterLink}
              to="/inventory"
              elevation={0}
              sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, textDecoration: 'none', display: 'block', '&:hover': { borderColor: 'warning.main' } }}
            >
              <CardContent>
                <Typography color="textSecondary" gutterBottom>Stok Nedeniyle Bekleyen</Typography>
                <Typography variant="h3" sx={{ color: 'warning.main' }}>{stockShortfallOrders}</Typography>
              </CardContent>
            </Card>
          </Box>

          <Divider sx={{ my: 4 }} />

          <Box sx={{ textAlign: 'center', py: 4, bgcolor: 'grey.50', borderRadius: 2, border: '1px dashed', borderColor: 'divider' }}>
            <Typography variant="h6" gutterBottom>
              Teslimat tahmini yapmak ister misiniz?
            </Typography>
            <Typography color="textSecondary" sx={{ mb: 3 }}>
              Siparişler listenize giderek açık siparişleriniz için üretim ve teslimat süresini hesaplayabilirsiniz.
            </Typography>
            <Button
              variant="contained"
              component={RouterLink}
              to="/orders"
              startIcon={<ListAltIcon />}
              sx={{ px: 4, py: 1.5, borderRadius: 2 }}
            >
              Siparişlere Gözat
            </Button>
          </Box>
        </>
      )}
    </Box>
  );
}
