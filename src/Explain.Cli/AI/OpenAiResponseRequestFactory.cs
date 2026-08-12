using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
public static class OpenAiResponseRequestFactory
{
    public static CreateResponseOptions Create(
        IEnumerable<ResponseItem> inputItems,
        OpenAiRequestSettings request,
        string? schemaName = null,
        BinaryData? jsonSchema = null,
        IEnumerable<ResponseTool>? localTools = null)
    {
        ArgumentNullException.ThrowIfNull(inputItems);
        ArgumentNullException.ThrowIfNull(request);

        var options = new CreateResponseOptions
        {
            Model = request.ModelName,
            StoredOutputEnabled = false
        };

        if (request.ReasoningEffort is not null)
        {
            options.ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = request.ReasoningEffort.Value
            };
        }

        options.Tools.Add(ResponseTool.CreateWebSearchTool());
        if (localTools is not null)
        {
            options.ParallelToolCallsEnabled = false;
            foreach (var tool in localTools)
                options.Tools.Add(tool);
        }

        foreach (var inputItem in inputItems)
            options.InputItems.Add(inputItem);

        if (!string.IsNullOrWhiteSpace(schemaName) && jsonSchema is not null)
        {
            options.TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: schemaName,
                    jsonSchema: jsonSchema,
                    jsonSchemaFormatDescription: $"Structured response according to {schemaName} schema",
                    jsonSchemaIsStrict: true)
            };
        }

        return options;
    }
}
#pragma warning restore OPENAI001
