#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// Private, explicit review fixture. Real source/data and shipped UI/presenters, pinned selection/date/target size.
// No simulation gameplay, browser, touch-device or map-camera acceptance is claimed by these UI-only renders.
public class RegionHebrewVisualProof
{
    [UnityTest, Explicit]
    public IEnumerator Capture()
    {
        var dir = Environment.GetEnvironmentVariable("STATECRAFT_REGION_PROOF_DIR");
        Assert.IsNotNull(dir); Directory.CreateDirectory(dir!);
        var inputs = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        var sub = Subdistricts1948.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Subdistricts1948.FileName)));
        var arm = Armistice1949.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", Armistice1949.FileName)));
        var regions = new MapRegions(sub, arm);
        var auja = arm.HexesOf(arm.Zones.Single(x => x.Key == "egypt_el_auja_dmz")).First();
        (int q, int r) On(string key) => arm.LineHexes.First(x => x.Value.Any(v => v.Key == key)).Key;
        var cases = new[] {
            ("subdistrict", new DateTime(1948, 5, 15), auja),
            ("auja", new DateTime(1949, 3, 1), auja),
            ("lebanon", new DateTime(1949, 8, 1), On("lebanon_adl")),
            ("jordan", new DateTime(1949, 5, 8), On("jordan_adl_iraqi_a_triangle")),
            ("modus-vivendi", new DateTime(1950, 3, 1), On("egypt_mv1950_demarcation")) };
        foreach (var (tag, strings) in new[] { ("en", UiPaths.Strings), ("he", UiPaths.StringsHebrew) })
        foreach (var (width, height) in new[] { (1920, 1080), (390, 844) })
        foreach (var (name, date, hex) in cases)
        {
            var text = UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(strings).text);
            var rt = new RenderTexture(width, height, 0);
            var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
            var go = new GameObject("RegionReviewFixture"); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml); go.SetActive(true);
            var root = doc.rootVisualElement; var hudRoot = root.Q("root") ?? root;
            hudRoot.EnableInClassList(UiLanguage.HebrewClass, tag == "he");
            hudRoot.RemoveFromClassList("loading"); // the completed-boot state, as GameRoot sets it
            hudRoot.style.backgroundColor = new Color(0.08f, 0.11f, 0.16f, 1f);
            var game = GameSession.New(inputs.Content, GoldenRun.Seed, Mode.Historical);
            game.State.Date = date; game.State.PendingEvents.Clear();
            var bridge = new SimBridge(game, text);
            var hud = new HudPresenter(root, bridge, text);
            var info = new InfoPanelPresenter(root, bridge, text, inputs.Content.SettlementNames, regions: regions);
            var direction = new UiDirection(root, bridge);
            try
            {
                // Let the shipped HUD select its responsive tier from a real nonzero bar width before hiding it.
                for (int i = 0; i < 6; i++) yield return null;
                foreach (var child in hudRoot.Children().ToList()) child.style.display = DisplayStyle.None;
                var panel = root.Q("info-panel"); panel.style.display = DisplayStyle.Flex;
                info.Show(hex.q, hex.r);
                var label = new Label(); label.AddToClassList("map-layer");
                label.style.display = DisplayStyle.Flex; label.style.position = Position.Absolute;
                label.style.left = 16; label.style.top = 16; label.style.right = StyleKeyword.Auto; label.style.bottom = StyleKeyword.Auto;
                label.style.maxWidth = 312;
                var translated = typeof(MapRegions).GetMethod("Label", new[] { typeof(DateTime), typeof(Func<string, object[], string>), typeof(bool) });
                label.text = translated == null ? regions.Label(date, text.Format)
                    : (string)translated.Invoke(regions, new object[] { date, (Func<string, object[], string>)text.Format, tag == "he" });
                hudRoot.Add(label); direction.Apply();
                for (int i = 0; i < 12; i++) yield return null;
                var region = root.Q<Label>("info-region"); Assert.IsNotEmpty(region.text);
                Assert.IsTrue(region.visible && panel.visible, name + ": the actual region panel must be visible");
                var screen = hudRoot.worldBound; var bounds = panel.worldBound;
                Assert.IsTrue(bounds.xMin >= screen.xMin - 1f && bounds.xMax <= screen.xMax + 1f
                    && bounds.yMin >= screen.yMin - 1f && bounds.yMax <= screen.yMax + 1f,
                    name + ": the complete info panel must fit the capture, " + bounds + " in " + screen);
                var measured = region.MeasureTextSize(region.text, region.contentRect.width,
                    VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.y, region.contentRect.height + 1f, name + ": region text clipped");
                var old = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try { tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
                    File.WriteAllBytes(Path.Combine(dir!, $"{name}-{tag}-{width}x{height}.png"), tex.EncodeToPNG()); }
                finally { RenderTexture.active = old; Object.DestroyImmediate(tex); }
                File.WriteAllText(Path.Combine(dir!, $"{name}-{tag}-{width}x{height}.txt"),
                    $"date={date:yyyy-MM-dd}\nhex={hex.q},{hex.r}\nfixture=shipped-UI-only; no map camera\nlang={tag}\nresolution={width}x{height}\nregion_bounds={region.worldBound}\nregion_content={region.contentRect}\nregion_measure={measured}\nlayer={label.text}\nregion={region.text}\n");
            }
            finally { direction.Dispose(); info.Dispose(); hud.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
