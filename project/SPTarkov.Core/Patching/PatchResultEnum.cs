/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

namespace SPTarkov.Core.Patching;

public enum PatchResultEnum
{
    Success,
    InputLengthMismatch,
    InputChecksumMismatch,
    OutputChecksumMismatch
}
