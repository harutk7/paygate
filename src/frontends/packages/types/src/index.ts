// ── Common Types ──

export interface PagedRequest {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  search?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ErrorResponse {
  message: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// ── Auth Types ──

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  organizationName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

// ── User Types ──

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string | number;
  isActive: boolean;
  createdAt: string;
  organizationId?: string;
  updatedAt?: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
}

export interface InviteUserRequest {
  email: string;
  role: string;
}

// ── Organization Types ──

export interface OrganizationDto {
  id: string;
  name: string;
  slug: string;
  planId: string;
  planName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateOrganizationRequest {
  name?: string;
}

// ── Plan Types ──

export interface PlanDto {
  id: string;
  name: string;
  tier: number;
  priceMonthly: number;
  transactionLimit: number;
  apiKeyLimit: number;
  rateLimit: number;
  features: string[];
  isActive: boolean;
  // Computed fields for frontend convenience
  description?: string;
  monthlyPrice?: number;
  annualPrice?: number;
}

// ── Subscription Types ──

export interface SubscriptionDto {
  id: string;
  organizationId: string;
  planId: string;
  planName: string;
  status: 'active' | 'canceled' | 'past_due' | 'trialing';
  currentPeriodStart: string;
  currentPeriodEnd: string;
  cancelAtPeriodEnd: boolean;
  createdAt: string;
}

export interface CreateSubscriptionRequest {
  planId: string;
  billingCycle: 'monthly' | 'annual';
  paymentMethodId?: string;
}

export interface CancelSubscriptionRequest {
  cancelAtPeriodEnd: boolean;
}

// ── Billing Types ──

export interface PaymentMethodDto {
  id: string;
  type: string;
  last4: string;
  brand: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
}

export interface AddPaymentMethodRequest {
  token: string;
  setDefault?: boolean;
}

export interface PaymentDto {
  id: string;
  amount: number;
  currency: string;
  status: 'succeeded' | 'pending' | 'failed' | 'refunded';
  description: string;
  createdAt: string;
}

export interface InvoiceDto {
  id: string;
  number: string;
  amount: number;
  currency: string;
  status: 'paid' | 'open' | 'void' | 'draft';
  periodStart: string;
  periodEnd: string;
  pdfUrl: string;
  createdAt: string;
}

// ── API Key Types ──

export interface ApiKeyDto {
  id: string;
  name: string;
  prefix: string;
  environment: 'test' | 'live';
  lastUsedAt: string | null;
  expiresAt: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateApiKeyRequest {
  name: string;
  environment: 'test' | 'live';
  expiresAt?: string;
}

export interface CreateApiKeyResponse {
  apiKey: ApiKeyDto;
  secretKey: string;
}

// ── Transaction Types ──

export interface TransactionDto {
  id: string;
  externalId: string;
  amount: number;
  currency: string;
  status: 'pending' | 'completed' | 'failed' | 'refunded' | 'partially_refunded';
  type: 'charge' | 'refund' | 'payout';
  description: string;
  customerEmail: string;
  createdAt: string;
}

export interface TransactionDetailDto extends TransactionDto {
  metadata: Record<string, string>;
  paymentMethod: string;
  refundedAmount: number;
  failureReason: string | null;
  timeline: TransactionEvent[];
}

export interface TransactionEvent {
  type: string;
  status: string;
  timestamp: string;
  details: string;
}

export interface CreateChargeRequest {
  amount: number;
  currency: string;
  description?: string;
  customerEmail: string;
  paymentMethodToken: string;
  metadata?: Record<string, string>;
}

export interface CreateChargeResponse {
  transactionId: string;
  externalId: string;
  status: string;
}

export interface RefundChargeRequest {
  amount?: number;
  reason?: string;
}

export interface TransactionStatsDto {
  totalVolume: number;
  totalCount: number;
  successRate: number;
  averageAmount: number;
  volumeByDay: { date: string; amount: number }[];
  countByStatus: { status: string; count: number }[];
}

// ── Webhook Types ──

export interface WebhookDto {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  secret: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWebhookRequest {
  url: string;
  events: string[];
}

export interface UpdateWebhookRequest {
  url?: string;
  events?: string[];
  isActive?: boolean;
}

export interface WebhookDeliveryDto {
  id: string;
  webhookId: string;
  event: string;
  statusCode: number;
  success: boolean;
  requestBody: string;
  responseBody: string;
  createdAt: string;
}

// ── Admin Types ──

export interface DashboardDto {
  totalCustomers: number;
  activeSubscriptions: number;
  monthlyRevenue: number;
  totalTransactions: number;
  recentTransactions: TransactionDto[];
  revenueByMonth: { month: string; revenue: number }[];
  customerGrowth: { month: string; count: number }[];
}

export interface CustomerDto {
  id: string;
  organizationName: string;
  email: string;
  planName: string;
  status: 'active' | 'suspended' | 'canceled';
  totalTransactions: number;
  totalVolume: number;
  createdAt: string;
}

export interface UpdateCustomerStatusRequest {
  status: 'active' | 'suspended';
  reason?: string;
}

export interface CreatePlanRequest {
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  transactionLimit: number;
  rateLimit: number;
  features: string[];
}

export interface UpdatePlanRequest {
  name?: string;
  description?: string;
  monthlyPrice?: number;
  annualPrice?: number;
  transactionLimit?: number;
  rateLimit?: number;
  features?: string[];
  isActive?: boolean;
}

export interface RevenueReportDto {
  totalRevenue: number;
  mrr: number;
  arr: number;
  churnRate: number;
  revenueByPlan: { planName: string; revenue: number }[];
  revenueByMonth: { month: string; revenue: number }[];
}

export interface AuditLogEntryDto {
  id: string;
  userId: string;
  userEmail: string;
  action: string;
  resource: string;
  resourceId: string;
  details: string;
  ipAddress: string;
  createdAt: string;
}
