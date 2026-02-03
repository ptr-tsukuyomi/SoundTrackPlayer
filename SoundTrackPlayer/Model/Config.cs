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
    }
}
