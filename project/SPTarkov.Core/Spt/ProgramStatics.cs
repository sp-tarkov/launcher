using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.SPT;

public static partial class ProgramStatics
{
    public static Version SptVersionCompiledFor
    {
        get { return SptVersion; }
    }
}
