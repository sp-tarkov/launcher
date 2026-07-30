namespace SPTarkov.Core.Patching;

public class PatchResultInfo(PatchResultEnum status, int numCompleted, int numTotal)
{
    public PatchResultEnum Status { get; } = status;

    private int NumCompleted { get; } = numCompleted;

    private int NumTotal { get; } = numTotal;

    public bool Ok
    {
        get { return (Status == PatchResultEnum.Success); }
    }

    public int PercentComplete
    {
        get { return (NumCompleted * 100) / NumTotal; }
    }
}
