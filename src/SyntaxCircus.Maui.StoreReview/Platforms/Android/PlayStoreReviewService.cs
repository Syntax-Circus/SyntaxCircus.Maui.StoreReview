using Android.Gms.Tasks;
using Com.Google.Android.Play.Core.Review;
using Microsoft.Maui.ApplicationModel;
using AndroidTask = Android.Gms.Tasks.Task;

namespace SyntaxCircus.Maui.StoreReview;

/// <summary>
/// Google Play In-App Review. Nothing is created before a method is called. The flow's task results carry no
/// outcome (Play may show nothing because of quotas), so success only means the request was issued.
/// </summary>
internal sealed class PlayStoreReviewService(StoreReviewOptions options) : IStoreReviewService
{
    private readonly string? _marketUrl = StoreReviewUrls.AndroidMarket(options.AndroidPackageId);
    private readonly string? _webUrl = StoreReviewUrls.AndroidWeb(options.AndroidPackageId);

    /// <inheritdoc />
    public bool CanOpenStoreReviewPage => _webUrl is not null;

    /// <inheritdoc />
    public async System.Threading.Tasks.Task<bool> RequestInAppReviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (activity is null || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            using var manager = ReviewManagerFactory.Create(activity);
            if (manager is null)
            {
                return false;
            }

            var request = await Complete(manager.RequestReviewFlow(), cancellationToken).ConfigureAwait(true);
            if (request is not { IsSuccessful: true } || request.Result is not Java.Lang.Object raw)
            {
                return false;
            }

            using var info = raw.JavaCast<ReviewInfo>();
            var current = Platform.CurrentActivity;
            if (current is null || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var launch = await Complete(manager.LaunchReviewFlow(current, info), cancellationToken).ConfigureAwait(true);
            return launch is { IsSuccessful: true };
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async System.Threading.Tasks.Task<bool> OpenStoreReviewPageAsync(CancellationToken cancellationToken = default)
    {
        if (_marketUrl is null || _webUrl is null || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            if (await Launcher.Default.TryOpenAsync(new Uri(_marketUrl)).ConfigureAwait(true))
            {
                return true;
            }

            return await Launcher.Default.TryOpenAsync(new Uri(_webUrl)).ConfigureAwait(true);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async System.Threading.Tasks.Task<AndroidTask?> Complete(AndroidTask task, CancellationToken cancellationToken)
    {
        var source = new TaskCompletionSource<AndroidTask>(TaskCreationOptions.RunContinuationsAsynchronously);
        task.AddOnCompleteListener(new CompleteListener(source));
        return await source.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
    }

    private sealed class CompleteListener(TaskCompletionSource<AndroidTask> source) : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(AndroidTask task) => source.TrySetResult(task);
    }
}
