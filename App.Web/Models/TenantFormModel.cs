namespace App.Web.Models;

public sealed class TenantFormModel
{
    public string Name { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
}
