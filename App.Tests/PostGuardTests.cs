using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using Xunit;

namespace App.Tests;

public class PostGuardTests
{
    [Fact]
    public void EnsureEditable_ThrowsForPublishedPost()
    {
        var post = new Post { Status = PostStatus.Published };

        Assert.Throws<InvalidOperationException>(() => PostGuard.EnsureEditable(post));
    }

    [Fact]
    public void EnsureEditable_AllowsDraft()
    {
        var post = new Post { Status = PostStatus.Draft };

        PostGuard.EnsureEditable(post);
    }
}
