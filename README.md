# Explain CLI

A command-line tool that uses OpenAI's GPT models to explain anything you throw at it. Whether you have a quick question or need detailed analysis of complex content, `explain` provides intelligent responses right in your terminal. AI wrote this readme.

## Features

- **Multiple Input Methods**: Support for direct questions and piped content
- **Smart Mode**: Advanced reasoning with o1 models for complex topics
- **Verbose Mode**: Detailed configuration and processing information
- **Input Validation**: Automatic token counting and size limits

## Installation

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for building from source)
- OpenAI API key

### Quick Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/grankko/explain.git
   cd explain
   ```

2. **Configure your API key**:
   ```bash
   cp src/Explain.Cli/appsettings.example.json src/Explain.Cli/appsettings.json
   # Edit appsettings.json with your OpenAI API key
   ```

3. **Build and publish**:
   ```bash
   chmod +x publish.sh
   ./publish.sh
   ```

4. **Install globally** (optional):
   ```bash
   # The publish script creates a self-contained executable at publish/explain
   # You can copy it to your PATH or use the provided wrapper script
   ```

## Usage

### Direct Questions
```bash
explain "What is machine learning?"
explain "How do I list hidden files in Linux?"
explain "Explain quantum computing in simple terms"
```
### Smart Mode
For complex topics requiring deep reasoning:
```bash
explain "Analyze the trade-offs between microservices and monolithic architecture" --think
```

### Piped Input
Analyze files, command output, or any text content:
```bash
# Explain file contents
cat README.md | explain

# Analyze command output
ls -la | explain "What do these file permissions mean?"

# Process log files
tail -n 100 /var/log/syslog | explain "Summarize any issues"

# Explain code
cat main.py | explain "What does this Python code do?"
```

### Building

```bash
# Build the project
dotnet build src/Explain.Cli/Explain.Cli.csproj

# Run tests
dotnet test src/Explain.Cli.Tests/

# Debug mode
dotnet run --project src/Explain.Cli -- "your question here"
```

### VS Code Integration

The project includes VS Code configuration for easy development:

- **F5**: Start debugging with sample arguments
- **Build task**: Available in the command palette
- **Launch configurations**: Pre-configured for different scenarios

### Publishing

Use the included publish script for creating distributable executables:

```bash
./publish.sh
```

This creates a self-contained Linux executable at `publish/explain` with all dependencies included.