# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```sh
# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~PokemonTests.ShouldLoadPokemonFromFile"

# Run the CLI
dotnet run --project src/PKHeX.CLI -- [savefile]

# Run the web app locally (requires JS build first)
cd src/PKHeX.Web/_js && npm ci && npm run build && cd ../../..
dotnet run --project src/PKHeX.Web

# Build web plugins (copies DLLs to ./plugins/<name>/<version>/)
./build-plugins.sh
```

After cloning, submodules must be initialized:
```sh
git submodule update --init --recursive
```

## Architecture

The repository is a .NET 10 solution providing two frontends (CLI and Blazor WebAssembly web app) on top of a shared facade library, all wrapping the upstream PKHeX.Core engine.

### Dependency direction

```
PKHeX.CLI
PKHeX.Web
  └── PKHeX.Web.Plugins (SDK)
        └── PKHeX.Facade
              └── PKHeX.Core.AutoMod (external submodule)
                    └── PKHeX.Core (NuGet 26.03.06)
```

### External submodules

- `external/PKHeX-Plugins` (branch: `cherrytree`) — forked AutoMod/AutoLegality plugins; references `PKHeX.Core` via NuGet

`PKHeX.Core` is consumed as a NuGet package (version `26.03.06`) transitively through `PKHeX.Core.AutoMod`.

### PKHeX.Facade (`src/PKHeX.Facade/`)

The central abstraction layer. It wraps `PKHeX.Core.SaveFile` into a domain model:

- **`Game`** — top-level entry point; loaded from bytes or file path via `Game.LoadFrom(...)`. Exposes `Trainer`, `BattlePoints`, and repositories (`SpeciesRepository`, `PokemonRepository`, `LocationRepository`, `ItemRepository`).
- **`Trainer`** — wraps trainer info and gives access to `Party` and `Box`.
- **`Pokemon`** — domain object for a single Pokemon, wrapping `PKM`.
- **Repositories** (`src/PKHeX.Facade/Repositories/`) — read/lookup helpers for species, items, moves, locations, forms, abilities, and game versions.
- **`AutoLegality`** — extension methods that call into `PKHeX.Core.AutoMod` to legalize Pokemon.

### PKHeX.Web (`src/PKHeX.Web/`)

Blazor WebAssembly app. Key patterns:

- **`GameService`** — singleton Blazor service that holds the currently loaded `Game`. Pages and components inject this service and react to `OnGameLoaded`.
- **Pages** (`src/PKHeX.Web/Pages/`) — Blazor pages for party, box, items, encounter search, plugin management, etc.
- **`_js/`** — TypeScript/Vite project bundled separately; produces JS interop helpers (file I/O, crypto, Firebase). Must be built before running the web project.
- **`_blog/`** — Static blog site built with npm.

### Plugin system (`src/PKHeX.Web.Plugins/` and `src/PKHeX.Web.Plugins.*/`)

`PKHeX.Web.Plugins` is a publishable NuGet SDK for external plugin authors. Plugins implement hook interfaces from `Contracts.cs`:

- `IRunOnPokemonChange` — fires on any Pokemon field edit
- `IRunOnPokemonSave` — fires when user saves a Pokemon
- `IPokemonEditAction` / `IPokemonStatsEditAction` — adds action buttons to the Pokemon edit UI
- `IQuickAction` — adds a button to the home page
- `IRunOnItemChanged` — fires on item changes

A plugin class extends `Settings` (from `src/PKHeX.Web.Plugins/Settings.cs`) and registers its hooks and default settings in the constructor via `EnabledByDefault<THook>()`.

First-party plugins (`src/PKHeX.Web.Plugins.AutoLegality`, `src/PKHeX.Web.Plugins.Nuzlocking`, `src/PKHeX.Web.Plugins.LiveRun`) serve as reference implementations.

### PKHeX.CLI (`src/PKHeX.CLI/`)

Terminal UI using Spectre.Console. Starts with `PkCommand` (`Program.cs`), loads a `Game`, then runs an interactive selection loop dispatching to command handlers in `Commands/`.

### Tests (`src/PKHeX.Facade.Tests/`)

xUnit tests using `AwesomeAssertions` (FluentAssertions fork). Test fixtures in `Base/` provide reusable `Game` and `Pokemon` instances. Save file fixtures are stored in `data/save/`. The `[SupportedSaveFiles]` attribute discovers test save files and parameterizes theory tests.
