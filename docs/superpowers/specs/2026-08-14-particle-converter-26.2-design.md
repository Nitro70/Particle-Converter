# Particle Converter — Minecraft 26.2 port

**Date:** 2026-08-14
**Upstream:** [kemo14331/Particle-Converter](https://github.com/kemo14331/Particle-Converter) @ v1.0.4 (last targeting MC 1.16)

## Problem

The tool converts an image into a `.mcfunction` full of `/particle` commands, one particle per
opaque pixel. It was written for Minecraft 1.16 and has not been updated. Three separate things
are now broken.

### 1. The project does not build

| Issue | Location |
|---|---|
| Targets `netcoreapp3.1` (EOL) | `ParticleConverter.csproj` |
| `SignAssembly=true` against `kemo14331.pfx`, which is not in the repo | `ParticleConverter.csproj` |
| `<Reference Include="OpenCvSharp">` with `HintPath` of `..\..\..\..\..\Desktop\a\OpenCvSharp.dll` | `ParticleConverter.csproj` |
| `<PackageReference Include="OpenCV" Version="2.4.11" />` — wrong package for OpenCvSharp | `ParticleConverter.csproj` |
| 51 MB `OpenCvSharpExtern.dll` and `dll/OpenCvSharp.dll` committed to git | repo root |

### 2. The generated commands are invalid on 26.2

| Concern | Tool emits (1.16) | 26.2 requires | Changed in |
|---|---|---|---|
| dust options | `dust 1 0 0 0.75 <pos> …` | `dust{color:[1,0,0],scale:0.75} <pos> …` | 1.20.5 |
| datapack function dir | `data/<ns>/functions/` | `data/<ns>/function/` | 1.21 |
| `pack.mcmeta` | not written at all | `min_format`/`max_format` arrays | 1.21.9 |
| particle IDs | 66 hardcoded 1.16 names, incl. `barrier` (removed 1.18) | 125 IDs in 26.2 | ongoing |

### 3. The particle size cap is wrong

`ParticleSizeBox_LostFocus` rejects any value `> 1.00`. The vanilla `dust` `scale` field accepts
**0.01–4.0**. On 1.19 there was no server-side limit at all
([MC-159741](https://bugs.mojang.com/browse/MC-159741)) — which is why the community workaround of
exporting at `1.0` and find-and-replacing in a text editor worked. On 26.2 `scale` is validated by
a codec range check, so a value above 4.0 is a **parse error**: the command does not run at all.

## Design

### Version profiles

A `McVersionProfile` carries every fact that differs between targets. Numbers below are from
`misode/mcmeta` `<version>-summary/version.json`, not guessed.

| Target | `pack.mcmeta` | Function dir | dust syntax |
|---|---|---|---|
| **26.2** (default) | `min_format`/`max_format` `[107,1]` | `function` | SNBT |
| 26.1 | `[101,1]` | `function` | SNBT |
| 1.21.11 | `[94,1]` | `function` | SNBT |
| 1.21.9 | `[88,0]` | `function` | SNBT |
| 1.21.8 | `"pack_format": 81` | `function` | SNBT |
| 1.21.5 | `"pack_format": 71` | `function` | SNBT |
| 1.21.4 | `"pack_format": 61` | `function` | SNBT |
| 1.21 | `"pack_format": 48` | `function` | SNBT |
| 1.20.6 | `"pack_format": 41` | `functions` | SNBT |
| 1.20.4 | `"pack_format": 26` | `functions` | legacy |
| 1.16.5 | `"pack_format": 6` | `functions` | legacy |

Versions at or above 1.21.9 use the `min_format`/`max_format` array form; earlier ones use the flat
integer `pack_format`.

### New `Minecraft/` namespace

Command generation currently lives as string concatenation inside `ExportButton_Click`
(`MainWindow.xaml.cs:636-659`). It moves into a WPF-free, unit-testable namespace:

- **`McVersion` / `McVersionProfile`** — the table above, plus per-version particle availability.
- **`ParticleRegistry`** — particle IDs per version, sourced from `misode/mcmeta` registry dumps
  (26.2 = 125 IDs), each tagged with an `OptionKind`.
- **`ParticleOptionsWriter`** — serialises options per `OptionKind`:

  | OptionKind | Particles | SNBT |
  |---|---|---|
  | `Dust` | `dust` | `{color:[r,g,b],scale:s}` |
  | `DustColorTransition` | `dust_color_transition` | `{from_color:[…],to_color:[…],scale:s}` |
  | `ColorRgba` | `entity_effect`, `flash`, `tinted_leaves` | `{color:[r,g,b,a]}` |
  | `BlockState` | `block`, `block_marker`, `falling_dust`, `dust_pillar`, `block_crumble` | `{block_state:"minecraft:stone"}` |
  | `Item` | `item` | `{item:"minecraft:stone"}` |
  | `Raw` | `sculk_charge`, `shriek`, `vibration`, `trail`, `geyser*` | user-supplied SNBT |
  | `None` | everything else | — |

- **`FunctionWriter`** — emits the `.mcfunction` lines.
- **`DatapackWriter`** — emits `pack.mcmeta` + `data/<namespace>/function/<name>.mcfunction`, and
  reports the exact `/function <ns>:<name>` to run.

Colour is written as a float list `[r,g,b]` by default, which parses on every SNBT-era target. A
packed-int form is offered only on targets confirmed to accept it.

### UI changes

- Particle size cap `1.00` → `4.00`.
- New **Minecraft Version** dropdown; drives syntax, function dir, and pack format.
- New **Namespace** and **Function name** fields.
- Particle Type dropdown fed from `ParticleRegistry`, filtered to the selected version, so IDs that
  do not exist on the target (e.g. `barrier` on 26.2) cannot be picked.
- Live preview of one generated command, so the emitted syntax is visible before export.
- An **Options (SNBT)** field appears for particles whose `OptionKind` is `Raw`.

### Bug fixed in passing

`ImageConverter.GetParticles` calls `GetGenericIndexer<Vec4b>()` unconditionally
(`util/ImageConverter.cs:209`). A JPEG loads as `CV_8UC3`, so reading four bytes per pixel from a
three-byte-per-pixel buffer misreads every colour and overruns the final row. `.jpg` is one of the
two formats the file picker accepts. Fix: convert to BGRA on load.

## Testing

New `ParticleConverter.Tests` (xunit) against the emitter layer:

- Golden-file `.mcfunction` output per version target.
- `dust` SNBT shape, including that colour components round-trip.
- Scale bounds — rejected below 0.01 and above 4.0.
- Datapack layout: `function` vs `functions`, `pack.mcmeta` shape per profile.
- Particle availability filtering per version.

The WPF shell is verified by a manual smoke test: load image → preview → export → confirm the
resulting pack loads and the function runs.

## Out of scope

- Rewriting the UI framework or the 3D preview.
- Bedrock Edition.
- Animation/sequencing across multiple functions.
