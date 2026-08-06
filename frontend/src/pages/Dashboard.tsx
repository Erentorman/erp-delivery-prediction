import { useEffect, useState, type ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Card, CardContent, Button,
  Alert, AlertTitle, CircularProgress, useTheme
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ListAltIcon from '@mui/icons-material/ListAlt';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined';
import PrecisionManufacturingOutlinedIcon from '@mui/icons-material/PrecisionManufacturingOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { getMockOrders, Order, ORDERS_DATA_IS_MOCK } from '../features/orders/orderMockData';
import { hasStockShortfall } from '../features/orders/orderDetailMockData';
import { useOpenOrderDelayRisk } from '../features/prediction/useOpenOrderDelayRisk';

interface StatCardProps {
  icon: ReactNode;
  label: string;
  value: ReactNode;
  accentColor: string;
  to?: string;
}

function StatCard({ icon, label, value, accentColor, to }: StatCardProps) {
  const cardProps = to ? { component: RouterLink, to } : {};

  return (
    <Card
      {...cardProps}
      elevation={0}
      sx={{
        borderTop: `3px solid ${accentColor}`,
        textDecoration: 'none',
        display: 'block',
        cursor: to ? 'pointer' : 'default',
        '&:hover': to ? { transform: 'translateY(-2px)', boxShadow: '0 4px 16px rgba(15,41,66,0.10)' } : undefined,
      }}
    >
      <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <Box sx={{
          width: 34, height: 34, borderRadius: 2,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          bgcolor: `${accentColor}14`,
        }}>
          {icon}
        </Box>
        <Typography sx={{ fontSize: '13px', color: 'textSecondary', fontWeight: 500 }}>{label}</Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography sx={{ fontSize: '30px', fontWeight: 700, color: 'textPrimary', lineHeight: 1 }}>{value}</Typography>
          {to && <ArrowForwardIcon sx={{ fontSize: 16, color: 'textMuted' }} />}
        </Box>
      </CardContent>
    </Card>
  );
}

export default function Dashboard() {
  const theme = useTheme();
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
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 0.5 }}>
        <Box sx={{
          width: 40, height: 40, borderRadius: 2, mr: 2,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          bgcolor: 'brand900',
        }}>
          <DashboardIcon sx={{ fontSize: 20, color: '#fff' }} />
        </Box>
        <Typography variant="h1" sx={{ fontSize: '24px', fontWeight: 700, color: 'textPrimary' }}>
          Kontrol Paneli
        </Typography>
      </Box>
      <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 3, ml: '52px' }}>
        Siparişlerinizin genel durumu ve gecikme riski tek bakışta.
      </Typography>

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
            <StatCard
              icon={<Inventory2OutlinedIcon sx={{ fontSize: 18, color: theme.palette.primary.main }} />}
              label="Toplam Sipariş"
              value={totalOrders}
              accentColor={theme.palette.primary.main}
            />
            <StatCard
              icon={<PendingActionsOutlinedIcon sx={{ fontSize: 18, color: theme.palette.warning.main }} />}
              label="Bekleyen Siparişler"
              value={pendingOrders}
              accentColor={theme.palette.warning.main}
            />
            <StatCard
              icon={<PrecisionManufacturingOutlinedIcon sx={{ fontSize: 18, color: theme.palette.info.main }} />}
              label="Üretimdekiler"
              value={inProductionOrders}
              accentColor={theme.palette.info.main}
            />
            <StatCard
              icon={<WarningAmberOutlinedIcon sx={{ fontSize: 18, color: theme.palette.error.main }} />}
              label="Gecikme Riski"
              value={delayRiskLoading ? <CircularProgress size={20} /> : `${delayedCount}${delayRiskStillCalculating ? '+' : ''}`}
              accentColor={theme.palette.error.main}
              to="/predictions/delayed"
            />
            <StatCard
              icon={<WarehouseOutlinedIcon sx={{ fontSize: 18, color: theme.palette.warning.main }} />}
              label="Stok Nedeniyle Bekleyen"
              value={stockShortfallOrders}
              accentColor={theme.palette.warning.main}
              to="/inventory"
            />
          </Box>

          <Card elevation={0} sx={{
            textAlign: 'center', py: 5, px: 4,
            backgroundImage: `linear-gradient(135deg, ${theme.palette.brand50} 0%, ${theme.palette.surfacePage} 100%)`,
            border: '1px solid', borderColor: 'divider',
          }}>
            <Typography sx={{ fontSize: '17px', fontWeight: 700, color: 'textPrimary', mb: 1 }}>
              Teslimat tahmini yapmak ister misiniz?
            </Typography>
            <Typography color="textSecondary" sx={{ mb: 3, fontSize: '13.5px' }}>
              Siparişler listenize giderek açık siparişleriniz için üretim ve teslimat süresini hesaplayabilirsiniz.
            </Typography>
            <Button
              variant="contained"
              component={RouterLink}
              to="/orders"
              startIcon={<ListAltIcon />}
              size="large"
              sx={{ px: 4, py: 1.4 }}
            >
              Siparişlere Gözat
            </Button>
          </Card>
        </>
      )}
    </Box>
  );
}
