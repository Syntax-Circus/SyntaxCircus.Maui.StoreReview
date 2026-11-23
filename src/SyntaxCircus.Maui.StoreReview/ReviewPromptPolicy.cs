namespace SyntaxCircus.Maui.StoreReview;

/// <summary>
/// Persisted facts the policy needs. The app stores these (for example in Preferences) and passes them in.
/// </summary>
/// <param name="InstallUtc">When the app was first run, or null if not recorded yet.</param>
/// <param name="SessionCount">Number of app sessions recorded.</param>
/// <param name="AskedUtc">When a review was last requested, or null.</param>
/// <param name="AskedVersion">App version at the last request, or null.</param>
/// <param name="AskCount">How many times a review has been requested.</param>
public readonly record struct ReviewPromptState(DateTime? InstallUtc, int SessionCount, DateTime? AskedUtc, string? AskedVersion, int AskCount);

/// <summary>Thresholds for <see cref="ReviewPromptPolicy.IsDue"/>.</summary>
public sealed record ReviewPromptSettings
{
    /// <summary>Minimum time since the first run before the first ask.</summary>
    public TimeSpan MinInstallAge { get; init; } = TimeSpan.FromDays(7);

    /// <summary>Minimum recorded sessions before the first ask.</summary>
    public int MinSessions { get; init; } = 3;

    /// <summary>Total number of asks allowed over the life of the install.</summary>
    public int MaxAsks { get; init; } = 2;

    /// <summary>Minimum time between a request and the next one.</summary>
    public TimeSpan RetryAfter { get; init; } = TimeSpan.FromDays(90);
}

/// <summary>
/// Pure, platform-free rules for when asking for a review is appropriate. Never throws on odd input.
/// </summary>
public static class ReviewPromptPolicy
{
    /// <summary>Returns true when the app may request a review now.</summary>
    /// <param name="state">The persisted state.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="currentVersion">The running app version.</param>
    /// <param name="settings">Thresholds; defaults are used when null.</param>
    public static bool IsDue(ReviewPromptState state, DateTime nowUtc, string currentVersion, ReviewPromptSettings? settings = null)
    {
        settings ??= new ReviewPromptSettings();

        if (state.InstallUtc is not { } install || nowUtc < install)
        {
            return false;
        }

        if (state.AskedUtc is { } askedEarly && askedEarly > nowUtc)
        {
            return false;
        }

        if (nowUtc - install < settings.MinInstallAge || state.SessionCount < settings.MinSessions)
        {
            return false;
        }

        if (state.AskCount <= 0)
        {
            return true;
        }

        if (state.AskCount >= settings.MaxAsks || state.AskedUtc is not { } asked)
        {
            return false;
        }

        if (!TryGetMajor(currentVersion, out var currentMajor) || !TryGetMajor(state.AskedVersion, out var askedMajor))
        {
            return false;
        }

        return currentMajor != askedMajor && nowUtc - asked >= settings.RetryAfter;
    }

    /// <summary>Records an app session: sets the install time on first use and increments the count.</summary>
    /// <param name="state">The persisted state.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public static ReviewPromptState RecordSession(ReviewPromptState state, DateTime nowUtc) =>
        state with
        {
            InstallUtc = state.InstallUtc ?? nowUtc,
            SessionCount = state.SessionCount == int.MaxValue ? int.MaxValue : Math.Max(state.SessionCount, 0) + 1,
        };

    /// <summary>Records that a review was requested now, at <paramref name="currentVersion"/>.</summary>
    /// <param name="state">The persisted state.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="currentVersion">The running app version.</param>
    public static ReviewPromptState MarkAsked(ReviewPromptState state, DateTime nowUtc, string currentVersion) =>
        state with
        {
            AskedUtc = nowUtc,
            AskedVersion = currentVersion,
            AskCount = state.AskCount == int.MaxValue ? int.MaxValue : Math.Max(state.AskCount, 0) + 1,
        };

    // Accepts "3", "3.1", "v3.1.2", "3.1.2-beta" and the like: only the leading digits of the first segment count.
    private static bool TryGetMajor(string? version, out int major)
    {
        major = 0;
        var text = version?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text[0] is 'v' or 'V')
        {
            text = text[1..];
        }

        var length = 0;
        while (length < text.Length && char.IsAsciiDigit(text[length]))
        {
            length++;
        }

        return length > 0 && int.TryParse(text.AsSpan(0, length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out major);
    }
}
