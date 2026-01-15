using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using App.Infrastructure.Data;
using App.Infrastructure.Publishing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services;

public sealed class PublishService
{
    private readonly AppDbContext _db;
    private readonly IFormattingConverter _converter;
    private readonly IEnumerable<IPublishAdapter> _adapters;
    private readonly ImageStorage _storage;
    private readonly ILogger<PublishService> _logger;

    public PublishService(
        AppDbContext db,
        IFormattingConverter converter,
        IEnumerable<IPublishAdapter> adapters,
        ImageStorage storage,
        ILogger<PublishService> logger)
    {
        _db = db;
        _converter = converter;
        _adapters = adapters;
        _storage = storage;
        _logger = logger;
    }

    public async Task PublishPostAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        var post = await _db.Posts
            .Include(entry => entry.Campaign)
            .Include(entry => entry.Variants)
            .Include(entry => entry.ImageVariants)
            .Include(entry => entry.ChannelPublishes)
            .FirstOrDefaultAsync(entry => entry.Id == postId && entry.TenantId == tenantId, ct);

        if (post == null || post.Campaign == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);

        var targets = await _db.CampaignTargets
            .Where(link => link.CampaignId == post.CampaignId)
            .Join(_db.Targets, link => link.TargetId, target => target.Id, (link, target) => target)
            .Where(target => target.TenantId == tenantId)
            .ToListAsync(ct);

        if (targets.Count == 0)
        {
            post.Status = PostStatus.Failed;
            post.LastError = "No targets configured for campaign.";
            post.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var selectedText = ResolveSelectedText(post);
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            post.Status = PostStatus.Failed;
            post.LastError = "No text variant selected.";
            post.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var selectedImagePath = ResolveSelectedImagePath(tenantId, post);
        var logLines = new List<string>();
        var anyFailed = false;
        post.PublishAtUtc ??= DateTime.UtcNow;

        foreach (var target in targets)
        {
            var entry = post.ChannelPublishes.FirstOrDefault(cp => cp.TargetId == target.Id);
            if (entry == null)
            {
                entry = new PostChannelPublish
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PostId = post.Id,
                    TargetId = target.Id,
                    Status = ChannelPublishStatus.NotAttempted
                };
                post.ChannelPublishes.Add(entry);
                _db.PostChannelPublishes.Add(entry);
            }

            if (target.HasError)
            {
                entry.Status = ChannelPublishStatus.Failed;
                entry.ErrorMessage = target.ErrorMessage ?? "Target is in error state.";
                logLines.Add(BuildLogLine(target, entry));
                anyFailed = true;
                continue;
            }

            try
            {
                var adapter = ResolveAdapter(target.Type);
                var formattedText = _converter.Convert(selectedText, target.Type);
                var request = new PublishRequest(
                    tenantId,
                    target.Id,
                    target.Type,
                    formattedText,
                    selectedImagePath,
                    target.SettingsJson,
                    null);

                var result = await adapter.PublishAsync(request, ct);
                entry.Status = ChannelPublishStatus.Succeeded;
                entry.PlatformMessageId = result.PlatformMessageId;
                entry.PublishedAtUtc = DateTime.UtcNow;
                entry.ErrorMessage = null;
                logLines.Add(BuildLogLine(target, entry));
            }
            catch (Exception ex)
            {
                anyFailed = true;
                entry.Status = ChannelPublishStatus.Failed;
                entry.ErrorMessage = ex.Message;
                logLines.Add(BuildLogLine(target, entry));
                target.HasError = true;
                target.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to publish post {PostId} to target {TargetId}", postId, target.Id);
            }
        }

        post.PublishRollupStatus = PublishRollupCalculator.Calculate(post.ChannelPublishes.Select(cp => cp.Status));
        post.PublicationLog = string.Join(Environment.NewLine, logLines);
        post.UpdatedUtc = DateTime.UtcNow;

        if (anyFailed)
        {
            post.Status = PostStatus.Failed;
            post.LastError = "One or more targets failed to publish.";
        }
        else
        {
            post.Status = PostStatus.Published;
            post.LastError = null;
        }

        await _db.SaveChangesAsync(ct);
    }

    private string? ResolveSelectedText(Post post)
    {
        if (!string.IsNullOrWhiteSpace(post.UserEditedMarkdown))
        {
            return post.UserEditedMarkdown;
        }

        if (post.SelectedTextVariantId.HasValue)
        {
            return post.Variants.FirstOrDefault(v => v.Id == post.SelectedTextVariantId)?.MarkdownText;
        }

        return post.Variants.FirstOrDefault()?.MarkdownText;
    }

    private string? ResolveSelectedImagePath(string tenantId, Post post)
    {
        if (post.SelectedImageVariantId.HasValue)
        {
            var variant = post.ImageVariants.FirstOrDefault(v => v.Id == post.SelectedImageVariantId);
            return variant == null ? null : Path.Combine(_storage.GetTenantRoot(tenantId), variant.ImageUri);
        }

        var first = post.ImageVariants.FirstOrDefault();
        return first == null ? null : Path.Combine(_storage.GetTenantRoot(tenantId), first.ImageUri);
    }

    private IPublishAdapter ResolveAdapter(TargetType type)
    {
        var adapter = _adapters.FirstOrDefault(entry => entry.TargetType == type);
        if (adapter == null)
        {
            throw new InvalidOperationException($"No publish adapter registered for {type}.");
        }

        return adapter;
    }

    private static string BuildLogLine(Target target, PostChannelPublish entry)
    {
        var status = entry.Status.ToString();
        var messageId = string.IsNullOrWhiteSpace(entry.PlatformMessageId) ? "" : $" MessageId={entry.PlatformMessageId}.";
        var error = string.IsNullOrWhiteSpace(entry.ErrorMessage) ? "" : $" Error={entry.ErrorMessage}.";
        return $"[{DateTime.UtcNow:O}] {target.Type} '{target.DisplayName}' -> {status}.{messageId}{error}";
    }
}
