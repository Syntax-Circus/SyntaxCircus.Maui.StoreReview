using Microsoft.Maui.ApplicationModel;
using StoreKit;
using UIKit;

namespace SyntaxCircus.Maui.StoreReview;

/// <summary>
/// SKStoreReviewController on the foreground-active window scene. iOS decides whether anything is shown
/// (it throttles the dialog) and reports nothing back, so true only means the request was issued.
/// </summary>
internal sealed class AppleStoreReviewService(StoreReviewOptions options) : IStoreReviewService
{
    private readonly string? _writeReviewUrl = StoreReviewUrls.AppleWriteReview(options.AppleAppId);

    /// <inheritdoc />
    public bool CanOpenStoreReviewPage => _writeReviewUrl is not null;

    /// <inheritdoc />
    public async Task<bool> RequestInAppReviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var scene = UIApplication.SharedApplication.ConnectedScenes
                    .OfType<UIWindowScene>()
                    .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);
                if (scene is null)
                {
                    return false;
                }

                SKStoreReviewController.RequestReview(scene);
                return true;
            }).ConfigureAwait(true);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenStoreReviewPageAsync(CancellationToken cancellationToken = default)
    {
        if (_writeReviewUrl is null || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            return await Launcher.Default.TryOpenAsync(new Uri(_writeReviewUrl)).ConfigureAwait(true);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
