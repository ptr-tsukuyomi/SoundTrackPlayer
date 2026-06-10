using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoundTrackPlayer.Model
{
    public interface ITrackSource
    {
        public abstract void SaveTrackConfig(TrackConfig config);
        public abstract TrackConfig? LoadTrackConfig();
        //public abstract Stream? GetStream();
        public abstract Stream Open();
        //public abstract void Close();
        public abstract string Name { get; }
    }

    public class FileOriginTrackSource : ITrackSource
    {
        public string FilePath { get; set; } = "";
        public string Name { get => Path.GetFileName(FilePath); }

        //public Stream? Stream { get; set; } = null;

        static private TrackConfig? LoadTrackConfigFromFile(string info_file_path)
        {
            var toml_mode_options = new Tomlyn.TomlModelOptions
            {
                ConvertToModel = (obj, t) =>
                {
                    if (t == typeof(TimeSpan))
                    {
                        return obj switch
                        {
                            long l => TimeSpan.FromSeconds(l),
                            double d => TimeSpan.FromSeconds(d),
                            _ => null,
                        };
                    }
                    return null;
                }
            };

            try
            {
                var toml_text = File.ReadAllText(info_file_path);
                return Tomlyn.Toml.ToModel<TrackConfig>(toml_text, info_file_path, toml_mode_options);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        static private void SaveTrackConfigToFile(string info_file_path, TrackConfig config)
        {
            var toml_mode_options = new Tomlyn.TomlModelOptions();
            toml_mode_options.ConvertToToml = (obj) =>
            {
                if (obj is TimeSpan ts)
                {
                    return ts.TotalSeconds;
                }
                return null;
            };

            var toml_text = Tomlyn.Toml.FromModel(config, toml_mode_options);
            File.WriteAllText(info_file_path, toml_text);
        }

        //public void Close()
        //{
        //    if (Stream != null)
        //    {
        //        Stream.Close();
        //        Stream = null;
        //    }
        //}

        //public Stream? GetStream()
        //{
        //    return Stream;
        //}

        public TrackConfig? LoadTrackConfig()
        {

            var read_config = LoadTrackConfigFromFile(FilePath + ".toml");
            return read_config;
        }

        public Stream Open()
        {
            //if (Stream != null)
            //{
            //    Stream.Close();
            //    Stream = null;
            //}
            return File.OpenRead(FilePath);
        }

        public void SaveTrackConfig(TrackConfig config)
        {
            SaveTrackConfigToFile(FilePath + ".toml", config);
        }
    }
}
