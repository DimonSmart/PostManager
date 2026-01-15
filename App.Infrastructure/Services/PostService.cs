using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services;

public sealed class PostService
{
    private readonly AppDbContext _db;
    private readonly PostGenerationService _generationService;
    private readonly PublishService _publishService;

    public PostService(AppDbContext db, PostGenerationService generationService, PublishService publishService)
    {
        _db = db;
        _generationService = generationService;
        _publishService = publishService;
    }

    public Task<List<Post>> GetPostsAsync(string tenantId, Guid campaignId, PostStatus? status, CancellationToken ct)
    {
        var query = _db.Posts
            .Include(post => post.Item)
            .Where(post => post.TenantId == tenantId && post.CampaignId == campaignId);

        if (status.HasValue)
        {
            query = query.Where(post => post.Status == status.Value);
        }

        return query.OrderBy(post => post.CreatedUtc).ToListAsync(ct);
    }

    public Task<Post?> GetPostAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        return _db.Posts
            .Include(post => post.Item)
            .Include(post => post.Campaign)
            .Include(post => post.Variants)
            .Include(post => post.ImageVariants)
            .Include(post => post.ChannelPublishes)
            .FirstOrDefaultAsync(post => post.TenantId == tenantId && post.Id == postId, ct);
    }

    public async Task GenerateAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        await _generationService.GenerateForPostAsync(tenantId, postId, ct);

        var post = await _db.Posts.Include(entry => entry.Campaign)
            .FirstOrDefaultAsync(entry => entry.Id == postId && entry.TenantId == tenantId, ct);

        if (post?.Campaign == null)
        {
            return;
        }

        if (!post.Campaign.RequiresModeration && post.Status == PostStatus.Draft)
        {
            await ApproveAsync(tenantId, postId, ct);
            await ScheduleAsync(tenantId, postId, ct, publishIfDue: true);
        }
    }

    public async Task UpdateSelectionAsync(string tenantId, Guid postId, Guid? textVariantId, Guid? imageVariantId, CancellationToken ct)
    {
        var post = await GetPostAsync(tenantId, postId, ct);
        if (post == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);
        post.SelectedTextVariantId = textVariantId;
        post.SelectedImageVariantId = imageVariantId;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateMarkdownAsync(string tenantId, Guid postId, string markdown, CancellationToken ct)
    {
        var post = await GetPostAsync(tenantId, postId, ct);
        if (post == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);
        post.UserEditedMarkdown = markdown;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApproveAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        var post = await GetPostAsync(tenantId, postId, ct);
        if (post?.Campaign == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);

        if (string.IsNullOrWhiteSpace(post.UserEditedMarkdown) && !post.SelectedTextVariantId.HasValue)
        {
            throw new InvalidOperationException("Select or edit a text variant before approval.");
        }

        post.Status = PostStatus.Approved;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        var post = await GetPostAsync(tenantId, postId, ct);
        if (post == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);
        post.Status = PostStatus.Cancelled;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ScheduleAsync(string tenantId, Guid postId, CancellationToken ct, bool publishIfDue)
    {
        var post = await GetPostAsync(tenantId, postId, ct);
        if (post?.Campaign == null)
        {
            return;
        }

        PostGuard.EnsureEditable(post);

        var next = ScheduleCalculator.GetNextOccurrenceUtc(post.Campaign, DateTimeOffset.UtcNow);
        post.PublishAtUtc = next?.UtcDateTime ?? DateTime.UtcNow;
        post.Status = PostStatus.Scheduled;
        post.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (publishIfDue && post.PublishAtUtc <= DateTime.UtcNow)
        {
            await _publishService.PublishPostAsync(tenantId, postId, ct);
        }
    }

    public Task PublishNowAsync(string tenantId, Guid postId, CancellationToken ct)
    {
        return _publishService.PublishPostAsync(tenantId, postId, ct);
    }
}
