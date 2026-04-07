import { AxiosInstance } from 'axios';
import {
  SubscriptionDto,
  CreateSubscriptionRequest,
  CancelSubscriptionRequest,
} from '@payment-gateway/types';

export function createSubscriptionsApi(client: AxiosInstance) {
  return {
    getCurrentSubscription: () =>
      client.get<SubscriptionDto>('/api/subscriptions/current').then((r) => r.data),

    createSubscription: (data: CreateSubscriptionRequest) =>
      client.post<SubscriptionDto>('/api/subscriptions', data).then((r) => r.data),

    cancelSubscription: (data: CancelSubscriptionRequest) =>
      client.put('/api/subscriptions/cancel', data).then((r) => r.data),
  };
}
