using System.Globalization;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.SPT;

public static partial class ProgramStatics
{
    // Development override for the SPT version the Forge and mod manager use.
    // TODO: This needs to be removed for any 4.1 launch.
    private static readonly Version? ForcedForgeSptVersion = Version.Parse("4.0.13");

    public static Version SptVersionCompiledFor
    {
        // TODO: This needs to be reverted for any 4.1 launch.
        // get { return SptVersion; }
        get { return ForcedForgeSptVersion ?? SptVersion; }
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
