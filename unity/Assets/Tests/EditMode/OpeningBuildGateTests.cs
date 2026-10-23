#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using Object = UnityEngine.Object;

/// <summary>#530B proposed Editor-gate tests. Fixtures change only cloned objects and YAML in memory, never reviews on disk.
/// On the unchanged Build, Require first fails an NUnit missing-method assertion, before any future API invocation.</summary>
public class OpeningBuildGateTests
{
    readonly List<Object> owned = new List<Object>();
    static Type BuildType()
    {
        var type = Type.GetType("Statecraft.Game.EditorTools.Build, Assembly-CSharp-Editor");
        Assert.IsNotNull(type, "the real Editor Build assembly must be compiled and loaded");
        return type!;
    }
    static MethodInfo Require(string name)
    {
        var method = BuildType().GetMethod(name, BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(method, "Build." + name + " must exist before the opening can be released (#530B)");
        return method!;
    }
    static YamlMappingNode Read(string text)
    {
        var yaml = new YamlStream(); yaml.Load(new StringReader(text));
        return (YamlMappingNode)yaml.Documents[0].RootNode;
    }
    static YamlNode Get(YamlMappingNode map, string key) => map.Children[new YamlScalarNode(key)];
    static void Set(YamlMappingNode map, string key, string value) => map.Children[new YamlScalarNode(key)] = new YamlScalarNode(value);
    static List<YamlMappingNode> Panels(YamlMappingNode doc) => ((YamlSequenceNode)Get(doc, "panels")).Children.Cast<YamlMappingNode>().ToList();
    static List<YamlMappingNode> Sources(YamlMappingNode paragraph) => ((YamlSequenceNode)Get(paragraph, "sources")).Children.Cast<YamlMappingNode>().ToList();
    static List<YamlMappingNode> Photos(YamlMappingNode json) => ((YamlSequenceNode)Get(json, "photos")).Children.Cast<YamlMappingNode>().ToList();

    OpeningContent Fixture(Action<YamlMappingNode>? change = null)
    {
        var shipped = AssetDatabase.LoadAssetAtPath<OpeningContent>("Assets/Resources/OpeningContent.asset");
        Assert.IsNotNull(shipped, "the product's reference-only catalog");
        var copy = Object.Instantiate(shipped); owned.Add(copy);
        copy.photos = shipped.photos.ToArray();
        var yaml = new YamlStream(); yaml.Load(new StringReader(shipped.source.text));
        var doc = (YamlMappingNode)yaml.Documents[0].RootNode;
        Set(doc, "status", "sensitivity-reviewed");
        foreach (var paragraph in Panels(doc).Concat(new[] { (YamlMappingNode)Get(doc, "closing") }))
            foreach (var source in Sources(paragraph)) Set(source, "checked", "2026-10-22");
        foreach (var panel in Panels(doc))
        {
            var image = (YamlMappingNode)Get(panel, "image");
            Set(image, "sensitive", "true"); Set(image, "sensitivity_reviewed", "true");
        }
        change?.Invoke(doc);
        using var writer = new StringWriter(); yaml.Save(writer, false);
        var text = new TextAsset(writer.ToString()); owned.Add(text); copy.source = text;
        return copy;   // a fully checked fixture is not a human approval of the shipped data
    }
    [TearDown]
    public void Clean() { for (int i = owned.Count - 1; i >= 0; i--) Object.DestroyImmediate(owned[i]); owned.Clear(); }

    [Test]
    public void ApprovedPhotosCannotReleaseDraftProse()
    {
        var gate = Require("OpeningShareableRefusal");
        var draft = Fixture(doc => Set(doc, "status", "draft"));
        Assert.IsTrue(draft.Read().PhotosReviewed);
        Assert.IsNotNull(gate.Invoke(null, new object?[] { draft }), "photo approval is not text approval");
    }

    [Test]
    public void FullyReviewedMemoryFixtureWithAllMediaCanPass()
    {
        var gate = Require("OpeningShareableRefusal");
        var reviewed = Fixture();
        Assert.IsTrue(reviewed.Read().Reviewed, "precondition: fixture only, not a signed source file");
        Assert.IsNull(gate.Invoke(null, new object?[] { reviewed }));
    }

    [TestCase("panel checked")]
    [TestCase("closing checked")]
    [TestCase("photo review")]
    [TestCase("photo reference")]
    public void MissingCheckOrPhotographReviewRefuses(string gap)
    {
        var gate = Require("OpeningShareableRefusal");
        var damaged = Fixture(doc =>
        {
            if (gap == "panel checked") Sources(Panels(doc)[0])[0].Children.Remove(new YamlScalarNode("checked"));
            if (gap == "closing checked") Sources((YamlMappingNode)Get(doc, "closing"))[0].Children.Remove(new YamlScalarNode("checked"));
            if (gap == "photo review") Set((YamlMappingNode)Get(Panels(doc)[0], "image"), "sensitivity_reviewed", "false");
        });
        if (gap == "photo reference") damaged.photos[1] = null!;
        Assert.IsNotNull(gate.Invoke(null, new object?[] { damaged }), gap + " must not become shareable");
    }

    [Test]
    public void ActualManifestFunctionIncludesThreeOpeningPhotosAndRefusesDraft()
    {
        var manifest = Require("PhotoManifestWithOpeningJson");
        var draft = Fixture(doc => Set(doc, "status", "draft"));
        var json = (string)manifest.Invoke(null, new object?[] { null, true, draft })!;
        var doc = Read(json);
        Assert.AreEqual("false", Get(doc, "shareable").ToString(), "even a requested shareable flag cannot approve draft text");
        var rows = Photos(doc);
        CollectionAssert.AreEqual(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" },
            rows.Select(p => Get(p, "event_id").ToString()));
        Assert.IsTrue(rows.All(p => Get(p, "sensitive").ToString() == "true" && Get(p, "sensitivity_reviewed").ToString() == "true"),
            "all three photos were approved; draft text still forces shareable:false");
    }

    [Test]
    public void ReviewedManifestRetainsEveryEventPhotoAlongsideTheOpening()
    {
        var manifest = Require("PhotoManifestWithOpeningJson");
        var photos = AssetDatabase.LoadAssetAtPath<EventImageCatalog>("Assets/Resources/EventImageCatalog.asset");
        Assert.IsNotNull(photos);
        var reviewed = Fixture();
        var doc = Read((string)manifest.Invoke(null, new object?[] { photos, true, reviewed })!);
        var actual = Photos(doc).Select(p => Get(p, "event_id").ToString()).ToArray();
        var expected = photos.entries.Where(p => p.image != null).Select(p => p.eventId)
            .Concat(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" }).ToArray();
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual("true", Get(doc, "shareable").ToString(), "positive fixture guards against an always-false implementation");
    }

    [Test]
    public void RealBuildManifestWriterUsesTheOpeningAwareFunction()
    {
        Require("PhotoManifestWithOpeningJson");
        var writer = BuildType().GetMethod("WritePhotoManifest", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(writer, "the production writer used by Web/Mac/Mono builds");
        Assert.IsFalse(Environment.GetCommandLineArgs().Contains("-shareable"), "this test is a normal Native test, not a shareable build");
        var source = File.ReadAllBytes(OpeningContent.SourcePath);
        var folder = Path.Combine(Path.GetTempPath(), "statecraft-opening-manifest-" + Guid.NewGuid().ToString("N"));
        try
        {
            writer!.Invoke(null, new object[] { folder });
            var doc = Read(File.ReadAllText(Path.Combine(folder, "event-photos.json")));
            Assert.AreEqual("false", Get(doc, "shareable").ToString());
            CollectionAssert.AreEqual(new[] { "opening/europe", "opening/arab_countries", "opening/yemen" },
                Photos(doc).Select(p => Get(p, "event_id").ToString()).Where(id => id.StartsWith("opening/", StringComparison.Ordinal)));
            CollectionAssert.AreEqual(source, File.ReadAllBytes(OpeningContent.SourcePath), "writer cannot sign off or rewrite the opening");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Test]
    public void ExistingReflectionApisRemainUnambiguousAndCallable()
    {
        var type = BuildType();
        foreach (var (name, count) in new[] { ("ShareableRefusal", 5), ("GateRefusal", 6), ("PhotoManifestJson", 2) })
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == name).ToArray();
            Assert.AreEqual(1, methods.Length, name + " must not become an ambiguous overload");
            Assert.AreEqual(count, methods[0].GetParameters().Length, name + " keeps its reflection argument count");
        }
        var content = ContentLoader.Load(Paths.Content);
        Assert.DoesNotThrow(() => Require("ShareableRefusal").Invoke(null, new object?[] { content, null, false, null, null }));
        Assert.IsNull(Require("GateRefusal").Invoke(null, new object?[] { new[] { "Unity" }, content, null, false, null, null }));
        var legacy = Read((string)Require("PhotoManifestJson").Invoke(null, new object?[] { null, true })!);
        Assert.AreEqual("true", Get(legacy, "shareable").ToString());
        Assert.IsEmpty(Photos(legacy));
    }
}
