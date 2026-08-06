import type { ComponentType } from 'react';
import { Outlet, Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Container, Box, IconButton, Tooltip } from '@mui/material';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';
import ListAltOutlinedIcon from '@mui/icons-material/ListAltOutlined';
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined';
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined';
import { useAuth } from '../context/AuthContext';
import { useThemeMode } from '../context/ThemeModeContext';

interface NavItem {
  label: string;
  to: string;
  icon: ComponentType<{ sx?: object }>;
}

// Sıra, iş değerine göre önceliklendirilmiştir: Panel (özet) → Gecikenler (günlük
// risk takibi, en kritik ekran) → Siparişler/Teslimat Tahmini (ara sıra bakılan
// yardımcı akışlar) → Stok (referans veri, en az sık kullanılan).
const navItems: NavItem[] = [
  { label: 'Panel', to: '/', icon: DashboardOutlinedIcon },
  { label: 'Gecikenler', to: '/predictions/delayed', icon: WarningAmberOutlinedIcon },
  { label: 'Siparişler', to: '/orders', icon: ListAltOutlinedIcon },
  { label: 'Teslimat Tahmini', to: '/predictions', icon: TimelineOutlinedIcon },
  { label: 'Stok', to: '/inventory', icon: WarehouseOutlinedIcon },
];

export default function Layout() {
  const { logout } = useAuth();
  const { mode, toggleMode } = useThemeMode();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const isActive = (path: string) => {
    if (path === '/') return location.pathname === '/';
    const matches = navItems.filter((item) => item.to !== '/' && location.pathname.startsWith(item.to));
    if (matches.length === 0) return false;
    const mostSpecific = matches.reduce((a, b) => (b.to.length > a.to.length ? b : a));
    return mostSpecific.to === path;
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="sticky" sx={{ top: 0 }}>
        <Toolbar sx={{ flexWrap: { xs: 'wrap', md: 'nowrap' }, gap: 1, py: { xs: 1, md: 0 } }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, flexGrow: 1 }}>
            <Box sx={{
              width: 30, height: 30, borderRadius: 1.5,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              bgcolor: 'rgba(255,255,255,0.12)', border: '1px solid rgba(255,255,255,0.16)',
              flexShrink: 0,
            }}>
              <InsightsOutlinedIcon sx={{ fontSize: 17, color: '#fff' }} />
            </Box>
            <Typography variant="h6" component="div" sx={{ fontWeight: 700, fontSize: { xs: '0.9rem', md: '1.05rem' }, whiteSpace: 'nowrap' }}>
              ERP Delivery Prediction
            </Typography>
          </Box>
          <Box
            sx={{
              display: 'flex',
              gap: 0.5,
              overflowX: 'auto',
              maxWidth: '100%',
              scrollbarWidth: 'none',
              '&::-webkit-scrollbar': { display: 'none' },
            }}
          >
            {navItems.map((item) => (
              <Button
                key={item.to}
                color="inherit"
                component={RouterLink}
                to={item.to}
                startIcon={<item.icon sx={{ fontSize: 16 }} />}
                sx={{
                  borderRadius: 999,
                  px: 2,
                  flexShrink: 0,
                  whiteSpace: 'nowrap',
                  fontWeight: isActive(item.to) ? 700 : 500,
                  bgcolor: isActive(item.to) ? 'rgba(255,255,255,0.16)' : 'transparent',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.1)',
                  },
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
          <Tooltip title={mode === 'dark' ? 'Aydınlık moda geç' : 'Koyu moda geç'}>
            <IconButton
              onClick={toggleMode}
              size="small"
              sx={{
                ml: { xs: 0, md: 1 },
                flexShrink: 0,
                color: '#fff',
                border: '1px solid rgba(255,255,255,0.2)',
                bgcolor: 'rgba(255,255,255,0.06)',
                '&:hover': { bgcolor: 'rgba(255,255,255,0.14)' },
              }}
            >
              {mode === 'dark' ? <LightModeOutlinedIcon sx={{ fontSize: 18 }} /> : <DarkModeOutlinedIcon sx={{ fontSize: 18 }} />}
            </IconButton>
          </Tooltip>
          <Button
            color="inherit"
            onClick={handleLogout}
            startIcon={<LogoutOutlinedIcon sx={{ fontSize: 16 }} />}
            sx={{ ml: { xs: 0, md: 1.5 }, flexShrink: 0, borderRadius: 999, border: '1px solid rgba(255,255,255,0.35)' }}
          >
            Çıkış
          </Button>
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flexGrow: 1, py: 4, display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', bgcolor: 'background.paper', borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="body2" color="textSecondary" sx={{ fontSize: '12.5px' }}>
          © {new Date().getFullYear()} ERP Delivery Prediction System
        </Typography>
      </Box>
    </Box>
  );
}
