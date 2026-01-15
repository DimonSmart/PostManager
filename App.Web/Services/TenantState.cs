namespace App.Web.Services;

public sealed class TenantState
{
    public string? TenantId { get; private set; }
    public string? TenantName { get; private set; }

    public event Action? OnChange;

    public void SetTenant(string tenantId, string tenantName)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        TenantId = null;
        TenantName = null;
        OnChange?.Invoke();
    }
}
