using System.Text.Json.Nodes;

namespace Cruma.Content.Tests;

public class ContentDocumentTests
{
    [Test]
    public void Parse_AllElementsOfFr7_SerializesUnchanged()
    {
        var json = TestDocuments.Fixture("document-v1-all-elements.json");

        var document = ContentDocument.Parse(json);

        Assert.That(JsonNode.DeepEquals(document.ToJson(), JsonNode.Parse(json)), Is.True);
        Assert.That(document.SchemaVersion, Is.EqualTo(1));
        Assert.That(document.Blocks.Select(block => block.Id),
            Is.EqualTo(new[] { "b-heading", "b-marks", "b-bullets", "b-ordered", "b-tasks", "b-unknown" }));
    }

    [Test]
    public void Parse_UnknownBlockType_IsPreservedOnLoadAndSave()
    {
        var document = ContentDocument.Parse(TestDocuments.Fixture("document-v1-all-elements.json"));

        var unknown = document.FindBlock("b-unknown")!;
        var reloaded = ContentDocument.Parse(document.ToJsonString());

        Assert.That(unknown.IsKnownType, Is.False);
        Assert.That(reloaded.FindBlock("b-unknown")!.ContentEquals(unknown), Is.True);
        Assert.That(reloaded.ToJson()["futureRootProperty"]?.GetValue<string>(), Is.EqualTo("zachovat"));
    }

    [Test]
    public void Create_NewDocument_CarriesCurrentSchemaVersion()
    {
        var document = ContentDocument.Create([TestDocuments.Paragraph("a", "text")]);

        Assert.That(document.ToJson()["schemaVersion"]?.GetValue<int>(), Is.EqualTo(ContentDocument.CurrentSchemaVersion));
        Assert.That(document.ToJson()["type"]?.GetValue<string>(), Is.EqualTo(ContentTypes.Document));
    }

    [Test]
    public void Parse_MissingSchemaVersion_Throws()
    {
        Assert.Throws<ContentFormatException>(() => ContentDocument.Parse("""{"type":"doc","content":[]}"""));
    }

    [Test]
    public void Parse_BlockWithoutId_Throws()
    {
        Assert.Throws<ContentFormatException>(() =>
            ContentDocument.Parse("""{"type":"doc","schemaVersion":1,"content":[{"type":"paragraph"}]}"""));
    }

    [Test]
    public void Parse_InvalidJson_Throws()
    {
        Assert.Throws<ContentFormatException>(() => ContentDocument.Parse("{not json"));
    }

    [Test]
    public void Create_DuplicateBlockId_Throws()
    {
        Assert.Throws<ContentFormatException>(() =>
            ContentDocument.Create([TestDocuments.Paragraph("a", "x"), TestDocuments.Paragraph("a", "y")]));
    }

    [Test]
    public void WithId_PastedBlock_ChangesOnlyIdentifier()
    {
        var original = TestDocuments.Paragraph("a", "text");

        var pasted = original.WithId("b");

        Assert.That(pasted.Id, Is.EqualTo("b"));
        Assert.That(original.Id, Is.EqualTo("a"));
        Assert.That(pasted.ContentEquals(TestDocuments.Paragraph("b", "text")), Is.True);
    }

    [Test]
    public void FromJson_ModifyingSourceAfterwards_DoesNotChangeBlock()
    {
        var source = TestDocuments.Paragraph("a", "text").ToJson();
        var block = ContentBlock.FromJson(source);

        source["attrs"]!["id"] = "changed";

        Assert.That(block.Id, Is.EqualTo("a"));
        Assert.That(block.ToJson()["attrs"]!["id"]!.GetValue<string>(), Is.EqualTo("a"));
    }

    [Test]
    public void Registry_Fr7Elements_AreRegistered()
    {
        string[] types =
        [
            ContentTypes.Heading, ContentTypes.Paragraph, ContentTypes.BulletList, ContentTypes.OrderedList, ContentTypes.TaskList,
            ContentTypes.Bold, ContentTypes.Italic, ContentTypes.Underline, ContentTypes.Strike, ContentTypes.Link, ContentTypes.Highlight,
        ];

        Assert.That(types.Select(ContentTypeRegistry.Find), Has.None.Null);
    }
}
