namespace SyntaxCircus.Maui.StoreReview;

/// <summary>Pure builders for store write-review URLs. Return null when the id is missing or malformed.</summary>
internal static class StoreReviewUrls
{
    /// <summary>Normalizes an App Store id to its digits (accepts an optional <c>id</c> prefix), or null.</summary>
    public static string? NormalizeAppleAppId(string? appleAppId)
    {
        var id = appleAppId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (id.StartsWith("id", StringComparison.OrdinalIgnoreCase))
        {
            id = id[2..];
        }

        return id.Length > 0 && id.All(char.IsAsciiDigit) ? id : null;
    }

    /// <summary>Returns a Java-style package id (dot separated identifiers) or null.</summary>
    public static string? NormalizeAndroidPackageId(string? packageId)
    {
        var id = packageId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var segments = id.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        foreach (var segment in segments)
        {
            if (segment.Length == 0 || !char.IsAsciiLetter(segment[0]) || !segment.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                return null;
            }
        }

        return id;
    }

    /// <summary>The App Store write-review page: <c>https://apps.apple.com/app/id{id}?action=write-review</c>.</summary>
    public static string? AppleWriteReview(string? appleAppId)
    {
        var id = NormalizeAppleAppId(appleAppId);
        return id is null ? null : $"https://apps.apple.com/app/id{id}?action=write-review";
    }

    /// <summary>The Play Store app page opened through the <c>market://</c> scheme.</summary>
    public static string? AndroidMarket(string? packageId)
    {
        var id = NormalizeAndroidPackageId(packageId);
        return id is null ? null : $"market://details?id={id}";
    }

    /// <summary>The Play Store web page, used when <c>market://</c> cannot be handled.</summary>
    public static string? AndroidWeb(string? packageId)
    {
        var id = NormalizeAndroidPackageId(packageId);
        return id is null ? null : $"https://play.google.com/store/apps/details?id={id}";
    }
}
