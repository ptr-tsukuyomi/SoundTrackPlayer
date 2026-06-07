using SoundTrackPlayer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoundTrackPlayer.ViewModel
{
    public class LoopModePickerItem
    {
        public string Name { get; set; } = string.Empty;
        public LoopMode Mode { get; set; } = LoopMode.Disabled;
    }

    internal class Common
    {
        public static IList<LoopModePickerItem> DefaultLoopModePickerItems { get; } = new List<LoopModePickerItem>()
        {
            new() { Name = "未設定", Mode = LoopMode.None },
            new() { Name = "有限ループ", Mode = LoopMode.Limited },
            new() { Name = "無限ループ", Mode = LoopMode.Unlimited },
            new() { Name = "ループ無効", Mode = LoopMode.Disabled }
        };

        public static IList<LoopModePickerItem> CurrentLoopModePickerItems { get; } = new List<LoopModePickerItem>()
        {
            new() { Name = "有限ループ", Mode = LoopMode.Limited },
            new() { Name = "無限ループ", Mode = LoopMode.Unlimited },
            new() { Name = "ループ無効", Mode = LoopMode.Disabled }
        };
    }
}
