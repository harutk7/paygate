import { AxiosInstance } from 'axios';
import {
  PaymentMethodDto,
  AddPaymentMethodRequest,
  PaymentDto,
  InvoiceDto,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createBillingApi(client: AxiosInstance) {
  return {
    addPaymentMethod: (data: AddPaymentMethodRequest) =>
      client.post<PaymentMethodDto>('/api/billing/add-payment-method', data).then((r) => r.data),

    getPaymentMethods: () =>
      client.get<PaymentMethodDto[]>('/api/billing/payment-methods').then((r) => r.data),

    removePaymentMethod: (id: string) =>
      client.delete(`/api/billing/payment-methods/${id}`).then((r) => r.data),

    getPayments: (params?: PagedRequest) =>
      client.get<PagedResult<PaymentDto>>('/api/billing/payments', { params }).then((r) => r.data),

    getInvoices: (params?: PagedRequest) =>
      client.get<PagedResult<InvoiceDto>>('/api/billing/invoices', { params }).then((r) => r.data),
  };
}
