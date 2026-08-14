using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParticleConverter
{
    /// <summary>
    /// User settings, persisted as JSON under %APPDATA%\ParticleConverter\settings.json.
    /// Replaces the old ApplicationSettingsBase designer file, which needed the
    /// System.Configuration.ConfigurationManager package to work outside .NET Framework.
    /// </summary>
    public sealed class Settings
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ParticleConverter");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        private static Settings _default;

        public static Settings Default => _default ??= Load();

        /// <summary>Where exported files are written.</summary>
        public string FolderPath { get; set; } = "./functions";

        /// <summary>Id of the selected <see cref="Minecraft.McVersionProfile"/>, e.g. "26.2".</summary>
        public string McVersion { get; set; } = Minecraft.McVersionProfile.Latest.Id;

        /// <summary>Datapack namespace, i.e. the "foo" in /function foo:bar.</summary>
        public string Namespace { get; set; } = "particles";

        /// <summary>Write a whole datapack rather than a bare .mcfunction.</summary>
        public bool ExportAsDatapack { get; set; } = true;

        /// <summary>Language file selected in the dropdown, e.g. "en-US". Empty means follow the OS.</summary>
        public string Language { get; set; } = "";

        private static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    Settings loaded = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                // A corrupt or unreadable settings file must not stop the app from starting.
                util.Logger.WriteExceptionLog(e);
            }

            return new Settings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
            }
            catch (Exception e)
            {
                util.Logger.WriteExceptionLog(e);
            }
        }
    }
}
