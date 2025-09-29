using System.Diagnostics;
using System.Text.Json.Serialization;

namespace SPTarkov.Core.Models;

public record SPTVersion
{
    public int Major { get; set; }

    public int Minor { get; set; }

    public int Build { get; set; }
    private string tag { get; set; }

    public SPTVersion(string version)
    {
        if (version.Contains('-'))
        {
            var split = version.Split('-');
            version = split[0];
            tag = split[1];
        }

        var splitversion = version!.Split('.');
        Major = int.Parse(splitversion[0]);
        Minor = int.Parse(splitversion[1]);
        Build = int.Parse(splitversion[2]);
    }

    public override string ToString()
    {
        return tag != null ? $"{Major}.{Minor}.{Build}-{tag}" : $"{Major}.{Minor}.{Build}";
    }
}
