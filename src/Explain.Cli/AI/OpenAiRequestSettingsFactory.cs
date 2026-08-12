using Explain.Cli.Commands.Explain;
using Explain.Cli.Configuration;
using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
/// <summary>
/// Resolves command-line model overrides against the configured defaults.
/// </summary>
public static class OpenAiRequestSettingsFactory
{
    public static OpenAiRequestSettings Create(OpenAiOptions options, ExplainArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(arguments);

        var modelName = string.IsNullOrWhiteSpace(arguments.ModelName)
            ? options.DefaultModelName
            : arguments.ModelName;
        if (string.IsNullOrWhiteSpace(modelName))
            throw new InvalidOperationException("No OpenAI model is configured. Set OpenAi:DefaultModelName or pass --model.");

        var effortName = string.IsNullOrWhiteSpace(arguments.ReasoningEffort)
            ? options.DefaultReasoningEffort
            : arguments.ReasoningEffort;

        return new OpenAiRequestSettings(modelName, ParseReasoningEffort(effortName));
    }

    private static ResponseReasoningEffortLevel? ParseReasoningEffort(string effortName)
    {
        return effortName.Trim().ToLowerInvariant() switch
        {
            "none" => (ResponseReasoningEffortLevel?)null,
            "minimal" => "minimal",
            "low" => ResponseReasoningEffortLevel.Low,
            "medium" => ResponseReasoningEffortLevel.Medium,
            "high" => ResponseReasoningEffortLevel.High,
            "xhigh" => "xhigh",
            _ => throw new InvalidOperationException(
                $"Unsupported reasoning effort '{effortName}'. Use none, minimal, low, medium, high, or xhigh.")
        };
    }
}
#pragma warning restore OPENAI001
