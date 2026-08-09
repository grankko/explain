namespace Explain.Cli.AI.Tools;

public interface ILocalToolSessionFactory
{
    ILocalToolSession Create(string projectScopeKey, string pipedContent);
}
