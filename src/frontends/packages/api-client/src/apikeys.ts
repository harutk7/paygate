import { AxiosInstance } from 'axios';
import {
  ApiKeyDto,
  CreateApiKeyRequest,
  CreateApiKeyResponse,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createApiKeysApi(client: AxiosInstance) {
  return {
    getApiKeys: (params?: PagedRequest) =>
      client.get<PagedResult<ApiKeyDto>>('/api/apikeys', { params }).then((r) => r.data),

    createApiKey: (data: CreateApiKeyRequest) =>
      client.post<CreateApiKeyResponse>('/api/apikeys', data).then((r) => r.data),

    revokeApiKey: (id: string) =>
      client.delete(`/api/apikeys/${id}`).then((r) => r.data),

    rotateApiKey: (id: string) =>
      client.post<CreateApiKeyResponse>(`/api/apikeys/${id}/rotate`).then((r) => r.data),
  };
}
