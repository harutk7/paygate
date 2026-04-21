import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { useAuthStore } from '../stores/auth';
import { LoadingSpinner } from '@payment-gateway/ui';

export function AuthCallback() {
  const [searchParams] = useSearchParams();
  const setTokens = useAuthStore((s) => s.setTokens);
  const initialize = useAuthStore((s) => s.initialize);
  const navigate = useNavigate();

  useEffect(() => {
    const accessToken = searchParams.get('accessToken');
    const refreshToken = searchParams.get('refreshToken');

    if (accessToken && refreshToken) {
      setTokens(accessToken, refreshToken);
      // Re-initialize to fetch user/org data
      initialize().then(() => {
        navigate('/', { replace: true });
      });
    } else {
      // No tokens — redirect to landing login
      window.location.href = 'http://84.247.177.146:30210/login';
    }
  }, [searchParams, setTokens, initialize, navigate]);

  return <LoadingSpinner tip="Signing you in..." />;
}
