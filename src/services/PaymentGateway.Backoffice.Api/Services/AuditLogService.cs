using Microsoft.EntityFrameworkCore;
using PaymentGateway.Backoffice.Api.Data;
using PaymentGateway.Contracts.Admin;
using PaymentGateway.Contracts.Common;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Backoffice.Api.Services;

public class AuditLogService
{
    private readonly BackofficeDbContext _db;
    private readonly ReadOnlyDbContext _readDb;

    public AuditLogService(BackofficeDbContext db, ReadOnlyDbContext readDb)
    {
        _db = db;
        _readDb = readDb;
    }

    public async Task LogAction(Guid? userId, Guid? orgId, string action, string? entityType, string? entityId, string? details, string? ipAddress)
    {
        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLog(
        int page, int pageSize, DateTime? from, DateTime? to, string? action, string? userId)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid))
            query = query.Where(a => a.UserId == uid);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userIds = await query
            .Where(a => a.UserId != null)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .ToListAsync();

        var userEmails = await _readDb.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.UserId != null && userEmails.ContainsKey(a.UserId.Value) ? userEmails[a.UserId.Value] : null,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Details,
                a.CreatedAt))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize, totalPages);
    }
}
