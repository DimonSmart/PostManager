namespace App.Infrastructure.Services;

public sealed class ImageStorage
{
    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=");

    private readonly string _dataRoot;

    public ImageStorage(string dataRoot)
    {
        _dataRoot = dataRoot;
    }

    public string GetTenantRoot(string tenantId) => Path.Combine(_dataRoot, tenantId);

    public string GetImageRelativePath(Guid postId, int variantIndex)
    {
        return $"images/{postId}/img_{variantIndex:00}.png";
    }

    public string GetImageAbsolutePath(string tenantId, Guid postId, int variantIndex)
    {
        return Path.Combine(GetTenantRoot(tenantId), GetImageRelativePath(postId, variantIndex));
    }

    public async Task<string> SavePlaceholderAsync(string tenantId, Guid postId, int variantIndex, CancellationToken ct)
    {
        var absolutePath = GetImageAbsolutePath(tenantId, postId, variantIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllBytesAsync(absolutePath, PlaceholderPng, ct);
        return GetImageRelativePath(postId, variantIndex);
    }
}

