using App.Domain.Entities;
using App.Domain.Enums;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

public sealed class TargetService
{
    private readonly AppDbContext _db;

    public TargetService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Target>> GetTargetsAsync(string tenantId, CancellationToken ct)
    {
        return _db.Targets.Where(target => target.TenantId == tenantId)
            .OrderBy(target => target.DisplayName)
            .ToListAsync(ct);
    }

    public Task<Target?> GetTargetAsync(string tenantId, Guid targetId, CancellationToken ct)
    {
        return _db.Targets.FirstOrDefaultAsync(target => target.TenantId == tenantId && target.Id == targetId, ct);
    }

    public async Task<Target> AddTargetAsync(string tenantId, TargetType type, string displayName, string? settingsJson, CancellationToken ct)
    {
        var target = new Target
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            DisplayName = displayName.Trim(),
            SettingsJson = settingsJson,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Targets.Add(target);
        await _db.SaveChangesAsync(ct);
        return target;
    }

    public async Task UpdateTargetAsync(string tenantId, Guid targetId, TargetType type, string displayName, string? settingsJson, CancellationToken ct)
    {
        var target = await GetTargetAsync(tenantId, targetId, ct);
        if (target == null)
        {
            return;
        }

        target.Type = type;
        target.DisplayName = displayName.Trim();
        target.SettingsJson = settingsJson;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetErrorAsync(string tenantId, Guid targetId, CancellationToken ct)
    {
        var target = await GetTargetAsync(tenantId, targetId, ct);
        if (target == null)
        {
            return;
        }

        target.HasError = false;
        target.ErrorMessage = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteTargetAsync(string tenantId, Guid targetId, CancellationToken ct)
    {
        var target = await GetTargetAsync(tenantId, targetId, ct);
        if (target == null)
        {
            return;
        }

        _db.Targets.Remove(target);
        await _db.SaveChangesAsync(ct);
    }
}
