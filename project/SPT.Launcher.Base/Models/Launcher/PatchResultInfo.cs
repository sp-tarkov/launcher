/* PatchResultInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

namespace SPT.Launcher.Models.Launcher
{
    public class PatchResultInfo
    {
        public PatchResultType Status { get; }
        
        public int NumCompleted { get; }
        
        public int NumTotal { get; }

        public bool OK => (Status == PatchResultType.Success);
        
        public int PercentComplete => (NumCompleted * 100) / NumTotal;

        public PatchResultInfo(PatchResultType Status, int NumCompleted, int NumTotal)
        {
            this.Status = Status;
            this.NumCompleted = NumCompleted;
            this.NumTotal = NumTotal;
        }
    }

    public enum PatchResultType
    {
        Success,
        InputLengthMismatch,
        InputChecksumMismatch,
        OutputChecksumMismatch
    }
}
