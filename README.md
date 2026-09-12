<h1 align="center">Utilla-Library</h1>

<p align="center">
  <strong>Utilla and GorillaLibrary compatibility in one Gorilla Tag plugin.</strong><br>
  Run mods built for either API through one shared game mode manager.
</p>

<p align="center">
  <a href="https://github.com/picoskids/Utilla-Library/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/picoskids/Utilla-Library?label=latest%20release&style=for-the-badge"></a>
  <a href="https://github.com/picoskids/Utilla-Library/releases/latest"><img alt="Latest release downloads" src="https://img.shields.io/github/downloads/picoskids/Utilla-Library/latest/UtillaLibrary.dll?style=for-the-badge"></a>
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/license-MIT-blue?style=for-the-badge"></a>
</p>

<p align="center">
  <a href="#installation">Installation</a> ·
  <a href="#using-game-modes">Using game modes</a> ·
  <a href="#how-it-works">How it works</a> ·
  <a href="#for-mod-authors">For mod authors</a> ·
  <a href="#known-limitations">Known limitations</a>
</p>

## Why it exists

Utilla and GorillaLibrary both manage modded game modes and room callbacks. Installing them together can cause duplicate patches and conflicting game mode lists; some mods also declare the two plugins incompatible. A mod compiled for one library cannot simply use the other because its assembly and type references are fixed at build time.

Utilla-Library puts both public APIs in `UtillaLibrary.dll` and routes them through one game mode manager and network controller.

## Installation

1. Download `UtillaLibrary.dll` from the [latest release](https://github.com/picoskids/Utilla-Library/releases/latest).
2. Put it in `BepInEx/plugins/UtillaLibrary/` inside your Gorilla Tag installation.
3. Remove existing `Utilla.dll` and `GorillaLibrary.dll` copies from `BepInEx` so they do not load alongside it.

<details>
<summary><strong>Build from source</strong></summary>

Run this from the repository root:

```bash
dotnet build UtillaLibrary/UtillaLibrary.csproj -c Release
```

The output is `UtillaLibrary/bin/Release/netstandard2.1/UtillaLibrary.dll`.

The build looks for Gorilla Tag in the usual Steam locations. For a different location, pass `-p:GamePath="/path/to/Gorilla Tag"`, set the `GamePath` environment variable, or edit [`Directory.Build.props`](Directory.Build.props).

</details>

## Using game modes

The game mode selector and Virtual Stump custom map selector gain two page buttons:

| Button | Action |
| :---: | --- |
| `-->` | Show the next page of modes |
| `<--` | Show the previous page of modes |

The pages contain the vanilla modes available on that selector, their modded versions, and custom modes registered by installed mods. Select a mode and join a room to trigger its join callback. Leaving the room or changing its mode triggers the corresponding leave callback.

> [!NOTE]
> `GorillaComputer.SetGameModeWithoutButton` is currently gated by the selector patches, so calls from other mod code may be ignored. Use the in-game selector to change modes.

## How it works

| Part | What it does |
| --- | --- |
| **One plugin, two APIs** | The [GorillaLibrary plugin](UtillaLibrary/GorillaLibrary/Plugin.cs) installs the Harmony patches. The [Utilla plugin](UtillaLibrary/Utilla/Plugin.cs) exposes Utilla types and events without creating a second game mode registry. Both BepInEx plugin GUIDs are present. |
| **Assembly redirect** | Existing mods may reference assemblies named `Utilla` or `GorillaLibrary`. An `AssemblyResolve` handler in [`Compat.cs`](UtillaLibrary/GorillaLibrary/Compat.cs) maps those requests to `UtillaLibrary.dll` after this plugin loads. |
| **Shared game mode list** | At game initialization, the [game mode manager](UtillaLibrary/GorillaLibrary/Behaviours/GameModeManager.cs) scans loaded plugins for `ModdedGamemode` attributes and older `UnbannedGamemode` names. It combines vanilla, modded vanilla, and custom modes and finds their join and leave callbacks. Attribute class names are recognized across both APIs. |
| **Shared room events** | The [network controller](UtillaLibrary/GorillaLibrary/Behaviours/NetworkController.cs) observes room joins, leaves, and mode changes, calls registered callbacks, and forwards room events to Utilla. The Utilla plugin also forwards GorillaLibrary's game initialization event. |
| **Incompatibility filter** | A runtime filter ignores incompatibility declarations targeting the two library GUIDs; it leaves all other declarations alone. Because it starts with the plugin, it may be too late for a mod BepInEx rejected during discovery. |

## For mod authors

Existing mods can keep their Utilla or GorillaLibrary API references and should keep the matching `[BepInDependency]` so they load after this plugin. New mods can use the GorillaLibrary API directly; its manager handles game mode registration and callbacks for both APIs.

## Known limitations

- `GorillaLibrary.Wardrobe` is not bundled. It is a separate plugin that can run alongside this one.
- Utilla's old `RoomUtils` joining helpers are still missing, as in current Utilla.
- Two mods claiming the same game mode ID still collide.
- The assembly redirect only exists after this plugin loads. A mod resolving a library reference earlier may fail to load; give it the appropriate BepInEx dependency.
- The incompatibility filter cannot guarantee that BepInEx will load a mod rejected earlier during discovery.

## Credits and licenses

Madman had the idea and asked me to build it. This exists because of him.

| Included code | Origin | Notes |
| --- | --- | --- |
| [`UtillaLibrary/GorillaLibrary/`](UtillaLibrary/GorillaLibrary/) | [GorillaTagModdingHub/GorillaLibrary](https://github.com/GorillaTagModdingHub/GorillaLibrary) | Vendored MIT snapshot with the Utilla incompatibility removed, a room bridge and assembly redirect added, and namespace-independent game mode scanning. |
| [`UtillaLibrary/Utilla/`](UtillaLibrary/Utilla/) | [legoandmars/Utilla](https://github.com/legoandmars/Utilla) | MIT public API reimplementation, checked against a shipped `Utilla.dll`. |

Both copyright notices are in the [MIT license](LICENSE). GorillaLibrary updates require a manual merge, especially in `Plugin.cs`, `Behaviours/GameModeManager.cs`, and `Behaviours/NetworkController.cs`.
