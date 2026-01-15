using App.Domain.Entities;
using App.Domain.Enums;

namespace App.Domain.Services;

public static class PostGuard
{
    public static void EnsureEditable(Post post)
    {
        if (post.Status == PostStatus.Published)
        {
            throw new InvalidOperationException("Published posts are immutable.");
        }
    }
}

