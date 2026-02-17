using CommunityToolkit.Maui.Storage;
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
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsViewModel()
        {
            ContentDirectories.CollectionChanged += _content_directories_CollectionChanged;

            ContentDirectoryAddButtonClickCommand = new Command(() =>
            {
                ContentDirectories.Add(ContentDirectoryTextBoxString);
                ContentDirectoryTextBoxString = string.Empty;
            });

            ContentDirectoryDeleteButtonClickCommand = new Command(() =>
            {
                if (ContentDirectorySelectedStrings is null) return;

                var targets = ContentDirectorySelectedStrings.ToList();

                foreach (string e in targets)
                {
                    ContentDirectories.Remove(e);
                }
            }, () => ContentDirectorySelectedStrings is not null && ContentDirectorySelectedStrings.Count > 0);

            ContentDirectoryOpenButtonClickCommand = new Command(async () =>
            {
                var result = await FolderPicker.Default.PickAsync();

                if (result.IsSuccessful)
                {
                    ContentDirectoryTextBoxString = result.Folder.Path;
                }
            });

            ContentDirectorySelectedStringsChangedCommand = new Command(() =>
            {
                ContentDirectoryDeleteButtonClickCommand.ChangeCanExecute();
            });

            FindLoopPointCompareDurationEntryUnfocusedCommand = new Command(async () =>
            {
                if (double.TryParse(_find_loop_point_compare_duration_string, out double result))
                {
                    Config.FindLoopPointCompareDuration = TimeSpan.FromSeconds(result);
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FindLoopPointCompareDurationString)));
            });

            FindLoopPointTargetDuration_WithoutLoopBeginEntryUnfocusedCommand = new Command(async () =>
            {
                if (double.TryParse(_find_loop_point_target_duration_without_loop_begin_string, out double result))
                {
                    Config.FindLoopPointTargetDuration_WithoutLoopBegin = TimeSpan.FromSeconds(result);
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FindLoopPointTargetDuration_WithoutLoopBeginString)));
            });

            FindLoopPointTargetDuration_WithLoopBeginEntryUnfocusedCommand = new Command(async () =>
            {
                if (double.TryParse(_find_loop_point_target_duration_with_loop_begin_string, out double result))
                {
                    Config.FindLoopPointTargetDuration_WithLoopBegin = TimeSpan.FromSeconds(result);
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FindLoopPointTargetDuration_WithLoopBeginString)));
            });
        }

        private void _content_directories_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Config.ContentDirectories = new List<string>(ContentDirectories);
        }

        public ObservableCollection<string> ContentDirectories { get; set; } = new ObservableCollection<string>(Config.ContentDirectories);


        public string ContentDirectoryTextBoxString
        {
            get
            {
                return _content_directory_textbox_string;
            }
            set
            {
                _content_directory_textbox_string = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContentDirectoryTextBoxString)));
            }
        }
        private string _content_directory_textbox_string = string.Empty;

        public System.Collections.Generic.IList<object>? ContentDirectorySelectedStrings
        {
            get
            {
                return _content_directory_selected_strings;
            }
            set
            {
                _content_directory_selected_strings = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContentDirectorySelectedStrings)));
            }
        }
        private System.Collections.Generic.IList<object>? _content_directory_selected_strings = null;

        public string FindLoopPointCompareDurationString
        {
            get
            {
                return Config.FindLoopPointCompareDuration.TotalSeconds.ToString();
            }
            set
            {
                _find_loop_point_compare_duration_string = value;
            }
        }
        private string _find_loop_point_compare_duration_string = Config.FindLoopPointCompareDuration.TotalSeconds.ToString();


        public string FindLoopPointTargetDuration_WithoutLoopBeginString
        {
            get
            {
                return Config.FindLoopPointTargetDuration_WithoutLoopBegin.TotalSeconds.ToString();
            }
            set
            {
                _find_loop_point_target_duration_without_loop_begin_string = value;
            }
        }
        private string _find_loop_point_target_duration_without_loop_begin_string = Config.FindLoopPointTargetDuration_WithoutLoopBegin.TotalSeconds.ToString();

        public string FindLoopPointTargetDuration_WithLoopBeginString
        {
            get
            {
                return Config.FindLoopPointTargetDuration_WithLoopBegin.TotalSeconds.ToString();
            }
            set
            {
                _find_loop_point_target_duration_with_loop_begin_string = value;
            }
        }
        private string _find_loop_point_target_duration_with_loop_begin_string = Config.FindLoopPointTargetDuration_WithLoopBegin.TotalSeconds.ToString();

        public Command ContentDirectoryOpenButtonClickCommand { get; set; }
        public Command ContentDirectoryAddButtonClickCommand { get; set; }
        public Command ContentDirectoryDeleteButtonClickCommand { get; set; }
        
        public Command ContentDirectorySelectedStringsChangedCommand { get; set; }

        public Command FindLoopPointCompareDurationEntryUnfocusedCommand { get; set; }
        public Command FindLoopPointTargetDuration_WithoutLoopBeginEntryUnfocusedCommand { get; set; }
        public Command FindLoopPointTargetDuration_WithLoopBeginEntryUnfocusedCommand { get; set; }
    }
}
