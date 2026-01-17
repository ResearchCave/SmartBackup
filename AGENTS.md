# Repository Guidelines

## Project Structure & Module Organization
- `SmartBackup.sln` is the solution entry point.
- Core app: `SmartBackup/` (console executable and config handling).
- Modules: `SmartBackup.MSSQL/`, `SmartBackup.MySQL/`, `SmartBackup.MongoDB/`, `SmartBackup.HyperV/`, `SmartBackup.Archiver/`.
- Shared utilities: `SmartBackup.Common/`.
- Agent service: `SmartBackup.MSSQLAgent/`.
- Third-party code: `ThirdParty/` (e.g., `ThirdParty/RoboSharp/`).
- Assets and samples: `Assets/`, plus the JSON schema at `SmartBackup/config-schema.json`.

## Build, Test, and Development Commands
- `dotnet build SmartBackup.sln` — build all projects in the solution.
- `dotnet run --project SmartBackup/SmartBackup.csproj -- --help` — run the CLI and display options.
- `dotnet publish SmartBackup/SmartBackup.csproj -c Release` — produce a release build.

## Coding Style & Naming Conventions
- Language: C# (.NET SDK-style projects).
- Indentation: 4 spaces; keep braces and spacing consistent with existing files.
- Naming: `PascalCase` for public types/members, `camelCase` for locals/parameters.
- No repo-wide formatter or linter is configured; follow existing patterns in the touched files.

## Testing Guidelines
- No automated tests are present in this repository.
- If you add tests, prefer `dotnet test` with a standard framework (xUnit/NUnit) and name test projects like `SmartBackup.Tests`.

## Commit & Pull Request Guidelines
- Commit messages are short, descriptive sentences (e.g., “Updated to .net 8”).
- PRs should include a brief summary, affected modules, and a sample command/config when behavior changes.

## Configuration & Security Notes
- Primary runtime configuration is via `config.json` with `$schema: "config-schema.json"`.
- Do not commit real credentials; use placeholders in examples (as in `README.md`).
