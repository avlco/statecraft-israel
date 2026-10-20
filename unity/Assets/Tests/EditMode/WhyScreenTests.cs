using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class WhyScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    [Test]
    public void EveryCauseOfTheLastTurnIsListedUnderItsSystem()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text);
        Assert.AreEqual("Nothing has happened yet: end the turn to see its causes.", ui.Q<Label>("why-empty").text);
        bridge.Queue(new SetMobilization(1.0));
        var r = bridge.EndTurn();
        Assert.AreEqual("15 May 1948 to 15 June 1948", ui.Q<Label>("why-turn").text);
        var lines = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        Assert.AreEqual(r.Why.Count, lines.Count);
        Assert.AreEqual("Immigration and population", ui.Q<Label>("why-group-population").text);
        Assert.IsTrue(lines.Any(l => l.text == "Reserve call-up set to 100%"));
        var groups = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-group").ToList().Select(l => l.name).ToList();
        Assert.Less(groups.IndexOf("why-group-security"), groups.IndexOf("why-group-economy"));
    }

    [Test]
    public void TheLinesAreTheSimsOwnMessagesInGroupOrder()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        new WhyPresenter(ui.Root, bridge, Text);
        var r = bridge.EndTurn();
        var expected = WhyPresenter.Order.SelectMany(sys => r.Why.Where(w => w.System == sys).Select(w => w.Message)).ToList();
        var shown = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Select(l => l.text).ToList();
        Assert.AreEqual(expected, shown);
    }

    [Test]
    public void EverySystemTheSimReportsHasAGroupName()
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 80 && g.State.GameOver == null; i++)
        {
            g.Step(HistoricalAI.Commands(C, g.State));
            foreach (var w in g.Last!.Why) seen.Add(w.System);
        }
        CollectionAssert.IsSubsetOf(seen, WhyPresenter.Order, "a system with no group would show its raw id to players");
        var text = Text;
        foreach (var sys in WhyPresenter.Order) Assert.DoesNotThrow(() => { var _ = text["why." + sys]; }, sys);
    }

    [Test]
    public void ADisposedPanelHearsNothing()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var why = new WhyPresenter(ui.Root, bridge, Text);
        why.Dispose();
        bridge.EndTurn();
        Assert.IsNotNull(ui.Root.Q<Label>("why-empty"), "still the empty note: the panel did not re-render");
    }

    static (UiTestKit ui, SimBridge bridge, WhyPresenter why) Open()
    {
        var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        return (ui, bridge, new WhyPresenter(ui.Root, bridge, Text));
    }

    [Test]
    public void ASystemWithoutAGroupShowsAsOtherAndLast()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var real = bridge.Session.Last!;
            var why2 = new System.Collections.Generic.List<Cause>(real.Why) { new Cause("zzz", "t", 0, "something odd happened") };
            why.Show(new StepResult(real.State, why2, real.Rejected));
            var rows = ui.Q<ScrollView>("why-rows");
            var headers = rows.Query<Label>(className: "why-group").ToList();
            Assert.AreEqual("why-group-zzz", headers.Last().name);
            Assert.AreEqual("Other", headers.Last().text);
            Assert.IsFalse(rows.Query<Label>().ToList().Any(l => l.text == "zzz"), "the raw id is never shown");
            Assert.AreEqual("Something odd happened", rows.Query<Label>(className: "why-line").ToList().Last().text);
        }
    }

    [Test]
    public void EverySystemNamedInTheSimSourcesHasAGroup()
    {
        var text = Text;
        var found = new System.Collections.Generic.SortedSet<string>();
        int literal = 0, all = 0;
        foreach (var f in System.IO.Directory.GetFiles(System.IO.Path.Combine(Paths.RepoRoot, "sim", "Statecraft.Sim"), "*.cs"))
        {
            var source = System.IO.File.ReadAllText(f);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, "new Cause\\(\"([a-z_]+)\""))
            { found.Add(m.Groups[1].Value); literal++; }
            all += System.Text.RegularExpressions.Regex.Matches(source, "new\\s+Cause\\s*\\(").Count;
        }
        Assert.AreEqual(all, literal, "every 'new Cause(' must name its system as a plain literal, or this scan cannot see it");
        Assert.Greater(found.Count, 5, "the scan found the sim's causes");
        foreach (var sys in found)
        {
            CollectionAssert.Contains(WhyPresenter.Order, sys, sys);
            Assert.DoesNotThrow(() => { var _ = text["why." + sys]; }, sys);
        }
    }

    [Test]
    public void ATurnWithNoCausesSaysSo()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var real = bridge.Session.Last!;
            why.Show(new StepResult(real.State, new Cause[0], real.Rejected));
            Assert.AreEqual("Nothing notable happened this turn.", ui.Q<Label>("why-nothing").text);
        }
    }

    [Test]
    public void ASecondTurnReplacesTheFirstAndAnUnchangedResultIsNotRebuilt()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            var first = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
            why.Render();
            Assert.AreSame(first[0], ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList()[0], "same result: no rebuild");
            var r = bridge.EndTurn();
            var second = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
            Assert.AreEqual(r.Why.Count, second.Count);
            Assert.AreNotSame(first[0], second[0]);
            Assert.AreEqual("15 June 1948 to 15 July 1948", ui.Q<Label>("why-turn").text);
        }
    }

    [Test]
    public void ReplacingTheSessionRerenders()
    {
        var (ui, bridge, why) = Open();
        using (ui)
        {
            bridge.EndTurn();
            Assert.IsNull(ui.Root.Q<Label>("why-empty"));
            var other = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            bridge.Replace(other);
            Assert.IsNotNull(ui.Root.Q<Label>("why-empty"), "a new game has no last turn");
            var loaded = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
            loaded.Step(new Command[0]);
            bridge.Replace(loaded);
            Assert.IsNull(ui.Root.Q<Label>("why-empty"));
            Assert.AreEqual(loaded.Last!.Why.Count, ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList().Count);
        }
    }

    /// <summary>#701: in the Hebrew UI the turn's span names its months in Hebrew (date.*), not "15 May 1948".</summary>
    [Test]
    public void InHebrewTheTurnsSpanIsAHebrewDate()
    {
        var he = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), he);
        new WhyPresenter(ui.Root, bridge, he);
        bridge.EndTurn();
        Assert.AreEqual("מ־15 במאי 1948 עד 15 ביוני 1948", ui.Q<Label>("why-turn").text);
    }

    /// <summary>Playtest 1, item 4: the effects group covers effects that are not policies too.</summary>
    [Test]
    public void TheEffectsGroupIsCalledOngoingEffects() => Assert.AreEqual("Ongoing effects", Text["why.effects"]);

    /// <summary>Playtest 1, item 5: every date the player sees goes through Statecraft.Sim.PlayerText ("15 May 1948") or,
    /// where the UI's language names the month, through the date.* strings (Control1948.DateText, #687, #701), so no UI
    /// string carries a date format of its own.</summary>
    [Test]
    public void NoUiStringFormatsADateItself()
    {
        var yaml = AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text;
        foreach (var line in yaml.Split('\n'))
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(line, "yyyy|MMM|MM-dd|:d "), "date format in strings.en.yaml: " + line);
    }

    // #558: these two Hebrew drafts remain agent-checked until a native speaker reviews them (#564).
    [TestCase(false, "en", "Foreign currency reserves are exhausted")]
    [TestCase(true, "en", "Foreign currency crisis eased")]
    [TestCase(false, "he", "יתרות מטבע החוץ אזלו")]
    [TestCase(true, "he", "משבר מטבע החוץ התמתן")]
    public void TheTwoRealFxTransitionsUseTheUiLanguageWithoutChangingTheReport(bool eased, string language, string expected)
    {
        var text = FxText(language);
        var game = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        game.State.FxReserves = eased ? C.RulesAt(game.State.Date).FxStart + 1 : -1;
        if (eased) game.State.ActiveSituations.Add(EconomySystem.FxCrisis);
        var causes = new System.Collections.Generic.List<Cause>();
        EconomySystem.Apply(C, game.State, 0, causes);
        var transition = causes.Single(w => w.Target == EconomySystem.FxCrisis);
        Assert.AreEqual(!eased, game.State.ActiveSituations.Contains(EconomySystem.FxCrisis));
        var original = causes.Select(w => w.Message).ToArray();
        var hash = game.State.Hash(); var save = game.Save.Write();
        using var ui = new UiTestKit();
        var bridge = new SimBridge(game, text);
        using var presenter = new WhyPresenter(ui.Root, bridge, text);
        presenter.Show(new StepResult(game.State, causes, new Rejection[0]));
        var shown = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        var wanted = causes.Select(w => w == transition ? expected : w.Message).ToArray();
        CollectionAssert.AreEqual(wanted, shown.Select(l => l.text).ToArray());
        Assert.AreEqual(text["why.economy"], ui.Q<Label>("why-group-economy").text);
        CollectionAssert.AreEqual(original, causes.Select(w => w.Message).ToArray(), "rendering must not rewrite the sim's English report");
        Assert.AreEqual(hash, game.State.Hash());
        Assert.AreEqual(save, game.Save.Write());
    }

    [TestCase("cause.fx_crisis_started", "יתרות מטבע החוץ אזלו")]
    [TestCase("cause.fx_crisis_eased", "משבר מטבע החוץ התמתן")]
    public void AnExplicitFxKeyChoosesTheHebrewLineWithoutInferringFromDeltaOrEnglish(string key, string expected)
    {
        var text = FxText("he", "cause.fx_crisis_started: \"יתרות מטבע החוץ אזלו\"\ncause.fx_crisis_eased: \"משבר מטבע החוץ התמתן\"\n", true);
        var cause = new Cause("economy", "unrelated-target", 91, "unrelated original message", key);
        Assert.AreEqual(expected, RenderFxCause(text, cause));
        Assert.AreEqual("Unrelated original message", cause.Message);
        Assert.AreEqual(91, cause.Delta);
    }

    [TestCase(1, "Foreign currency reserves are exhausted")]
    [TestCase(-1, "Foreign currency crisis eased")]
    public void MatchingFxFieldsOrEnglishWithoutAnExplicitKeyStayUntranslated(double delta, string english)
    {
        var text = FxText("he", "cause.fx_crisis_started: \"יתרות מטבע החוץ אזלו\"\ncause.fx_crisis_eased: \"משבר מטבע החוץ התמתן\"\n", true);
        Assert.AreEqual(english, RenderFxCause(text, new Cause("economy", EconomySystem.FxCrisis, delta, english)));
    }

    [TestCase("")]
    [TestCase("future.unknown")]
    [TestCase("cause.fx_crisis_started")]
    [TestCase("cause.fx_crisis_eased")]
    public void EmptyUnknownOrUntranslatedKeysKeepTheirEnglishFallback(string key)
    {
        // A foreign key is deliberately present: only the two recognized identities may be localized.
        var text = FxText("he", "future.unknown: \"Unexpected fixture translation\"\n", true);
        Assert.AreEqual("Original fallback", RenderFxCause(text, new Cause("economy", EconomySystem.FxCrisis, 1, "original fallback", key)));
    }

    [TestCase("cause.fx_crisis_started")]
    [TestCase("cause.fx_crisis_eased")]
    public void EnglishUsesTheOriginalMessageEvenIfAResourceDisagrees(string key)
    {
        var text = FxText("en", key + ": \"Unexpected fixture translation\"\n", true);
        Assert.AreEqual("Original English message", RenderFxCause(text, new Cause("economy", EconomySystem.FxCrisis, -7, "original English message", key)));
    }

    static UiText FxText(string language, string extra = "", bool removeFxKeys = false)
    {
        var yaml = AssetDatabase.LoadAssetAtPath<TextAsset>(language == "he" ? UiPaths.StringsHebrew : UiPaths.Strings).text;
        if (removeFxKeys) yaml = string.Join("\n", yaml.Split('\n').Where(l => !l.StartsWith("cause.fx_crisis_started:") && !l.StartsWith("cause.fx_crisis_eased:")));
        return UiText.Parse(yaml + "\n" + extra, "strings." + language + ".yaml");
    }

    static string RenderFxCause(UiText text, Cause cause)
    {
        var game = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        var hash = game.State.Hash(); var save = game.Save.Write();
        using var ui = new UiTestKit();
        var bridge = new SimBridge(game, text);
        using var presenter = new WhyPresenter(ui.Root, bridge, text);
        presenter.Show(new StepResult(game.State, new[] { cause }, new Rejection[0]));
        var shown = ui.Q<ScrollView>("why-rows").Query<Label>(className: "why-line").ToList();
        Assert.AreEqual(1, shown.Count);
        Assert.AreEqual(hash, game.State.Hash());
        Assert.AreEqual(save, game.Save.Write());
        return shown[0].text;
    }
}
