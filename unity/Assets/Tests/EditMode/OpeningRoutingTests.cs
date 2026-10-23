#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class OpeningRoutingTests
{
    static GameInputs? inputs;
    static GameInputs Inputs => inputs ??= GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
    readonly List<string> directories = new List<string>();
    readonly List<(PanelSettings panel, RenderTexture texture)> panels = new List<(PanelSettings, RenderTexture)>();
    static VisualElement Ui => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    static bool StartOpen => Ui.Q("root").ClassListContains(StartScreenPresenter.OpenClass);
    GameRoot Open(bool saved, out SaveStore store)
    {
        var dir = Path.Combine(Path.GetTempPath(), "statecraft-opening-routing-" + Guid.NewGuid().ToString("N"));
        directories.Add(dir); store = new SaveStore(dir);
        if (saved) store.WriteAutosave(GameSession.New(Inputs.Content, 23UL, Mode.Historical).Save.Write());
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var doc = Object.FindFirstObjectByType<UIDocument>();
        var panel = Object.Instantiate(doc.panelSettings);
        var texture = new RenderTexture(1280, 720, 0);
        panel.targetTexture = texture;
        panels.Add((panel, texture));
        doc.panelSettings = panel;   // real layout/focus in EditMode; never mutate the shared PanelSettings asset
        Object.FindFirstObjectByType<MapScene>().Build(Inputs.Map);
        var root = Object.FindFirstObjectByType<GameRoot>();
        root.Store = store; root.Language = "en"; root.Begin(Inputs, 0, startScreen: true);
        return root;
    }
    [TearDown]
    public void Clean()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (var dir in directories) if (Directory.Exists(dir)) Directory.Delete(dir, true);
        directories.Clear();
        foreach (var (panel, texture) in panels)
        {
            Object.DestroyImmediate(panel); texture.Release(); Object.DestroyImmediate(texture);
        }
        panels.Clear();
    }

    [UnityTest]
    public IEnumerator SavedNewGameStillRequiresBothConfirmations()
    {
        var root = Open(saved: true, out var store);
        string save = File.ReadAllText(store.AutosavePath), hash = root.Bridge!.State.Hash();
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.IsTrue(StartOpen, "one press does not erase the save or open its replacement");
        Assert.AreEqual(DisplayStyle.None, Ui.Q("opening-layer").resolvedStyle.display);
        Assert.AreEqual(save, File.ReadAllText(store.AutosavePath));
        Assert.AreEqual(hash, root.Bridge.State.Hash());
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.IsFalse(StartOpen);
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("opening-layer").resolvedStyle.display);
        Assert.IsFalse(File.Exists(store.AutosavePath), "the confirmed new game uses the existing deletion flow");
        Assert.AreEqual(0, root.Bridge.State.Turn, "opening is before the first turn");
    }

    [UnityTest]
    public IEnumerator FullGameRootOffersReplayToStartScreenNavigation()
    {
        var root = Open(saved: true, out _);
        for (int frame = 0; frame < 6; frame++) yield return null;
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("start-opening-replay").resolvedStyle.display);
        var start = (StartScreenPresenter)typeof(GameRoot).GetField("start", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(root)!;
        var line = start.ButtonCentres();
        Assert.IsNotNull(line, "precondition: the start screen is laid out");
        StringAssert.Contains("start-opening-replay=", line, "the full GameRoot route exposes its visible replay button to browser navigation");
    }

    [UnityTest]
    public IEnumerator LanguageSwitchKeepsTheActivePanelAndItsDestination()
    {
        foreach (bool replay in new[] { false, true })
        {
            var root = Open(saved: replay, out var store);
            UiTestKit.Press(Ui.Q<Button>(replay ? "start-opening-replay" : "start-new-game"));
            UiTestKit.Press(Ui.Q<Button>("opening-next"));
            Assert.IsNull(root.Bridge!.Queue(new SetMobilization(1.0)), "precondition: a real queued command survives presentation");
            var session = root.Bridge.Session; var hash = root.Bridge.State.Hash(); var orders = root.Bridge.Queued.ToArray();
            var save = store.ReadAutosave();
            typeof(GameRoot).GetMethod("SwitchLanguage", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(root, new object[] { Inputs, "he" });
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, Ui.Q("opening-layer").resolvedStyle.display, "language rebuild keeps the opening open");
            Assert.AreEqual("The Arab countries", Ui.Q<Label>("opening-title").text, "language rebuild keeps panel 2, not panel 1");
            Assert.AreEqual("הבא", Ui.Q<Button>("opening-next").text);
            Assert.AreEqual(LanguageDirection.LTR, Ui.Q<Label>("opening-text").languageDirection);
            Assert.AreSame(session, root.Bridge!.Session);
            Assert.AreEqual(hash, root.Bridge.State.Hash());
            CollectionAssert.AreEqual(orders, root.Bridge.Queued);
            Assert.AreEqual(save, store.ReadAutosave());
            UiTestKit.Press(Ui.Q<Button>("opening-next"));
            Assert.AreEqual("Yemen", Ui.Q<Label>("opening-title").text, "one press advances once after disposal/rebuild");
            UiTestKit.Press(Ui.Q<Button>("opening-skip"));
            yield return null;
            Assert.AreEqual(replay, StartOpen, "replay returns to start; new game returns to map");
            Assert.AreEqual(!replay, Object.FindFirstObjectByType<CameraController>().enabled);
        }
    }

    [UnityTest]
    public IEnumerator KeyboardSkipReturnsFocusToTheUncoveredDecisionCard()
    {
        var root = Open(saved: false, out _);
        Assert.IsTrue(root.Guide!.Seen, "EditMode has no personal settings store; no guide can steal this control's focus");
        Assert.IsNotNull(root.Card!.Shown, "precondition: the initial decision card is waiting underneath");
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        var next = Ui.Q<Button>("opening-next"); next.Focus();
        using (var tab = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Next)) { tab.target = next; next.SendEvent(tab); }
        yield return null;
        Assert.AreSame(Ui.Q<Button>("opening-skip"), Ui.focusController?.focusedElement, "Tab remains on the opening controls");
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        yield return null; yield return null;
        Assert.AreSame(Ui.Q("event-card"), Ui.focusController?.focusedElement, "ScreenNav's OnUncovered restores the waiting card's keyboard focus");
    }
    [UnityTest]
    public IEnumerator SmokeAndDisableCannotLeaveAnOpeningOrItsHandlers()
    {
        var root = Open(saved: false, out _);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        yield return null;
        Assert.AreEqual(DisplayStyle.Flex, Ui.Q("opening-layer").resolvedStyle.display);
        root.enabled = false;
        UiTestKit.Press(Ui.Q<Button>("opening-next"));
        UiTestKit.Press(Ui.Q<Button>("opening-skip"));
        Assert.IsFalse(Ui.Q("root").ClassListContains(OpeningPresenter.OpenClass));
        Assert.IsTrue(Object.FindFirstObjectByType<CameraController>().enabled, "disposing returns the camera");
        root.enabled = true;
        LogAssert.Expect(LogType.Warning, "SMOKE FAIL the smoke run needs play mode or a player");
        root.Begin(Inputs, 5, startScreen: true);
        yield return null;
        Assert.IsFalse(StartOpen);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("opening-layer").resolvedStyle.display);
        Assert.AreEqual(DisplayStyle.None, Ui.Q("start-opening-replay").resolvedStyle.display);
    }

    [Test]
    public void FirstCardInstrumentSkipsTheActualOpeningBeforeAwaitingTheCard()
    {
        var root = Open(saved: false, out _);
        var lines = new List<string>();
        double clock = 0;
        var check = GameRoot.FirstCardCheck(Ui, root.Card!, lines.Add, () => clock++);
        int steps = 0;
        while (check.MoveNext()) Assert.Less(++steps, 12, "the existing clock-bound instrument is finite");
        Assert.IsFalse(Ui.Q("root").ClassListContains(OpeningPresenter.OpenClass));
        Assert.IsTrue(lines.Any(line => line.StartsWith("FIRST CARD OK ", StringComparison.Ordinal)), string.Join("\n", lines));
        Assert.IsFalse(lines.Any(line => line.StartsWith("FIRST CARD FAIL", StringComparison.Ordinal)));
    }

}
