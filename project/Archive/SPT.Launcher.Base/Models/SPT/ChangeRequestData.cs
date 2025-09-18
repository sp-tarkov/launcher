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
        public string password;
        public string change;

        public ChangeRequestData(string username, string password, string change)
        {
            this.username = username;
            this.password = password;
            this.change = change;
        }
    }
}
