namespace SyntaxCircus.Maui.StoreReview;

/// <summary>Store identifiers used to build write-review links. Both are optional; the in-app prompt needs neither.</summary>
public sealed class StoreReviewOptions
{
    /// <summary>The numeric App Store id (for example <c>1234567890</c>; an <c>id</c> prefix is tolerated).</summary>
    public string? AppleAppId { get; set; }

    /// <summary>The Google Play package id (for example <c>com.example.app</c>).</summary>
    public string? AndroidPackageId { get; set; }
}
