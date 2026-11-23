using Microsoft.Extensions.DependencyInjection;

namespace SyntaxCircus.Maui.StoreReview.Tests;

public sealed class ServiceTests
{
    [Fact]
    public async Task Unavailable_service_reports_false_everywhere()
    {
        var service = new UnavailableStoreReviewService();
        service.CanOpenStoreReviewPage.ShouldBeFalse();
        (await service.RequestInAppReviewAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await service.OpenStoreReviewPageAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public void AddStoreReview_registers_the_unavailable_service_on_plain_net()
    {
        using var provider = new ServiceCollection().AddStoreReview().BuildServiceProvider();
        provider.GetRequiredService<IStoreReviewService>().ShouldBeOfType<UnavailableStoreReviewService>();
        provider.GetRequiredService<IStoreReviewService>().ShouldBeSameAs(provider.GetRequiredService<IStoreReviewService>());
    }

    [Fact]
    public void AddStoreReview_applies_options_and_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddStoreReview(o =>
        {
            o.AppleAppId = "123";
            o.AndroidPackageId = "com.example.app";
        });
        services.AddStoreReview(o => o.AppleAppId = "999");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<StoreReviewOptions>();
        options.AppleAppId.ShouldBe("123");
        options.AndroidPackageId.ShouldBe("com.example.app");
        services.Count(d => d.ServiceType == typeof(IStoreReviewService)).ShouldBe(1);
    }

    [Fact]
    public void AddStoreReview_rejects_null_services()
    {
        Should.Throw<ArgumentNullException>(() => StoreReviewServiceCollectionExtensions.AddStoreReview(null!));
    }

    [Fact]
    public async Task Fake_records_calls_and_returns_configured_results()
    {
        var fake = new FakeStoreReviewService();
        fake.CanOpenStoreReviewPage.ShouldBeTrue();
        (await fake.RequestInAppReviewAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await fake.OpenStoreReviewPageAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();

        fake.CanOpenStoreReviewPageResult = false;
        fake.InAppReviewResult = false;
        fake.OpenPageResult = false;
        fake.CanOpenStoreReviewPage.ShouldBeFalse();
        (await fake.RequestInAppReviewAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await fake.RequestInAppReviewAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await fake.OpenStoreReviewPageAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();

        fake.InAppRequestCount.ShouldBe(3);
        fake.OpenPageCount.ShouldBe(2);
    }
}
