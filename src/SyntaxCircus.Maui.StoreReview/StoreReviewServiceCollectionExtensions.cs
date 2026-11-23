using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyntaxCircus.Maui.StoreReview;

/// <summary>DI registration for store review.</summary>
public static class StoreReviewServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IStoreReviewService"/> as a singleton (idempotent). Nothing touches a native API
    /// until one of its methods is called.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the store ids used for write-review links.</param>
    public static IServiceCollection AddStoreReview(this IServiceCollection services, Action<StoreReviewOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new StoreReviewOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

#if IOS
        services.TryAddSingleton<IStoreReviewService, AppleStoreReviewService>();
#elif ANDROID
        services.TryAddSingleton<IStoreReviewService, PlayStoreReviewService>();
#else
        services.TryAddSingleton<IStoreReviewService, UnavailableStoreReviewService>();
#endif

        return services;
    }
}
