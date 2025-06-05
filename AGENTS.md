# Agent Instructions

This repository contains a .NET command line application `explain` that uses OpenAI's API to answer questions or analyze piped input. Follow these guidelines when working with the project.

## Building

- Restore and build the CLI project:

  ```bash
  dotnet build src/Explain.Cli/Explain.Cli.csproj
  ```

## Running Tests

- Execute the automated test suite:

  ```bash
  dotnet test src/Explain.Cli.Tests/Explain.Cli.Tests.csproj
  ```

The project uses MSTest and tests should pass without needing an API key.

## Running the Application

- `src/Explain.Cli/appsettings.json` contains placeholder settings but does not need a real API key. Set the `EXPLAIN_OPENAI_KEY` environment variable (and optional `EXPLAIN_OPENAI_MODEL_NAME` or `EXPLAIN_OPENAI_SMART_MODEL_NAME`) to override these values. The container used for evaluation typically provides `EXPLAIN_OPENAI_KEY` automatically.
- Run the application with a question or by piping input:

  ```bash
  dotnet run --project src/Explain.Cli -- "Your question here"
  echo "some text" | dotnet run --project src/Explain.Cli -- "Explain this"
  ```

The `publish.sh` script can be used to build a standalone executable.

## Code Quality Expectations

- Keep classes and interfaces in separate files as demonstrated in the existing code base.
- Favour straightforward solutions over complex designs.
- Maintain clear separation of responsibilities between components (commands, configuration, AI service, etc.).

### Testing Philosophy

- Provide tests that verify real logic and behaviour. Empty or trivial tests should be avoided.
- Prefer a small number of meaningful tests over a large suite of shallow ones.

