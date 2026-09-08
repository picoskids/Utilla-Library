# Utilla-Library

One DLL that keeps both Utilla mods and GorillaLibrary mods working at the same time. Nothing
changes on the mod side. A plugin built against either library loads, registers its gamemodes and
gets its join and leave callbacks, without being recompiled or edited.

## The problem

Utilla and GorillaLibrary do the same job in ways that cannot coexist. They patch the same game
methods, keep separate gamemode registries, and declare each other incompatible, so BepInEx refuses
to run them together. Pick one and you lose every mod written for the other.

A normal shared library cannot fix that. A compiled mod holds a hard reference to an assembly
*name*, either `Utilla` or `GorillaLibrary`, plus the type names inside it. Ship a third assembly
full of new types and the mod cannot see it. So this is one file pretending to be both.

## Install

```bash
dotnet build UtillaLibrary/UtillaLibrary.csproj -c Release
```

The DLL lands in `UtillaLibrary/bin/Release/netstandard2.1/UtillaLibrary.dll`. Copy it into
`BepInEx/plugins/UtillaLibrary/`, then delete any `Utilla.dll` or `GorillaLibrary.dll` still sitting
under `BepInEx`, or they patch the same methods a second time and you get two of everything.

The build finds Gorilla Tag in the usual Steam spots. If yours is elsewhere, pass
`-p:GamePath="/path/to/Gorilla Tag"`, set a `GamePath` environment variable, or edit
`Directory.Build.props`.

## How it works

Both APIs live in the one assembly. `GorillaLibrary.*` is the implementation, `Utilla.*` is a skin
over it that owns no patches and no registry of its own. One set of Harmony patches, one gamemode
list, which is the whole reason the two can run together.

The file is called `UtillaLibrary.dll`, which is neither name a mod asks for, so an
`AssemblyResolve` handler answers both requests with this assembly. Both plugin GUIDs get
registered, so `[BepInDependency]` on either one resolves.

Gamemode attributes are matched by name instead of by type. Anything called `ModdedGamemode`,
`ModdedGamemodeJoin` or `ModdedGamemodeLeave`, plus the older `UnbannedGamemode` spellings,
registers the same way no matter which namespace it came from.

Room events come from one place. The network controller raises an internal event as it hands out
join and leave callbacks, and the Utilla half turns that into `Utilla.Events.RoomJoined`, `RoomLeft`
and `GameInitialized`.

Mods carrying `[BepInIncompatibility]` against Utilla or GorillaLibrary were guarding against double
patching that does not happen here, so those two GUIDs get filtered out of what the chainloader
sees. Anything else a mod declares is untouched.

## Changing gamemodes

Same board you always used. Two buttons get added to the left column of the game mode selector and
the Virtual Stump custom map selector: `-->` for the next page up top, `<--` for the previous page
below it. Page past the vanilla modes for `MODDED INFECTION`, `MODDED CASUAL` and the rest, then
every custom gamemode any installed mod registered. Pick one, join a room, callbacks fire.

Setting the mode from code does not work. `GorillaComputer.SetGameModeWithoutButton` is gated behind
a flag only true during the stump selector's own callbacks, so every other caller gets dropped.
Inherited from GorillaLibrary, on the list to open up.

## For mod authors

Nothing to do. Keep building against whichever library you already use. Starting fresh, use the
GorillaLibrary API, it is the bigger one and it is what actually runs.

## Credits

madman had the idea and told me to build it. This exists because of him.

## What is vendored

- `UtillaLibrary/GorillaLibrary/` is [GorillaTagModdingHub/GorillaLibrary](https://github.com/GorillaTagModdingHub/GorillaLibrary),
  MIT. The Utilla incompatibility is gone, the room bridge and assembly redirect are added, and the
  gamemode scanner no longer cares about namespaces.
- `UtillaLibrary/Utilla/` reimplements the public API of
  [legoandmars/Utilla](https://github.com/legoandmars/Utilla), MIT, checked member for member
  against a shipped `Utilla.dll`.

Both are MIT and both copyright lines are in `LICENSE`. The GorillaLibrary copy is a snapshot, so
pulling upstream changes is a manual merge, and `Plugin.cs`, `Behaviours/GameModeManager.cs` and
`Behaviours/NetworkController.cs` are the files that will fight you.

## Known gaps

- Not tested in a live game yet.
- `GorillaLibrary.Wardrobe` is not bundled. Separate plugin, works fine on top of this.
- Utilla's old `RoomUtils` joining helpers are still missing, same as current Utilla.
- Two mods claiming the same gamemode ID still collide, exactly like before.
- The assembly redirect only exists once this plugin has loaded. Mods that declare a dependency on
  either GUID load after it and are fine, which is essentially all of them. If one skips the
  dependency and fails to resolve, copy the file under the name it wants,
  `cp UtillaLibrary.dll Utilla.dll` or `GorillaLibrary.dll`. BepInEx resolves by filename so that
  settles it, same assembly either way.
