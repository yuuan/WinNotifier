using Xunit;
using WinNotifier.Models;

namespace WinNotifier.Tests;

public class NotificationRequestValidationTests
{
    [Fact]
    public void Validate_BothFieldsPresent_ReturnsNoErrors()
    {
        var request = new NotificationRequest { Title = "Hello", Message = "World" };

        var errors = request.Validate();

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingTitle_ReturnsError(string? title)
    {
        var request = new NotificationRequest { Title = title, Message = "Valid" };

        var errors = request.Validate();

        Assert.Single(errors);
        Assert.Contains("Title", errors[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingMessage_ReturnsError(string? message)
    {
        var request = new NotificationRequest { Title = "Valid", Message = message };

        var errors = request.Validate();

        Assert.Single(errors);
        Assert.Contains("Message", errors[0]);
    }

    [Fact]
    public void Validate_BothMissing_ReturnsTwoErrors()
    {
        var request = new NotificationRequest();

        var errors = request.Validate();

        Assert.Equal(2, errors.Count);
    }
}
