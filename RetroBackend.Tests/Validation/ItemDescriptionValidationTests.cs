using System.ComponentModel.DataAnnotations;
using RetroBackend.Models;
using Xunit;
using Dto = RetroBackend.Dtos;

namespace RetroBackend.Tests.Validation;

public class ItemDescriptionValidationTests
{
    [Fact]
    public void CreateItemRequest_RejectsDescriptionOverMaxLength()
    {
        var request = new Dto.CreateItemRequest
        {
            ColumnId = Guid.NewGuid(),
            Description = new string('a', ItemLimits.DescriptionMaxLength + 1),
            Position = 0,
        };

        Assert.False(TryValidate(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Dto.CreateItemRequest.Description)));
    }

    [Fact]
    public void CreateItemRequest_AcceptsDescriptionAtMaxLength()
    {
        var request = new Dto.CreateItemRequest
        {
            ColumnId = Guid.NewGuid(),
            Description = new string('a', ItemLimits.DescriptionMaxLength),
            Position = 0,
        };

        Assert.True(TryValidate(request, out _));
    }

    [Fact]
    public void CreateActionItemRequest_RejectsDescriptionOverMaxLength()
    {
        var request = new Dto.CreateActionItemRequest
        {
            ColumnId = Guid.NewGuid(),
            Description = new string('a', ItemLimits.DescriptionMaxLength + 1),
            Position = 0,
        };

        Assert.False(TryValidate(request, out _));
    }

    [Fact]
    public void UpdateItemRequest_RejectsDescriptionOverMaxLength()
    {
        var request = new Dto.UpdateItemRequest
        {
            Description = new string('a', ItemLimits.DescriptionMaxLength + 1),
        };

        Assert.False(TryValidate(request, out _));
    }

    private static bool TryValidate(object instance, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}
