using System.Security.Cryptography;

namespace SPTarkov.Core.Update;

public static class ManifestSignature
{
    /// <summary>Produces a DER-encoded ECDSA P-256/SHA-256 signature over the manifest bytes.</summary>
    public static byte[] Sign(byte[] data, string privateKeyPem)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);
        return ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    /// <summary>Verifies a DER-encoded ECDSA P-256/SHA-256 signature.</summary>
    public static bool Verify(byte[] data, byte[] signatureDer, string publicKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(publicKeyBase64))
        {
            return false;
        }

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            return ecdsa.VerifyData(data, signatureDer, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
