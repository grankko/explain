using System;

namespace Explain.Cli.Extensions
{
    /// <summary>
    /// Extension methods for Console to provide convenient colorized output methods.
    /// </summary>
    public static class ConsoleExtensions
    {
        /// <summary>
        /// Writes a line to the console with the specified color and automatically resets color afterward.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="text">The text to write</param>
        /// <param name="color">The foreground color to use</param>
        public static void WriteLineColored(this TextWriter console, string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes text to the console with the specified color and automatically resets color afterward.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="text">The text to write</param>
        /// <param name="color">The foreground color to use</param>
        public static void WriteColored(this TextWriter console, string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes an error message in red color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="errorMessage">The error message to display</param>
        public static void WriteError(this TextWriter console, string errorMessage)
        {
            console.WriteLineColored(errorMessage, ConsoleColor.Red);
        }

        /// <summary>
        /// Writes a success message in green color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="successMessage">The success message to display</param>
        public static void WriteSuccess(this TextWriter console, string successMessage)
        {
            console.WriteLineColored(successMessage, ConsoleColor.Green);
        }

        /// <summary>
        /// Writes an informational message in cyan color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="infoMessage">The informational message to display</param>
        public static void WriteInfo(this TextWriter console, string infoMessage)
        {
            console.WriteLineColored(infoMessage, ConsoleColor.Cyan);
        }

        /// <summary>
        /// Writes a warning message in yellow color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="warningMessage">The warning message to display</param>
        public static void WriteWarning(this TextWriter console, string warningMessage)
        {
            console.WriteLineColored(warningMessage, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Writes an AI response in magenta color (matching application color scheme).
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="response">The AI response to display</param>
        public static void WriteAiResponse(this TextWriter console, string response)
        {
            console.WriteLineColored(response, ConsoleColor.Magenta);
        }

        /// <summary>
        /// Writes a header/title in yellow color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="header">The header text to display</param>
        public static void WriteHeader(this TextWriter console, string header)
        {
            console.WriteLineColored(header, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Writes a separator line in green color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="length">The length of the separator line</param>
        public static void WriteSeparator(this TextWriter console, int length = 10)
        {
            console.WriteLineColored(new string('-', length), ConsoleColor.Green);
        }

        /// <summary>
        /// Executes an action while temporarily setting the console color, then resets it.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="color">The color to set</param>
        /// <param name="action">The action to execute while the color is set</param>
        public static void WithColor(this TextWriter console, ConsoleColor color, Action action)
        {
            Console.ForegroundColor = color;
            try
            {
                action();
            }
            finally
            {
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Writes text with a label in one color and content in another color.
        /// </summary>
        /// <param name="console">The console (use Console)</param>
        /// <param name="label">The label text</param>
        /// <param name="content">The content text</param>
        /// <param name="labelColor">The color for the label</param>
        /// <param name="contentColor">The color for the content (optional, defaults to default console color)</param>
        public static void WriteLabelAndContent(this TextWriter console, string label, string content,
            ConsoleColor labelColor, ConsoleColor? contentColor = null)
        {
            console.WriteColored(label, labelColor);

            if (contentColor.HasValue)
                console.WriteLineColored(content, contentColor.Value);
            else
                Console.WriteLine(content);
        }
        

        public static async Task ShowThinkingAnimationAsync(this TextWriter console, CancellationToken cancellationToken, string label)
        {
            string[] spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            int spinnerIndex = 0;

            Console.Write(" "); // Add a space before the animation

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write($"\r {spinner[spinnerIndex]} {label}");
                    spinnerIndex = (spinnerIndex + 1) % spinner.Length;
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean up the animation line
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1));
            }
        }        
    }
}
