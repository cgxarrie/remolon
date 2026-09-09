using System.ComponentModel.DataAnnotations;
using RetroBackend.Dtos;
using Xunit;

namespace RetroBackend.Tests.Validation;

public class HeaderColorValidationTests
{
    [Theory]
    [InlineData("not-a-color")]
    [InlineData("url(x)")]
    [InlineData("#fff")]
    [InlineData("#GGGGGG")]
    public void UpdateRetrospectiveColumnRequest_RejectsInvalidHeaderColor(string color)
    {
        var request = new UpdateRetrospectiveColumnRequest
        {
            Id = Guid.NewGuid(),
            HeaderColor = color,
        };

        Assert.False(TryValidate(request, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateRetrospectiveColumnRequest.HeaderColor)));
    }

    [Fact]
    public void UpdateRetrospectiveColumnRequest_AcceptsHexRgb()
    {
        var request = new UpdateRetrospectiveColumnRequest
        {
            Id = Guid.NewGuid(),
            HeaderColor = "#4f46e5",
        };

        Assert.True(TryValidate(request, out _));
    }

    private static bool TryValidate(object instance, out List<ValidationResult> results)
    {
        results = [];
        return Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
    }
}
