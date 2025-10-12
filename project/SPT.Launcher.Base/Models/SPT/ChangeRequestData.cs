/* ChangeRequestData.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


namespace SPT.Launcher
{
    public struct ChangeRequestData
    {
        public string username;
        public string change;

        public ChangeRequestData(string username, string change)
        {
            this.username = username;
            this.change = change;
        }
    }
}
