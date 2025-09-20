namespace SPTarkov.Core.Patching;

public class PatchResultInfo
{
    public PatchResultEnum Status { get; }

    public int NumCompleted { get; }

    public int NumTotal { get; }

    public bool OK => (Status == PatchResultEnum.Success);

    public int PercentComplete => (NumCompleted * 100) / NumTotal;

    public PatchResultInfo(PatchResultEnum Status, int NumCompleted, int NumTotal)
    {
        this.Status = Status;
        this.NumCompleted = NumCompleted;
        this.NumTotal = NumTotal;
    }
}
