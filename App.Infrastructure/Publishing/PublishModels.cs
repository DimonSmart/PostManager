using App.Domain.Enums;

namespace App.Infrastructure.Publishing;

public sealed record PublishRequest(
    string TenantId,
    Guid TargetId,
    TargetType TargetType,
    string Text,
    string? ImagePath,
    string? TargetSettingsJson,
    string? Metadata);

public sealed record PublishResult(string? PlatformMessageId);

public interface IPublishAdapter
{
    TargetType TargetType { get; }
    Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct);
}

public interface IFormattingConverter
{
    string Convert(string markdown, TargetType targetType);
}

