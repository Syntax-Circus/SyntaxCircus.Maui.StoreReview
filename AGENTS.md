# Store review library (MAUI)

Use feature branches in this checkout, never worktrees.

Library rules:

- No app-specific copy, UI, persistence or prompt wording in the library. The app owns where `ReviewPromptState` is stored and when it calls the service.
- Platform APIs are called directly (`SKStoreReviewController`, Play In-App Review). No custom native bindings and no third-party review plugin.
- Native APIs are touched only when a service method is called, never at construction or DI registration.
- Service methods never throw: any failure or unsupported platform returns false. The OS reports no outcome for the in-app dialog, so true means only "the request was issued".
- `ReviewPromptPolicy` stays pure (no MAUI or platform types, no clock access, never throws on garbage input).
- The library never gates the native dialog behind a "Do you like this app?" pre-prompt; that is against Apple's guidelines. Keep that out of docs and APIs.
- Android Java dependency note: `Xamarin.Google.Android.Play.Review` 2.0.2.x drags in `Xamarin.GooglePlayServices.Basement` 118.10, which forces AndroidX versions that conflict with `Microsoft.Maui.Controls` (NU1608 as error). 2.0.1.11 is pinned for that reason; re-check when MAUI's AndroidX floor moves.
- Tests: `dotnet test --project tests/SyntaxCircus.Maui.StoreReview.Tests` (net10.0 core only; the iOS and Android services need a device). iOS builds need a Mac. Allman braces, TreatWarningsAsErrors, XML docs on public API.
