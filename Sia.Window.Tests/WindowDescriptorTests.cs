namespace Sia.Window.Tests;

public sealed class WindowDescriptorTests
{
    [Fact]
    public void ValidateAcceptsPositiveDimensions()
    {
        var descriptor = new WindowDescriptor(1280, 720, "Sia");

        WindowDescriptor.Validate(descriptor);
    }

    [Theory]
    [InlineData(0, 720)]
    [InlineData(-1, 720)]
    [InlineData(1280, 0)]
    [InlineData(1280, -1)]
    public void ValidateRejectsNonPositiveDimensions(int width, int height)
    {
        var descriptor = new WindowDescriptor(width, height, "Sia");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WindowDescriptor.Validate(descriptor));
    }
}
