using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Admin;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Contracts.Organizations;
using PaymentGateway.Contracts.Subscriptions;
using PaymentGateway.Contracts.Transactions;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Backoffice.Api.Services;

public class CustomerManagementService
{
    private readonly ReadOnlyDbContext _readDb;
    private readonly AuditLogService _auditLogService;

    public CustomerManagementService(ReadOnlyDbContext readDb, AuditLogService auditLogService)
    {
        _readDb = readDb;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomers(
        int page, int pageSize, string? search, string? statusFilter)
    {
        var query = _readDb.Organizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(searchLower) ||
                o.Users.Any(u => u.Email.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var isActive = statusFilter.Equals("active", StringComparison.OrdinalIgnoreCase);
            query = query.Where(o => o.IsActive == isActive);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new CustomerDto(
                o.Id,
                o.Name,
                o.Users.Where(u => u.Role == UserRole.CustomerAdmin).Select(u => u.Email).FirstOrDefault() ?? "",
                o.Subscriptions
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.Plan.Name)
                    .FirstOrDefault() ?? "No Plan",
                o.Subscriptions
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.Status)
                    .FirstOrDefault(),
                o.Transactions.Count(),
                o.CreatedAt))
            .ToListAsync();

        return new PagedResult<CustomerDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<CustomerDetailDto?> GetCustomerDetail(Guid orgId)
    {
        var org = await _readDb.Organizations
            .Where(o => o.Id == orgId)
            .Select(o => new OrganizationDto(o.Id, o.Name, o.Slug, o.IsActive, o.CreatedAt))
            .FirstOrDefaultAsync();

        if (org == null) return null;

        var subscription = await _readDb.Subscriptions
            .Where(s => s.OrganizationId == orgId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto(
                s.Id, s.PlanId, s.Plan.Name, s.Status,
                s.CurrentPeriodStart, s.CurrentPeriodEnd,
                s.TrialEnd, s.CancelledAt))
            .FirstOrDefaultAsync();

        var recentTransactions = await _readDb.Transactions
            .Where(t => t.OrganizationId == orgId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new TransactionDto(
                t.Id, t.Amount, t.Currency, t.Status,
                t.ProviderTransactionId, t.CreatedAt, t.ApiKey.Name))
            .ToListAsync();

        var apiKeyCount = await _readDb.ApiKeys
            .Where(a => a.OrganizationId == orgId)
            .CountAsync();

        var totalVolume = await _readDb.Transactions
            .Where(t => t.OrganizationId == orgId && t.Status == TransactionStatus.Succeeded)
            .SumAsync(t => t.Amount);

        return new CustomerDetailDto(org, subscription, recentTransactions, apiKeyCount, totalVolume);
    }

    public async Task<bool> UpdateCustomerStatus(Guid orgId, bool isActive, Guid? userId, string? ipAddress)
    {
        var org = await _readDb.Organizations.FindAsync(orgId);
        if (org == null) return false;

        // We need to use the ReadOnlyDbContext here since Organization lives in identity schema.
        // In a real system, this would go through a dedicated Identity API call.
        // For the backoffice, we allow direct write to update the status.
        _readDb.Entry(org).Property(o => o.IsActive).CurrentValue = isActive;
        _readDb.Entry(org).Property(o => o.UpdatedAt).CurrentValue = DateTime.UtcNow;
        await _readDb.SaveChangesAsync();

        await _auditLogService.LogAction(
            userId, orgId,
            isActive ? "CustomerActivated" : "CustomerSuspended",
            "Organization", orgId.ToString(),
            $"Customer status changed to {(isActive ? "active" : "suspended")}",
            ipAddress);

        return true;
    }
}
