using System.Reflection;
using Photino.NET;

namespace SPTarkov.Core.Extensions;

public static class PhotinoWindowExtensions
{
    public static PhotinoWindow SetIconFile(this PhotinoWindow window, Stream iconStream, string fileName)
    {
        var iconpath = ExtractEmbeddedResourceToTempFile(iconStream, fileName);
        return iconpath != null ? window.SetIconFile(iconpath) : window;
    }

    private static string? ExtractEmbeddedResourceToTempFile(Stream iconStream, string fileName)
    {
        if (iconStream == null)
        {
            Console.WriteLine("Icon stream is null");
            return null;
        }
        string tempFile = Path.Join(Path.GetTempPath(), fileName);
        using (FileStream fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
        {
            iconStream.CopyTo(fileStream);
        }
        return tempFile;
    }
}
