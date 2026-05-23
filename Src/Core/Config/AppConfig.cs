using System;
using System.IO;
using System.Text.Json.Serialization;

namespace ProjektSlowkaRemasterd.Src.Core.Config;

public class AppConfig
{
    public string BackupDirectoryPath { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public string MediaDirectoryPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string ResolvedDatabasePath => ResolvePath(DatabasePath, "slowka.db");

    [JsonIgnore]
    public string ResolvedMediaDirectoryPath => ResolvePath(MediaDirectoryPath, "media");

    private static string ResolvePath(string path, string defaultRelativePath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultRelativePath);
        }

        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var relative = path.Substring(1).TrimStart('/', '\\');
            return Path.Combine(home, relative);
        }

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }
}
