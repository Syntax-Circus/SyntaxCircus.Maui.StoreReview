namespace SyntaxCircus.Maui.StoreReview;

/// <summary>
/// Test double that records calls and returns configured results. Public so consuming apps' tests can use it.
/// </summary>
public sealed class FakeStoreReviewService : IStoreReviewService
{
    private int _inAppRequestCount;
    private int _openPageCount;

    /// <summary>Value reported by <see cref="CanOpenStoreReviewPage"/>. Defaults to true.</summary>
    public bool CanOpenStoreReviewPageResult { get; set; } = true;

    /// <summary>Result returned by <see cref="RequestInAppReviewAsync"/>. Defaults to true.</summary>
    public bool InAppReviewResult { get; set; } = true;

    /// <summary>Result returned by <see cref="OpenStoreReviewPageAsync"/>. Defaults to true.</summary>
    public bool OpenPageResult { get; set; } = true;

    /// <summary>Number of <see cref="RequestInAppReviewAsync"/> calls so far.</summary>
    public int InAppRequestCount => Volatile.Read(ref _inAppRequestCount);

    /// <summary>Number of <see cref="OpenStoreReviewPageAsync"/> calls so far.</summary>
    public int OpenPageCount => Volatile.Read(ref _openPageCount);

    /// <inheritdoc />
    public bool CanOpenStoreReviewPage => CanOpenStoreReviewPageResult;

    /// <inheritdoc />
    public Task<bool> RequestInAppReviewAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _inAppRequestCount);
        return Task.FromResult(InAppReviewResult);
    }

    /// <inheritdoc />
    public Task<bool> OpenStoreReviewPageAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _openPageCount);
        return Task.FromResult(OpenPageResult);
    }
}
