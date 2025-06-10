namespace Explain.Cli.Configuration
{

#nullable enable

    public class StorageOptions
    {
        public const string SectionName = "Storage";

        public string ConnectionString { get; set; }= string.Empty;
    }
}