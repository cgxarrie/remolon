using RetroBackend.Mappings;
using RetroBackend.Models;
using Xunit;

namespace RetroBackend.Tests.Mappings;

public class GetRetrospectiveMappingsTests
{
    [Fact]
    public void ToGetDto_WhenUnrevealed_ReturnsOwnItemsAndOtherAuthorCounts()
    {
        var retro = BuildRetroWithItems();

        var dto = retro.ToGetDto("alice");
        var column = dto.Columns.Single(c => c.Title == "Went Well");

        Assert.False(dto.IsRevealed);
        Assert.Equal(2, column.Items.Count);
        Assert.All(column.Items, item => Assert.Equal("alice", item.CreatedBy));
        Assert.Equal(["Alice item 1", "Alice item 2"], column.Items.Select(i => i.Description));

        var hidden = column.HiddenAuthorCounts.Single();
        Assert.Equal("bob", hidden.CreatedBy);
        Assert.Equal("Bob", hidden.CreatedByNickname);
        Assert.Equal(2, hidden.Count);
        Assert.DoesNotContain(column.HiddenAuthorCounts, c => c.CreatedBy == "charlie");
        Assert.DoesNotContain(column.HiddenAuthorCounts, c => c.CreatedBy == "alice");
    }

    [Fact]
    public void ToGetDto_WhenRevealed_ReturnsAllItemsAndEmptyHiddenCounts()
    {
        var retro = BuildRetroWithItems();
        retro.Reveal();

        var dto = retro.ToGetDto("alice");
        var column = dto.Columns.Single(c => c.Title == "Went Well");

        Assert.True(dto.IsRevealed);
        Assert.Equal(4, column.Items.Count);
        Assert.Empty(column.HiddenAuthorCounts);
        Assert.Contains(column.Items, i => i.CreatedBy == "bob" && i.Description == "Bob item 1");
    }

    private static Retrospective BuildRetroWithItems()
    {
        var retro = Retrospective.CreateNew("owner", "Sprint 1", Guid.NewGuid());
        retro.AddColumn("owner", "Went Well", 1);
        var column = retro.Columns.Single(c => c.Title == "Went Well");
        column.Items.Add(new Item("alice", "Alice", column.Id, "Alice item 1", 0));
        column.Items.Add(new Item("bob", "Bob", column.Id, "Bob item 1", 1));
        column.Items.Add(new Item("alice", "Alice", column.Id, "Alice item 2", 2));
        column.Items.Add(new Item("bob", "Bob", column.Id, "Bob item 2", 3));
        return retro;
    }
}
