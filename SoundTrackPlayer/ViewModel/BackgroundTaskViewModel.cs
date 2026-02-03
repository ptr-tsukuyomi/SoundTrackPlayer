using SoundTrackPlayer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.ViewModel
{
    public class BackgroundTaskViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<BackgroundTaskItemViewModel> BackgroundTaskItemViewModels { get; set; }

        public BackgroundTaskViewModel()
        {
            StaticResource.BackgroundTaskRunner.TaskEnqueued += BackgroundTaskRunner_TaskEnqueued;

            BackgroundTaskItemViewModels = new ObservableCollection<BackgroundTaskItemViewModel>(StaticResource.BackgroundTaskRunner.GetTasks().Select((e) => new BackgroundTaskItemViewModel(e)));
        }

        private void BackgroundTaskRunner_TaskEnqueued(object? sender, BackgroundTask e)
        {
            BackgroundTaskItemViewModels.Add(new BackgroundTaskItemViewModel(e));
        }
    }

    public class BackgroundTaskItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private BackgroundTask _task;
        private BackgroundTaskState _old_state;

        public BackgroundTaskItemViewModel(BackgroundTask t)
        {
            if (Application.Current is null) throw new Exception();

            Application.Current.RequestedThemeChanged += (s, e) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
            };

            _task = t;
            _old_state = _task.State;
            _task.TaskStateChanged += _task_TaskStateChanged;
        }

        private void _task_TaskStateChanged(object? sender, EventArgs e)
        {
            if (sender is null) throw new Exception("Sender is null in TaskCompleted event.");

            if (sender is BackgroundTask t)
            {
                if (t.State != _old_state)
                {
                    _old_state = t.State;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                } else
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
                }
            }
        }

        public ImageSource Image
        {
            get
            {
                if (Application.Current is null) throw new Exception();
                string theme = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";

                if (_task is null) return ImageSource.FromFile($"question_mark_circled_{theme}.png");

                return _task.State switch
                {
                    BackgroundTaskState.Waiting => ImageSource.FromFile($"clock_{theme}.png"),
                    BackgroundTaskState.InProgress => ImageSource.FromFile($"play_{theme}.png"),
                    BackgroundTaskState.Succeeded => _task.IsFailureFound switch {
                        true => ImageSource.FromFile($"exclamation_triangle_{theme}.png"),
                        false => ImageSource.FromFile($"check_circled_{theme}.png")
                    },
                    BackgroundTaskState.Failed => ImageSource.FromFile($"cross_circled_{theme}.png"),
                    _ => throw new Exception("Unknown BackgroundTaskState"),
                };
            }
        }

        public Double? Progress
        {
            get
            {
                return _task?.Progress;
            }
        }

        public String? Name
        {
            get
            {
                return _task?.Name;
            }
        }

        public DateTime? TaskStartedDateTime
        {
            get { return _task?.TaskStartedDateTime; }
        }

        public DateTime? TaskCompletedDateTime
        {
            get { return _task?.TaskCompletedDateTime; }
        }

        public String? Result
        {
            get { return _task?.Result; }
        }
    }
}
