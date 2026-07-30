using Photino.NET;

namespace SPTarkov.Core.Extensions;

public static class PhotinoWindowExtensions
{
    public static PhotinoWindow SetIconFile(this PhotinoWindow window, Stream iconStream, string fileName)
    {
        var iconpath = ExtractEmbeddedResourceToTempFile(window.TemporaryFilesPath, iconStream, fileName);
        return iconpath != null ? window.SetIconFile(iconpath) : window;
    }

    private static string? ExtractEmbeddedResourceToTempFile(string temporaryFilesPath, Stream? iconStream, string fileName)
    {
        if (iconStream == null)
        {
            Console.WriteLine("Icon stream is null");
            return null;
        }
        var tempFile = Path.Join(temporaryFilesPath, fileName);
        using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
        iconStream.CopyTo(fileStream);
        return tempFile;
    }
}
