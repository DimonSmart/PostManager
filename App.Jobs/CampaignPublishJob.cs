using App.Domain.Enums;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace App.Jobs;

public sealed class CampaignPublishJob : IJob
{
    private readonly AppDbContext _db;
    private readonly PublishService _publishService;
    private readonly ILogger<CampaignPublishJob> _logger;

    public CampaignPublishJob(AppDbContext db, PublishService publishService, ILogger<CampaignPublishJob> logger)
    {
        _db = db;
        _publishService = publishService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var campaignIdRaw = context.MergedJobDataMap.GetString("CampaignId");
        var tenantId = context.MergedJobDataMap.GetString("TenantId");

        if (!Guid.TryParse(campaignIdRaw, out var campaignId) || string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("CampaignPublishJob missing CampaignId/TenantId");
            return;
        }

        var campaign = await _db.Campaigns.FirstOrDefaultAsync(entry => entry.Id == campaignId && entry.TenantId == tenantId);
        if (campaign == null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var posts = await _db.Posts
            .Where(post => post.CampaignId == campaignId
                && post.TenantId == tenantId
                && post.Status == PostStatus.Scheduled
                && post.PublishAtUtc <= now.UtcDateTime)
            .ToListAsync();

        foreach (var post in posts)
        {
            if (post.PublishAtUtc == null)
            {
                continue;
            }

            if (ScheduleCalculator.ShouldCatchUp(campaign, post.PublishAtUtc.Value, now))
            {
                _logger.LogInformation("Scheduled publish post {PostId} for campaign {CampaignId}", post.Id, campaignId);
                await _publishService.PublishPostAsync(tenantId, post.Id, context.CancellationToken);
            }
            else
            {
                post.LastError = "Publish time missed; manual action required.";
                post.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
