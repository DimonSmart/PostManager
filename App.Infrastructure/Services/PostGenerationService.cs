using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using App.Infrastructure.Data;
using App.Infrastructure.Generation;
using App.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace App.Infrastructure.Services;

public sealed class PostGenerationService
{
    private readonly AppDbContext _db;
    private readonly ITenantSettingsProvider _settingsProvider;
    private readonly MultiAgentTextGenerator _textGenerator;
    private readonly IImageGenerator _imageGenerator;
    private readonly IOptions<LlmDefaults> _llmDefaults;
    private readonly ILogger<PostGenerationService> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public PostGenerationService(
        AppDbContext db,
        ITenantSettingsProvider settingsProvider,
        MultiAgentTextGenerator textGenerator,
        IImageGenerator imageGenerator,
        IOptions<LlmDefaults> llmDefaults,
        ILogger<PostGenerationService> logger)
    {
        _db = db;
        _settingsProvider = settingsProvider;
        _textGenerator = textGenerator;
        _imageGenerator = imageGenerator;
        _llmDefaults = llmDefaults;
        _logger = logger;
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2)
            })
            .Build();
    }

    public async Task GenerateForPostAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        var post = await _db.Posts
            .Include(entry => entry.Variants)
            .Include(entry => entry.ImageVariants)
            .FirstOrDefaultAsync(entry => entry.Id == postId && entry.TenantId == tenantId, ct);

        if (post == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);

        var campaign = await _db.Campaigns.FirstOrDefaultAsync(entry => entry.Id == post.CampaignId, ct);
        var item = await _db.Items.FirstOrDefaultAsync(entry => entry.Id == post.ItemId, ct);
        var tenant = await _db.Tenants.FirstOrDefaultAsync(entry => entry.Id == tenantId, ct);

        if (campaign == null || item == null || tenant == null)
        {
            return;
        }

        post.Status = PostStatus.Generating;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var settings = _settingsProvider.GetSettings(tenant);
        var llmConfig = ResolveLlmConfig(settings, campaign);
        var errors = new List<string>();

        for (var i = 0; i < Math.Max(1, campaign.TextVariantsPerPost); i++)
        {
            try
            {
                var variant = await _retryPipeline.ExecuteAsync(async token =>
                    await _textGenerator.GenerateVariantAsync(post, campaign, item, llmConfig, token), ct);

                _db.PostVariants.Add(variant);
                post.IsTextReady = true;
            }
            catch (Exception ex)
            {
                errors.Add($"Text variant {i + 1}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to generate text variant {Index} for post {PostId}", i + 1, postId);
            }
        }

        for (var i = 0; i < Math.Max(1, campaign.ImageVariantsPerPost); i++)
        {
            try
            {
                var request = new ImageGenerationRequest(
                    tenantId,
                    post.Id,
                    i + 1,
                    campaign,
                    item,
                    settings);

                var imageVariant = await _retryPipeline.ExecuteAsync(
                    async token => await _imageGenerator.GenerateAsync(request, token),
                    ct);

                _db.ImageVariants.Add(imageVariant);
                post.IsImageReady = true;
            }
            catch (Exception ex)
            {
                errors.Add($"Image variant {i + 1}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to generate image variant {Index} for post {PostId}", i + 1, postId);
            }
        }

        if (post.IsTextReady || post.IsImageReady)
        {
            post.Status = PostStatus.Draft;
        }
        else
        {
            post.Status = PostStatus.Failed;
            post.LastError = string.Join(" | ", errors);
        }

        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private LlmRuntimeConfig ResolveLlmConfig(TenantSettings settings, Campaign campaign)
    {
        var defaults = _llmDefaults.Value ?? new LlmDefaults();
        var endpoint = _settingsProvider.ResolveEnv(settings.LlmEndpointEnv) ?? defaults.Endpoint;
        var apiKey = _settingsProvider.ResolveEnv(settings.LlmApiKeyEnv) ?? defaults.ApiKey;

        return new LlmRuntimeConfig(endpoint, apiKey);
    }
}

