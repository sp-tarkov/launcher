using System.Globalization;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.SPT;

public static partial class ProgramStatics
{
    public static Version SptVersionCompiledFor
    {
        get { return SptVersion; }
    }

    public static string SptCommit
    {
        get { return SptCommitTag; }
    }

    public static System.Version LauncherVersion
    {
        get { return LauncherVersionValue; }
    }

    public static DateTime LauncherBuildUtc
    {
        get
        {
            return DateTime.Parse(
                LauncherBuildDateRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
            );
        }
    }

    /// <summary>Base64 SubjectPublicKeyInfo of the ECDSA P-256 key that verifies update manifests.</summary>
    public const string UpdateSigningPublicKey =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENNtehjqF8s+cKtwvS9fKFJHsKTN7dNrjKu3Rq5ByH7oc02Uktd2hdrU2bgQslAT3i6pFSt6Szfh64ZBRqQwM6Q==";
}
