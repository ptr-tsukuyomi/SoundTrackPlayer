using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    public class BackgroundTaskProgress
    {
        public Double Progress { get; set; } = Double.NegativeInfinity;
        public BackgroundTaskState State { get; set; } = BackgroundTaskState.Waiting;
        public String Result { get; set; } = "";
        public bool IsFailureFound { get; set; } = false;
    }

    public enum BackgroundTaskState
    {
        Waiting,
        InProgress,
        Succeeded,
        Failed
    }

    public class BackgroundTask : INotifyPropertyChanged
    {
        public BackgroundTask() { }
        public string Name { get; set; } = "New Background Task";

        public Task Task { get; set; } = Task.CompletedTask;

        public string Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                TaskStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private string _result = "";

        public BackgroundTaskState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));

                    if (_state == BackgroundTaskState.Succeeded || _state == BackgroundTaskState.Failed)
                    {
                        TaskCompletedDateTime = DateTime.Now;
                    }
                    TaskStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private BackgroundTaskState _state = BackgroundTaskState.Waiting;


        public Progress<BackgroundTaskProgress>? ProgressReporter
        {
            get
            {
                return _progress_reporter;
            } 
            set
            {
                _progress_reporter = value;
                if (_progress_reporter is not null)
                {
                    _progress_reporter.ProgressChanged += (s, e) =>
                    {
                        if (!(e.Progress < 0))
                        {
                            Progress = e.Progress;
                        }
                        State = e.State;
                        if (!string.IsNullOrEmpty(e.Result))
                        {
                            Result = string.IsNullOrEmpty(Result) ? e.Result : Result + "\r\n" + e.Result;
                        }
                        if (e.IsFailureFound)
                        {
                            IsFailureFound = true;
                        }
                    };
                }
            }
        }
        private Progress<BackgroundTaskProgress>? _progress_reporter = null;

        public bool IsFailureFound
        {
            get
            {
                return _is_failure_found;
            }
            set
            {
                _is_failure_found = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFailureFound)));
                TaskStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private bool _is_failure_found = false;

        public Double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                TaskStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private Double _progress = 0.0;

        public DateTime TaskStartedDateTime { get; set; } = DateTime.Now;
        public DateTime? TaskCompletedDateTime { get; set; } = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? TaskStateChanged;
    }

    public class BackgroundTaskRunner
    {
        private List<BackgroundTask> _tasks = new List<BackgroundTask>();

        private List<BackgroundTask> _non_completed_tasks { get; set; } = new List<BackgroundTask>();

        public void StartFirstWaitingTask()
        {
            var bg_task = _non_completed_tasks.Find((t) => t.State == BackgroundTaskState.Waiting);
            if (bg_task is not null)
            {
                bg_task.Task.Start();
                bg_task.State = BackgroundTaskState.InProgress;
            }
        }

        public void Enqueue(BackgroundTask task)
        {
            task.TaskStateChanged += TaskStateChangedHandler;
            _tasks.Add(task);
            TaskEnqueued?.Invoke(this, task);

            if (!task.Task.IsCompleted)
            {
                _non_completed_tasks.Add(task);
                if (_non_completed_tasks.Count == 1)
                {
                    StartFirstWaitingTask();
                }
            }
        }

        private void TaskStateChangedHandler(object? sender, EventArgs e)
        {
            if (sender is null) throw new Exception("Sender is null in TaskCompleted event.");

            if (sender is BackgroundTask t)
            {
                switch (t.State)
                {
                    case BackgroundTaskState.Succeeded:
                    case BackgroundTaskState.Failed:
                        _non_completed_tasks.Remove(t);
                        StartFirstWaitingTask();
                        break;
                    default:
                        break;
                }
            }
        }

        public List<BackgroundTask> GetTasks()
        {
            return _tasks;
        }

        public event EventHandler<BackgroundTask>? TaskEnqueued;
    }
}
