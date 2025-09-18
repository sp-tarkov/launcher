/* ProgressInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

namespace SPT.Launcher.Models.Launcher
{
    public class ProgressInfo
    {
        public int Percentage { get; private set; }
        public string Message { get; private set; }

        public ProgressInfo(int Percentage, string Message)
        {
            this.Percentage = Percentage;
            this.Message = Message;
        }
    }
}
