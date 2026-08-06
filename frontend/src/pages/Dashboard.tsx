import { useEffect, useState, type ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Typography, Box, Card, CardContent, Button,
  Alert, AlertTitle, CircularProgress, useTheme,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Link
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
import { useOpenOrderDelayRisk, getRequestedDeliveryDate } from '../features/prediction/useOpenOrderDelayRisk';
import { formatUserFriendlyDate } from '../features/prediction/predictionHelpers';
import { RiskGauge } from '../components/RiskGauge';

interface StatCardProps {
  icon: ReactNode;
  label: string;
  value: ReactNode;
  accentColor: string;
  to?: string;
  extra?: ReactNode;
}

function StatCard({ icon, label, value, accentColor, to, extra }: StatCardProps) {
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
      <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 1.5, minHeight: 148 }}>
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
        {extra && <Box sx={{ mt: 'auto', pt: 0.5 }}>{extra}</Box>}
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
  const openOrdersCount = pendingOrders + inProductionOrders;
  const delayRatio = openOrdersCount > 0 ? Math.round((delayedCount / openOrdersCount) * 100) : 0;
  const topRiskyOrders = [...delayRiskRows]
    .filter((r) => r.status === 'delayed')
    .sort((a, b) => (b.delayDays ?? 0) - (a.delayDays ?? 0))
    .slice(0, 5);

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
              extra={!delayRiskLoading && openOrdersCount > 0 && (
                <RiskGauge value={delayRatio} caption={`${delayedCount} / ${openOrdersCount} açık sipariş`} size={80} />
              )}
            />
            <StatCard
              icon={<WarehouseOutlinedIcon sx={{ fontSize: 18, color: theme.palette.warning.main }} />}
              label="Stok Nedeniyle Bekleyen"
              value={stockShortfallOrders}
              accentColor={theme.palette.warning.main}
              to="/inventory"
            />
          </Box>

          {topRiskyOrders.length > 0 && (
            <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', mb: 4, overflow: 'hidden' }}>
              <Box sx={{ px: '20px', pt: '16px', pb: '10px', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Typography sx={{ fontSize: '13px', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600, color: 'textPrimary' }}>
                  En Riskli Siparişler
                </Typography>
                <Link component={RouterLink} to="/predictions/delayed" underline="hover" sx={{ fontSize: '12.5px', color: 'interactiveBlue', fontWeight: 600, display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  Tümünü gör <ArrowForwardIcon sx={{ fontSize: 14 }} />
                </Link>
              </Box>
              <TableContainer component={Paper} elevation={0} sx={{ border: 'none' }}>
                <Table size="small">
                  <TableHead sx={{ bgcolor: 'surfaceSubtle' }}>
                    <TableRow>
                      <TableCell sx={{ fontWeight: 'bold' }}>Sipariş</TableCell>
                      <TableCell sx={{ fontWeight: 'bold' }}>Ürün</TableCell>
                      <TableCell sx={{ fontWeight: 'bold' }}>İstenen Teslim</TableCell>
                      <TableCell sx={{ fontWeight: 'bold', textAlign: 'right' }}>Gecikme</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {topRiskyOrders.map((row) => (
                      <TableRow key={row.order.orderReference} hover>
                        <TableCell>
                          <Link component={RouterLink} to={`/orders/${encodeURIComponent(row.order.orderReference)}`} underline="hover" sx={{ color: 'interactiveBlue', fontWeight: 600 }}>
                            {row.order.orderReference}
                          </Link>
                        </TableCell>
                        <TableCell>{row.order.productSummary}</TableCell>
                        <TableCell>{formatUserFriendlyDate(getRequestedDeliveryDate(row.order))}</TableCell>
                        <TableCell sx={{ textAlign: 'right' }}>
                          <Typography component="span" sx={{ fontSize: '12.5px', fontWeight: 700, color: 'error.main' }}>
                            {row.delayDays} gün
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          )}

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
