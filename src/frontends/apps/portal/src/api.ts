import {
  createApiClient,
  createAuthApi,
  createTransactionsApi,
  createApiKeysApi,
  createWebhooksApi,
  createBillingApi,
  createSubscriptionsApi,
  createPlansApi,
  createUsersApi,
  createOrganizationsApi,
  type TokenStorage,
} from '@payment-gateway/api-client';

const tokenStorage: TokenStorage = {
  getAccessToken: () => accessTokenMemory,
  setAccessToken: (token: string) => {
    accessTokenMemory = token;
  },
  getRefreshToken: () => localStorage.getItem('pg_refresh_token'),
  setRefreshToken: (token: string) => {
    localStorage.setItem('pg_refresh_token', token);
  },
  clear: () => {
    accessTokenMemory = null;
    localStorage.removeItem('pg_refresh_token');
  },
};

let accessTokenMemory: string | null = null;

export function setAccessTokenInMemory(token: string | null) {
  accessTokenMemory = token;
}

export function getAccessTokenFromMemory() {
  return accessTokenMemory;
}

const identityClient = createApiClient('', tokenStorage);
const billingClient = createApiClient('', tokenStorage);
const gatewayClient = createApiClient('', tokenStorage);

export const authApi = createAuthApi(identityClient);
export const usersApi = createUsersApi(identityClient);
export const organizationsApi = createOrganizationsApi(identityClient);
export const plansApi = createPlansApi(billingClient);
export const subscriptionsApi = createSubscriptionsApi(billingClient);
export const billingApi = createBillingApi(billingClient);
export const transactionsApi = createTransactionsApi(gatewayClient);
export const apiKeysApi = createApiKeysApi(gatewayClient);
export const webhooksApi = createWebhooksApi(gatewayClient);
