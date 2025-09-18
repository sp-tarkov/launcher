/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

namespace SPTarkov.Core.Models;

public enum PatchResultEnum
{
    Success,
    InputLengthMismatch,
    InputChecksumMismatch,
    AlreadyPatched,
    OutputChecksumMismatch
}
