using System.Reflection;

namespace Explain.Cli;

public static class AppVersion
{
    public static string Current
    {
        get
        {
            var informationalVersion = typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informationalVersion))
                return informationalVersion.Split('+', 2)[0];

            var assemblyVersion = typeof(AppVersion).Assembly.GetName().Version;
            return assemblyVersion is null
                ? "unknown"
                : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
        }
    }
}
