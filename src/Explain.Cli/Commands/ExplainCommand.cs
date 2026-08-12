using Explain.Cli.AI;
using Explain.Cli.AI.Context;
using Explain.Cli.AI.Tools;
using Explain.Cli.Commands.Explain;
using Explain.Cli.Configuration;
using Explain.Cli.Extensions;
using Explain.Cli.Memory;
using Explain.Cli.Storage;
using Explain.Cli;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Commands;

/// <summary>
/// Command implementation for explaining content using AI.
/// </summary>
public sealed class ExplainCommand : ICommand, IDisposable
{
    private readonly IOpenAIServiceAgent _openAiServiceAgent;
    private readonly IHistoryService _historyService;
    private readonly IMemoryService _memoryService;
    private readonly ISystemProfileService _systemProfileService;
    private readonly IAiContextBuilder _contextBuilder;
    private readonly ILocalToolSessionFactory _toolSessionFactory;
    private readonly IConfigurationDisplayService _configurationDisplayService;
    private readonly OpenAiOptions _openAiOptions;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _animationCts = new();
    private bool _disposed;

    public ExplainCommand(
        IOpenAIServiceAgent openAiServiceAgent,
        IHistoryService historyService,
        IMemoryService memoryService,
        ISystemProfileService systemProfileService,
        IAiContextBuilder contextBuilder,
        ILocalToolSessionFactory toolSessionFactory,
        IConfigurationDisplayService configurationDisplayService,
        IOptions<OpenAiOptions> openAiOptions,
        TimeProvider timeProvider)
    {
        _openAiServiceAgent = openAiServiceAgent;
        _historyService = historyService;
        _memoryService = memoryService;
        _systemProfileService = systemProfileService;
        _contextBuilder = contextBuilder;
        _toolSessionFactory = toolSessionFactory;
        _configurationDisplayService = configurationDisplayService;
        _openAiOptions = openAiOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var parsedArgs = ExplainArgumentParser.ParseArguments(args);
            if (parsedArgs.ShowHelp)
            {
                ExplainInputHandler.ShowUsage();
                return 0;
            }
            if (parsedArgs.ShowVersion)
            {
                Console.WriteLine($"explain {AppVersion.Current}");
                return 0;
            }

            var validationError = ExplainArgumentValidator.GetValidationError(parsedArgs);
            if (validationError is not null)
            {
                Console.Out.WriteError($"Error: {validationError}");
                return 1;
            }

            if (!parsedArgs.NoHistory)
                await InitializeOrRefreshSystemProfileAsync(parsedArgs.RefreshSystem);

            if (parsedArgs.ShowHistory)
                return await ShowHistoryAsync(parsedArgs.HistoryLimit);
            if (parsedArgs.ClearHistory)
                return await ClearHistoryAsync();
            if (parsedArgs.ShowMemory)
                return await ShowMemoryAsync();
            if (parsedArgs.ClearMemory)
                return await ClearMemoryAsync();

            var inputContent = await ExplainInputHandler.ProcessInputAsync(parsedArgs);
            if (inputContent.IsEmpty)
            {
                if (parsedArgs.RefreshSystem)
                    return 0;

                ExplainInputHandler.ShowUsage();
                return 1;
            }

            _openAiServiceAgent.IsVerbose = parsedArgs.IsVerbose;
            if (parsedArgs.IsVerbose)
                ShowVerboseInformation(inputContent, parsedArgs);

            var context = await _contextBuilder.BuildAsync(inputContent, parsedArgs);
            foreach (var warning in context.Warnings)
                Console.Error.WriteLine($"Warning: {warning}");

            ILocalToolSession? toolSession = parsedArgs.NoHistory || inputContent.HasPipedInput
                ? null
                : _toolSessionFactory.Create(context.ProjectScopeKey, inputContent.PipedContent);
            var aiResponse = await GenerateAiResponseAsync(context, parsedArgs, toolSession);

            IReadOnlyList<MemoryChange> memoryChanges = [];
            if (toolSession is not null)
            {
                try
                {
                    memoryChanges = await toolSession.CommitAsync();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Warning: memory changes were not saved: {exception.Message}");
                }
            }

            Console.Out.WriteAiResponse(aiResponse.Response);
            DisplayMemoryChanges(memoryChanges);

            if (!parsedArgs.NoHistory)
                await SaveHistoryBestEffortAsync(inputContent, aiResponse);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task InitializeOrRefreshSystemProfileAsync(bool forceRefresh)
    {
        try
        {
            if (forceRefresh)
            {
                await _systemProfileService.RefreshAsync();
                Console.Out.WriteInfo("System profile refreshed.");
            }
            else if (await _systemProfileService.EnsureInitializedAsync())
            {
                Console.Out.WriteInfo("System profile initialized.");
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Warning: system profile is unavailable: {exception.Message}");
        }
    }

    private async Task<int> ShowHistoryAsync(int limit)
    {
        var history = await _historyService.GetLatestAsync(limit);
        if (history.Count == 0)
            Console.WriteLine("No history found.");
        else
            history.DisplayColorized();
        return 0;
    }

    private async Task<int> ShowMemoryAsync()
    {
        var memories = await _memoryService.GetAllAsync();
        if (memories.Count == 0)
            Console.WriteLine("No memory found.");
        else
            memories.DisplayColorized();
        return 0;
    }

    private async Task<int> ClearHistoryAsync()
    {
        if (!Confirm("This will permanently delete all conversation history."))
            return 0;

        await _historyService.ClearAsync();
        Console.Out.WriteSuccess("History cleared successfully.");
        return 0;
    }

    private async Task<int> ClearMemoryAsync()
    {
        if (!Confirm("This will permanently delete AI-created global and project memories. Reserved system facts will remain."))
            return 0;

        await _memoryService.ClearAiMemoriesAsync();
        Console.Out.WriteSuccess("AI-created memory cleared successfully.");
        return 0;
    }

    private static bool Confirm(string message)
    {
        Console.WriteLine(message);
        Console.Write("Are you sure you want to continue? Type 'yes' to confirm: ");
        var confirmation = Console.ReadLine();
        if (string.Equals(confirmation?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
            return true;

        Console.WriteLine("Operation cancelled.");
        return false;
    }

    private async Task<AiResponse<string>> GenerateAiResponseAsync(
        AiContextResult context,
        ExplainArguments parsedArgs,
        ILocalToolSession? toolSession)
    {
        var request = OpenAiRequestSettingsFactory.Create(
            _openAiOptions,
            parsedArgs);
        var label = request.ReasoningEffort is null ? "thinking.." : "thinking with reasoning..";
        var animationTask = Console.Out.ShowThinkingAnimationAsync(_animationCts.Token, label);

        try
        {
            return await _openAiServiceAgent.GetResponseAsync(
                context.Items,
                request,
                toolSession);
        }
        finally
        {
            _animationCts.Cancel();
            try
            {
                await animationTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when animation is canceled.
            }
            Console.WriteLine();
        }
    }

    private async Task SaveHistoryBestEffortAsync(ExplainInputContent input, AiResponse<string> response)
    {
        try
        {
            await _historyService.AddAsync(new HistoryEntry
            {
                RecordedAtUtc = _timeProvider.GetUtcNow(),
                QuestionText = input.ArgumentContent,
                HadPipedInput = input.HasPipedInput,
                PipedCharacterCount = input.PipedContent.Length,
                OutputText = response.Response,
                ModelName = response.ModelName,
                PromptTokens = response.PromptTokens,
                CompletionTokens = response.CompletionTokens,
                TotalTokens = response.TotalTokens
            });
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Warning: this exchange was not saved to history: {exception.Message}");
        }
    }

    private static void DisplayMemoryChanges(IReadOnlyList<MemoryChange> changes)
    {
        foreach (var change in changes)
        {
            var scope = change.Scope.ToString().ToLowerInvariant();
            var verb = change.Action switch
            {
                "added" => "Memory added",
                "updated" => "Memory updated",
                "deleted" => "Memory deleted",
                "refreshed" => "Memory refreshed",
                _ => "Memory changed"
            };
            Console.Out.WriteInfo($"{verb} [{scope}]: {change.Key}");
        }
    }

    private void ShowVerboseInformation(ExplainInputContent inputContent, ExplainArguments parsedArgs)
    {
        Console.WriteLine("Verbose mode enabled.");
        if (!string.IsNullOrWhiteSpace(parsedArgs.ModelName))
            Console.WriteLine($"Model override: {parsedArgs.ModelName}");
        if (!string.IsNullOrWhiteSpace(parsedArgs.ReasoningEffort))
            Console.WriteLine($"Reasoning override: {parsedArgs.ReasoningEffort}");
        if (parsedArgs.NoHistory)
            Console.WriteLine("Ephemeral mode enabled: stored history and memory are disabled.");

        _configurationDisplayService.DisplayConfiguration();
        if (inputContent.HasPipedInput)
            Console.WriteLine($"Piped input received: {inputContent.PipedContent.Length:N0} characters.");
        if (inputContent.HasArgumentInput)
            Console.WriteLine($"Argument content: {inputContent.ArgumentContent}");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _animationCts.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
