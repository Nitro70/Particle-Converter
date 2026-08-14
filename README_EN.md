# Particle Converter 
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/kemo14331/Particle-Converter)  [![GitHub license](https://img.shields.io/github/license/kemo14331/Particle-Converter)](https://github.com/kemo14331/Particle-Converter/blob/main/LICENSE)  
Particle Converter is an application to convert image files into particle commands.

## ScreenShot
 ![screenshot0](https://imgur.com/HvnhBgF.jpg,"screenshot")
 <details>
 <summary>and more</summary><div>  
 <img src="https://imgur.com/Ld544Cx.jpg", "screenshot1">
 <img src="https://imgur.com/hdSbSkc.jpg" alt="screenshot2" />
 </div></details>  

> **This fork targets Minecraft 26.2.** Upstream last targeted 1.16, and its output no longer
> runs: particle options became SNBT in 1.20.5 and the datapack `functions` directory was renamed
> to `function` in 1.21. See [what changed](#whats-different-in-this-fork).

## Feature
* Convert the image file (.jpg|.png) into a particle command that can be displayed in Minecraft and output as a mcfunction format
* Correspond to world relative coordinates(~) and local relative coordinates(^).
* Real-time preview of parameter changes.
* Display size can be specified by block.
* Support for changing resolution.
* Supports the color specification of dust.
* Compatible with particles other than dust.
* Multi-language support for the app.
* **Exports a complete datapack** - `pack.mcmeta` plus `data/<namespace>/function/<name>.mcfunction` - and tells you the `/function` command to run.
* **Targets any Minecraft version from 1.16.5 to 26.2**, switching command syntax, directory names and pack format automatically.

Translated with www.DeepL.com/Translator (free version)

## What's different in this fork

| | Upstream (1.16) | This fork (26.2) |
|---|---|---|
| dust options | `dust 1 0 0 0.75 …` | `dust{color:[1,0,0],scale:0.75} …` (SNBT, since 1.20.5) |
| datapack directory | `data/<ns>/functions/` | `data/<ns>/function/` (singular, since 1.21) |
| `pack.mcmeta` | not written | `min_format`/`max_format` `[107,1]` |
| particle list | 66 ids from 1.16, including `barrier` (removed in 1.18) | 125 ids, filtered per selected version |
| max particle size | capped at `1.00` | capped at `4.00` |
| `.jpg` colours | misread - four bytes per pixel from a three-byte buffer | correct |

### About the particle size cap

The upstream input box rejected anything above `1.00`, and the usual workaround was to export at
`1.0` and find-and-replace the number in a text editor. That worked on 1.19 because there was no
server-side limit at all ([MC-159741](https://bugs.mojang.com/browse/MC-159741)).

From 1.20.5 the `scale` field is validated by a codec range check of **0.01 - 4.0**. A value above
4 is not clamped, it is a parse error, so the command silently never runs. This fork raises the
input cap to the real maximum of 4.

## Usage

[docs/USAGE.md](docs/USAGE.md) walks through the whole workflow on 26.2 — settings, exporting a
datapack, anchoring the image to an entity, and making it rotate. Worth reading if you're
following a tutorial made before 1.21, because the datapack folder name and the particle size
advice in those are now wrong.

[CHANGELOG.md](CHANGELOG.md) lists everything that changed from upstream.

## Downloads
 [Particle-Converter/Release](https://github.com/kemo14331/Particle-Converter/releases/latest)

## Requirement

 * .NET 10 Desktop Runtime

## Building

```
dotnet build ParticleConverter/ParticleConverter.csproj
dotnet test  ParticleConverter.Tests/ParticleConverter.Tests.csproj
```

The particle id list is generated from the [misode/mcmeta](https://github.com/misode/mcmeta)
registry dumps. To add a Minecraft version, add it to `McVersionProfile.All` and re-run
`tools/gen_registry.ps1`.

## Library
 * [Material Design In Xaml](http://materialdesigninxaml.net/)
 * [OpenCVSharp4](https://github.com/shimat/opencvsharp)
 * [HelixToolkit.SharpDX.Core.Wpf](https://github.com/helix-toolkit/helix-toolkit) 

## Author

* Kemo431  
* Twitter: [@newkemo431](https://twitter.com/newkemo431)
 
## License
This app is under the [MIT license](https://en.wikipedia.org/wiki/MIT_License).
