using Explain.Cli.AI;
using Explain.Cli.Commands.Explain;
using Explain.Cli.Configuration;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class OpenAiRequestSettingsFactoryTests
{
    private static readonly OpenAiOptions Defaults = new()
    {
        DefaultModelName = "default-model",
        DefaultReasoningEffort = "medium"
    };

    [TestMethod]
    public void Create_WithoutOverrides_UsesConfiguredDefaults()
    {
        var result = OpenAiRequestSettingsFactory.Create(Defaults, new ExplainArguments());

        Assert.AreEqual("default-model", result.ModelName);
        Assert.AreEqual(ResponseReasoningEffortLevel.Medium, result.ReasoningEffort);
    }

    [TestMethod]
    public void Create_WithOverrides_UsesRequestSelection()
    {
        var result = OpenAiRequestSettingsFactory.Create(Defaults, new ExplainArguments
        {
            ModelName = "other-model",
            ReasoningEffort = "high"
        });

        Assert.AreEqual("other-model", result.ModelName);
        Assert.AreEqual(ResponseReasoningEffortLevel.High, result.ReasoningEffort);
    }

    [TestMethod]
    public void Create_WithNone_LeavesOutReasoningOptions()
    {
        var result = OpenAiRequestSettingsFactory.Create(Defaults, new ExplainArguments
        {
            ReasoningEffort = "none"
        });

        Assert.IsNull(result.ReasoningEffort);
    }

    [TestMethod]
    public void Create_WithApiSupportedExtendedEffort_PreservesIt()
    {
        var result = OpenAiRequestSettingsFactory.Create(Defaults, new ExplainArguments
        {
            ReasoningEffort = "xhigh"
        });

        Assert.AreEqual("xhigh", result.ReasoningEffort?.ToString());
    }

    [TestMethod]
    public void Create_WithUnsupportedEffort_ExplainsAcceptedValues()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenAiRequestSettingsFactory.Create(Defaults, new ExplainArguments { ReasoningEffort = "maximum" }));

        StringAssert.Contains(exception.Message, "none, minimal, low, medium, high, or xhigh");
    }
}
#pragma warning restore OPENAI001
