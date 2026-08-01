import { Typography, Box, Card, CardContent, Chip } from '@mui/material';

export default function Predictions() {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Predictions
      </Typography>
      <Typography color="textSecondary" sx={{ mb: 4 }}>
        Hybrid Prediction Results (Rule-Based + AI)
      </Typography>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' }, gap: 3 }}>
        <Box>
          <Card variant="outlined" sx={{ borderColor: 'primary.main', borderWidth: 2 }}>
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="h6">Final Hybrid</Typography>
                <Chip label="Calculated" color="success" size="small" />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Combination of Rule-Based and AI.
              </Typography>
              <Typography variant="h3" sx={{ mt: 2 }}>
                3.5 Days
              </Typography>
            </CardContent>
          </Card>
        </Box>

        <Box>
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="h6">Rule-Based</Typography>
                <Chip label="Success" color="primary" size="small" />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Deterministic CPM Pipeline
              </Typography>
              <Typography variant="h4" sx={{ mt: 2 }}>
                4 Days
              </Typography>
            </CardContent>
          </Card>
        </Box>

        <Box>
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
                <Typography variant="h6">AI Model</Typography>
                <Chip label="Success" color="secondary" size="small" />
              </Box>
              <Typography variant="body2" color="textSecondary">
                Baseline Regressor
              </Typography>
              <Typography variant="h4" sx={{ mt: 2 }}>
                2.8 Days
              </Typography>
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
}
