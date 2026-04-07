import { AxiosInstance } from 'axios';
import { PlanDto } from '@payment-gateway/types';

export function createPlansApi(client: AxiosInstance) {
  return {
    getPlans: () =>
      client.get<PlanDto[]>('/api/plans').then((r) => r.data),

    getPlan: (id: string) =>
      client.get<PlanDto>(`/api/plans/${id}`).then((r) => r.data),
  };
}
