using Statecraft.Sim;

/// <summary>#558: only the two fixed FX transition messages acquire optional presentation metadata.</summary>
public class FxCauseLanguageTests
{
    static readonly Content FixtureContent = ContentLoader.Load(Fixture.Write());

    // A reflected read lets these behavioral regressions execute against the unchanged baseline,
    // where the missing explicit metadata must be a runtime assertion failure, not a compile failure.
    static string Key(Cause cause) => typeof(Cause).GetProperty("MessageKey")?.GetValue(cause) as string ?? "";

    static List<Cause> Transition(bool eased)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(FixtureContent, s);
        s.Population["veterans"] = 1000;
        s.FxReserves = eased ? 20 : -1;
        if (eased) s.ActiveSituations.Add(EconomySystem.FxCrisis);
        var why = new List<Cause>();
        EconomySystem.Apply(FixtureContent, s, 0, why);
        Assert.Equal(!eased, s.ActiveSituations.Contains(EconomySystem.FxCrisis));
        return why;
    }

    [Theory]
    [InlineData(false, "cause.fx_crisis_started", "Foreign currency reserves are exhausted", 1)]
    [InlineData(true, "cause.fx_crisis_eased", "Foreign currency crisis eased", -1)]
    public void RealFxTransitionHasAnExplicitKeyAndPreservesEnglish(bool eased, string key, string english, double delta)
    {
        var why = Transition(eased);
        var cause = Assert.Single(why, w => w.Target == EconomySystem.FxCrisis);
        Assert.Equal("economy", cause.System);
        Assert.Equal(delta, cause.Delta);
        Assert.Equal(english, cause.Message);
        Assert.Equal("[economy] " + english, cause.ToString());
        Assert.All(why.Where(w => w != cause), w => Assert.Equal("", Key(w)));
        Assert.Equal(key, Key(cause));
    }

    [Fact]
    public void AnUnchangedCrisisEmitsNoTransition()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1) };
        EconomySystem.Init(FixtureContent, s);
        s.FxReserves = 5;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        var why = new List<Cause>();
        EconomySystem.Apply(FixtureContent, s, 0, why);
        Assert.Contains(EconomySystem.FxCrisis, s.ActiveSituations);
        Assert.DoesNotContain(why, w => w.Target == EconomySystem.FxCrisis);
        Assert.All(why, w => Assert.Equal("", Key(w)));
    }

    [Fact]
    public void ExistingConstructorAndReferenceIdentityKeepTheirBehavior()
    {
        var cause = new Cause("fixture", "target", 2, "a fixture message");
        var other = new Cause("fixture", "target", 2, "a fixture message");
        Assert.Equal("A fixture message", cause.Message);
        Assert.Equal("[fixture] A fixture message", cause.ToString());
        Assert.Equal("", Key(cause));
        Assert.NotEqual(cause, other);
        Assert.NotNull(typeof(Cause).GetConstructor(new[] { typeof(string), typeof(string), typeof(double), typeof(string) }));
    }

    [Fact]
    public void AnOpaqueUnknownKeyDoesNotChangeEnglishOrNumericFields()
    {
        var ctor = typeof(Cause).GetConstructor(new[] { typeof(string), typeof(string), typeof(double), typeof(string), typeof(string) });
        Assert.NotNull(ctor);
        var cause = Assert.IsType<Cause>(ctor.Invoke(new object[] { "fixture", "target", -3.0, "original fixture message", "future.unknown" }));
        Assert.Equal("fixture", cause.System);
        Assert.Equal("target", cause.Target);
        Assert.Equal(-3.0, cause.Delta);
        Assert.Equal("Original fixture message", cause.Message);
        Assert.Equal("[fixture] Original fixture message", cause.ToString());
        Assert.Equal("future.unknown", Key(cause));
    }

    [Fact]
    public void TheRealGoldenCommandsReplayTheSameReportsWithoutSavingPresentationKeys()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var game = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var reports = new List<(string system, string target, double delta, string english, string key)[]>();
        while (game.State.Date < GoldenRun.Until1952 && game.State.GameOver == null)
        {
            var result = game.Step(GoldenRun.Orders(c, game.State).ToArray());
            reports.Add(result.Why.Select(w => (w.System, w.Target, w.Delta, w.Message, Key(w))).ToArray());
        }
        Assert.Equal(GoldenRun.Hash1952, game.State.Hash());
        var saved = game.Save.Write();
        Assert.DoesNotContain("cause.fx_crisis", saved);
        var replay = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var commands = SaveGame.Parse(saved).Turns;
        Assert.Equal(reports.Count, commands.Count);
        for (var i = 0; i < commands.Count; i++)
        {
            var result = replay.Step(commands[i]);
            Assert.Equal(reports[i], result.Why.Select(w => (w.System, w.Target, w.Delta, w.Message, Key(w))).ToArray());
        }
        Assert.Equal(saved, replay.Save.Write());
        Assert.Equal(game.State.Hash(), replay.State.Hash());
        var loaded = GameSession.Load(c, saved, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(game.State.Hash(), loaded.State.Hash());
        Assert.Contains(reports.SelectMany(r => r), r => r.key == "cause.fx_crisis_started");
    }
}
