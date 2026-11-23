namespace SyntaxCircus.Maui.StoreReview.Tests;

public sealed class ReviewPromptPolicyTests
{
    private static readonly DateTime s_install = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime s_now = s_install.AddDays(30);

    private static ReviewPromptState Fresh(int sessions = 5) => new(s_install, sessions, null, null, 0);

    private static ReviewPromptState Asked(string? version = "1.0.0", int count = 1, DateTime? at = null, int sessions = 5) =>
        new(s_install, sessions, at ?? s_install.AddDays(10), version, count);

    [Fact]
    public void Due_when_first_ask_thresholds_are_met()
    {
        ReviewPromptPolicy.IsDue(Fresh(), s_now, "1.0.0").ShouldBeTrue();
    }

    [Fact]
    public void Not_due_without_install_date()
    {
        ReviewPromptPolicy.IsDue(new ReviewPromptState(null, 10, null, null, 0), s_now, "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Not_due_when_clock_went_backwards()
    {
        ReviewPromptPolicy.IsDue(Fresh(), s_install.AddDays(-1), "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Not_due_when_asked_in_the_future()
    {
        var state = Fresh() with { AskedUtc = s_now.AddMinutes(1) };
        ReviewPromptPolicy.IsDue(state, s_now, "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Install_age_boundary_is_inclusive()
    {
        ReviewPromptPolicy.IsDue(Fresh(), s_install.AddDays(7), "1.0.0").ShouldBeTrue();
        ReviewPromptPolicy.IsDue(Fresh(), s_install.AddDays(7).AddTicks(-1), "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Session_boundary_is_inclusive()
    {
        ReviewPromptPolicy.IsDue(Fresh(3), s_now, "1.0.0").ShouldBeTrue();
        ReviewPromptPolicy.IsDue(Fresh(2), s_now, "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Negative_session_count_is_not_due()
    {
        ReviewPromptPolicy.IsDue(Fresh(-5), s_now, "1.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Custom_settings_are_honored()
    {
        var settings = new ReviewPromptSettings { MinInstallAge = TimeSpan.FromDays(1), MinSessions = 1 };
        ReviewPromptPolicy.IsDue(Fresh(1), s_install.AddDays(1), "1.0.0", settings).ShouldBeTrue();
        ReviewPromptPolicy.IsDue(Fresh(0), s_install.AddDays(1), "1.0.0", settings).ShouldBeFalse();
    }

    [Fact]
    public void Defaults_match_the_documented_values()
    {
        var settings = new ReviewPromptSettings();
        settings.MinInstallAge.ShouldBe(TimeSpan.FromDays(7));
        settings.MinSessions.ShouldBe(3);
        settings.MaxAsks.ShouldBe(2);
        settings.RetryAfter.ShouldBe(TimeSpan.FromDays(90));
    }

    [Fact]
    public void First_ask_ignores_version_text()
    {
        ReviewPromptPolicy.IsDue(Fresh(), s_now, "garbage").ShouldBeTrue();
        ReviewPromptPolicy.IsDue(Fresh(), s_now, "").ShouldBeTrue();
    }

    [Fact]
    public void Retry_needs_a_new_major_version_and_enough_time()
    {
        var now = s_install.AddDays(10 + 90);
        ReviewPromptPolicy.IsDue(Asked("1.4.0"), now, "2.0.0").ShouldBeTrue();
    }

    [Fact]
    public void Retry_not_due_for_same_major()
    {
        var now = s_install.AddDays(400);
        ReviewPromptPolicy.IsDue(Asked("1.4.0"), now, "1.9.9").ShouldBeFalse();
    }

    [Fact]
    public void Retry_not_due_before_retry_window()
    {
        var now = s_install.AddDays(10 + 90).AddTicks(-1);
        ReviewPromptPolicy.IsDue(Asked("1.4.0"), now, "2.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Retry_window_boundary_is_inclusive()
    {
        ReviewPromptPolicy.IsDue(Asked("1.0.0"), s_install.AddDays(100), "2.0").ShouldBeTrue();
    }

    [Fact]
    public void Retry_stops_at_max_asks()
    {
        var now = s_install.AddDays(1000);
        ReviewPromptPolicy.IsDue(Asked("1.0.0", count: 2), now, "3.0.0").ShouldBeFalse();
        ReviewPromptPolicy.IsDue(Asked("1.0.0", count: 5), now, "3.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Max_asks_is_configurable()
    {
        var settings = new ReviewPromptSettings { MaxAsks = 3 };
        var now = s_install.AddDays(1000);
        ReviewPromptPolicy.IsDue(Asked("1.0.0", count: 2), now, "3.0.0", settings).ShouldBeTrue();
        ReviewPromptPolicy.IsDue(Asked("1.0.0", count: 3), now, "4.0.0", settings).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("-1.0")]
    [InlineData(".5")]
    public void Retry_not_due_when_asked_version_is_unparseable(string? askedVersion)
    {
        ReviewPromptPolicy.IsDue(Asked(askedVersion), s_install.AddDays(1000), "2.0.0").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-2.0")]
    public void Retry_not_due_when_current_version_is_unparseable(string current)
    {
        ReviewPromptPolicy.IsDue(Asked("1.0.0"), s_install.AddDays(1000), current).ShouldBeFalse();
    }

    [Fact]
    public void Retry_not_due_when_current_version_is_null()
    {
        ReviewPromptPolicy.IsDue(Asked("1.0.0"), s_install.AddDays(1000), null!).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1", "2.0.0", true)]
    [InlineData("v1.2", "v2.0", true)]
    [InlineData("1.2.3-beta", "2.0.0-rc.1", true)]
    [InlineData("1.2.3+build5", "1.9", false)]
    [InlineData("10.0", "9.0", true)]
    [InlineData("1.0", "01.5", false)]
    [InlineData("99999999999.0", "1.0", false)]
    public void Major_version_comparison_handles_common_shapes(string asked, string current, bool expected)
    {
        ReviewPromptPolicy.IsDue(Asked(asked), s_install.AddDays(1000), current).ShouldBe(expected);
    }

    [Fact]
    public void Retry_not_due_when_asked_date_is_missing()
    {
        var state = new ReviewPromptState(s_install, 5, null, "1.0.0", 1);
        ReviewPromptPolicy.IsDue(state, s_install.AddDays(1000), "2.0.0").ShouldBeFalse();
    }

    [Fact]
    public void Negative_ask_count_is_treated_as_never_asked()
    {
        var state = Fresh() with { AskCount = -1 };
        ReviewPromptPolicy.IsDue(state, s_now, "1.0.0").ShouldBeTrue();
    }

    [Fact]
    public void Extreme_dates_do_not_throw()
    {
        var state = new ReviewPromptState(DateTime.MinValue, int.MaxValue, DateTime.MinValue, "1", 1);
        Should.NotThrow(() => ReviewPromptPolicy.IsDue(state, DateTime.MaxValue, "2"));
        ReviewPromptPolicy.IsDue(state, DateTime.MaxValue, "2").ShouldBeTrue();
        Should.NotThrow(() => ReviewPromptPolicy.IsDue(new ReviewPromptState(DateTime.MaxValue, 0, null, null, 0), DateTime.MinValue, "1"));
    }

    [Fact]
    public void RecordSession_sets_install_time_once_and_counts()
    {
        var first = ReviewPromptPolicy.RecordSession(default, s_install);
        first.InstallUtc.ShouldBe(s_install);
        first.SessionCount.ShouldBe(1);

        var second = ReviewPromptPolicy.RecordSession(first, s_now);
        second.InstallUtc.ShouldBe(s_install);
        second.SessionCount.ShouldBe(2);
    }

    [Fact]
    public void RecordSession_preserves_other_fields()
    {
        var state = Asked("1.0.0", 1);
        var next = ReviewPromptPolicy.RecordSession(state, s_now);
        next.AskedUtc.ShouldBe(state.AskedUtc);
        next.AskedVersion.ShouldBe("1.0.0");
        next.AskCount.ShouldBe(1);
    }

    [Fact]
    public void RecordSession_saturates_and_repairs_negative_counts()
    {
        ReviewPromptPolicy.RecordSession(Fresh(int.MaxValue), s_now).SessionCount.ShouldBe(int.MaxValue);
        ReviewPromptPolicy.RecordSession(Fresh(-10), s_now).SessionCount.ShouldBe(1);
    }

    [Fact]
    public void MarkAsked_records_time_version_and_count()
    {
        var next = ReviewPromptPolicy.MarkAsked(Fresh(), s_now, "2.1.0");
        next.AskedUtc.ShouldBe(s_now);
        next.AskedVersion.ShouldBe("2.1.0");
        next.AskCount.ShouldBe(1);
        next.InstallUtc.ShouldBe(s_install);
        next.SessionCount.ShouldBe(5);

        ReviewPromptPolicy.MarkAsked(next, s_now, "3.0.0").AskCount.ShouldBe(2);
    }

    [Fact]
    public void MarkAsked_saturates_count()
    {
        ReviewPromptPolicy.MarkAsked(Fresh() with { AskCount = int.MaxValue }, s_now, "1").AskCount.ShouldBe(int.MaxValue);
        ReviewPromptPolicy.MarkAsked(Fresh() with { AskCount = -4 }, s_now, "1").AskCount.ShouldBe(1);
    }

    [Fact]
    public void Full_lifecycle_asks_twice_then_stops()
    {
        var state = default(ReviewPromptState);
        for (var i = 0; i < 3; i++)
        {
            state = ReviewPromptPolicy.RecordSession(state, s_install.AddDays(i));
        }

        ReviewPromptPolicy.IsDue(state, s_install.AddDays(3), "1.0.0").ShouldBeFalse();

        var firstAsk = s_install.AddDays(8);
        ReviewPromptPolicy.IsDue(state, firstAsk, "1.0.0").ShouldBeTrue();
        state = ReviewPromptPolicy.MarkAsked(state, firstAsk, "1.0.0");
        ReviewPromptPolicy.IsDue(state, firstAsk.AddDays(1), "1.0.0").ShouldBeFalse();

        var later = firstAsk.AddDays(91);
        ReviewPromptPolicy.IsDue(state, later, "1.5.0").ShouldBeFalse();
        ReviewPromptPolicy.IsDue(state, later, "2.0.0").ShouldBeTrue();
        state = ReviewPromptPolicy.MarkAsked(state, later, "2.0.0");

        ReviewPromptPolicy.IsDue(state, later.AddDays(1000), "9.0.0").ShouldBeFalse();
    }
}
