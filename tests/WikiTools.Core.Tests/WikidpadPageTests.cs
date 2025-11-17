using System;
using System.IO;
using Xunit;

namespace WikiLib.Tests
{
    public class WikidpadPageTests
    {
        private static string _testFolder;

        public WikidpadPageTests()
        {
            _testFolder = TestUtilities.SetTestFolder();
        }

        [Fact]
        public void Constructor_ThrowsIfFileNotExists()
        {
            //arrange
            var path = "NoSuchFile.wiki";

            //assert
            Assert.Throws<FileNotFoundException>(() => new WikidpadPage(path));
        }

        [Fact]
        public void Constructor_ThrowsIfWrongExtension()
        {
            //arrange
            var path = Path.Combine(_testFolder, "TestPage.md");
            File.Create(path);

            //assert
            Assert.Throws<FormatException>( () => new WikidpadPage(path));
        }

        [Fact]
        public void GetPageContent_ReadSimpleText()
        {
            //arrange
            var path = Path.Combine(_testFolder, "SimpleRead.wiki");
            var expected = "test1";
            File.WriteAllText(path, expected);

            //actual
            var sut = new WikidpadPage(path);
            var actual = LocalPages.GetPageContent(path);

            //assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetPageContent_ThrowsIfFileNotExists()
        {
            //arrange
            var path = "NoFileOfThisNameExists.txt";
            //assert
            Assert.Throws<FileNotFoundException>(() => new WikidpadPage(path));
        }
    }
}
