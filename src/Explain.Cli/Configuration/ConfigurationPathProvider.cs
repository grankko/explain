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

        public static string GetExplainLocalAppDataDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AppName);
        }
        
        public static string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), "appsettings.json");
        }
        
        public static string GetDefaultDatabasePath()
        {
            return Path.Combine(GetExplainLocalAppDataDirectory(), "explain_history.sqlite");
        }

        public static void EnsureApplicationDirectoriesExists()
        {
            var configDirectory = GetConfigDirectory();
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);
            
            var localAppDataDirectory = GetExplainLocalAppDataDirectory();
            if (!Directory.Exists(localAppDataDirectory))
                Directory.CreateDirectory(localAppDataDirectory);
        }
    }
}
