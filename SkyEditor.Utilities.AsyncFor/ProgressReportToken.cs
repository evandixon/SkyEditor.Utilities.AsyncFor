using System;
using System.Collections.Generic;
using System.Text;

namespace SkyEditor.Utilities.AsyncFor
{
    /// <summary>
    /// A basic implementation of <see cref="IReportProgress"/> that can be used to relay progress reporting when a function's parent class cannot implement this interface.
    /// </summary>
    public class ProgressReportToken : IReportProgress
    {
        public event EventHandler<ProgressReportedEventArgs> ProgressChanged;
        public event EventHandler Completed;

        public float Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                RaiseProgressChanged();
            }
        }
        private float _progress;

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                RaiseProgressChanged();
            }
        }
        private string _message;

        public bool IsIndeterminate
        {
            get
            {
                return _isIndeterminate;
            }
            set
            {
                _isIndeterminate = value;
                RaiseProgressChanged();
            }
        }
        private bool _isIndeterminate;

        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
            set
            {
                _isCompleted = value;
                if (value)
                {
                    _progress = 1;
                }
                RaiseCompleted();
            }
        }
        private bool _isCompleted;

        protected void RaiseProgressChanged()
        {
            ProgressChanged?.Invoke(this, new ProgressReportedEventArgs() { IsIndeterminate = IsIndeterminate, Message = Message, Progress = Progress });
        }

        protected void RaiseCompleted()
        {
            Completed?.Invoke(this, new EventArgs());
        }
    }
}
