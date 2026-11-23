# SyntaxCircus.Maui.StoreReview

[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Maui.StoreReview.svg)](https://www.nuget.org/packages/SyntaxCircus.Maui.StoreReview)

In-app review requests and store write-review links for .NET MAUI apps, plus a small pure policy for deciding when asking is appropriate. iOS uses `SKStoreReviewController`; Android uses Google Play In-App Review. No UI, persistence or app copy, and nothing is called at construction or registration.

## Install

```
dotnet add package SyntaxCircus.Maui.StoreReview
```

Targets `net10.0` (core only; the service reports false for everything), `net10.0-android` (API 24+) and `net10.0-ios` (iOS 15+).

## Usage

```csharp
// MauiProgram.cs
builder.Services.AddStoreReview(o =>
{
    o.AppleAppId = "1234567890";          // numeric App Store id (only needed for the write-review link)
    o.AndroidPackageId = "com.example.app"; // Play package id (only needed for the write-review link)
});

public sealed class ReviewAsker(IStoreReviewService review)
{
    public async Task AskAsync()
    {
        // Native dialog. The OS may show nothing (quotas); true only means the request was issued.
        if (!await review.RequestInAppReviewAsync())
        {
            return;
        }
    }

    // For a permanent "Rate this app" row in Settings, which is always user initiated.
    public Task RateAsync() => review.CanOpenStoreReviewPage ? review.OpenStoreReviewPageAsync() : Task.FromResult(false);
}
```

Every method returns false instead of throwing. Opening the review page uses `https://apps.apple.com/app/id{id}?action=write-review` on iOS and `market://details?id={package}` with an `https://play.google.com/store/apps/details?id={package}` fallback on Android.

## Deciding when to ask

`ReviewPromptPolicy` is pure and has no MAUI types. You persist a `ReviewPromptState` (for example in `Preferences`) and the policy answers `IsDue`.

```csharp
var state = Load();
state = ReviewPromptPolicy.RecordSession(state, DateTime.UtcNow);   // once per app launch
Save(state);

if (ReviewPromptPolicy.IsDue(state, DateTime.UtcNow, AppInfo.Current.VersionString))
{
    if (await review.RequestInAppReviewAsync())
    {
        Save(ReviewPromptPolicy.MarkAsked(state, DateTime.UtcNow, AppInfo.Current.VersionString));
    }
}
```

Rules (defaults from `ReviewPromptSettings`: 7 days, 3 sessions, 2 asks, 90 days):

- Never due without an install date, if the clock went backwards, or if the last ask is in the future.
- Never due until the app is at least `MinInstallAge` old and has `MinSessions` recorded sessions.
- With no earlier ask, it is due.
- A retry is due only when fewer than `MaxAsks` asks were made, the major version differs from the version at the last ask, and at least `RetryAfter` has passed. Unparseable versions are never due.
- Bad input never throws; it just answers false.

Pick a calm moment (after a success, not mid-task or at launch). There is deliberately no "Do you enjoy this app?" pre-prompt gate: Apple's guidelines forbid filtering who gets the native dialog, and this package should not be used to nag. Ask rarely and let the OS decide whether to show anything.

## Testing

`FakeStoreReviewService` records call counts and returns configured results, for tests of code that depends on `IStoreReviewService`.
