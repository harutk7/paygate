import { AxiosInstance } from 'axios';
import {
  WebhookDto,
  CreateWebhookRequest,
  UpdateWebhookRequest,
  WebhookDeliveryDto,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createWebhooksApi(client: AxiosInstance) {
  return {
    getWebhooks: () =>
      client.get<WebhookDto[]>('/api/webhooks').then((r) => r.data),

    createWebhook: (data: CreateWebhookRequest) =>
      client.post<WebhookDto>('/api/webhooks', data).then((r) => r.data),

    updateWebhook: (id: string, data: UpdateWebhookRequest) =>
      client.put<WebhookDto>(`/api/webhooks/${id}`, data).then((r) => r.data),

    deleteWebhook: (id: string) =>
      client.delete(`/api/webhooks/${id}`).then((r) => r.data),

    getDeliveries: (webhookId: string, params?: PagedRequest) =>
      client
        .get<PagedResult<WebhookDeliveryDto>>(`/api/webhooks/${webhookId}/deliveries`, { params })
        .then((r) => r.data),

    testWebhook: (id: string) =>
      client.post(`/api/webhooks/${id}/test`).then((r) => r.data),
  };
}
