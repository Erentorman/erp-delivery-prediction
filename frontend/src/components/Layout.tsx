import { Outlet, Link } from 'react-router-dom';

export default function Layout() {
  return (
    <div style={{ display: 'flex', minHeight: '100vh', flexDirection: 'column' }}>
      <header style={{ padding: '1rem', background: '#eee', borderBottom: '1px solid #ccc' }}>
        <nav style={{ display: 'flex', gap: '1rem' }}>
          <Link to="/">Dashboard</Link>
          <Link to="/orders">Orders</Link>
          <Link to="/predictions">Predictions</Link>
          <Link to="/login" style={{ marginLeft: 'auto' }}>Login</Link>
        </nav>
      </header>
      <main style={{ flex: 1, padding: '2rem' }}>
        <Outlet />
      </main>
      <footer style={{ padding: '1rem', background: '#eee', textAlign: 'center' }}>
        ERP Delivery Prediction System
      </footer>
    </div>
  );
}
