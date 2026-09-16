using System.Text.Json.Nodes;

namespace Cruma.Content.Tests;

/// <summary>Pomocné dokumenty a bloky pro testy.</summary>
internal static class TestDocuments
{
    public static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static ContentBlock Paragraph(string id, string text) =>
        ContentBlock.FromJson(new JsonObject
        {
            ["type"] = ContentTypes.Paragraph,
            ["attrs"] = new JsonObject { ["id"] = id },
            ["content"] = new JsonArray(new JsonObject { ["type"] = ContentTypes.Text, ["text"] = text }),
        });
}
