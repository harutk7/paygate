import { useEffect } from 'react';
import { Outlet } from 'react-router';
import { useAuthStore } from '../stores/auth';
import { LoadingSpinner } from '@payment-gateway/ui';

export function ProtectedRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isInitialized = useAuthStore((s) => s.isInitialized);
  const initialize = useAuthStore((s) => s.initialize);

  useEffect(() => {
    if (!isInitialized) {
      initialize();
    }
  }, [isInitialized, initialize]);

  if (!isInitialized) {
    return <LoadingSpinner tip="Initializing..." />;
  }

  if (!isAuthenticated) {
    window.location.href = 'http://84.247.177.146:30210/login';
    return null;
  }

  return <Outlet />;
}
