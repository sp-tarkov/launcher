/* ServerProfileInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

namespace SPT.Launcher.Models.SPT
{
    public class ServerProfileInfo
    {
        public string username { get; set; }
        public string nickname { get; set; }
        public string side { get; set; }
        public int currlvl { get; set; }
        public long currexp { get; set; }
        public long prevexp { get; set; }
        public long nextlvl { get; set; }
        public int maxlvl { get; set; }
        public SPTData SPTData { get; set; }
    }
}
