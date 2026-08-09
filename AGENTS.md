# Repository Instructions

## Project summary

This repository contains a .NET command-line application, `explain`, that uses the OpenAI Responses API to answer questions or analyze piped input. It primarily targets Linux.

## Tech stack

- .NET 10
- C#
- OpenAI .NET SDK
- SQLite for local history

## Configuration

The application uses layered configuration:

1. Explicit command-line configuration
2. `EXPLAIN_OPENAI_KEY`, `EXPLAIN_OPENAI_MODEL_NAME`, and `EXPLAIN_OPENAI_SMART_MODEL_NAME`
3. `~/.config/explain/appsettings.json`
4. The application-directory `appsettings.json`

The default and smart models are GPT-5.6 Terra. Regular requests use low reasoning effort; `--think` uses high reasoning effort. Hosted web search is available on every request and the model decides when to use it.

Never inspect, print, or commit real API keys. Use `src/Explain.Cli/appsettings.example.json` for documentation and placeholders.

## Common commands

```bash
dotnet run --project src/Explain.Cli -- "Your question here"
echo "some text" | dotnet run --project src/Explain.Cli -- "Explain this"
dotnet run --project src/Explain.Cli -- "complex question" --think --verbose
dotnet run --project src/Explain.Cli -- "follow-up" --include-history 5
dotnet run --project src/Explain.Cli -- --show-history 10
dotnet run --project src/Explain.Cli -- --clear-history
dotnet test src/Explain.sln
```

## Engineering expectations

- Favor straightforward solutions and clear separation between commands, configuration, AI integration, and storage.
- Keep classes and interfaces in separate files unless a small result type is inseparable from its formatter.
- Preserve the plain-text terminal output contract; web-derived answers must expose visible source URLs.
- Prefer a small number of meaningful tests. Keep unit and integration tests in their existing namespaces.
- Integration tests must remain opt-in so ordinary agent runs cannot make paid API calls or hang.
- Use the latest stable dependencies and do not downgrade packages to bypass a problem.
- Use `publish.sh` to build the standalone Linux executable. Ask before starting an unrelated major refactor.
