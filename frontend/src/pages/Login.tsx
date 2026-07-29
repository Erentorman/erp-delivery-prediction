import { Box, Card, CardContent, Typography, TextField, Button } from '@mui/material';

export default function Login() {
  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.100' }}>
      <Card sx={{ maxWidth: 400, width: '100%', p: 2 }}>
        <CardContent>
          <Typography variant="h5" component="h1" gutterBottom textAlign="center" fontWeight="bold">
            System Login
          </Typography>
          <Typography variant="body2" color="textSecondary" textAlign="center" sx={{ mb: 4 }}>
            Enter your credentials to access the ERP Prediction System.
          </Typography>
          <Box component="form" noValidate sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField label="Email Address" variant="outlined" fullWidth />
            <TextField label="Password" type="password" variant="outlined" fullWidth />
            <Button variant="contained" color="primary" size="large" fullWidth sx={{ mt: 2 }}>
              Sign In
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
