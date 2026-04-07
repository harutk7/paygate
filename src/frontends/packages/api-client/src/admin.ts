import { AxiosInstance } from 'axios';
import {
  DashboardDto,
  CustomerDto,
  UpdateCustomerStatusRequest,
  PlanDto,
  CreatePlanRequest,
  UpdatePlanRequest,
  TransactionDto,
  RevenueReportDto,
  AuditLogEntryDto,
  PagedResult,
  PagedRequest,
} from '@payment-gateway/types';

export function createAdminApi(client: AxiosInstance) {
  return {
    getDashboard: () =>
      client.get<DashboardDto>('/api/admin/dashboard').then((r) => r.data),

    getCustomers: (params?: PagedRequest) =>
      client.get<PagedResult<CustomerDto>>('/api/admin/customers', { params }).then((r) => r.data),

    getCustomer: (id: string) =>
      client.get<CustomerDto>(`/api/admin/customers/${id}`).then((r) => r.data),

    updateCustomerStatus: (id: string, data: UpdateCustomerStatusRequest) =>
      client.patch(`/api/admin/customers/${id}/status`, data).then((r) => r.data),

    getPlans: (params?: PagedRequest) =>
      client.get<PagedResult<PlanDto>>('/api/admin/plans', { params }).then((r) => r.data),

    createPlan: (data: CreatePlanRequest) =>
      client.post<PlanDto>('/api/admin/plans', data).then((r) => r.data),

    updatePlan: (id: string, data: UpdatePlanRequest) =>
      client.patch<PlanDto>(`/api/admin/plans/${id}`, data).then((r) => r.data),

    getTransactions: (params?: PagedRequest) =>
      client
        .get<PagedResult<TransactionDto>>('/api/admin/transactions', { params })
        .then((r) => r.data),

    getRevenueReport: () =>
      client.get<RevenueReportDto>('/api/admin/revenue').then((r) => r.data),

    getAuditLog: (params?: PagedRequest) =>
      client
        .get<PagedResult<AuditLogEntryDto>>('/api/admin/audit-log', { params })
        .then((r) => r.data),
  };
}
