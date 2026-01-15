using App.Infrastructure.Data;
using App.Infrastructure.Generation;
using App.Infrastructure.Models;
using App.Infrastructure.Publishing;
using App.Infrastructure.Publishing.Adapters;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace App.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmDefaults>(configuration.GetSection("LlmDefaults"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));

        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=data/app.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return new ImageStorage(options.DataRoot);
        });

        services.AddSingleton<ITenantSettingsProvider, TenantSettingsProvider>();

        services.AddHttpClient<OpenAiCompatibleLlmClient>();
        services.AddHttpClient<TelegramPublishAdapter>();

        services.AddTransient<MockLlmClient>();
        services.AddTransient<LlmClientFactory>();
        services.AddTransient<ContentCreatorAgent>();
        services.AddTransient<EditorAgent>();
        services.AddTransient<MultiAgentTextGenerator>();
        services.AddTransient<IImageGenerator, ImageGenerator>();

        services.AddScoped<TenantService>();
        services.AddScoped<TargetService>();
        services.AddScoped<CampaignService>();
        services.AddScoped<PostService>();
        services.AddScoped<PostGenerationService>();
        services.AddScoped<PublishService>();
        services.AddScoped<ScheduleCatchUpService>();

        services.AddScoped<IFormattingConverter, SimpleFormattingConverter>();
        services.AddScoped<IPublishAdapter, TelegramPublishAdapter>();
        services.AddScoped<IPublishAdapter, TwitterPublishAdapter>();
        services.AddScoped<IPublishAdapter, WordPressPublishAdapter>();
        services.AddScoped<IPublishAdapter, InstagramPublishAdapter>();

        return services;
    }
}
