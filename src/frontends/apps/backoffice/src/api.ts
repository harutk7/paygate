import { createApiClient, createAdminApi, createAuthApi } from '@payment-gateway/api-client';

const tokenStorage = {
  getAccessToken: () => localStorage.getItem('bo_accessToken'),
  setAccessToken: (token: string) => localStorage.setItem('bo_accessToken', token),
  getRefreshToken: () => localStorage.getItem('bo_refreshToken'),
  setRefreshToken: (token: string) => localStorage.setItem('bo_refreshToken', token),
  clear: () => {
    localStorage.removeItem('bo_accessToken');
    localStorage.removeItem('bo_refreshToken');
  },
};

const backofficeClient = createApiClient('', tokenStorage);
const identityClient = createApiClient('', tokenStorage);

export const adminApi = createAdminApi(backofficeClient);
export const authApi = createAuthApi(identityClient);
