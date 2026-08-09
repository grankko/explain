using Explain.Cli.Memory;

namespace Explain.Cli.AI.Tools;

public sealed class MemoryToolSessionFactory : ILocalToolSessionFactory
{
    private readonly IMemoryService _memoryService;
    private readonly ISystemProfileService _systemProfileService;

    public MemoryToolSessionFactory(
        IMemoryService memoryService,
        ISystemProfileService systemProfileService)
    {
        _memoryService = memoryService;
        _systemProfileService = systemProfileService;
    }

    public ILocalToolSession Create(string projectScopeKey, string pipedContent)
    {
        return new MemoryToolSession(_memoryService, _systemProfileService, projectScopeKey, pipedContent);
    }
}
