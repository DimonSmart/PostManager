using System.Text.Json;
using App.Domain.Entities;
using App.Infrastructure.Models;

namespace App.Infrastructure.Services;

public interface ITenantSettingsProvider
{
    TenantSettings GetSettings(Tenant tenant);
    string? ResolveEnv(string? envVarName);
}

public sealed class TenantSettingsProvider : ITenantSettingsProvider
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TenantSettings GetSettings(Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.SettingsJson))
        {
            return new TenantSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<TenantSettings>(tenant.SettingsJson, _options) ?? new TenantSettings();
        }
        catch
        {
            return new TenantSettings();
        }
    }

    public string? ResolveEnv(string? envVarName)
    {
        if (string.IsNullOrWhiteSpace(envVarName))
        {
            return null;
        }

        return Environment.GetEnvironmentVariable(envVarName);
    }
}

