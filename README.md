# Bannerlord Cheat Toggle

Toggles `config.cheat_mode` with a hotkey (default **Ctrl+Shift+C**) and shows ON/OFF in the game log and as a top banner.

Optionally suspends cheat mode while the **party** or **inventory** screen opens. With cheats on, the game fills those screens with every troop or item in the game, which makes them slow to open. Cheat mode is switched back on when the screen closes.

Everything is configurable in-game under **Mod Options → Cheat Toggle**.

## Requirements

- Harmony
- ButterLib
- UIExtenderEx
- Mod Configuration Menu v5

Load all four above Cheat Toggle in the launcher.

## Install

Download `CheatToggle.zip` from [Releases](../../releases) (or the latest build artifact under [Actions](../../actions)) and extract it into `<game>\Modules\`, giving:

```
Modules\CheatToggle\SubModule.xml
Modules\CheatToggle\bin\Win64_Shipping_Client\CheatToggle.dll
Modules\CheatToggle\bin\Win64_Shipping_Client\CheatToggle.pdb
```

## Build locally

Requires the .NET SDK (8 or newer).

```
dotnet build -c Release
```

- With the game installed (Steam install auto-detected from the registry, or `-p:GameFolder="X:\...\Mount & Blade II Bannerlord"`), it compiles against your game DLLs and installs into `Modules\CheatToggle`. Add `-p:Install=false` to skip the install.
- Without the game, it compiles against `Bannerlord.ReferenceAssemblies.Core` (`-p:BannerlordRefVersion=1.4.8.*` to pick a version).
- Either way the ready-to-ship module is staged in `out\Modules\CheatToggle`.

## Releases

Every push to `main` is built by GitHub Actions; the module is attached to the run as an artifact.

To publish a release, bump `<Version value="vX.Y.Z" />` in `SubModule.xml` and push to `main`. If no release exists for that version yet, CI tags the commit and publishes `CheatToggle.zip`. Pushing a `v*` tag yourself also publishes a release.
