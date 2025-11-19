using System.IO;
using WikiTools;
using Xunit;

namespace WikiTools.Tests;

public class StaticLocalPagesTests
{
    private static string _testFolder;

        
    public StaticLocalPagesTests()
    {
        _testFolder = TestUtilities.SetTestFolder();
    }

    [Fact]
    public void GetPageContent_ReadSimpleText()
    {
        //arrange
        var path = Path.Combine(_testFolder, "SimpleRead.wiki");
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
        var path = Path.Combine(_testFolder, "Test2.wiki");
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