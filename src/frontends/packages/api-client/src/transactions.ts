import { AxiosInstance } from 'axios';
import {
  TransactionDto,
  TransactionDetailDto,
  TransactionStatsDto,
  CreateChargeRequest,
  CreateChargeResponse,
  RefundChargeRequest,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createTransactionsApi(client: AxiosInstance) {
  return {
    getTransactions: (params?: PagedRequest) =>
      client.get<PagedResult<TransactionDto>>('/api/transactions', { params }).then((r) => r.data),

    getTransaction: (id: string) =>
      client.get<TransactionDetailDto>(`/api/transactions/${id}`).then((r) => r.data),

    getTransactionStats: () =>
      client.get<TransactionStatsDto>('/api/transactions/stats').then((r) => r.data),

    createCharge: (data: CreateChargeRequest) =>
      client.post<CreateChargeResponse>('/api/transactions/charge', data).then((r) => r.data),

    refundCharge: (id: string, data?: RefundChargeRequest) =>
      client.post(`/api/transactions/${id}/refund`, data).then((r) => r.data),
  };
}
