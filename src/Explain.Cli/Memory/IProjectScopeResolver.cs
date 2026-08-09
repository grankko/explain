namespace Explain.Cli.Memory;

public interface IProjectScopeResolver
{
    string Resolve(string workingDirectory);
    string RedactForPrompt(string path);
}
