using System;
using System.Collections.Generic;
using System.Text;

namespace SkyEditor.Utilities.AsyncFor
{
    public class ProgressReportedEventArgs : EventArgs
    {
        /// <summary>
        /// A percentage representing the current progress of the operation
        /// </summary>
        public float Progress { get; set; }

        /// <summary>
        /// A user-friendly string identifying what the operation is doing
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Whether or not progress can be somewhat-accurately determined
        /// </summary>
        public bool IsIndeterminate { get; set; }
    }
}
