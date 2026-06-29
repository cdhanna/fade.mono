# FadeBasic.MonoGame.Templates

`dotnet new` templates for Fade Basic games on MonoGame, modeled on
`dby/FadeBasic/Templates`. This project packs `templates/**` as a NuGet template
package; it compiles no code.

## Templates

| shortName | what it makes |
| --- | --- |
| `fadebasic-monogame` | A MonoGame game project: `game.fbasic` + a `Program.cs` that launches `Game1`. Desktop by default; `dotnet publish -p:FadeMonoGamePlatform=Web` for web export. |
| `fadebasic-monogame-commands` | An author command/system lib (sibling of dby's `fadebasic-commands`, but referencing `FadeBasic.MonoGame.Game` — real systems + raw MonoGame). Ships a `[FadeBasicCommand]` sample and a commented `IFadeGameModule` (Phase 2) example. |

## Try it locally

```sh
dotnet pack
dotnet new install ./bin/Release/FadeBasic.MonoGame.Templates.<version>.nupkg
dotnet new fadebasic-monogame -n MyGame
cd MyGame
dotnet run                                   # desktop
dotnet publish -p:FadeMonoGamePlatform=Web   # web export
```

## Versioning

The game template references **only** `FadeBasic.MonoGame.Game`. Its `Version`
attribute is rewritten to this package's `$(Version)` at pack time (the
`StampFadeVersions` target). The Fade **core** (`Lang.Core`, `Lib.Standard`, and
the `FadeBasic.Build` generator) rides in **transitively** from that one package
— a single version stream, nothing else to pin in the generated project.

## ⚠️ Prerequisite (Phase 2)

For the generated game to restore/build, **`FadeBasic.MonoGame.Game` must bring
the Fade core + the `FadeBasic.Build` generator transitively** (today `.Game`
references `Lang.Core`/`Lib.Standard` but **not** `FadeBasic.Build`). Until the
Game package/props add that, a generated project won't produce `GeneratedFade`.

This template is the **baseline shape**; it becomes buildable once the Phase-2
package work (props consolidation + `FadeBasic.Build` transitive) lands. See
`../../WEB_EXPORT_AND_EXTENSIBILITY_SPEC.md`.
