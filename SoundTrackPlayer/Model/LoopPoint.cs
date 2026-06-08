using System.Collections.Concurrent;
using System.Numerics;

namespace SoundTrackPlayer.Model
{
    public class FindLoopBeginProgress
    {
        public int Current { get; set; } = 0;
        public int Total { get; set; } = 0;
    }

    public static class LoopPoint
    {
        public static BackgroundTask CreateFindLoopBeginTask(Track t)
        {
            var progress_reporter = new Progress<BackgroundTaskProgress>();
            var bg_task = new BackgroundTask()
            {
                Name = $"ループ開始点探索: {t.Info.Title}",
                Task = new Task(() =>
                {
                    if (t.Config.LoopEnd is null)
                    {
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Failed,
                            Result = "ループ終了点が設定されていません。",
                            Progress = 1.0
                        });
                        return;
                    }
                    if (t.Info.Length is null)
                    {
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Failed,
                            Result = "トラックの長さが不明です。",
                            Progress = 1.0
                        });
                        return;
                    }

                    var inner_progress_reporter = new Progress<FindLoopBeginProgress>();
                    inner_progress_reporter.ProgressChanged += (s, e) =>
                    {
                        double p = (double)e.Current / e.Total;

                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.InProgress,
                            Result = $"{e.Current} / {e.Total} サンプル",
                            Progress = p
                        });
                    };

                    //var result = LoopPoint.FindLoopBeginTimeSpan(t, t.Config.LoopEnd.Value, inner_progress_reporter, 44100 * 3);
                    var compare_duration = Config.FindLoopPointCompareDuration;
                    var target_begin = t.Config.LoopBegin == null ? TimeSpan.Zero : t.Config.LoopBegin.Value - Config.FindLoopPointTargetDuration_WithLoopBegin / 2;
                    var target_end = t.Config.LoopBegin == null ? Config.FindLoopPointTargetDuration_WithoutLoopBegin : t.Config.LoopBegin.Value + Config.FindLoopPointTargetDuration_WithLoopBegin * 1.5;
                    var result = LoopPoint.FindLoopBeginSample(t, target_begin, target_end, t.Config.LoopEnd.Value, compare_duration, inner_progress_reporter);

                    if (result.Any())
                    {
                        t.Config.LoopBegin = result.First();

                        var str = string.Join("\r\n", result.Select((e) => { return e.ToString(@"hh\:mm\:ss\.ffffff"); }));

                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Succeeded,
                            Result = "ループ開始点: " + t.Config.LoopBegin.Value.ToString(@"hh\:mm\:ss\.ffffff") + "\r\n<類似度上位10点>\r\n" + str,
                            Progress = 1.0
                        });
                    }
                    else
                    {
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Failed,
                            Result = "ループ開始点が見つかりませんでした。",
                            Progress = 1.0
                        });
                    }
                }),
                ProgressReporter = progress_reporter,
                State = BackgroundTaskState.Waiting
            };

            return bg_task;
        }

        public static IEnumerable<TimeSpan> FindLoopBeginTimeSpan(Track t, TimeSpan loop_end, IProgress<FindLoopBeginProgress> progress, int compare_samples = 44100, TimeSpan? loop_begin = null)
        {

            SoundFlow.Abstracts.AudioEngine engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            SoundFlow.Structs.AudioFormat audio_format = SoundFlow.Structs.AudioFormat.Cd;

            if (t.Source is null) return [];

            var stream = t.Source.Open();
            if (stream is null) throw new TrackDataLoadException();

            SoundFlow.Providers.StreamDataProvider src = new SoundFlow.Providers.StreamDataProvider(engine, audio_format, stream);
            if (src.FormatInfo is null) return [];

            var whole_data = ReadAllSamples(src);
            var mixed_data = MixChannel(whole_data, src.FormatInfo.ChannelCount);
            whole_data = null;

            TimeSpan target_begin = TimeSpan.Zero;
            TimeSpan target_end = src.FormatInfo.Duration / 4;
            TimeSpan compare_duration = TimeSpan.FromSeconds((double)compare_samples / src.FormatInfo.SampleRate);

            var result = FindLoopBeginSample(mixed_data, src.FormatInfo.SampleRate, target_begin, target_end, loop_end, compare_duration, progress).Select(
                (e) =>
                {
                    return TimeSpan.FromSeconds((double)e / src.FormatInfo.SampleRate);
                });

            src.Dispose();
            stream.Close();
            engine.Dispose();
            return result;
        }

        private static float[] ReadAllSamples(SoundFlow.Providers.StreamDataProvider src)
        {
            if (src.FormatInfo is null) throw new TrackDataLoadException();

            int whole_samples = src.Length;
            if (whole_samples == 0 || whole_samples == -1 || !src.CanSeek) throw new TrackDataLoadException();

            src.Seek(0);

            var whole_data = new float[whole_samples];
            Span<float> data_span = whole_data;
            int read = 0;
            while (read < whole_samples)
            {
                int ret = src.ReadBytes(data_span);
                if (ret < 0)
                {
                    return [];
                }

                read += ret;
                data_span = data_span[read..];
            }
            return whole_data;
        }

        private static float[] MixChannel(float[] whole_data, int channel)
        {
            return SoundFlow.Utils.ChannelMixer.Mix(whole_data, channel, 1);
        }

        public static IEnumerable<TimeSpan> FindLoopBeginSample(Track t, TimeSpan target_begin, TimeSpan target_end, TimeSpan loop_end, TimeSpan compare_duration, IProgress<FindLoopBeginProgress>? progress = null)
        {
            if (t.Source is null) throw new TrackDataLoadException();
            var stream = t.Source.Open();
            if (stream is null) throw new TrackDataLoadException();

            SoundFlow.Abstracts.AudioEngine? engine = null;
            SoundFlow.Structs.AudioFormat audio_format = SoundFlow.Structs.AudioFormat.Cd;
            SoundFlow.Providers.StreamDataProvider? src = null;

            float[]? whole_data;
            float[]? mixed_data;

            try
            {
                engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
                src = new SoundFlow.Providers.StreamDataProvider(engine, audio_format, stream);
                if (src.FormatInfo is null) return [];

                whole_data = ReadAllSamples(src);
                mixed_data = MixChannel(whole_data, src.FormatInfo.ChannelCount);
                whole_data = null;

                var result = FindLoopBeginSample(mixed_data, src.FormatInfo.SampleRate, target_begin, target_end, loop_end, compare_duration, progress).Select(
                    (e) =>
                    {
                        return TimeSpan.FromSeconds((double)e / src.FormatInfo.SampleRate);
                    });
                return result;
            }
            finally
            {
                whole_data = null;
                mixed_data = null;
                src?.Dispose();
                stream.Close();
                engine?.Dispose();
            }
        }

        public static IEnumerable<int> FindLoopBeginSample(float[] mixed_data, int sample_rate, TimeSpan target_begin, TimeSpan target_end, TimeSpan loop_end, TimeSpan compare_duration, IProgress<FindLoopBeginProgress>? progress = null)
        {
            if (sample_rate <= 0) throw new Exception();

            int target_begin_sample = (int)(target_begin.TotalSeconds * sample_rate);
            int target_end_sample = (int)(target_end.TotalSeconds * sample_rate);
            int loop_end_sample = (int)(loop_end.TotalSeconds * sample_rate);
            int compare_samples = (int)(compare_duration.TotalSeconds * sample_rate);

            return FindLoopBeginSample(mixed_data, target_begin_sample, target_end_sample, loop_end_sample, compare_samples, progress);
        }

        private static IEnumerable<int> FindLoopBeginSample(float[] mixed_data, int target_begin_sample, int target_end_sample, int loop_end_sample, int compare_samples = 44100, IProgress<FindLoopBeginProgress>? progress = null)
        {
            if (mixed_data.Length == 0) throw new Exception();

            if (target_begin_sample < 0) target_begin_sample = 0;
            if (target_begin_sample >= mixed_data.Length) throw new Exception();
            if (target_end_sample < 0) throw new Exception();
            if (target_end_sample > mixed_data.Length) target_end_sample = mixed_data.Length - 1;
            if (compare_samples <= 0) throw new Exception();
            if (compare_samples > mixed_data.Length) throw new Exception();
            if (loop_end_sample + compare_samples >= mixed_data.Length) loop_end_sample = mixed_data.Length - 1 - compare_samples;
            if (loop_end_sample < 0) throw new Exception();

            if (target_begin_sample >= target_end_sample) throw new Exception();

            int compares_total = target_end_sample - target_begin_sample - compare_samples;
            if (compares_total <= 0) throw new Exception();

            System.Diagnostics.Debug.WriteLine(String.Format("Total compares: {0}", compares_total));
            progress?.Report(new FindLoopBeginProgress()
            {
                Current = 0,
                Total = compares_total
            });

            var e_values = new ConcurrentBag<(int, float)>();
            var parallel_options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            };

            var r = Parallel.For(target_begin_sample, target_begin_sample + compares_total, parallel_options, (i, s) =>
            {
                var samples_to_compare = new ReadOnlySpan<float>(mixed_data, loop_end_sample + 1, compare_samples);

                if (i % 10000 == 0)
                {
                    System.Diagnostics.Debug.Write(String.Format("{0} ", i));
                    progress?.Report(new FindLoopBeginProgress()
                    {
                        Current = e_values.Count,
                        Total = compares_total
                    });
                }
                var target = new ReadOnlySpan<float>(mixed_data, i, compare_samples);
                var e = 0.0f;

                if (Vector.IsHardwareAccelerated)
                {
                    int remaining = compare_samples % Vector<float>.Count;

                    for (int k = 0; k < compare_samples - remaining; k += Vector<float>.Count)
                    {
                        var v1 = new Vector<float>(target.Slice(k));
                        var v2 = new Vector<float>(samples_to_compare.Slice(k));
                        e += Vector.Sum(Vector.Abs(v1 - v2));

                        if (e / compare_samples > 0.5)
                        {
                            return;
                        }
                    }

                    for (int k = compare_samples - remaining; k < compare_samples; ++k)
                    {
                        e += Math.Abs(target[k] - samples_to_compare[k]);
                    }
                    if (e / compare_samples > 0.5)
                    {
                        return;
                    }
                } else
                {
                    for (int k = 0; k < compare_samples; ++k)
                    {
                        e += Math.Abs(target[k] - samples_to_compare[k]);
                        if (e / compare_samples > 0.5)
                        {
                            return;
                        }
                    }
                }

                e_values.Add((i, e));
            });
            progress?.Report(new FindLoopBeginProgress()
            {
                Current = e_values.Count,
                Total = compares_total
            });

            var min_pair = e_values.MinBy(e => e.Item2);
            var sorted_pair = e_values.OrderBy(e => e.Item2).Take(10);

            foreach(var e in sorted_pair)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("e = {0} at sample {1}", e.Item2, e.Item1));
            }

            return sorted_pair.Select(e => e.Item1);
        }

    }
}
