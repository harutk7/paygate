import { AxiosInstance } from 'axios';
import {
  UserDto,
  UpdateProfileRequest,
  InviteUserRequest,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createUsersApi(client: AxiosInstance) {
  return {
    getMe: () =>
      client.get<UserDto>('/api/users/me').then((r) => r.data),

    updateProfile: (data: UpdateProfileRequest) =>
      client.put<UserDto>('/api/users/me', data).then((r) => r.data),

    getUsers: (params?: PagedRequest) =>
      client.get<PagedResult<UserDto>>('/api/users', { params }).then((r) => r.data),

    inviteUser: (data: InviteUserRequest) =>
      client.post('/api/users/invite', data).then((r) => r.data),
  };
}
