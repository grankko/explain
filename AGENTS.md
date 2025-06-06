# Agent Instructions

This repository contains a .NET command line application `explain` that uses OpenAI's API to answer questions or analyze piped input. Follow these guidelines when working with the project.

## Building

- Restore and build the CLI project:

  ```bash
  dotnet build src/Explain.Cli/Explain.Cli.csproj
  ```

## Running Tests

The project has two types of tests organized in separate namespaces:

### Unit Tests (Default)
- Execute the unit test suite (runs by default):
  ```bash
  dotnet test src/Explain.Cli.Tests/
  ```
- Unit tests are located in `src/Explain.Cli.Tests/UnitTests/` and test individual components in isolation
- These tests run quickly and don't require external dependencies or API keys

### Integration Tests (Manual)
- Integration tests are **ignored by default** to prevent environment lockup
- Run integration tests specifically (requires CLI to be built first):
  ```bash
  dotnet test src/Explain.Cli.Tests/ --filter "FullyQualifiedName~IntegrationTests"
  ```
- Integration tests are located in `src/Explain.Cli.Tests/IntegrationTests/` 
- These tests execute the CLI as separate processes and verify end-to-end functionality
- They include timeout mechanisms to prevent hanging during test execution

All tests should pass without needing a real API key. Integration tests verify graceful error handling when no valid API key is configured.

## Running the Application

### Configuration
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
   - The container used for evaluation typically provides `EXPLAIN_OPENAI_KEY` automatically

### Running
Run the application with a question or by piping input:

```bash
dotnet run --project src/Explain.Cli -- "Your question here"
echo "some text" | dotnet run --project src/Explain.Cli -- "Explain this"

# Use flags for additional functionality
dotnet run --project src/Explain.Cli -- "complex question" --think --verbose
dotnet run --project src/Explain.Cli -- --show-history 10
dotnet run --project src/Explain.Cli -- --clear-history
```

The `publish.sh` script can be used to build a standalone executable.

## Code Quality Expectations

- Keep classes and interfaces in separate files as demonstrated in the existing code base.
- Favour straightforward solutions over complex designs.
- Maintain clear separation of responsibilities between components (commands, configuration, AI service, etc.).

### Testing Philosophy

- **Unit Tests**: Verify real logic and behavior of individual components. Located in `UnitTests/` namespace.
- **Integration Tests**: Test end-to-end CLI functionality through process execution. Located in `IntegrationTests/` namespace and ignored by default.
- Prefer a small number of meaningful tests over a large suite of shallow ones.
- Tests are designed to run without external dependencies and handle missing API keys gracefully.
- Integration tests include timeout mechanisms and proper process management to prevent environment lockup.

