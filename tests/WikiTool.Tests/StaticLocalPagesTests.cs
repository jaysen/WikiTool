using System.IO;
using WikiTool;
using Xunit;

namespace WikiTool.Tests;

public class StaticLocalPagesTests
{
    private readonly string _testFolder;


    public StaticLocalPagesTests()
    {
        _testFolder = TestUtilities.GetTestFolder("local_pages_tests");
    }

    [Fact]
    public void GetPageContent_ReadSimpleText()
    {
        //arrange
        var path = Path.Combine(_testFolder, "simple_read_test.wiki");
        var expected = "test1";
        File.WriteAllText(path, expected);

        //actual
        var actual = LocalPages.GetPageContent(path);

        //assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetPageContent_ThrowsIfFileNotExist()
    {
        //arrange
        var path = "NoFileOfThisNameExists.txt";
        //assert
        Assert.Throws<FileNotFoundException>(() => LocalPages.GetPageContent(path));
    }

    [Theory]
    [InlineData("NUMNUM",false)]
    [InlineData("a has",true)]
    [InlineData("textand",false)]
    public void ContainsText_ReturnsCorrectly(string searchStr, bool expected)
    {
        //arrange
        var path = Path.Combine(_testFolder, "contains_text_test.wiki");
        using (var sw = File.CreateText(path))
        {
            sw.WriteLine("## MD Heading");
            sw.WriteLine("some text");
            sw.WriteLine("and a hashtag #test");
        }

        //actual
        Assert.Equal(expected, LocalPages.ContainsText(path, searchStr));
    }
    [Fact]
    public void ContainsText_ThrowsIfFileNotExist()
    {
        //arrange
        var path = "NoFileOfThisNameExists.txt";
        //assert
        Assert.Throws<FileNotFoundException>(() => LocalPages.ContainsText(path,""));
    }
}