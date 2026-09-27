# Ember

Kleines natives Windows-Studio zum Zusammensetzen von Chat-Charakterkarten.
Mitgeliefert werden nur **Demo-Engines, Demo-Figuren und Demo-Orte**. Keine Konten, kein Posten, keine privaten Geschichten.

A small native Windows studio for assembling chat-character cards. The catalogue is original demo fiction only: three engines, four people, two places, two sample cards. Ember does not sign in, does not upload, and does not post.

## What you can do

- Open a card and edit Name, Introduction, Situation, World, Greeting, and Reply style.
- Pull people and places in with `%include npc:mara#profile` or `%include location:harbor-flat#zones`.
- **Fire** (F5) expands those lines, pins a short demo rules block plus the `#demo` signature, and shows the card as fields, JSON, and Markdown.
- Checks flag empty names, ages under 18, bracket-names in the greeting, missing includes, duplicate rules, over-limit fields, unticked engine facts, and leftover `TODO`s.
- **New** builds a skeleton from an engine, a cast, and a place.

Reply style stays in the Markdown twin. It is not part of the JSON payload.

## Demo catalogue

| Kind | Ids |
|---|---|
| Engines | `shared-deadline`, `last-train`, `shared-landing` |
| People | Mara Ellison, Inez Calder, Jonah Peck, Theo Marsh (all 25) |
| Places | Harbor Flat, East Platform |
| Cards | Sunday at Four, Last Train Home |

## Build

Requires the .NET 8 SDK.

Core and checks, on any OS:

```bash
dotnet run --project src/Ember.Cli/Ember.Cli.csproj -c Release -- check
dotnet run --project src/Ember.Cli/Ember.Cli.csproj -c Release -- fire sunday-at-four
dotnet test tests/Ember.Core.Tests/Ember.Core.Tests.csproj -c Release
```

The window is WinUI 3 (Windows App SDK 1.8) and builds on Windows:

```powershell
powershell -File build-windows.ps1
```

Or open `Ember.sln` in Visual Studio 2022/2026 with the Windows App SDK C# workload. The unpackaged app lands in:

`src/Ember.App/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/Ember.App.exe`

GitHub Actions uploads that folder as `ember-win-x64` on every push to `main`.

## Layout

```
src/Ember.Core     compile, checks, demo catalogue
src/Ember.Cli      ember list | fire | check
src/Ember.App      WinUI 3 window
data/catalog.json  the only fiction in the repo
```

## Out of scope

Accounts, network posting, image upload, git sync, and any private cast or engine library. Locked rules in this demo are a short original block, not a copy of anyone else's guardrail text.

License: MIT.
