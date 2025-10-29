namespace SPTarkov.Core.Patching;

public class PatchResultInfo
{
    public PatchResultEnum Status { get; }

    public int NumCompleted { get; }

    public int NumTotal { get; }

    public bool Ok
    {
        get { return (Status == PatchResultEnum.Success); }
    }

    public int PercentComplete
    {
        get { return (NumCompleted * 100) / NumTotal; }
    }

    public PatchResultInfo(PatchResultEnum status, int numCompleted, int numTotal)
    {
        Status = status;
        NumCompleted = numCompleted;
        NumTotal = numTotal;
    }
}
