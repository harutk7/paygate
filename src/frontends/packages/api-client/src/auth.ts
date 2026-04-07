import { AxiosInstance } from 'axios';
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RefreshTokenRequest,
  RefreshTokenResponse,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from '@payment-gateway/types';

export function createAuthApi(client: AxiosInstance) {
  return {
    login: (data: LoginRequest) =>
      client.post<LoginResponse>('/api/auth/login', data).then((r) => r.data),

    register: (data: RegisterRequest) =>
      client.post<LoginResponse>('/api/auth/register', data).then((r) => r.data),

    logout: (data?: { refreshToken: string }) =>
      client.post('/api/auth/logout', data).then((r) => r.data),

    refresh: (data: RefreshTokenRequest) =>
      client.post<RefreshTokenResponse>('/api/auth/refresh', data).then((r) => r.data),

    forgotPassword: (data: ForgotPasswordRequest) =>
      client.post('/api/auth/forgot-password', data).then((r) => r.data),

    resetPassword: (data: ResetPasswordRequest) =>
      client.post('/api/auth/reset-password', data).then((r) => r.data),
  };
}
