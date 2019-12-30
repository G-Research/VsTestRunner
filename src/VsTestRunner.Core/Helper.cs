using System.IO;

namespace VsTestRunner.Core
{
    public static class Helper
    {
        public static string FileNameWithoutExtension(this FileInfo file)
        {
            return Path.GetFileNameWithoutExtension(file.Name);
        }
    }
}
