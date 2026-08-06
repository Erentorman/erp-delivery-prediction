import { useState } from 'react';
import { 
  Typography, Box, Card, CardContent, Button, Chip,
  Divider, CircularProgress, Alert, TextField, MenuItem
} from '@mui/material';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing';
import { usePrediction } from '../hooks/usePrediction';

const MOCK_PRODUCTS = [
  { id: 'P001', name: 'Masa', category: 'Mobilya', icon: <PrecisionManufacturingIcon fontSize="large" /> },
  { id: 'P002', name: 'Sandalye', category: 'Mobilya', icon: <PrecisionManufacturingIcon fontSize="large" /> },
  { id: 'P003', name: 'Dolap', category: 'Mobilya', icon: <PrecisionManufacturingIcon fontSize="large" /> },
  { id: 'P004', name: 'Kapı', category: 'Ahşap', icon: <PrecisionManufacturingIcon fontSize="large" /> },
];

const LOCATIONS = ['İstanbul', 'Ankara', 'İzmir', 'Antalya'];

export default function CustomerSimulationView() {
  const { isLoading, error, customerResult, fetchSimulation, resetState } = usePrediction();
  
  const [selectedProductId, setSelectedProductId] = useState<string>('');
  const [quantity, setQuantity] = useState<number>(1);
  const [location, setLocation] = useState<string>('İstanbul');

  const handleSimulate = () => {
    if (!selectedProductId) return;
    fetchSimulation(selectedProductId, location, quantity);
  };

  if (customerResult) {
    const product = MOCK_PRODUCTS.find(p => p.id === selectedProductId);
    return (
      <Box sx={{ animation: 'fadeIn 0.5s ease-in' }}>
        <Box sx={{ mb: 3 }}>
          <Button variant="outlined" onClick={resetState} sx={{ fontWeight: 'bold' }}>
            &larr; Yeni Tahmin Al
          </Button>
        </Box>

        <Card sx={{ overflow: 'visible', border: '1px solid', borderColor: 'divider' }}>
          <CardContent sx={{ p: 0 }}>
            <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' } }}>
              <Box sx={{ p: 4, flex: 1, bgcolor: 'background.default' }}>
                <Chip icon={<CheckCircleIcon />} label="Üretim ve Teslimat Planlanabilir" color="success" sx={{ mb: 3 }} />
                
                <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 1 }}>Sipariş No</Typography>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 3 }}>{customerResult.orderReference}</Typography>

                <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 1 }}>Seçilen Ürün</Typography>
                <Typography variant="h6" sx={{ mb: 3 }}>{product?.name}</Typography>

                <Typography variant="body2" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 1 }}>Miktar / Lokasyon</Typography>
                <Typography variant="h6" sx={{ mb: 3 }}>{quantity} Adet - {location}</Typography>
              </Box>

              <Box sx={{ p: 4, flex: 1, bgcolor: 'primary.main', color: 'primary.contrastText', display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, gap: 1 }}>
                  <LocalShippingIcon fontSize="large" />
                  <Typography variant="h6" sx={{ opacity: 0.9 }}>Tahmini Teslimat Tarihi</Typography>
                </Box>
                <Typography variant="h3" sx={{ fontWeight: 800 }}>
                  {new Date(customerResult.finalDeliveryDate).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })}
                </Typography>
                <Typography variant="h5" sx={{ opacity: 0.8, mt: 1 }}>
                  {new Date(customerResult.finalDeliveryDate).toLocaleDateString('tr-TR', { year: 'numeric', weekday: 'long' })}
                </Typography>
              </Box>
            </Box>
          </CardContent>
        </Card>
      </Box>
    );
  }

  return (
    <Box sx={{ animation: 'fadeIn 0.5s ease-in' }}>
      <Typography variant="h6" sx={{ mb: 1, color: 'text.secondary' }}>
        Sipariş detaylarını girerek teslimat tarihi tahmini alın.
      </Typography>
      <Divider sx={{ mb: 4 }} />
      
      {error && (
        <Alert severity="error" variant="filled" sx={{ mb: 4 }}>
          {error}
        </Alert>
      )}

      {isLoading ? (
        <Card sx={{ p: 10, textAlign: 'center' }}>
          <CircularProgress size={60} sx={{ mb: 3 }} />
          <Typography variant="h6" color="text.secondary">Tedarik zinciri hesaplanıyor...</Typography>
        </Card>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '2fr 1fr' }, gap: 4 }}>
          <Box>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 2 }}>
              {MOCK_PRODUCTS.map((product) => (
                <Card 
                  key={product.id}
                  sx={{ 
                    cursor: 'pointer', 
                    transition: 'all 0.2s',
                    border: '2px solid',
                    borderColor: selectedProductId === product.id ? 'primary.main' : 'divider',
                    bgcolor: selectedProductId === product.id ? 'action.selected' : 'background.paper'
                  }}
                  onClick={() => setSelectedProductId(product.id)}
                >
                  <CardContent sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', py: 3 }}>
                    <Box sx={{ p: 1, borderRadius: '50%', color: 'primary.main', mb: 1 }}>
                      {product.icon}
                    </Box>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>{product.name}</Typography>
                  </CardContent>
                </Card>
              ))}
            </Box>
          </Box>

          <Box>
            <Card sx={{ p: 2 }}>
              <CardContent>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 2 }}>Sipariş Detayları</Typography>
                
                <TextField
                  fullWidth
                  type="number"
                  label="Miktar"
                  value={quantity}
                  onChange={(e) => setQuantity(Number(e.target.value))}
                  slotProps={{ htmlInput: { min: 1 } }}
                  sx={{ mb: 3 }}
                />

                <TextField
                  select
                  fullWidth
                  label="Teslimat Lokasyonu"
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  sx={{ mb: 4 }}
                >
                  {LOCATIONS.map((loc) => (
                    <MenuItem key={loc} value={loc}>{loc}</MenuItem>
                  ))}
                </TextField>

                <Button 
                  fullWidth 
                  variant="contained" 
                  size="large"
                  disabled={!selectedProductId}
                  onClick={handleSimulate}
                  sx={{ fontWeight: 'bold', py: 1.5 }}
                >
                  TAHMİN AL
                </Button>
              </CardContent>
            </Card>
          </Box>
        </Box>
      )}
    </Box>
  );
}
