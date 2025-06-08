namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Wrapper class that includes history in AI composition while preserving original content for storage.
    /// </summary>
    public class ExplainInputContentWithHistory : ExplainInputContent
        {
            private readonly ExplainInputContent _originalContent;
            private readonly string _history;
            
            public ExplainInputContentWithHistory(ExplainInputContent originalContent, string history)
            {
                _originalContent = originalContent;
                _history = history;
                PipedContent = originalContent.PipedContent;
                ArgumentContent = originalContent.ArgumentContent;
            }
            
            public override string ComposeForAI()
            {
                var originalComposition = _originalContent.ComposeForAI();
                return $"{_history}\n{originalComposition}";
            }
        }
}
