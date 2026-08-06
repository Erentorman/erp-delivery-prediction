import { Outlet, Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Button, Container, Box } from '@mui/material';
import { useAuth } from '../context/AuthContext';

interface NavItem {
  label: string;
  to: string;
}

const navItems: NavItem[] = [
  { label: 'Panel', to: '/' },
  { label: 'Siparişler', to: '/orders' },
  { label: 'Teslimat Tahmini', to: '/predictions' },
  { label: 'Gecikenler', to: '/predictions/delayed' },
  { label: 'Stok', to: '/inventory' },
];

export default function Layout() {
  const { logout } = useAuth();
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
      <AppBar position="static" color="primary" elevation={0}>
        <Toolbar sx={{ flexWrap: { xs: 'wrap', md: 'nowrap' }, gap: 1, py: { xs: 1, md: 0 } }}>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1, fontWeight: 'bold', fontSize: { xs: '1rem', md: '1.25rem' } }}>
            ERP Delivery Prediction
          </Typography>
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
                sx={{
                  borderRadius: 1,
                  px: 1.5,
                  flexShrink: 0,
                  whiteSpace: 'nowrap',
                  bgcolor: isActive(item.to) ? 'rgba(255,255,255,0.15)' : 'transparent',
                  borderBottom: isActive(item.to) ? '2px solid white' : '2px solid transparent',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.1)',
                  },
                }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
          <Button
            color="inherit"
            onClick={handleLogout}
            sx={{ ml: { xs: 0, md: 2 }, flexShrink: 0, border: '1px solid rgba(255,255,255,0.5)' }}
          >
            Logout
          </Button>
        </Toolbar>
      </AppBar>

      <Container component="main" sx={{ flexGrow: 1, py: 4, display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', bgcolor: 'background.paper', borderTop: '1px solid', borderColor: 'divider' }}>
        <Typography variant="body2" color="textSecondary">
          © {new Date().getFullYear()} ERP Delivery Prediction System. AI-Powered.
        </Typography>
      </Box>
    </Box>
  );
}

