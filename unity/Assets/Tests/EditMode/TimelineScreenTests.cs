#nullable enable
using System;
using System.Linq;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>#955: the read-only view consumes the real sim helper, never pending/queued answers.</summary>
public class TimelineScreenTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText Text(bool hebrew = false) => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(hebrew ? UiPaths.StringsHebrew : UiPaths.Strings).text);
    static IDisposable Presenter(VisualElement root, SimBridge bridge, UiText text) => new TimelinePresenter(root, bridge, text);
    static GameSession Historical(int turns)
    {
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < turns; i++) g.Step(GoldenRun.Orders(C, g.State).ToArray());
        return g;
    }
    static VisualElement[] Rows(VisualElement root) => root.Query<VisualElement>(className: "timeline-row").ToList().ToArray();

    [Test]
    public void TheHudOpensTheTimelineAndCloseReturnsItsFocus()
    {
        using var ui = new UiTestKit();
        using var nav = new ScreenNav(ui.Root);
        var b = ui.Q<Button>("nav-timeline");
        UiTestKit.Press(b);
        Assert.AreEqual("timeline", nav.Current);
        Assert.IsTrue(ScreenNav.IsOpen(ui.Q<VisualElement>("screen-timeline")));
        var close = ui.Q<Button>("screen-close-timeline"); close.Focus(); UiTestKit.Press(close);
        Assert.IsNull(nav.Current);
        Assert.AreSame(b, ui.Root.panel.focusController.focusedElement);
    }

    [Test]
    public void OneRowPerResolvedEventAndNoMarkOnTheRecord()
    {
        using var ui = new UiTestKit();
        var g = Historical(6); var text = Text(); var bridge = new SimBridge(g, text);
        using var screen = Presenter(ui.Root, bridge, text);
        using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var entries = Timeline.Entries(C, g.State); Assert.Greater(entries.Count, 0, "real resolved history");
        CollectionAssert.AreEqual(entries.Select(e => e.Event), Rows(ui.Root).Select(r => r.userData));
        Assert.Zero(ui.Root.Query<VisualElement>(className: "timeline-diverged").ToList().Count);
        foreach (var row in Rows(ui.Root))
        {
            Assert.IsNotEmpty(row.Q<Label>("timeline-title").text);
            Assert.AreEqual(row.Q<Label>("timeline-yours").text, row.Q<Label>("timeline-record").text);
        }
    }

    [Test]
    public void ARoadNotTakenIsMarkedBesideTheRecord()
    {
        using var ui = new UiTestKit(); var text = Text();
        var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        // Force only a real pending event's alternative; all other answers use the reference player.
        for (int i = 0; !g.State.PendingEvents.Contains("idf_founding") && i < 2; i++) g.Step(Array.Empty<Command>());
        Assert.Contains("idf_founding", g.State.PendingEvents);
        var e = C.Events.Single(x => x.Id == "idf_founding"); var alternative = e.Options.First(o => !o.Historical);
        var orders = GoldenRun.Orders(C, g.State).Where(x => !(x is ChooseEventOption choice && choice.EventId == e.Id)).ToList();
        orders.Add(new ChooseEventOption(e.Id, alternative.Id)); g.Step(orders);
        var bridge = new SimBridge(g, text); using var screen = Presenter(ui.Root, bridge, text);
        using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var row = Rows(ui.Root).Single(r => (string)r.userData == e.Id);
        Assert.IsTrue(row.ClassListContains("timeline-diverged"));
        StringAssert.Contains(alternative.Text, row.Q<Label>("timeline-yours").text);
        StringAssert.Contains(e.Options.Single(o => o.Historical).Text, row.Q<Label>("timeline-record").text);
        Assert.AreNotEqual(row.Q<Label>("timeline-yours").text, row.Q<Label>("timeline-record").text);
    }

    [Test]
    public void PendingAndQueuedAnswersAreAbsentAndReadingChangesNoStateOrSave()
    {
        using var ui = new UiTestKit(); var text = Text(); var g = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        if (g.State.PendingEvents.Count == 0) g.Step(Array.Empty<Command>());
        var e = C.Events.Single(x => x.Id == g.State.PendingEvents.First());
        var bridge = new SimBridge(g, text); bridge.Queue(new ChooseEventOption(e.Id, e.Options.First().Id));
        Assert.AreEqual(1, bridge.Queued.Count);
        var state = g.State.Hash(); var save = g.Save.Write(); var queued = bridge.Queued.Single();
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root);
        nav.Open("timeline"); nav.Close(); nav.Open("timeline");
        Assert.IsFalse(Rows(ui.Root).Any(r => (string)r.userData == e.Id), "pending/queued is not resolved");
        Assert.AreEqual(state, g.State.Hash()); Assert.AreEqual(save, g.Save.Write()); Assert.AreSame(queued, bridge.Queued.Single());
    }

    [Test]
    public void NewTurnsRefreshRowsAndDisposedScreensStopListening()
    {
        using var ui = new UiTestKit(); var text = Text(); var bridge = new SimBridge(Historical(1), text);
        var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var before = Rows(ui.Root).Length;
        for (int i = 0; i < 6; i++) { foreach (var c in GoldenRun.Orders(C, bridge.State)) bridge.Queue(c); bridge.EndTurn(); }
        Assert.Greater(Rows(ui.Root).Length, before);
        CollectionAssert.AreEqual(Timeline.Entries(C, bridge.State).Select(e => e.Event), Rows(ui.Root).Select(r => r.userData));
        screen.Dispose(); var rows = Rows(ui.Root);
        bridge.EndTurn(); Assert.AreSame(rows[0], Rows(ui.Root)[0], "disposed presenter cannot rebuild the tree");
    }

    [Test]
    public void AFixtureRejoinKeepsBothTheOriginalRoadAndTheMissingRecordFallback([Values(false, true)] bool hebrew)
    {
        // UI-only fixture: no consequence card is added to shipped content and no historical attribution is invented.
        var c = ContentLoader.Load(Paths.Content); var g = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        c.Events.Add(new GameEvent { Id = "fixture_parent", Title = "Fixture parent", From = new DateTime(1948, 5, 15),
            Options = { new EventOption { Id = "record", Text = "Recorded answer", Historical = true }, new EventOption { Id = "road", Text = "Other answer" } } });
        c.Events.Add(new GameEvent { Id = "fixture_followup", Kind = EventKinds.Consequence, Title = "Fixture follow-up", From = new DateTime(1948, 5, 16),
            Options = { new EventOption { Id = "return", Text = "Return answer", Rejoin = "fixture_parent" } } });
        g.State.ChosenOptions["fixture_followup"] = "return"; g.State.ChosenOptions["fixture_parent"] = "road";
        using var ui = new UiTestKit(); var text = Text(hebrew); var bridge = new SimBridge(g, text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var rows = Rows(ui.Root); Assert.AreEqual(2, rows.Length);
        Assert.AreEqual("fixture_parent", rows[0].userData); Assert.AreEqual("fixture_followup", rows[1].userData);
        Assert.IsTrue(rows[0].ClassListContains("timeline-diverged"));
        Assert.AreEqual("Other answer", rows[0].Q<Label>("timeline-yours").text);
        Assert.AreEqual("Recorded answer", rows[0].Q<Label>("timeline-record").text);
        Assert.AreEqual(text["timeline.rejoined"], rows[0].Q<Label>("timeline-rejoined").text);
        Assert.AreEqual(text["timeline.no_record"], rows[1].Q<Label>("timeline-record").text);
        Assert.AreEqual(hebrew ? "15 במאי 1948" : "15 May 1948", rows[0].Q<Label>("timeline-date").text);
    }

    [Test]
    public void ReopeningAndQueuingKeepTheSameRowsButReplacingTheSessionRefreshesThem()
    {
        using var ui = new UiTestKit(); var text = Text(); var bridge = new SimBridge(Historical(6), text);
        using var screen = Presenter(ui.Root, bridge, text); using var nav = new ScreenNav(ui.Root); nav.Open("timeline");
        var first = Rows(ui.Root)[0]; nav.Close(); nav.Open("timeline");
        Assert.AreSame(first, Rows(ui.Root)[0], "reading again must not reset the scroll tree");
        bridge.Queue(new SetMobilization(1.0)); Assert.AreSame(first, Rows(ui.Root)[0], "an unrelated queued order cannot rebuild history");
        bridge.Replace(GameSession.New(C, GoldenRun.Seed, Mode.Historical));
        Assert.IsEmpty(Rows(ui.Root), "a new game cannot retain the previous session's history");
    }

    [Test]
    public void LanguageRebuildUsesNewHeadingsAndTheExistingEventText()
    {
        using var ui = new UiTestKit(); var en = Text(); var he = Text(true); var bridge = new SimBridge(Historical(6), en);
        using var nav = new ScreenNav(ui.Root);
        var old = Presenter(ui.Root, bridge, en); nav.Open("timeline"); old.Dispose();
        using var current = Presenter(ui.Root, bridge, he); nav.Open("timeline");
        Assert.AreEqual(he["timeline.title"], ui.Q<Label>("timeline-screen-title").text);
        Assert.AreEqual(he["timeline.yours"], ui.Q<Label>("timeline-yours-heading").text);
        Assert.AreEqual(he["timeline.record"], ui.Q<Label>("timeline-record-heading").text);
        Assert.IsNotEmpty(Rows(ui.Root)); Assert.IsNotEmpty(Rows(ui.Root)[0].Q<Label>("timeline-yours").text);
    }

    [UnityTest]
    public IEnumerator ThePairedAnswersFitLaptopAndPhoneAndPageKeysReachTheLastRow()
    {
        foreach (var hebrew in new[] { false, true })
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(390, 844) })
            {
                var rt = new RenderTexture(size.x, size.y, 0);
                var ps = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
                var go = new GameObject("TimelineLayoutTest"); go.SetActive(false);
                var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
                var root = doc.rootVisualElement; root.Q("root").RemoveFromClassList("loading");
                if (hebrew) root.Q("root").AddToClassList(UiLanguage.HebrewClass);
                var text = Text(hebrew); var bridge = new SimBridge(Historical(12), text);
                var hud = new HudPresenter(root, bridge, text);
                var screen = Presenter(root, bridge, text);
                var nav = new ScreenNav(root); nav.Open("timeline");
                var direction = new UiDirection(root, bridge);
                try
                {
                    for (int i = 0; i < 12; i++) yield return null;
                    var panel = root.Q("screen-timeline"); var window = root.worldBound;
                    Assert.Greater(panel.worldBound.width, 0); Assert.Greater(panel.worldBound.height, 0);
                    Assert.GreaterOrEqual(panel.worldBound.xMin, window.xMin); Assert.LessOrEqual(panel.worldBound.xMax, window.xMax);
                    Assert.GreaterOrEqual(panel.worldBound.yMin, window.yMin); Assert.LessOrEqual(panel.worldBound.yMax, window.yMax);
                    var view = root.Q<ScrollView>("timeline-list"); Assert.Greater(view.contentViewport.worldBound.height, 0);
                    foreach (var row in Rows(root))
                    {
                        var yours = row.Q<Label>("timeline-yours"); var record = row.Q<Label>("timeline-record");
                        Assert.AreEqual(hebrew, yours.worldBound.center.x > record.worldBound.center.x, "columns follow the interface language");
                        foreach (var label in row.Query<Label>().ToList())
                        {
                            Assert.IsNotEmpty(label.text, label.name);
                            Assert.Greater(label.contentRect.width, 0, label.name);
                            Assert.GreaterOrEqual(label.worldBound.xMin, view.contentViewport.worldBound.xMin - 1f, label.name);
                            Assert.LessOrEqual(label.worldBound.xMax, view.contentViewport.worldBound.xMax + 1f, label.name);
                            var measured = label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                            Assert.LessOrEqual(measured.y, label.contentRect.height + 1f, label.name + " text must fit rather than clip");
                        }
                    }
                    var close = root.Q<Button>("screen-close-timeline"); close.Focus();
                    Assert.Greater(view.verticalScroller.highValue, 0, "real history must overflow this reading screen");
                    Assert.IsTrue(KeyScroll.Press(root, "timeline", false, KeyScroll.Key.End));
                    Assert.AreEqual(view.verticalScroller.highValue, view.scrollOffset.y, 0.5f);
                    Assert.AreSame(close, root.panel.focusController.focusedElement, "scroll keys never lose the focused Close button");
                    Assert.IsTrue(KeyScroll.Press(root, "timeline", false, KeyScroll.Key.Home));
                    Assert.AreEqual(0f, view.scrollOffset.y, 0.5f);
                }
                finally
                {
                    nav.Dispose(); screen.Dispose(); hud.Dispose(); direction.Dispose();
                    UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(ps); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                }
            }
    }
}
