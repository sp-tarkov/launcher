/* IUpdateProgress
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using SPT.Launcher.Models.Launcher;
using System;

namespace SPT.Launcher.Interfaces
{
    public interface IUpdateProgress
    {
        /// <summary>
        /// The task that will report progress to the <see cref="Custom_Controls.Dialogs.ProgressDialog"/>
        /// </summary>
        public Action ProgressableTask { get; }

        /// <summary>
        /// Cancel the ProgressableTask with a reason.
        /// </summary>
        public event EventHandler<object> TaskCancelled;

        /// <summary>
        /// The <see cref="Custom_Controls.Dialogs.ProgressDialog"/> will subscribe to this event to update its main progress bar (top bar)
        /// </summary>
        public event EventHandler<ProgressInfo> ProgressChanged;
    }
}
