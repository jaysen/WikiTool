using System.IO;
using System.Linq;

namespace WikiTool.Tests
{
    public static class TestUtilities
    {
        private static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

        internal static string GetTestDataRoot()
        {
            var solutionPath = TryGetSolutionDirectoryInfo();
            return Path.Combine(solutionPath.FullName, "test_data");
        }

        internal static string GetTestFolder(string folderName)
        {
            var testFolder = Path.Combine(GetTestDataRoot(), folderName);
            Directory.CreateDirectory(testFolder);
            return testFolder;
        }
    }
}
