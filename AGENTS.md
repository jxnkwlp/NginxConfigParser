# AGENTS.md

## Cursor Cloud specific instructions

This repository is a single **.NET / C# class library** — `NginxConfigParser` (parses/builds/writes nginx config files). There are no servers, databases, or web/UI services to run; correctness is verified via the xUnit test suite. Standard commands live in `README.md` and `.github/workflows/build.yml`.

### Toolchain
- Requires the **.NET 10 SDK** (the library multi-targets `netstandard2.0;netstandard2.1;net9.0;net10.0`; test/console projects target `net10.0`).
- The SDK is installed at `~/.dotnet` and added to `PATH`/`DOTNET_ROOT` via `~/.bashrc`, so `dotnet` is available in interactive shells. If a non-interactive shell can't find it, either source `~/.bashrc` or call the full path `~/.dotnet/dotnet`.

### Projects
- `NginxConfigParser/` — the core library.
- `NginxConfigParserUnitTests/` — the authoritative xUnit test suite (`dotnet test`).
- `NginxConfigParserTests/` — a manual console demo/smoke-test harness (not a real test project).

### Build / test (CI-gated, see `.github/workflows/build.yml`)
```bash
dotnet restore
dotnet build --no-restore -c Release
dotnet test --no-build -c Release --verbosity normal
```

### Running the console demo
`dotnet run --project NginxConfigParserTests` reads `test.conf` using a path relative to the **current working directory**, so run it from inside `NginxConfigParserTests/` (or a directory containing `test.conf`). Running from the repo root throws `FileNotFoundException: test.conf`. The demo writes `temp.conf`/`temp2.conf` into the working directory — these are throwaway artifacts and should not be committed.

### Lint / formatting
- There is no dedicated linter; style is governed by the root `.editorconfig`. Use `dotnet format` (`--verify-no-changes` to check).
- `dotnet format` is **not** run in CI; only restore/build/test are gated. `dotnet format --verify-no-changes` currently reports pre-existing style deviations (line-ending/charset) in some test files, so do not treat a non-zero format exit as a build/test failure.
