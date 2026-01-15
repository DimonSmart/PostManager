using App.Domain.Entities;
using App.Domain.Enums;
using App.Domain.Services;
using App.Infrastructure.Models;
using App.Infrastructure.Services;

namespace App.Infrastructure.Generation;

public sealed record ImageGenerationRequest(
    string TenantId,
    Guid PostId,
    int VariantIndex,
    Campaign Campaign,
    Item Item,
    TenantSettings TenantSettings);

public interface IImageGenerator
{
    Task<ImageVariant> GenerateAsync(ImageGenerationRequest request, CancellationToken ct);
}

public sealed class ImageGenerator : IImageGenerator
{
    private readonly ImageStorage _storage;

    public ImageGenerator(ImageStorage storage)
    {
        _storage = storage;
    }

    public async Task<ImageVariant> GenerateAsync(ImageGenerationRequest request, CancellationToken ct)
    {
        var prompt = PromptBuilder.BuildImagePrompt(request.Campaign, request.Item);
        var imagePath = await _storage.SavePlaceholderAsync(request.TenantId, request.PostId, request.VariantIndex, ct);
        var generatorInfo = request.Campaign.ImageProvider switch
        {
            ImageProvider.Azure => "Azure (placeholder)",
            _ => "StableDiffusionLocal (placeholder)"
        };

        return new ImageVariant
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            PostId = request.PostId,
            ImageUri = imagePath,
            PromptUsed = prompt,
            GeneratorInfo = generatorInfo,
            CreatedUtc = DateTime.UtcNow
        };
    }
}

