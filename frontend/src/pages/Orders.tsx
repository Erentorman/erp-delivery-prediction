import { Typography, Box, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow } from '@mui/material';

const dummyOrders = [
  { id: 'ORD-001', product: 'Widget A', quantity: 100, date: '2026-07-29' },
  { id: 'ORD-002', product: 'Widget B', quantity: 50, date: '2026-07-30' },
];

export default function Orders() {
  return (
    <Box>
      <Typography variant="h4" component="h1" gutterBottom>
        Orders (Read Only)
      </Typography>
      <TableContainer component={Paper} sx={{ mt: 3, boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}>
        <Table>
          <TableHead sx={{ bgcolor: 'grey.100' }}>
            <TableRow>
              <TableCell>Order ID</TableCell>
              <TableCell>Product</TableCell>
              <TableCell>Quantity</TableCell>
              <TableCell>Date</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {dummyOrders.map((row) => (
              <TableRow key={row.id}>
                <TableCell>{row.id}</TableCell>
                <TableCell>{row.product}</TableCell>
                <TableCell>{row.quantity}</TableCell>
                <TableCell>{row.date}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
