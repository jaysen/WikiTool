using System.IO;
using System.Linq;
using System.Text;

namespace WikiTools.Core.Tests
{
    public static class TestUtilities
    {
        internal static DirectoryInfo TryGetSolutionDirectoryInfo(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

        internal static string SetTestFolder()
        {
            var solutionPath = TestUtilities.TryGetSolutionDirectoryInfo();
            var dataFolder = Path.Combine(solutionPath.FullName, @"test_data");
            Directory.CreateDirectory(dataFolder);
            return dataFolder;
        }
    }
}
