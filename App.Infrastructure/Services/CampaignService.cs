using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

public sealed class CampaignService
{
    private readonly AppDbContext _db;

    public CampaignService(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Campaign>> GetCampaignsAsync(string tenantId, CancellationToken ct)
    {
        return _db.Campaigns
            .Where(campaign => campaign.TenantId == tenantId)
            .OrderBy(campaign => campaign.Name)
            .ToListAsync(ct);
    }

    public Task<Campaign?> GetCampaignAsync(string tenantId, Guid campaignId, CancellationToken ct)
    {
        return _db.Campaigns
            .Include(campaign => campaign.CampaignTargets)
            .FirstOrDefaultAsync(campaign => campaign.TenantId == tenantId && campaign.Id == campaignId, ct);
    }

    public async Task<Campaign> CreateCampaignAsync(string tenantId, Campaign campaign, IReadOnlyCollection<Guid> targetIds, CancellationToken ct)
    {
        if (targetIds.Count == 0)
        {
            throw new InvalidOperationException("Campaign must have at least one target.");
        }

        campaign.Id = campaign.Id == Guid.Empty ? Guid.NewGuid() : campaign.Id;
        campaign.TenantId = tenantId;
        campaign.CreatedUtc = DateTime.UtcNow;
        campaign.UpdatedUtc = campaign.CreatedUtc;

        _db.Campaigns.Add(campaign);

        foreach (var targetId in targetIds.Distinct())
        {
            _db.CampaignTargets.Add(new CampaignTarget
            {
                CampaignId = campaign.Id,
                TargetId = targetId,
                TenantId = tenantId
            });
        }

        await _db.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task UpdateCampaignAsync(string tenantId, Campaign updated, IReadOnlyCollection<Guid> targetIds, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(entry => entry.Id == updated.Id && entry.TenantId == tenantId, ct);
        if (campaign == null)
        {
            return;
        }

        campaign.Name = updated.Name;
        campaign.Description = updated.Description;
        campaign.RequiresModeration = updated.RequiresModeration;
        campaign.TextVariantsPerPost = Math.Max(1, updated.TextVariantsPerPost);
        campaign.TextPrompt = updated.TextPrompt;
        campaign.TextEditorRulesPrompt = updated.TextEditorRulesPrompt;
        campaign.TextLlmProvider = updated.TextLlmProvider;
        campaign.TextLlmModel = updated.TextLlmModel;
        campaign.TextLlmTemperature = updated.TextLlmTemperature;
        campaign.TextLlmMaxTokens = updated.TextLlmMaxTokens;
        campaign.ImageVariantsPerPost = Math.Max(1, updated.ImageVariantsPerPost);
        campaign.ImageProvider = updated.ImageProvider;
        campaign.ImagePositivePrompt = updated.ImagePositivePrompt;
        campaign.ImageNegativePrompt = updated.ImageNegativePrompt;
        campaign.ImageOptions = updated.ImageOptions;
        campaign.ScheduleCron = updated.ScheduleCron;
        campaign.ScheduleTimezone = updated.ScheduleTimezone;
        campaign.MissedCatchUpWithinMinutes = updated.MissedCatchUpWithinMinutes;
        campaign.MissedIfMissedLongerThanMinutes = updated.MissedIfMissedLongerThanMinutes;
        campaign.UpdatedUtc = DateTime.UtcNow;

        var desiredTargets = targetIds.Distinct().ToList();
        if (desiredTargets.Count == 0)
        {
            throw new InvalidOperationException("Campaign must have at least one target.");
        }

        var existingTargets = await _db.CampaignTargets
            .Where(link => link.CampaignId == campaign.Id)
            .Select(link => link.TargetId)
            .ToListAsync(ct);

        var toAdd = desiredTargets.Except(existingTargets).ToList();
        var toRemove = existingTargets.Except(desiredTargets).ToList();

        foreach (var targetId in toAdd)
        {
            _db.CampaignTargets.Add(new CampaignTarget
            {
                CampaignId = campaign.Id,
                TargetId = targetId,
                TenantId = tenantId
            });
        }

        if (toRemove.Count > 0)
        {
            var links = _db.CampaignTargets.Where(link => link.CampaignId == campaign.Id && toRemove.Contains(link.TargetId));
            _db.CampaignTargets.RemoveRange(links);
        }

        await SyncPostTargetsAsync(campaign.Id, tenantId, desiredTargets, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCampaignAsync(string tenantId, Guid campaignId, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(entry => entry.Id == campaignId && entry.TenantId == tenantId, ct);
        if (campaign == null)
        {
            return;
        }

        _db.Campaigns.Remove(campaign);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddItemsAsync(string tenantId, Guid campaignId, string rawItems, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(entry => entry.Id == campaignId && entry.TenantId == tenantId, ct);
        if (campaign == null)
        {
            return;
        }

        var parsed = ItemParser.ParseItems(rawItems);
        if (parsed.Count == 0)
        {
            return;
        }

        var maxIndex = await _db.Items
            .Where(item => item.CampaignId == campaignId)
            .Select(item => (int?)item.OrderIndex)
            .MaxAsync(ct) ?? -1;

        var targetIds = await _db.CampaignTargets
            .Where(link => link.CampaignId == campaignId)
            .Select(link => link.TargetId)
            .ToListAsync(ct);

        foreach (var itemText in parsed)
        {
            maxIndex++;
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                TenantId = tenantId,
                CampaignId = campaignId,
                SourceText = itemText,
                OrderIndex = maxIndex,
                CreatedUtc = DateTime.UtcNow
            };

            var postId = Guid.NewGuid();
            var post = new Post
            {
                Id = postId,
                TenantId = tenantId,
                CampaignId = campaignId,
                ItemId = itemId,
                Status = PostStatus.Created,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            _db.Items.Add(item);
            _db.Posts.Add(post);

            foreach (var targetId in targetIds)
            {
                _db.PostChannelPublishes.Add(new PostChannelPublish
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PostId = postId,
                    TargetId = targetId,
                    Status = ChannelPublishStatus.NotAttempted
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Item>> GetItemsAsync(string tenantId, Guid campaignId, CancellationToken ct)
    {
        return _db.Items.Where(item => item.TenantId == tenantId && item.CampaignId == campaignId)
            .OrderBy(item => item.OrderIndex)
            .ToListAsync(ct);
    }

    private async Task SyncPostTargetsAsync(Guid campaignId, string tenantId, IReadOnlyCollection<Guid> desiredTargets, CancellationToken ct)
    {
        var posts = await _db.Posts
            .Where(post => post.CampaignId == campaignId && post.TenantId == tenantId && post.Status != PostStatus.Published)
            .ToListAsync(ct);

        if (posts.Count == 0)
        {
            return;
        }

        var postIds = posts.Select(post => post.Id).ToList();
        var existingLinks = await _db.PostChannelPublishes
            .Where(entry => postIds.Contains(entry.PostId))
            .ToListAsync(ct);

        foreach (var post in posts)
        {
            var postLinks = existingLinks.Where(entry => entry.PostId == post.Id).ToList();
            var existingTargetIds = postLinks.Select(entry => entry.TargetId).Where(id => id.HasValue).Select(id => id!.Value).ToList();

            foreach (var targetId in desiredTargets.Except(existingTargetIds))
            {
                _db.PostChannelPublishes.Add(new PostChannelPublish
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PostId = post.Id,
                    TargetId = targetId,
                    Status = ChannelPublishStatus.NotAttempted
                });
            }

            var toRemove = postLinks
                .Where(link => link.TargetId.HasValue && !desiredTargets.Contains(link.TargetId.Value))
                .ToList();

            if (toRemove.Count > 0)
            {
                _db.PostChannelPublishes.RemoveRange(toRemove);
            }
        }
    }
}
