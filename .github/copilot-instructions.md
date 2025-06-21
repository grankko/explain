# Agent Instructions

## Project summary
This repository contains a .NET command line application `explain` that uses OpenAI's API to answer questions or analyze piped input. It is primarily designed to run on Linux.

## Tech stack
- dotnet 9
- C#

## Configuration
The application uses a layered configuration approach:

1. **User Configuration Directory**: `~/.config/explain/`
   - Configuration file: `~/.config/explain/appsettings.json` (optional)
   - Database: `~/.config/explain/explain_history.sqlite`
   - Directory created automatically on first run

2. **Configuration Priority** (highest to lowest):
   - Environment variables: `EXPLAIN_OPENAI_KEY`, `EXPLAIN_OPENAI_MODEL_NAME`, `EXPLAIN_OPENAI_SMART_MODEL_NAME`
   - User config file: `~/.config/explain/appsettings.json`
   - Application config file: `src/Explain.Cli/appsettings.json`

3. **Development Setup**:
   - `src/Explain.Cli/appsettings.json` contains placeholder settings but does not need a real API key
   - Set the `EXPLAIN_OPENAI_KEY` environment variable (and optional model name variables) to override defaults

## Running the application

```bash
dotnet run --project src/Explain.Cli -- "Your question here"
echo "some text" | dotnet run --project src/Explain.Cli -- "Explain this"

# Use flags for additional functionality.
# The --think flag indicates if a reasoner model should be used, for though questions
dotnet run --project src/Explain.Cli -- "complex question" --think --verbose
dotnet run --project src/Explain.Cli -- "complex question" --include-history
# Include last 3 history entries
dotnet run --project src/Explain.Cli -- "complex question" --include-history 5
# Include last 5 history entries
dotnet run --project src/Explain.Cli -- --show-history 10
dotnet run --project src/Explain.Cli -- --clear-history
```

## Code Quality Expectations
- Always keep classes and interfaces in separate files as demonstrated in the existing code base.
- Always favour straightforward solutions over complex designs.
- Maintain clear separation of responsibilities between components (commands, configuration, AI service, etc.).
- When a code file becomes too large, consider refactoring to maintain readeable and clear code, but avoid overly complex solutions.

## Testing Philosophy
- Always prefer a small number of meaningful tests over a large suite of shallow ones.
- Always separate UnitTests and IntegrationTests in their respective namespace.
- Integration tests are **ignored by default** to prevent environment lockup

## Dependency Philosophy
- Always use the latest stable version of referenced packages.
- Never attempt to downgrade dependencies to solve issues.
- Always prefer Microsoft packages over third party alternatives.

## Misc
- The `publish.sh` script can be used to build a standalone executable.