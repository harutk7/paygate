import { create } from 'zustand';
import type { UserDto, LoginResponse } from '@payment-gateway/types';
import { authApi } from '../api';
import axios from 'axios';

interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  initialize: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: localStorage.getItem('bo_accessToken'),
  isAuthenticated: !!localStorage.getItem('bo_accessToken'),

  login: async (email: string, password: string) => {
    const data = await authApi.login({ email, password });

    if (data.user.role !== 'PlatformAdmin') {
      throw new Error('Access denied. Only platform administrators can access the backoffice.');
    }

    localStorage.setItem('bo_accessToken', data.accessToken);
    localStorage.setItem('bo_refreshToken', data.refreshToken);
    set({
      user: data.user,
      accessToken: data.accessToken,
      isAuthenticated: true,
    });
  },

  logout: () => {
    localStorage.removeItem('bo_accessToken');
    localStorage.removeItem('bo_refreshToken');
    set({ user: null, accessToken: null, isAuthenticated: false });
  },

  initialize: async () => {
    const token = localStorage.getItem('bo_accessToken');
    if (!token) {
      set({ isAuthenticated: false });
      return;
    }
    try {
      const { data } = await axios.get<UserDto>('/api/users/me', {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (data.role !== 'PlatformAdmin') {
        get().logout();
        return;
      }
      set({ user: data, accessToken: token, isAuthenticated: true });
    } catch {
      get().logout();
    }
  },
}));
