using System.Reflection;

namespace SPT.Launcher.PatchGen
{
    // Mostly stolen from the downgrade patcher
    public static class ResourceManager
    {
        private static string TempDir = Path.Combine(Environment.CurrentDirectory, "Resources");
        private static string HDiffEXE = "hdiffz.exe";
        public static string HDiffPath = Path.Combine(TempDir, HDiffEXE);

        public static void ExtractResourcesToTempDir()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                switch (resource)
                {
                    case string a when a.EndsWith(HDiffEXE):
                        {
                            StreamResourceOut(assembly, resource, HDiffPath);
                            break;
                        }
                }
            }
        }

        private static void StreamResourceOut(Assembly assembly, string ResourceName, string OutputFilePath)
        {
            FileInfo outputFile = new FileInfo(OutputFilePath);

            if (outputFile.Exists)
            {
                outputFile.Delete();
            }

            if (outputFile.Directory?.Exists == false)
            {
                Directory.CreateDirectory(outputFile.Directory.FullName);
            }

            using (FileStream fs = File.Create(OutputFilePath))
            using (Stream? stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream != null)
                {
                    stream.CopyTo(fs);
                }
            }
        }

        public static void CleanupTempDir()
        {
            DirectoryInfo dir = new DirectoryInfo(TempDir);

            if (dir.Exists)
            {
                dir.Delete(true);
            }
        }
    }
}
