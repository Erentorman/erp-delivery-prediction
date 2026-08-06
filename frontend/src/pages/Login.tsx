import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Card, CardContent, Typography, TextField, Button, Alert,
  InputAdornment, CircularProgress, IconButton, Tooltip
} from '@mui/material';
import PersonOutlineIcon from '@mui/icons-material/PersonOutlineOutlined';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined';
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined';
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined';
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined';
import { useAuth } from '../context/AuthContext';
import { useThemeMode } from '../context/ThemeModeContext';
import { apiClient } from '../api/client';

const VALUE_PROPS = [
  'Kural tabanlı + kritik yol (CPM) ile açıklanabilir teslim tarihi',
  'Gecikme riskini ve nedenlerini önceden gösterir',
  'Salt-okunur ERP entegrasyonu, tek komutla ayağa kalkar',
];

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const { mode, toggleMode } = useThemeMode();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const response = await apiClient.post('/api/auth/login', { username, password });

      if (response.data?.token) {
        login(response.data.token);
        navigate('/');
      } else {
        setError('Sunucudan geçerli bir oturum tokeni alınamadı. Lütfen tekrar deneyin.');
      }
    } catch (err: any) {
      console.error('Login error:', err);
      if (err.response?.status === 401) {
        setError('Geçersiz kullanıcı adı veya şifre.');
      } else {
        setError('Giriş yapılırken bir hata oluştu. Lütfen tekrar deneyin.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{
      display: 'flex',
      minHeight: '100vh',
      bgcolor: 'brand900',
      position: 'relative',
    }}>
      <Tooltip title={mode === 'dark' ? 'Aydınlık moda geç' : 'Koyu moda geç'}>
        <IconButton
          onClick={toggleMode}
          size="small"
          sx={{
            position: 'absolute', top: 20, right: 20, zIndex: 2,
            color: mode === 'dark' ? '#fff' : 'textSecondary',
            border: '1px solid',
            borderColor: mode === 'dark' ? 'rgba(255,255,255,0.2)' : 'borderDefault',
            bgcolor: mode === 'dark' ? 'rgba(255,255,255,0.06)' : 'surfaceCard',
          }}
        >
          {mode === 'dark' ? <LightModeOutlinedIcon sx={{ fontSize: 18 }} /> : <DarkModeOutlinedIcon sx={{ fontSize: 18 }} />}
        </IconButton>
      </Tooltip>

      {/* Brand / value proposition panel */}
      <Box sx={{
        display: { xs: 'none', md: 'flex' },
        flexDirection: 'column',
        justifyContent: 'center',
        flex: '0 0 46%',
        px: 8,
        py: 6,
        color: '#fff',
        position: 'relative',
        overflow: 'hidden',
        backgroundImage: 'linear-gradient(160deg, #0f2942 0%, #16324f 55%, #1a3a5c 100%)',
      }}>
        {/* Decorative background accents — pure CSS, no assets */}
        <Box sx={{
          position: 'absolute', top: -80, right: -80, width: 280, height: 280,
          borderRadius: '50%', bgcolor: 'rgba(37,99,235,0.18)', filter: 'blur(10px)',
        }} />
        <Box sx={{
          position: 'absolute', bottom: -100, left: -60, width: 240, height: 240,
          borderRadius: '50%', bgcolor: 'rgba(255,255,255,0.05)',
        }} />

        <Box sx={{ position: 'relative', zIndex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 5 }}>
            <Box sx={{
              width: 40, height: 40, borderRadius: 2, bgcolor: 'rgba(255,255,255,0.12)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              border: '1px solid rgba(255,255,255,0.16)',
            }}>
              <InsightsOutlinedIcon sx={{ fontSize: 22, color: '#fff' }} />
            </Box>
            <Typography sx={{ fontWeight: 700, fontSize: '15px', letterSpacing: '0.02em' }}>
              ERP Delivery Prediction
            </Typography>
          </Box>

          <Typography sx={{ fontSize: { md: '30px', lg: '34px' }, fontWeight: 700, lineHeight: 1.25, mb: 2.5, maxWidth: 440 }}>
            Teslim tarihini tahmin etmenin ötesinde, <Box component="span" sx={{ color: '#7fb2ff' }}>nedenini de gösterir.</Box>
          </Typography>

          <Typography sx={{ fontSize: '14.5px', color: 'rgba(255,255,255,0.72)', lineHeight: 1.7, maxWidth: 420, mb: 5 }}>
            ERP verilerinizi kullanarak üretim, sevkiyat ve teslim tarihlerini kural tabanlı motor ve kritik yol analiziyle hesaplar; gecikme riskini erkenden görünür kılar.
          </Typography>

          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {VALUE_PROPS.map((text) => (
              <Box key={text} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.5 }}>
                <CheckCircleOutlineIcon sx={{ fontSize: 19, color: '#7fb2ff', mt: '1px', flexShrink: 0 }} />
                <Typography sx={{ fontSize: '13.5px', color: 'rgba(255,255,255,0.82)', lineHeight: 1.6 }}>
                  {text}
                </Typography>
              </Box>
            ))}
          </Box>
        </Box>

        <Typography sx={{ position: 'relative', zIndex: 1, fontSize: '12px', color: 'rgba(255,255,255,0.4)', mt: 6 }}>
          © {new Date().getFullYear()} ERP Delivery Prediction System
        </Typography>
      </Box>

      {/* Login form panel */}
      <Box sx={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'surfacePage',
        px: 3,
      }}>
        <Card elevation={0} sx={{ maxWidth: 400, width: '100%', border: 'none', boxShadow: 'none', bgcolor: 'transparent' }}>
          <CardContent sx={{ p: { xs: 2, sm: 0 } }}>
            <Typography component="h1" sx={{ fontSize: '22px', fontWeight: 700, color: 'textPrimary', mb: 0.75 }}>
              Tekrar hoş geldiniz
            </Typography>
            <Typography sx={{ fontSize: '13.5px', color: 'textSecondary', mb: 4 }}>
              Devam etmek için hesabınıza giriş yapın.
            </Typography>

            {error && (
              <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
                {error}
              </Alert>
            )}

            <Box component="form" onSubmit={handleLogin} noValidate sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
              <TextField
                label="Kullanıcı Adı"
                variant="outlined"
                fullWidth
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                disabled={loading}
                required
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <PersonOutlineIcon sx={{ fontSize: 19, color: 'textMuted' }} />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <TextField
                label="Şifre"
                type="password"
                variant="outlined"
                fullWidth
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={loading}
                required
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <LockOutlinedIcon sx={{ fontSize: 19, color: 'textMuted' }} />
                      </InputAdornment>
                    ),
                  },
                }}
              />
              <Button
                type="submit"
                variant="contained"
                color="primary"
                size="large"
                fullWidth
                sx={{ mt: 1, py: 1.4, fontSize: '14px' }}
                disabled={loading || !username || !password}
              >
                {loading ? <CircularProgress size={18} sx={{ color: 'inherit' }} /> : 'Giriş Yap'}
              </Button>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
}
