namespace SPTarkov.Core.Patching;

public class PatchResultInfo
{
    public PatchResultEnum Status { get; }

    public int NumCompleted { get; }

    public int NumTotal { get; }

    public bool Ok => (Status == PatchResultEnum.Success);

    public int PercentComplete => (NumCompleted * 100) / NumTotal;

    public PatchResultInfo(PatchResultEnum status, int numCompleted, int numTotal)
    {
        this.Status = status;
        this.NumCompleted = numCompleted;
        this.NumTotal = numTotal;
    }
}
