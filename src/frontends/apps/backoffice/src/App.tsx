import { Routes, Route } from 'react-router';
import { ProtectedRoute } from './components/ProtectedRoute';
import { BackofficeLayout } from './layouts/BackofficeLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Customers } from './pages/Customers';
import { CustomerDetail } from './pages/CustomerDetail';
import { Plans } from './pages/Plans';
import { Transactions } from './pages/Transactions';
import { Revenue } from './pages/Revenue';
import { AuditLog } from './pages/AuditLog';

export function App() {
  return (
    <Routes>
      <Route path="login" element={<Login />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<BackofficeLayout />}>
          <Route index element={<Dashboard />} />
          <Route path="customers" element={<Customers />} />
          <Route path="customers/:id" element={<CustomerDetail />} />
          <Route path="plans" element={<Plans />} />
          <Route path="transactions" element={<Transactions />} />
          <Route path="revenue" element={<Revenue />} />
          <Route path="audit-log" element={<AuditLog />} />
        </Route>
      </Route>
    </Routes>
  );
}
