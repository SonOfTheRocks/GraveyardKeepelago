using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class BadgePixelTests
{
    [Fact]
    public void FullSizeSkullIsDrawnOnlyInTheVisibleStarCornerOfAPaddedCanvas()
    {
        var skull = Rectangle(32, 32, 4, 1, 24, 30);
        var star = Rectangle(64, 64, 45, 46, 15, 14);
        var result = BadgePixels.Compose(skull, 32, 32, star, 64, 64);
        var visible = BadgePixels.Bounds(result, 64, 64);
        Assert.Equal(11, visible.Width);
        Assert.Equal(14, visible.Height);
        Assert.Equal(60, visible.X + visible.Width);
        Assert.Equal(60, visible.Y + visible.Height);
        Assert.All(Enumerable.Range(0, 32 * 64), pixel => Assert.Equal(0, result[pixel * 4 + 3]));
        Assert.Equal(64 * 64 * 4, result.Length);
    }

    [Theory]
    [InlineData(32, 21, 18)]
    [InlineData(64, 43, 46)]
    public void ActualAlphaBoundsControlTheSizeEvenWhenSpriteRectanglesAreIdentical(int size, int x, int y)
    {
        var source = Rectangle(size, size, 0, 0, size, size);
        var template = Rectangle(size, size, x, y, 10, 12);
        var output = BadgePixels.Compose(source, size, size, template, size, size);
        var bounds = BadgePixels.Bounds(output, size, size);
        Assert.True(bounds.Width <= 10 && bounds.Height <= 12);
        Assert.Equal(x + 10, bounds.X + bounds.Width);
        Assert.Equal(y + 12, bounds.Y + bounds.Height);
    }

    [Fact]
    public void EmptySourceOrTemplateProducesNoStaleBadge()
    {
        var filled = Rectangle(8, 8, 0, 0, 8, 8);
        Assert.All(BadgePixels.Compose(new byte[256], 8, 8, filled, 8, 8), value => Assert.Equal(0, value));
        Assert.All(BadgePixels.Compose(filled, 8, 8, new byte[256], 8, 8), value => Assert.Equal(0, value));
    }

    [Fact]
    public void CompositionDoesNotModifySourceSpritesAndKeepsTheirPixelColors()
    {
        var source = Rectangle(32, 32, 1, 1, 24, 30);
        var template = Rectangle(64, 64, 45, 46, 15, 14);
        var before = source.ToArray();
        var starBefore = template.ToArray();
        var output = BadgePixels.Compose(source, 32, 32, template, 64, 64);
        Assert.Equal(before, source);
        Assert.Equal(starBefore, template);
        var first = Enumerable.Range(0, 64 * 64).First(i => output[i * 4 + 3] != 0) * 4;
        Assert.Equal(new byte[] { 12, 34, 56, 255 }, output.Skip(first).Take(4));
    }

    private static byte[] Rectangle(int width, int height, int x, int y, int w, int h)
    {
        var result = new byte[width * height * 4];
        for (var row = y; row < y + h; row++)
            for (var column = x; column < x + w; column++)
                new byte[] { 12, 34, 56, 255 }.CopyTo(result, (row * width + column) * 4);
        return result;
    }
}
