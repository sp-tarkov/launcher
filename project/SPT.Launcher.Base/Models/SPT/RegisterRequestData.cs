/* RegisterRequestData.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


namespace SPT.Launcher
{
    public struct RegisterRequestData
    {
        public string username;
        public string edition;

        public RegisterRequestData(string username, string edition)
        {
            this.username = username;
            this.edition = edition;
        }
    }
}
