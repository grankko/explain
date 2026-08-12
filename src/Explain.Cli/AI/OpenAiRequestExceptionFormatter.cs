namespace Explain.Cli.AI;

/// <summary>
/// Replaces common model-selection API failures with an actionable, safe message.
/// </summary>
public static class OpenAiRequestExceptionFormatter
{
    public static string? GetFriendlyMessage(int status, string serviceMessage, OpenAiRequestSettings request)
    {
        ArgumentNullException.ThrowIfNull(serviceMessage);
        ArgumentNullException.ThrowIfNull(request);

        var message = serviceMessage.ToLowerInvariant();
        if (status is 403 or 404 ||
            (message.Contains("model") &&
             (message.Contains("does not exist") || message.Contains("does not have access") ||
              message.Contains("not found") || message.Contains("not available"))))
        {
            return $"The current OpenAI API key cannot use model '{request.ModelName}'. " +
                   "Choose a model available to this key, or change OpenAi:DefaultModelName.";
        }

        if (message.Contains("reasoning_effort") ||
            (message.Contains("reasoning") && message.Contains("not supported")))
        {
            var effort = request.ReasoningEffort?.ToString().ToLowerInvariant() ?? "none";
            return $"Model '{request.ModelName}' cannot use reasoning effort '{effort}'. " +
                   "Choose a supported effort or pass --reasoning none.";
        }

        return null;
    }
}
