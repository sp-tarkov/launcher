/* LoginRequestData.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


namespace SPT.Launcher
{
    public struct LoginRequestData
    {
        public string username;

        public LoginRequestData(string username)
        {
            this.username = username;
        }
    }
}
