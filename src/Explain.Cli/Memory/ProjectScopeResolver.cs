namespace Explain.Cli.Memory;

public sealed class ProjectScopeResolver : IProjectScopeResolver
{
    public string Resolve(string workingDirectory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(workingDirectory));
        for (var directory = current; directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        return current.FullName;
    }

    public string RedactForPrompt(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd(Path.DirectorySeparatorChar);
        if (!string.IsNullOrWhiteSpace(home) &&
            (fullPath.Equals(home, StringComparison.Ordinal) ||
             fullPath.StartsWith(home + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
        {
            return "~" + fullPath[home.Length..];
        }

        return fullPath;
    }
}
