using System.Text.Json;
using GroupDocs.Comparison.Mcp.Tools;
using GroupDocs.Comparison.Result;
using Xunit;

namespace GroupDocs.Comparison.Mcp.Tests;

public class ChangeProjectorTests
{
    [Fact]
    public void Project_EmptyInput_ReturnsEmptyList()
    {
        var result = ChangeProjector.Project(Array.Empty<ChangeInfo>());

        Assert.Empty(result);
    }

    [Fact]
    public void Project_Null_ReturnsEmptyList()
    {
        var result = ChangeProjector.Project(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void Project_SkipsNullEntries()
    {
        var changes = new[] { null, new ChangeInfo { Id = 1, SourceText = "a" }, null };

        var result = ChangeProjector.Project(changes!);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Project_MapsCoreTextFields()
    {
        // Mirrors a real PDF inserted-run change: the changed fragment is a short
        // word ("Microsoft") while source/target carry the full paragraph.
        var change = new ChangeInfo
        {
            Id = 7,
            ComponentType = "Text",
            Text = "Microsoft",
            SourceText = "The proposed solution leverages AWS services.",
            TargetText = "The proposed solution leverages Microsoft Azure services.",
        };

        var projected = ChangeProjector.Project(new[] { change });

        var single = Assert.Single(projected);
        Assert.Equal(7, single.Id);
        Assert.Equal("Text", single.ComponentType);
        Assert.Equal("Microsoft", single.ChangedText);
        Assert.Equal("The proposed solution leverages AWS services.", single.SourceText);
        Assert.Equal("The proposed solution leverages Microsoft Azure services.", single.TargetText);
    }

    [Fact]
    public void Project_DropsChangedText_WhenEqualToSourceText()
    {
        // Paragraph-level delete: engine sets Text == SourceText. No point
        // repeating the same string in both fields.
        var change = new ChangeInfo
        {
            Id = 1,
            Text = "a whole deleted paragraph",
            SourceText = "a whole deleted paragraph",
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.Null(single.ChangedText);
        Assert.Equal("a whole deleted paragraph", single.SourceText);
    }

    [Fact]
    public void Project_DropsChangedText_WhenEqualToTargetText()
    {
        var change = new ChangeInfo
        {
            Id = 1,
            Text = "a whole inserted paragraph",
            TargetText = "a whole inserted paragraph",
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.Null(single.ChangedText);
        Assert.Equal("a whole inserted paragraph", single.TargetText);
    }

    [Fact]
    public void Project_MapsPageNumber_FromPageInfo()
    {
        var change = new ChangeInfo
        {
            Id = 1,
            PageInfo = new PageInfo(3, 800, 600),
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.Equal(3, single.Page);
    }

    [Fact]
    public void Project_MapsCellsCoordinates()
    {
        var change = new ChangeInfo
        {
            Id = 1,
            ComponentType = "Cell",
            Row = 4,
            Column = 1,
            ColumnHeader = "Price",
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.Equal(4, single.Row);
        Assert.Equal(1, single.Column);
        Assert.Equal("Price", single.ColumnHeader);
    }

    [Fact]
    public void Project_MapsStyleChanges()
    {
        var change = new ChangeInfo
        {
            Id = 1,
            StyleChanges = new[]
            {
                new StyleChangeInfo { PropertyName = "FontSize", OldValue = 11, NewValue = 14 },
                new StyleChangeInfo { PropertyName = "Bold", OldValue = false, NewValue = true },
            },
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.NotNull(single.StyleChanges);
        Assert.Equal(2, single.StyleChanges!.Count);
        Assert.Equal("FontSize", single.StyleChanges[0].Property);
        Assert.Equal("11", single.StyleChanges[0].OldValue);
        Assert.Equal("14", single.StyleChanges[0].NewValue);
        Assert.Equal("Bold", single.StyleChanges[1].Property);
        Assert.Equal("False", single.StyleChanges[1].OldValue);
        Assert.Equal("True", single.StyleChanges[1].NewValue);
    }

    [Fact]
    public void Project_EmptyStyleChanges_MapsToNull()
    {
        var change = new ChangeInfo
        {
            Id = 1,
            StyleChanges = Array.Empty<StyleChangeInfo>(),
        };

        var single = Assert.Single(ChangeProjector.Project(new[] { change }));

        Assert.Null(single.StyleChanges);
    }

    [Fact]
    public void Serialize_OmitsEmptyAndNullFields()
    {
        // A minimal change should serialize to just id + type — every optional
        // field is null/empty and must be omitted to keep the payload compact.
        var change = new ChangeInfo { Id = 42 };

        var json = ChangeProjector.Serialize(new[] { change });

        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement[0];

        Assert.Equal(42, element.GetProperty("id").GetInt32());
        Assert.True(element.TryGetProperty("type", out _));
        Assert.False(element.TryGetProperty("componentType", out _));
        Assert.False(element.TryGetProperty("changedText", out _));
        Assert.False(element.TryGetProperty("page", out _));
        Assert.False(element.TryGetProperty("sourceText", out _));
        Assert.False(element.TryGetProperty("targetText", out _));
        Assert.False(element.TryGetProperty("row", out _));
        Assert.False(element.TryGetProperty("column", out _));
        Assert.False(element.TryGetProperty("columnHeader", out _));
        Assert.False(element.TryGetProperty("styleChanges", out _));
    }

    [Fact]
    public void Serialize_ProducesValidJsonArray()
    {
        var changes = new[]
        {
            new ChangeInfo { Id = 1, Text = "Microsoft", SourceText = "AWS", TargetText = "Microsoft Azure" },
            new ChangeInfo { Id = 2, ComponentType = "Cell", Row = 0, Column = 2, ColumnHeader = "Total" },
        };

        var json = ChangeProjector.Serialize(changes);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal("Microsoft", doc.RootElement[0].GetProperty("changedText").GetString());
        Assert.Equal("Total", doc.RootElement[1].GetProperty("columnHeader").GetString());
    }
}
