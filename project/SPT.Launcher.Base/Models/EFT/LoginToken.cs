/* LoginToken.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 */


namespace SPT.Launcher
{
    public struct LoginToken
    {
        public string username;
        public bool toggle;
        public long timestamp;

        public LoginToken(string username)
        {
            this.username = username;
            toggle = true;
            timestamp = 0;
        }
    }
}
