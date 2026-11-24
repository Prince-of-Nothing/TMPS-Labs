# TMPS Labs — Consumed Media Watchlist

**Course:** Tehnici și Mecanisme de Proiectare Software (TMPS)  
**Author:** Dinu Pleșu, FAF241  
**Instructor:** Istrati Daniel  
**Technology:** C# / .NET 10

The repository follows one domain — a personal consumed-media watchlist —
through the four labs. Each lab is a separate runnable project and report.

| Lab | Topic | Client | Report |
|---|---|---|---|
| [Lab 0](Lab0/README.md) | SOLID principles | .NET console | [Report](Lab0/Report.md) |
| [Lab 1](Lab1/README.md) | Creational patterns | .NET MAUI Android | [Report](Reports/Lab1.md) |
| [Lab 2](Lab2/README.md) | Structural patterns | .NET console | [Report](Reports/Lab2.md) |
| [Lab 3](Lab3/README.md) | Behavioral patterns | .NET MAUI Android | [Report](Reports/Lab3.md) |

## Shared domain

Lab 2's `Lab2/Core/Watchlist.Core` project is the shared domain and storage
layer for Labs 2 and 3. It contains:

- the Markdown vault (`index.md`, `entries/*.md`, `covers/`) and its parser;
- the media model and the reused Builder/Factory Method;
- the structural cover pipeline (Adapter, Decorator, Proxy).

Lab 3 references that project and adds the behavioral layer in
`Lab3/Behavioral/Watchlist.Behavioral` (Command, Observer, State, Strategy,
AniList/MAL import and CSV/Markdown export).

## Run everything

```powershell
# Console lab
dotnet run --project .\Lab2\Console\Lab2.csproj

# Tests for both labs
dotnet test .\Tests\Watchlist.Tests\Watchlist.Tests.csproj

# Android lab (needs MAUI workload, JDK 21, Android SDK)
dotnet build .\Lab3\App\Lab3.csproj -f net10.0-android
```

The Markdown library is readable and editable without the app. Open
`index.md` for the overview, or any entry file for the notes. On first launch
Lab 3 asks whether to keep the vault in shared `Documents/TMPS-Watchlist`
(visible in the Android Files app, with an All files access prompt) or in
app-private storage.

The design record is in
[`docs/superpowers/specs/2026-11-24-labs-2-3-watchlist-design.md`](docs/superpowers/specs/2026-11-24-labs-2-3-watchlist-design.md).
