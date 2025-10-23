namespace SPTarkov.Core.Models.Spt;

public record SptVersion
{
    public int Major { get; set; }

    public int Minor { get; set; }

    public int Build { get; set; }
    private string Tag { get; set; }

    public SptVersion(string version)
    {
        if (version.Contains('-'))
        {
            var split = version.Split('-');
            version = split[0];
            Tag = split[1];
        }

        var splitversion = version!.Split('.');
        Major = int.Parse(splitversion[0]);
        Minor = int.Parse(splitversion[1]);
        Build = int.Parse(splitversion[2]);
    }

    public override string ToString()
    {
        return Tag != null ? $"{Major}.{Minor}.{Build}-{Tag}" : $"{Major}.{Minor}.{Build}";
    }
}
