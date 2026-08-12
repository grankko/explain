using Explain.Cli.AI;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class OpenAiRequestExceptionFormatterTests
{
    private static readonly OpenAiRequestSettings Request = new(
        "unavailable-model",
        ResponseReasoningEffortLevel.Medium);

    [TestMethod]
    public void GetFriendlyMessage_WhenKeyCannotAccessModel_ExplainsNextStep()
    {
        var result = OpenAiRequestExceptionFormatter.GetFriendlyMessage(404, "model not found", Request);

        StringAssert.Contains(result, "cannot use model 'unavailable-model'");
        StringAssert.Contains(result, "DefaultModelName");
    }

    [TestMethod]
    public void GetFriendlyMessage_WhenModelRejectsReasoning_ExplainsNoneOverride()
    {
        var result = OpenAiRequestExceptionFormatter.GetFriendlyMessage(
            400,
            "reasoning_effort is not supported for this model",
            Request);

        StringAssert.Contains(result, "--reasoning none");
    }
}
#pragma warning restore OPENAI001
