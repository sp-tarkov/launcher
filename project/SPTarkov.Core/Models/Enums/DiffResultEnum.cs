/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

namespace SPTarkov.Core.Models;

public enum DiffResultEnum
{
    Success,
    OriginalFilePathInvalid,
    OriginalFileNotFound,
    OriginalFileReadFailed,
    PatchedFilePathInvalid,
    PatchedFileNotFound,
    PatchedFileReadFailed,
    FilesMatch
}
