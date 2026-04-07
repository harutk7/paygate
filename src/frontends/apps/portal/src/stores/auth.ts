import { create } from 'zustand';
import type { UserDto, OrganizationDto } from '@payment-gateway/types';
import {
  authApi,
  usersApi,
  organizationsApi,
  setAccessTokenInMemory,
  getAccessTokenFromMemory,
} from '../api';

interface AuthState {
  user: UserDto | null;
  organization: OrganizationDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isInitialized: boolean;

  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  setTokens: (accessToken: string, refreshToken: string) => void;
  initialize: () => Promise<void>;
  setUser: (user: UserDto) => void;
  setOrganization: (org: OrganizationDto) => void;
}

// Deduplicates concurrent initialize() calls (e.g. React StrictMode double-fire)
let _initPromise: Promise<void> | null = null;

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  organization: null,
  accessToken: null,
  isAuthenticated: !!localStorage.getItem('pg_refresh_token'),
  isInitialized: false,

  login: async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    setAccessTokenInMemory(response.accessToken);
    localStorage.setItem('pg_refresh_token', response.refreshToken);
    set({
      user: response.user,
      accessToken: response.accessToken,
      isAuthenticated: true,
    });
    try {
      const org = await organizationsApi.getMyOrg();
      set({ organization: org });
    } catch {
      // org fetch is optional
    }
  },

  logout: async () => {
    const refreshToken = localStorage.getItem('pg_refresh_token');
    try {
      if (refreshToken) {
        await authApi.logout({ refreshToken });
      }
    } catch {
      // ignore logout API errors
    }
    setAccessTokenInMemory(null);
    localStorage.removeItem('pg_refresh_token');
    set({
      user: null,
      organization: null,
      accessToken: null,
      isAuthenticated: false,
    });
  },

  setTokens: (accessToken: string, refreshToken: string) => {
    setAccessTokenInMemory(accessToken);
    localStorage.setItem('pg_refresh_token', refreshToken);
    set({ accessToken, isAuthenticated: true });
  },

  initialize: async () => {
    // Return existing promise if already initializing (prevents StrictMode race)
    if (_initPromise) return _initPromise;

    _initPromise = (async () => {
      const existingAccessToken = getAccessTokenFromMemory();
      const refreshToken = localStorage.getItem('pg_refresh_token');

      if (!refreshToken && !existingAccessToken) {
        set({ isInitialized: true, isAuthenticated: false });
        return;
      }

      try {
        // If we already have an access token (set by setTokens/AuthCallback),
        // skip the refresh and go straight to fetching user data
        if (!existingAccessToken) {
          const tokenResponse = await authApi.refresh({ refreshToken: refreshToken! });
          setAccessTokenInMemory(tokenResponse.accessToken);
          localStorage.setItem('pg_refresh_token', tokenResponse.refreshToken);
          set({ accessToken: tokenResponse.accessToken, isAuthenticated: true });
        }

        const [user, org] = await Promise.all([
          usersApi.getMe(),
          organizationsApi.getMyOrg(),
        ]);
        set({ user, organization: org, isInitialized: true });
      } catch {
        setAccessTokenInMemory(null);
        localStorage.removeItem('pg_refresh_token');
        set({
          isAuthenticated: false,
          accessToken: null,
          user: null,
          organization: null,
          isInitialized: true,
        });
      }
    })();

    try {
      await _initPromise;
    } finally {
      _initPromise = null;
    }
  },

  setUser: (user) => set({ user }),
  setOrganization: (organization) => set({ organization }),
}));
