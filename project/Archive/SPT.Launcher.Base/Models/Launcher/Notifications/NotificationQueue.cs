/* NotificationQueue.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */


using SPT.Launcher.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using SPT.Launcher.Utilities;

namespace SPT.Launcher.Models.Launcher.Notifications
{
    public class NotificationQueue : NotifyPropertyChangedBase, IDisposable
    {
        public Timer queueTimer = new Timer();
        private Timer animateChangeTimer = new Timer(230);
        private Timer animateCloseTimer = new Timer(230);

        public ObservableCollection<NotificationItem> queue { get; set; } = [];

        private bool _showBanner;
        public bool ShowBanner
        {
            get => _showBanner;
            set => SetProperty(ref _showBanner, value);
        }

        public NotificationQueue(int showTimeInMilliseconds)
        {
            ShowBanner = false;
            queueTimer.Interval = showTimeInMilliseconds;
            queueTimer.Elapsed += QueueTimer_Elapsed;

            animateChangeTimer.Elapsed += AnimateChange_Elapsed;
            animateCloseTimer.Elapsed += AnimateCloseTimer_Elapsed;
        }

        private void AnimateCloseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            animateCloseTimer.Stop();

            queue.Clear();
            queueTimer.Stop();
        }

        public void CloseQueue()
        {
            ShowBanner = false;
            animateCloseTimer.Start();
        }

        private void CheckAndShowNotifications()
        {
            if (!queueTimer.Enabled)
            {
                ShowBanner = true;
                queueTimer.Start();
            }
        }

        public void Enqueue(string message, bool autoNext = false, bool noDefaultButton = false)
        {
            if (queue.All(x => x.Message != message))
            {
                if (noDefaultButton)
                {
                    queue.Add(new NotificationItem(message));
                }
                else
                {
                    queue.Add(new NotificationItem(message, LocalizationProvider.Instance.ok, () => { }));
                }

                CheckAndShowNotifications();

                if (autoNext && queue.Count == 2)
                {
                    Next(true);
                }
            }
        }

        public void Enqueue(string message, string buttonText, Action buttonAction, bool allowNext = false)
        {
            if (queue.All(x=>x.Message != message && x.ButtonText != buttonText))
            {
                queue.Add(new NotificationItem(message, buttonText, buttonAction));
                CheckAndShowNotifications();

                if (allowNext && queue.Count == 2)
                {
                    Next(true);
                }
            }
        }

        public void Next(bool resetTimer = false)
        {
            if (queue.Count - 1 <= 0)
            {
                CloseQueue();
                return;
            }

            if (resetTimer)
            {
                queueTimer.Stop();
                queueTimer.Start();
            }

            ShowBanner = false;
            animateChangeTimer.Start();
        }

        private void QueueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Next();
        }

        private void AnimateChange_Elapsed(object sender, ElapsedEventArgs e)
        {
            animateChangeTimer.Stop();

            if (queue.Count > 0)
            {
                queue.RemoveAt(0);
            }

            ShowBanner = true;
        }

        public void Dispose()
        {
            queueTimer.Dispose();
            animateChangeTimer.Dispose();
            animateCloseTimer.Dispose();
        }
    }
}
