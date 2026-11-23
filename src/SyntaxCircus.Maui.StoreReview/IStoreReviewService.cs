namespace SyntaxCircus.Maui.StoreReview;

/// <summary>Asks for a store review, either with the native in-app dialog or by opening the store's review page.</summary>
public interface IStoreReviewService
{
    /// <summary>True when a store write-review URL can be built for this platform (ids configured).</summary>
    bool CanOpenStoreReviewPage { get; }

    /// <summary>
    /// Shows the native in-app review dialog if the OS allows. Never throws (returns false on any
    /// failure or when unsupported). The OS gives no outcome signal; true means "the request was issued".
    /// </summary>
    /// <param name="cancellationToken">Cancels waiting for the request to be issued.</param>
    Task<bool> RequestInAppReviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the store's write-review page. Never throws; false if the ids are missing or the launch failed.
    /// </summary>
    /// <param name="cancellationToken">Cancels before the launch is attempted.</param>
    Task<bool> OpenStoreReviewPageAsync(CancellationToken cancellationToken = default);
}
