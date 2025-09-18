/* License: NCSA Open Source License
 *
 * Copyright: SPT
 * AUTHORS:
 * Basuro
 */

namespace SPTarkov.Core.Models;

public class DiffResult
{
    public DiffResult(DiffResultEnum resultEnum, PatchInfo patchInfo)
    {
        ResultEnum = resultEnum;
        PatchInfo = patchInfo;
    }

    public DiffResultEnum ResultEnum { get; }

    public PatchInfo PatchInfo { get; }
}
