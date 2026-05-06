using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    internal static class Config
    {
        public static Double Volume
        {
            get
            {
                return Preferences.Default.Get("Volume", 0.5);
            }
            set
            {
                Preferences.Default.Set("Volume", value);
            }
        }

        public static bool IsMuted
        {
            get
            {
                return Preferences.Default.Get("IsMuted", false);
            }
            set
            {
                Preferences.Default.Set("IsMuted", value);
            }
        }

        public static Model.ShuffleMode ShuffleMode
        {
            get
            {
                return (Model.ShuffleMode)Preferences.Default.Get("ShuffleMode", (int)Model.ShuffleMode.Off);
            }
            set
            {
                Preferences.Default.Set("ShuffleMode", ((int)value));
            }
        }

        public static Model.ContinuousPlayMode ContinuousPlayMode
        {
            get {
                return (Model.ContinuousPlayMode)Preferences.Default.Get("ContinuousPlayMode", (int)Model.ContinuousPlayMode.Off);
            }
            set
            {
                Preferences.Default.Set("ContinuousPlayMode", ((int)value));
            }
        }
                

        public static List<string> PlayListSources
        {
            get
            {
                var srcs = Preferences.Default.Get("PlayListSources", "");
                if (string.IsNullOrEmpty(srcs))
                {
                    return new List<string>();
                }
                else
                {
                    return new List<string>(srcs.Split(["\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries));
                }
            }
            set
            {
                var str = String.Join("\r\n", value);
                Preferences.Default.Set("PlayListSources", str);
            }
        }

        public static List<string> ContentDirectories
        {
            get
            {
                var srcs = Preferences.Default.Get("ContentDirectories", "");
                if (string.IsNullOrEmpty(srcs))
                {
                    return new List<string>();
                } else
                {
                    return new List<string>(srcs.Split(["\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries));
                }
            }
            set
            {
                var str = String.Join("\r\n", value);
                Preferences.Default.Set("ContentDirectories", str);
            }
        }

        public static TimeSpan FindLoopPointCompareDuration
        {
            get
            {
                if (TimeSpan.TryParse(Preferences.Default.Get("FindLoopPointCompareDuration", "00:00:03"), out TimeSpan result))
                {
                    return result;
                }
                return TimeSpan.FromSeconds(3);
            }
            set
            {
                Preferences.Default.Set("FindLoopPointCompareDuration", value.ToString());
            }
        }

        public static TimeSpan FindLoopPointTargetDuration_WithoutLoopBegin
        {
            get
            {
                if (TimeSpan.TryParse(Preferences.Default.Get("FindLoopPointTargetDuration_WithoutLoopBegin", "00:00:30"), out TimeSpan result))
                {
                    return result;
                }
                return TimeSpan.FromSeconds(30);
            }
            set
            {
                Preferences.Default.Set("FindLoopPointTargetDuration_WithoutLoopBegin", value.ToString());
            }
        }

        public static TimeSpan FindLoopPointTargetDuration_WithLoopBegin
        {
            get
            {
                if (TimeSpan.TryParse(Preferences.Default.Get("FindLoopPointTargetDuration_WithLoopBegin", "00:00:06"), out TimeSpan result))
                {
                    return result;
                }
                return TimeSpan.FromSeconds(6);
            }
            set
            {
                Preferences.Default.Set("FindLoopPointTargetDuration_WithLoopBegin", value.ToString());
            }
        }
    }
}
