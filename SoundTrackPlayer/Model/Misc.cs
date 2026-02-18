using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;

namespace SoundTrackPlayer.Model
{
    internal class Misc
    {
        public static List<string> ParseArguments(string arguments)
        {
            var reader = new StringReader(arguments);
            var parser = new TextFieldParser(reader);

            parser.SetDelimiters(" ");
            parser.HasFieldsEnclosedInQuotes = true;

            var result = parser.ReadFields();
            if (result is not null)
            {
                return new List<string>(result).FindAll((x) => !string.IsNullOrEmpty(x));
            } else
            {
                return new List<string>();
            }
        }
    }

    public class LoopModeToStringConverter : IValueConverter
    {
        readonly Dictionary<LoopMode, string> ModeStringMap = new()
        {
            { LoopMode.None, "未設定" },
            { LoopMode.Limited, "有限ループ" },
            { LoopMode.Unlimited, "無限ループ" },
            { LoopMode.Disabled, "ループ無効" }
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                LoopMode x => ModeStringMap.TryGetValue(x, out string? value1) ? value1 : "未定義",
                null => null,
                _ => "不明な値"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string x && ModeStringMap.ContainsValue(x))
            {
                return ModeStringMap.First((y) => y.Value == x);
            } else
            {
                return null;
            }
        }
    }
}
