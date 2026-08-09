using System.Runtime.InteropServices;

namespace Explain.Cli.Memory;

public sealed class SystemProfileCollector : ISystemProfileCollector
{
    public IReadOnlyList<SystemFact> Collect()
    {
        var facts = new List<SystemFact>();
        Add(facts, "system.os", GetOperatingSystemDescription());
        Add(facts, "system.kernel", GetKernelVersion());
        Add(facts, "system.architecture", RuntimeInformation.OSArchitecture.ToString());
        Add(facts, "system.default_shell", GetShellName());
        Add(facts, "system.timezone", TimeZoneInfo.Local.Id);
        return facts;
    }

    private static string GetOperatingSystemDescription()
    {
        const string osReleasePath = "/etc/os-release";
        if (OperatingSystem.IsLinux() && File.Exists(osReleasePath))
        {
            var prettyName = File.ReadLines(osReleasePath)
                .FirstOrDefault(line => line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal));
            if (prettyName is not null)
                return prettyName["PRETTY_NAME=".Length..].Trim().Trim('"');
        }

        return RuntimeInformation.OSDescription.Trim();
    }

    private static string GetShellName()
    {
        var shellPath = Environment.GetEnvironmentVariable("SHELL");
        return string.IsNullOrWhiteSpace(shellPath) ? string.Empty : Path.GetFileName(shellPath);
    }

    private static string GetKernelVersion()
    {
        const string kernelReleasePath = "/proc/sys/kernel/osrelease";
        if (OperatingSystem.IsLinux() && File.Exists(kernelReleasePath))
            return File.ReadAllText(kernelReleasePath).Trim();

        return Environment.OSVersion.VersionString;
    }

    private static void Add(List<SystemFact> facts, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            facts.Add(new SystemFact(key, value.Trim()));
    }
}
