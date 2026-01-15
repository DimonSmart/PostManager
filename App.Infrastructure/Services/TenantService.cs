using System.Text.RegularExpressions;
using App.Domain.Entities;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

public sealed class TenantService
{
    private readonly AppDbContext _db;

    public TenantService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Tenant>> GetTenantsAsync(CancellationToken ct)
    {
        return _db.Tenants.OrderBy(tenant => tenant.Name).ToListAsync(ct);
    }

    public Task<Tenant?> GetTenantAsync(string tenantId, CancellationToken ct)
    {
        return _db.Tenants.FirstOrDefaultAsync(tenant => tenant.Id == tenantId, ct);
    }

    public async Task<Tenant> CreateTenantAsync(string name, string? settingsJson, CancellationToken ct)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Tenant name is required.", nameof(name));
        }

        var baseId = Slugify(trimmed);
        if (string.IsNullOrWhiteSpace(baseId))
        {
            baseId = "tenant";
        }

        var candidate = baseId;
        var suffix = 1;
        while (await _db.Tenants.AnyAsync(tenant => tenant.Id == candidate, ct))
        {
            candidate = $"{baseId}-{suffix++}";
        }

        var tenant = new Tenant
        {
            Id = candidate,
            Name = trimmed,
            SettingsJson = settingsJson,
            CreatedUtc = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task UpdateSettingsAsync(string tenantId, string? settingsJson, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(entry => entry.Id == tenantId, ct);
        if (tenant == null)
        {
            return;
        }

        tenant.SettingsJson = settingsJson;
        await _db.SaveChangesAsync(ct);
    }

    private static string Slugify(string input)
    {
        var lower = input.ToLowerInvariant();
        lower = Regex.Replace(lower, "[^a-z0-9]+", "-");
        lower = lower.Trim('-');
        return lower;
    }
}
