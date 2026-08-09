using System.Text.Json;
using Explain.Cli.Memory;
using OpenAI.Responses;

namespace Explain.Cli.AI.Tools;

#pragma warning disable OPENAI001
public sealed class MemoryToolSession : ILocalToolSession
{
    public const string UpsertToolName = "upsert_memory";
    public const string DeleteToolName = "delete_memory";
    public const string RefreshSystemToolName = "refresh_system_profile";

    private static readonly IReadOnlyList<ResponseTool> ToolDefinitions =
    [
        ResponseTool.CreateFunctionTool(
            functionName: UpsertToolName,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "scope": { "type": "string", "enum": ["global", "project"] },
                    "key": { "type": "string", "description": "Stable 1-64 character key using letters, digits, dots, underscores, or hyphens." },
                    "value": { "type": "string", "description": "One atomic durable fact, at most 300 characters." },
                    "category": { "type": "string", "enum": ["preference", "user_fact", "project_fact", "other"] }
                  },
                  "required": ["scope", "key", "value", "category"],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true,
            functionDescription: "Create or update one durable, non-sensitive fact or preference. Use project scope for facts specific to the current project."),
        ResponseTool.CreateFunctionTool(
            functionName: DeleteToolName,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "scope": { "type": "string", "enum": ["global", "project"] },
                    "key": { "type": "string" }
                  },
                  "required": ["scope", "key"],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true,
            functionDescription: "Forget one AI-managed memory. Use this when the user explicitly asks to forget it or clearly corrects it."),
        ResponseTool.CreateFunctionTool(
            functionName: RefreshSystemToolName,
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {},
                  "required": [],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true,
            functionDescription: "Refresh Explain's reserved, privacy-balanced system profile using local deterministic inspection.")
    ];

    private readonly IMemoryService _memoryService;
    private readonly ISystemProfileService _systemProfileService;
    private readonly string _projectScopeKey;
    private readonly string _pipedContent;
    private readonly List<MemoryMutation> _pendingMutations = [];
    private bool _committed;

    public MemoryToolSession(
        IMemoryService memoryService,
        ISystemProfileService systemProfileService,
        string projectScopeKey,
        string pipedContent = "")
    {
        _memoryService = memoryService;
        _systemProfileService = systemProfileService;
        _projectScopeKey = projectScopeKey;
        _pipedContent = pipedContent;
    }

    public IReadOnlyList<ResponseTool> Tools => ToolDefinitions;

    public Task<string> ExecuteAsync(
        FunctionCallResponseItem functionCall,
        CancellationToken cancellationToken = default)
    {
        if (_committed)
            throw new InvalidOperationException("This memory tool session has already been committed.");

        try
        {
            using var arguments = JsonDocument.Parse(functionCall.FunctionArguments);
            var root = arguments.RootElement;

            return Task.FromResult(functionCall.FunctionName switch
            {
                UpsertToolName => StageUpsert(root),
                DeleteToolName => StageDelete(root),
                RefreshSystemToolName => StageSystemRefresh(),
                _ => SerializeResult("error", $"Unknown local tool '{functionCall.FunctionName}'.")
            });
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return Task.FromResult(SerializeResult("error", exception.Message));
        }
    }

    public async Task<IReadOnlyList<MemoryChange>> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_committed)
            return [];

        _committed = true;
        return await _memoryService.ApplyAsync(_pendingMutations, cancellationToken);
    }

    private string StageUpsert(JsonElement root)
    {
        var scope = ParseScope(GetRequiredString(root, "scope"));
        var key = GetRequiredString(root, "key");
        var value = GetRequiredString(root, "value").ReplaceLineEndings(" ").Trim();
        var category = GetRequiredString(root, "category");
        ValidateMemoryValue(key, value);
        if (category is not ("preference" or "user_fact" or "project_fact" or "other"))
            throw new InvalidOperationException("Memory category is invalid.");
        _pendingMutations.Add(new MemoryMutation(
            MemoryMutationKind.Upsert,
            scope,
            ScopeKey(scope),
            key,
            value,
            category));
        return SerializeResult("queued", $"Memory '{key}' will be saved after the final answer.");
    }

    private string StageDelete(JsonElement root)
    {
        var scope = ParseScope(GetRequiredString(root, "scope"));
        var key = GetRequiredString(root, "key");
        _pendingMutations.Add(new MemoryMutation(
            MemoryMutationKind.Delete,
            scope,
            ScopeKey(scope),
            key,
            string.Empty,
            string.Empty));
        return SerializeResult("queued", $"Memory '{key}' will be forgotten after the final answer.");
    }

    private string StageSystemRefresh()
    {
        var facts = _systemProfileService.Collect();
        _pendingMutations.Add(new MemoryMutation(
            MemoryMutationKind.RefreshSystem,
            MemoryScope.System,
            string.Empty,
            "system.profile",
            string.Empty,
            "system",
            facts));
        return SerializeResult("queued", $"System profile refresh collected {facts.Count} facts.");
    }

    private string ScopeKey(MemoryScope scope) => scope == MemoryScope.Project ? _projectScopeKey : string.Empty;

    private static MemoryScope ParseScope(string value) => value switch
    {
        "global" => MemoryScope.Global,
        "project" => MemoryScope.Project,
        _ => throw new InvalidOperationException("Memory scope must be global or project.")
    };

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"The '{propertyName}' argument is required.");

        return property.GetString() ?? string.Empty;
    }

    private void ValidateMemoryValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 300)
            throw new InvalidOperationException("Memory values must use 1-300 characters.");
        if (key.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The system.* namespace is reserved.");
        if (_pipedContent.Length > 0 && value.Length >= 8 && _pipedContent.Contains(value, StringComparison.Ordinal))
            throw new InvalidOperationException("Raw piped content cannot be saved as durable memory.");

        var suspicious = new[] { "sk-", "ghp_", "-----BEGIN ", "password=", "api_key=", "apikey=" };
        if (suspicious.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Potential credentials or secrets cannot be saved as durable memory.");
    }

    private static string SerializeResult(string status, string message)
    {
        return JsonSerializer.Serialize(new { status, message });
    }
}
#pragma warning restore OPENAI001
