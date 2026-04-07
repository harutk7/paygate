import { Routes, Route, Navigate } from 'react-router';
import { ProtectedRoute } from './components/ProtectedRoute';
import { PortalLayout } from './layouts/PortalLayout';
import { Dashboard } from './pages/Dashboard';
import { Transactions } from './pages/Transactions';
import { ApiKeys } from './pages/ApiKeys';
import { Webhooks } from './pages/Webhooks';
import { Billing } from './pages/Billing';
import { Settings } from './pages/Settings';
import { AuthCallback } from './pages/AuthCallback';

export function App() {
  return (
    <Routes>
      <Route path="auth/callback" element={<AuthCallback />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<PortalLayout />}>
          <Route index element={<Dashboard />} />
          <Route path="transactions" element={<Transactions />} />
          <Route path="api-keys" element={<ApiKeys />} />
          <Route path="webhooks" element={<Webhooks />} />
          <Route path="billing" element={<Billing />} />
          <Route path="settings" element={<Settings />} />
        </Route>
      </Route>
      <Route path="login" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
