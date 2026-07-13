# Launcher auto-update

The launcher can update itself between full SPT releases over two channels, **stable** and **edge**. This document covers the one-time setup and the release process.

## How it works

The launcher version is `Major.Minor.Patch.Revision`:

- `Major.Minor.Patch` is the SPT version line the launcher targets (the auto-update compatibility key).
- `Revision` is the launcher build within that line. **`.0` is reserved** for the launcher that ships inside the full SPT releases; auto-update builds start at `.1` and never push a full SPT release version.

`Major.Minor.Patch` comes from `SptVersion` in `project/Build.props`. `Revision` stays `0` in the source and is overwritten by the release workflow.

## Making a release

Push a tag from the branch whose `Build.props` `SptVersion` matches the tag's line:

| Tag            | Channel | Written to                    |
|----------------|---------|-------------------------------|
| `4.1.0.3`      | stable  | `stable.json` and `edge.json` |
| `4.1.0.3-EDGE` | edge    | `edge.json`                   |

Rules the workflow enforces:

- The revision must be `1` or greater (never `.0`).
- The revision must not already exist in the target manifest.
- The tag's `Major.Minor.Patch` must equal `Build.props` `SptVersion`.
- The client delta (`Assembly-CSharp.dll.delta`) must be unchanged from the previous build on the same line. A new delta belongs to a full SPT release, not a launcher auto-update.
- The structure of the build must abide by the rules within `UpdatePayload`; two executables and the rest of the files within a `SPT_Data/Launcher/` subdirectory.

The workflow builds `win-x64` and `linux-x64`, merges both launcher binaries and `SPT_Data` into a single cross-platform payload zip, publishes it as an immutable GitHub release, appends a signed entry to the channel manifests, and uploads the manifests to R2.

## Setup

### 1. Signing key

The manifests are signed with an ECDSA P-256 key. The private half is a repository secret; the public half is compiled into the launcher, so a manifest can't be forged without a rebuild.

```bash
# Generate the private key (PKCS#8, unencrypted).
openssl ecparam -name prime256v1 -genkey -noout -out ec.pem
openssl pkcs8 -topk8 -nocrypt -in ec.pem -out launcher-manifest-private.pem
rm ec.pem

# Print the public key as base64 SubjectPublicKeyInfo.
openssl pkey -in launcher-manifest-private.pem -pubout -outform DER | base64 -w0
```

- Add the contents of `launcher-manifest-private.pem` as the repository secret `LAUNCHER_MANIFEST_PRIVATE_KEY`.
- Paste the base64 public key into `ProgramStatics.UpdateSigningPublicKey` (`project/SPTarkov.Core/Spt/ProgramStatics.cs`).
- Delete the local private key file once the secret is stored.

Until the public key is set, the launcher treats every manifest as unverified and offers no updates.

### 2. R2 secrets

Add these repository secrets:

| Secret                 | Value                                        |
|------------------------|----------------------------------------------|
| `R2_ACCESS_KEY`        | R2 access key id                             |
| `R2_SECRET_ACCESS_KEY` | R2 secret access key                         |
| `R2_BUCKET_NAME`       | `launcher-auto-update`                       |
| `R2_ENDPOINT`          | `https://<account>.r2.cloudflarestorage.com` |
| `R2_FRONT`             | `https://launcher-auto-update.sp-tarkov.com` |

The bucket is served publicly at `R2_FRONT` via a Cloudflare custom domain. The workflow sets a 60-second `Cache-Control` so a withdrawn build stops being offered quickly.

### 3. Immutable releases

Enable **Immutable releases** in the repository settings. Every new release locks its assets and tag on publish. Releases can be deleted, but the tag used to create the release can never be pushed again.

## Verifying a published manifest by hand

After a launcher has been published the manifest can be verified manually using the following commands:

```bash
curl -sO https://launcher-auto-update.sp-tarkov.com/edge.json
curl -sO https://launcher-auto-update.sp-tarkov.com/edge.json.sig

# Replace PUBLIC_KEY with the value from ProgramStatics.UpdateSigningPublicKey.
echo "PUBLIC_KEY" | base64 -d | openssl pkey -pubin -inform DER -out pub.pem

base64 -d edge.json.sig > edge.sig.der
openssl dgst -sha256 -verify pub.pem -signature edge.sig.der edge.json
```

## Yanking a release

Run the `Yank Launcher Release` workflow from the Actions tab with the launcher version to withdraw (e.g. `4.1.0.3`). It marks the version "yanked" in every manifest that lists it, re-signs them, and re-uploads. Launchers stop being offered the build within the manifest cache window (about a minute). The GitHub release and its assets stay published; yanking only stops the manifests from offering the release.
