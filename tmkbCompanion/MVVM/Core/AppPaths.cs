using System;
using System.IO;

namespace tmkbCompanion.MVVM.Core
{
    public static class AppPaths
    {
        public static string BaseDataDirectory { get; } = GetBaseDataDirectory();

        private static string GetBaseDataDirectory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tmkbCompanion");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
