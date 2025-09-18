/* ProfileInfo.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using SPT.Launcher.Helpers;
using SPT.Launcher.MiniCommon;
using SPT.Launcher.Models.SPT;
using System;
using System.ComponentModel;
using System.IO;
using SPT.Launcher.Utilities;
using System.Threading.Tasks;

namespace SPT.Launcher.Models.Launcher
{
    public class ProfileInfo : NotifyPropertyChangedBase
    {
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _nickname;
        public string Nickname
        {
            get => _nickname;
            set => SetProperty(ref _nickname, value);
        }

        private string _sideImage;
        public string SideImage
        {
            get => _sideImage;
            set => SetProperty(ref _sideImage, value);
        }

        private string _side;
        public string Side
        {
            get => _side;
            set => SetProperty(ref _side, value);
        }

        private string _level;
        public string Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        private int _XPLevelProgress;
        public int XPLevelProgress
        {
            get => _XPLevelProgress;
            set => SetProperty(ref _XPLevelProgress, value);
        }

        private long _currentXP;
        public long CurrentExp
        {
            get => _currentXP;
            set => SetProperty(ref _currentXP, value);
        }

        private long _remainingExp;
        public long RemainingExp
        {
            get => _remainingExp;
            set => SetProperty(ref _remainingExp, value);
        }

        private long _nextLvlExp;
        public long NextLvlExp
        {
            get => _nextLvlExp;
            set => SetProperty(ref _nextLvlExp, value);
        }

        private bool _hasData;
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }

        public string MismatchMessage => VersionMismatch ? LocalizationProvider.Instance.profile_version_mismath : null;

        private bool _versionMismatch;
        public bool VersionMismatch
        {
            get => _versionMismatch;
            set => SetProperty(ref _versionMismatch, value);
        }

        private SPTData _spt;
        public SPTData SPT
        {
            get => _spt;
            set => SetProperty(ref _spt, value);
        }

        public void UpdateDisplayedProfile(ProfileInfo pInfo)
        {
            if (pInfo.Side == null || string.IsNullOrWhiteSpace(pInfo.Side) || pInfo.Side == "unknown") return;

            HasData = true;
            Nickname = pInfo.Nickname;
            Side = pInfo.Side;
            SideImage = pInfo.SideImage;
            Level = pInfo.Level;
            CurrentExp = pInfo.CurrentExp;
            NextLvlExp = pInfo.NextLvlExp;
            RemainingExp = pInfo.RemainingExp;
            XPLevelProgress = pInfo.XPLevelProgress;

            SPT = pInfo.SPT;
        }

        /// <summary>
        /// Checks if the SPT versions are compatible (non-major changes)
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="expectedVersion"></param>
        /// <returns></returns>
        private bool CompareVersions(string currentVersion, string expectedVersion)
        {
            if (expectedVersion == "") return false;

            var v1 = new SPTVersion(currentVersion);
            var v2 = new SPTVersion(expectedVersion);

            // check 'X'.x.x
            if (v1.Major != v2.Major) return false;

            // check x.'X'.x
            if(v1.Minor != v2.Minor) return false;

            //otherwise probably good
            return true;
        }

        public ProfileInfo(ServerProfileInfo serverProfileInfo)
        {
            Username = serverProfileInfo.username;
            Nickname = serverProfileInfo.nickname;
            Side = serverProfileInfo.side;

            SPT = serverProfileInfo.SPTData;

            if (SPT != null)
            {
                Task.Run(async () =>
                {
                    VersionMismatch = !CompareVersions(SPT.version, await ServerManager.GetVersionAsync());
                });
            }

            SideImage = Path.Combine(ImageRequest.ImageCacheFolder, $"side_{Side.ToLower()}.png");

            HasData = Side != null && !string.IsNullOrWhiteSpace(Side) && Side != "unknown";

            Level = serverProfileInfo.currlvl.ToString();
            CurrentExp = serverProfileInfo.currexp;

            //check if player is max level
            if (Level == serverProfileInfo.maxlvl.ToString())
            {
                NextLvlExp = 0;
                XPLevelProgress = 100;
                return;
            }

            NextLvlExp = serverProfileInfo.nextlvl;
            RemainingExp = NextLvlExp - CurrentExp;

            long currentLvlTotal = NextLvlExp - serverProfileInfo.prevexp;

            XPLevelProgress = (int)Math.Floor((((double)currentLvlTotal) - RemainingExp) / currentLvlTotal * 100);
        }
    }
}
