import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Card, CardContent, Typography, TextField, Button, Alert } from '@mui/material';
import { useAuth } from '../context/AuthContext';
import { apiClient } from '../api/client';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  
  const { login } = useAuth();
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
        // API başardı ama token dönmedi — bu sunucu tarafı bir hatadır
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
    <Box sx={{ display: 'flex', minHeight: '100vh', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.100' }}>
      <Card sx={{ maxWidth: 400, width: '100%', p: 2 }}>
        <CardContent>
          <Typography variant="h5" component="h1" gutterBottom sx={{ textAlign: 'center', fontWeight: 'bold' }}>
            System Login
          </Typography>
          <Typography variant="body2" color="textSecondary" sx={{ textAlign: 'center', mb: 3 }}>
            Enter your credentials to access the ERP Prediction System.
          </Typography>
          
          {error && (
            <Alert severity="error" sx={{ mb: 3 }}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleLogin} noValidate sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField 
              label="Username" 
              variant="outlined" 
              fullWidth 
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              disabled={loading}
              required
            />
            <TextField 
              label="Password" 
              type="password" 
              variant="outlined" 
              fullWidth 
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={loading}
              required
            />
            <Button 
              type="submit" 
              variant="contained" 
              color="primary" 
              size="large" 
              fullWidth 
              sx={{ mt: 2 }}
              disabled={loading || !username || !password}
            >
              {loading ? 'Signing in...' : 'Sign In'}
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
