# jot.Test

Unit test project for the jot console application.

## Overview

This project provides comprehensive unit tests for all commands in the jot console application, using the same testing technologies as Figment.Test (MSTest framework with .NET 9.0).

## Test Structure

### Test Categories

- **Unit Tests**: Individual command logic, settings validation, business rule verification
- **Integration Tests**: Command pipeline execution, storage provider interactions, multi-command workflows
- **Mock Strategy**: Mock external dependencies while using real MemoryStorageProvider for data operations

### Directory Structure

```
Commands/
├── Interactive/           # Interactive command tests
├── Schemas/              # Schema management tests
│   └── ImportMaps/       # Import map specific tests
├── Things/               # Thing management tests
└── Pomodoro/            # Pomodoro command tests
TestUtilities/            # Shared test helpers
Integration/              # End-to-end command tests
```

## Current Implementation Status

The test project has been created with a comprehensive framework but may need adjustments to match the actual jot command API. Some compilation errors need to be resolved by examining the actual command interfaces and updating the test code accordingly.

## Usage

```bash
# Build tests
dotnet build jot.Test

# Run tests
dotnet test jot.Test
```

## Note

This is an initial implementation that demonstrates the testing approach. The actual command APIs may differ from the assumptions made in the test code, requiring updates to match the real implementation.