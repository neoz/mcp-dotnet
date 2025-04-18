using MCPPOC;
using Xunit;

namespace MCPPOC;

public class paginateTest
{

    [Fact]
    public void Paginate_ReturnsCorrectPage()
    {
        // Arrange
        string[] items = { "a", "b", "c", "d", "e" };
        int offset = 1;
        int pageSize = 2;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Equal(new[] { "b", "c" }, result);
    }

    [Fact]
    public void Paginate_ReturnsEmptyArray_WhenOffsetExceedsLength()
    {
        // Arrange
        string[] items = { "a", "b", "c" };
        int offset = 5;
        int pageSize = 2;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Paginate_ReturnsRemainingItems_WhenPageSizeExceedsRemainingItems()
    {
        // Arrange
        string[] items = { "a", "b", "c" };
        int offset = 1;
        int pageSize = 5;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Equal(new[] { "b", "c" }, result);
    }

    [Fact]
    public void Paginate_ReturnsEmptyArray_WhenItemsArrayIsEmpty()
    {
        // Arrange
        string[] items = Array.Empty<string>();
        int offset = 0;
        int pageSize = 2;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Paginate_ReturnsEmptyArray_WhenPageSizeIsZero()
    {
        // Arrange
        string[] items = { "a", "b", "c" };
        int offset = 1;
        int pageSize = 0;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Paginate_ReturnsFullArray_WhenOffsetIsZeroAndPageSizeExceedsLength()
    {
        // Arrange
        string[] items = { "a", "b", "c" };
        int offset = 0;
        int pageSize = 10;

        // Act
        string[] result = paginate.Paginate(items, offset, pageSize);

        // Assert
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }
}