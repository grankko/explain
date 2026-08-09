using Explain.Cli.Memory;

namespace Explain.Cli.Tests.UnitTests.Memory;

[TestClass]
public class ProjectScopeResolverTests
{
    [TestMethod]
    public void Resolve_UsesNearestGitRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"scope_{Guid.NewGuid()}");
        var nested = Path.Combine(root, "src", "feature");
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(nested);

        try
        {
            var result = new ProjectScopeResolver().Resolve(nested);
            Assert.AreEqual(root, result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void RedactForPrompt_ReplacesHomePrefix()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = new ProjectScopeResolver().RedactForPrompt(Path.Combine(home, "projects", "explain"));

        Assert.AreEqual($"~{Path.DirectorySeparatorChar}projects{Path.DirectorySeparatorChar}explain", result);
    }
}
