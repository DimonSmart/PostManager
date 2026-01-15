namespace App.Infrastructure.Models;

public sealed class TenantSettings
{
    public string? LlmEndpointEnv { get; set; }
    public string? LlmApiKeyEnv { get; set; }
    public string? StableDiffusionUrlEnv { get; set; }
    public string? AzureImageEndpointEnv { get; set; }
    public string? AzureImageApiKeyEnv { get; set; }
}

