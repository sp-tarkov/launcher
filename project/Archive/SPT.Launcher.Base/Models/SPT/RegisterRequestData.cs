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
        public string password;
        public string edition;

        public RegisterRequestData(string username, string password, string edition)
        {
            this.username = username;
            this.password = password;
            this.edition = edition;
        }
    }
}
