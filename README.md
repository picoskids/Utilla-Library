<h1 align="center">Utilla-Library</h1>

<p align="center">
  <strong>One DLL for Gorilla Tag mods that use Utilla or GorillaLibrary.</strong><br>
  Both APIs use the same game modes and room callbacks.
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

Install Utilla and GorillaLibrary side by side and they both patch the game's mode code while keeping separate mode lists. Some mods also mark the two plugins as incompatible. Removing either library leaves the mods built for it without the assembly they expect.

Utilla-Library puts both APIs in `UtillaLibrary.dll`. Mods can still call the API they were built for, while one manager handles the game modes and room events.

## Installation

1. Download `UtillaLibrary.dll` from the [latest release](https://github.com/picoskids/Utilla-Library/releases/latest).
2. Put it in `BepInEx/plugins/UtillaLibrary/` inside your Gorilla Tag installation.
3. Remove any old `Utilla.dll` and `GorillaLibrary.dll` files from `BepInEx` so they do not load alongside it.

<details>
<summary><strong>Build from source</strong></summary>

Run this from the repository root:

```bash
dotnet build UtillaLibrary/UtillaLibrary.csproj -c Release
```

The output is `UtillaLibrary/bin/Release/netstandard2.1/UtillaLibrary.dll`.

The build checks the usual Steam locations for Gorilla Tag. If your copy is elsewhere, pass `-p:GamePath="/path/to/Gorilla Tag"`, set the `GamePath` environment variable, or edit [`Directory.Build.props`](Directory.Build.props).

</details>

## Using game modes

The regular game mode selector and the Virtual Stump custom map selector have two extra buttons:

| Button | Action |
| :---: | --- |
| `-->` | Show the next page of modes |
| `<--` | Show the previous page of modes |

Keep paging past the usual modes to find their modded versions and any custom modes your installed mods add. Pick a mode, then join a room. Mods registered for that mode get a join callback; they get a leave callback when you leave or the room changes mode.

> [!NOTE]
> Calling `GorillaComputer.SetGameModeWithoutButton` from another mod may do nothing because the selector patches gate it. For now, change modes on the in-game board.

## How it works

### One DLL, two APIs

The [GorillaLibrary plugin](UtillaLibrary/GorillaLibrary/Plugin.cs) installs the Harmony patches and owns the game mode list. The [Utilla plugin](UtillaLibrary/Utilla/Plugin.cs) provides Utilla's types and events without setting up a second list. Both BepInEx plugin IDs are present, so mods can depend on either one.

Mods built for the original libraries may still ask for an assembly named `Utilla` or `GorillaLibrary`. The [`AssemblyResolve` handler](UtillaLibrary/GorillaLibrary/Compat.cs) sends those requests to `UtillaLibrary.dll` once this plugin has loaded.

### Game modes and room events

When the game starts, the [game mode manager](UtillaLibrary/GorillaLibrary/Behaviours/GameModeManager.cs) checks loaded mods for `ModdedGamemode` attributes, including the older `UnbannedGamemode` names. It puts their custom modes beside the vanilla and modded vanilla modes. It also finds the methods those mods marked for join and leave callbacks. The scanner matches attribute class names, so it accepts either library's API.

The [network controller](UtillaLibrary/GorillaLibrary/Behaviours/NetworkController.cs) watches for room joins, leaves, and mode changes. It calls the registered methods and passes room events to Utilla. The Utilla plugin passes along GorillaLibrary's game initialization event too.

### Incompatibility declarations

The plugin filters out incompatibility declarations aimed at the Utilla and GorillaLibrary plugin IDs. Other declarations still apply. This only starts after the plugin loads, so a mod BepInEx rejected earlier may still be blocked.

## For mod authors

If your mod already uses Utilla or GorillaLibrary, you can keep that reference. Keep its matching `[BepInDependency]` too, so your mod loads after this plugin. For a new mod, use the GorillaLibrary API; it is the one that manages modes and callbacks here.

## Known limitations

- `GorillaLibrary.Wardrobe` is a separate plugin. It is not bundled, but you can run it alongside this one.
- Utilla's old `RoomUtils` joining helpers are still missing, just as they are in current Utilla.
- Two mods claiming the same game mode ID still collide.
- The assembly redirect only works after this plugin loads. A mod that tries to resolve its library reference earlier may fail; make sure it declares the appropriate BepInEx dependency.
- The incompatibility filter cannot rescue a mod BepInEx rejected before this plugin loaded.

## Credits and licenses

Madman had the idea and asked me to build it. This exists because of him.

The [`GorillaLibrary` code](UtillaLibrary/GorillaLibrary/) comes from [GorillaTagModdingHub/GorillaLibrary](https://github.com/GorillaTagModdingHub/GorillaLibrary) under the MIT license. This copy removes its Utilla incompatibility and adds the room bridge and assembly redirect. It also lets the game mode scanner recognize attributes from either API. The code is a snapshot, so upstream updates need a manual merge, especially in `Plugin.cs`, `Behaviours/GameModeManager.cs`, and `Behaviours/NetworkController.cs`.

The [`Utilla` code](UtillaLibrary/Utilla/) reimplements the public API from [legoandmars/Utilla](https://github.com/legoandmars/Utilla), also under MIT. The API was checked member by member against a shipped `Utilla.dll`. Both copyright notices are in [LICENSE](LICENSE).
