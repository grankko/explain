using System;
using System.IO;

namespace Explain.Cli.Configuration
{
    public static class ConfigurationPathProvider
    {
        private const string AppName = "explain";
        
        public static string GetConfigDirectory()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDirectory, ".config", AppName);
        }
        
        public static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), "appsettings.json");
        }
        
        public static string GetDefaultDatabasePath()
        {
            return Path.Combine(GetConfigDirectory(), "explain_history.sqlite");
        }
        
        public static void EnsureConfigDirectoryExists()
        {
            var configDirectory = GetConfigDirectory();
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
        }
    }
}
