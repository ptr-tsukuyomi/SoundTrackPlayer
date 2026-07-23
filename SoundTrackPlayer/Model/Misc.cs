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
            }
            else
            {
                return new List<string>();
            }
        }

        public class TrackConfigRecord
        {
            public static TrackConfigRecord Create(TrackConfig c, string? track_title)
            {
                return new TrackConfigRecord
                {
                    TrackTitle = track_title,
                    LoopBegin = c.LoopBegin,
                    LoopEnd = c.LoopEnd,
                    DefaultLoopMode = c.DefaultLoopMode,
                    LoopCount = c.LoopCount
                };
            }
            public string? TrackTitle { get; set; }
            public TimeSpan? LoopBegin { get; set; }
            public TimeSpan? LoopEnd { get; set; }
            public LoopMode DefaultLoopMode { get; set; }
            public uint? LoopCount { get; set; }
        }

        public static string GenerateTrackConfigCsv(IEnumerable<Track> tracks)
        {
            var records = tracks.Select((e) => TrackConfigRecord.Create(e.Config, e.Info.Title)).ToList();

            using (var writer = new StringWriter())
            using (var cw = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                cw.WriteRecords(records);
                return writer.ToString();
            }
        }

        public static IEnumerable<TrackConfig> CreateTrackConfigFromCsv(string csv)
        {
            var cfg = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using (var reader = new StringReader(csv))
            using (var cr = new CsvHelper.CsvReader(reader, cfg))
            {
                var records = cr.GetRecords<TrackConfigRecord>();

                return records.Select((e) => new TrackConfig()
                {
                    LoopBegin = e.LoopBegin,
                    LoopEnd = e.LoopEnd,
                    DefaultLoopMode = e.DefaultLoopMode,
                    LoopCount = e.LoopCount
                }).ToList();
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
