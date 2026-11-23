namespace SyntaxCircus.Maui.StoreReview.Tests;

public sealed class StoreReviewUrlsTests
{
    [Theory]
    [InlineData("1234567890", "https://apps.apple.com/app/id1234567890?action=write-review")]
    [InlineData(" 1234567890 ", "https://apps.apple.com/app/id1234567890?action=write-review")]
    [InlineData("id1234567890", "https://apps.apple.com/app/id1234567890?action=write-review")]
    [InlineData("ID42", "https://apps.apple.com/app/id42?action=write-review")]
    public void Apple_write_review_url_is_built(string id, string expected)
    {
        StoreReviewUrls.AppleWriteReview(id).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("id")]
    [InlineData("12a4")]
    [InlineData("123/456")]
    [InlineData("123?x=1")]
    [InlineData("-1")]
    public void Apple_write_review_url_rejects_bad_ids(string? id)
    {
        StoreReviewUrls.AppleWriteReview(id).ShouldBeNull();
    }

    [Fact]
    public void Android_urls_are_built()
    {
        StoreReviewUrls.AndroidMarket("com.example.app_1").ShouldBe("market://details?id=com.example.app_1");
        StoreReviewUrls.AndroidWeb("com.example.app_1").ShouldBe("https://play.google.com/store/apps/details?id=com.example.app_1");
        StoreReviewUrls.AndroidWeb("  com.example.app ").ShouldBe("https://play.google.com/store/apps/details?id=com.example.app");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("nodots")]
    [InlineData("com..double")]
    [InlineData(".leading.dot")]
    [InlineData("trailing.dot.")]
    [InlineData("com.1starts.digit")]
    [InlineData("com.exa mple.app")]
    [InlineData("com.example.app&x=1")]
    [InlineData("com.example.app/../x")]
    public void Android_urls_reject_bad_package_ids(string? id)
    {
        StoreReviewUrls.AndroidMarket(id).ShouldBeNull();
        StoreReviewUrls.AndroidWeb(id).ShouldBeNull();
    }
}
