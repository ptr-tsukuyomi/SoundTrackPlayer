using SoundFlow.Structs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
//using Windows.ApplicationModel.Background;

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

                    var result = LoopPoint.FindLoopBeginTimeSpan(t, t.Config.LoopEnd.Value, inner_progress_reporter, 44100 * 3);
                    if (result.Any())
                    {
                        t.Config.LoopBegin = result.First();
                        ((IProgress<BackgroundTaskProgress>)progress_reporter).Report(new BackgroundTaskProgress()
                        {
                            State = BackgroundTaskState.Succeeded,
                            Result = "ループ開始点: " + t.Config.LoopBegin.Value.ToString(@"hh\:mm\:ss\.ffffff"),
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

        public static IEnumerable<TimeSpan> FindLoopBeginTimeSpan(Track t, TimeSpan loop_end, IProgress<FindLoopBeginProgress> progress, int compare_samples = 44100, float threshold = 100)
        {
            SoundFlow.Abstracts.AudioEngine engine = new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            SoundFlow.Structs.AudioFormat audio_format = SoundFlow.Structs.AudioFormat.Cd;

            if (t.Source is null) return [];

            var stream = t.Source.Open();
            if (stream is null) throw new TrackDataLoadException();

            SoundFlow.Providers.StreamDataProvider src = new SoundFlow.Providers.StreamDataProvider(engine, audio_format, stream);
            if (src.FormatInfo is null) throw new TrackDataLoadException();

            int loop_end_sample = (int)(loop_end.TotalSeconds * src.FormatInfo.SampleRate);

            var result = FindLoopBeginSample(src, loop_end_sample, progress, compare_samples, threshold).Select(
                (e) => { 
                    return TimeSpan.FromSeconds((double)e / src.FormatInfo.SampleRate); 
                });

            src.Dispose();
            stream.Close();
            engine.Dispose();
            return result;
        }

        public static IEnumerable<int> FindLoopBeginSample(SoundFlow.Interfaces.ISoundDataProvider src, int loop_end_sample, IProgress<FindLoopBeginProgress> progress, int compare_samples = 44100, float threshold = 100)
        {
            if (src.FormatInfo is null) return [];

            int whole_samples = src.Length;
            if (whole_samples == 0 || whole_samples == -1 || !src.CanSeek) return [];

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

            var mixed_data = SoundFlow.Utils.ChannelMixer.Mix(whole_data, src.FormatInfo.ChannelCount, 1);
            if (loop_end_sample + compare_samples > mixed_data.Length) return [];

            var e_min = float.PositiveInfinity;
            int sample_no_e_min = -1;
            int compares_total = mixed_data.Length / 4 - compare_samples;
            System.Diagnostics.Debug.Write(String.Format("Total compares: {0}", compares_total));
            progress.Report(new FindLoopBeginProgress()
            {
                Current = 0,
                Total = compares_total
            });

            //for (int i = 0; i < compares_total; ++i)
            //{
            //    if (i % 1000 == 0)
            //    {
            //        System.Diagnostics.Debug.Write(String.Format("{0} ", i, mixed_data.Length / 4 - compare_samples));
            //    }
            //    var target = new Span<float>(mixed_data, i, compare_samples);
            //    var e = 0.0f;
            //    for (int k = 0; k < compare_samples; ++k)
            //    {
            //        e += Math.Abs(target[k] - samples_to_compare[k]);
            //        if (e / compare_samples > 0.5)
            //        {
            //            e = float.PositiveInfinity;
            //            break;
            //        }
            //    }
            //    //System.Diagnostics.Debug.WriteLine(String.Format("e = {0}", e));
            //    if (e < e_min)
            //    {
            //        e_min = e;
            //        sample_no_e_min = i;
            //        System.Diagnostics.Debug.WriteLine("");
            //        System.Diagnostics.Debug.WriteLine(String.Format("e_min changed to {0} at sample {1}", e_min, sample_no_e_min));
            //    }
            //}
            var e_values = new ConcurrentBag<(int, float)>();
            var parallel_options = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            };

            var r = Parallel.For(0, compares_total, parallel_options, (i, s) =>
            {
                var samples_to_compare = new ReadOnlySpan<float>(mixed_data, loop_end_sample + 1, compare_samples);

                if (i % 100000 == 0)
                {
                    System.Diagnostics.Debug.Write(String.Format("{0} ", i, mixed_data.Length / 4 - compare_samples));
                    progress.Report(new FindLoopBeginProgress()
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
                    float[] result = new float[compare_samples];

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
            progress.Report(new FindLoopBeginProgress()
            {
                Current = e_values.Count,
                Total = compares_total
            });

            //var e_max = 0.0f;
            //var max_pair = e_values.MaxBy(e => e.Item2);
            //e_max = max_pair.Item2;
            //var sample_no_e_max = max_pair.Item1;
            //System.Diagnostics.Debug.WriteLine("");
            //System.Diagnostics.Debug.WriteLine(String.Format("e_max = {0} at sample {1}", e_max, sample_no_e_max));

            //return [sample_no_e_max];

            var min_pair = e_values.MinBy(e => e.Item2);

            e_min = min_pair.Item2;
            sample_no_e_min = min_pair.Item1;
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine(String.Format("e_min = {0} at sample {1}", e_min, sample_no_e_min));

            return [sample_no_e_min];
        }

    }
}
