using App.Domain.Enums;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services;

public sealed class ScheduleCatchUpService
{
    private readonly AppDbContext _db;
    private readonly PublishService _publishService;
    private readonly ILogger<ScheduleCatchUpService> _logger;

    public ScheduleCatchUpService(AppDbContext db, PublishService publishService, ILogger<ScheduleCatchUpService> logger)
    {
        _db = db;
        _publishService = publishService;
        _logger = logger;
    }

    public async Task CatchUpAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var posts = await _db.Posts
            .Include(post => post.Campaign)
            .Where(post => post.Status == PostStatus.Scheduled && post.PublishAtUtc < now.UtcDateTime)
            .ToListAsync(ct);

        foreach (var post in posts)
        {
            if (post.Campaign == null || post.PublishAtUtc == null)
            {
                continue;
            }

            if (ScheduleCalculator.ShouldCatchUp(post.Campaign, post.PublishAtUtc.Value, now))
            {
                _logger.LogInformation("Catch-up publishing post {PostId}.", post.Id);
                await _publishService.PublishPostAsync(post.TenantId, post.Id, ct);
            }
            else
            {
                post.LastError = "Publish time missed; manual action required.";
                post.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
