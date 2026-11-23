namespace SyntaxCircus.Maui.StoreReview;

/// <summary>Used on platforms without a store review API. Every operation reports false.</summary>
internal sealed class UnavailableStoreReviewService : IStoreReviewService
{
    /// <inheritdoc />
    public bool CanOpenStoreReviewPage => false;

    /// <inheritdoc />
    public Task<bool> RequestInAppReviewAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    /// <inheritdoc />
    public Task<bool> OpenStoreReviewPageAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
}
