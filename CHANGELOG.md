# Changelog

## 2.0.0 — Minecraft 26.2

A fork of [kemo14331/Particle-Converter](https://github.com/kemo14331/Particle-Converter) 1.0.4,
which last targeted Minecraft 1.16.

### Minecraft compatibility

- Particle options are written as SNBT: `dust{color:[1,0,0],scale:0.75}` rather than
  `dust 1 0 0 0.75`. Changed in 1.20.5.
- Datapack functions are written to `data/<namespace>/function/`, singular. Renamed in 1.21.
- `pack.mcmeta` is written, using `min_format`/`max_format` arrays from 1.21.9 and a flat
  `pack_format` integer before that. 26.2 is `[107, 1]`.
- The particle list is per-version and generated from the
  [misode/mcmeta](https://github.com/misode/mcmeta) registry dumps — 125 ids on 26.2, against the
  66 hardcoded 1.16 ids the tool shipped with. Those still offered `barrier`, removed in 1.18.
- A version dropdown covers 1.16.5 through 26.2 and switches command syntax, directory name and
  pack format together.

### Particle size

- The maximum accepted size is now `4.00`, up from `1.00`.

  The old cap is why the well-known workaround was to export at `1.0` and find-and-replace the
  number in a text editor. That worked on 1.19 because there was no server-side limit at all
  ([MC-159741](https://bugs.mojang.com/browse/MC-159741)). From 1.20.5 the `scale` field is
  validated by a codec range check of 0.01–4.0, and a value above it is a parse error rather than
  a clamp — the command silently never runs. 4.0 is the real ceiling, not 5.

### Export

- Exports a complete, ready-to-drop datapack by default, and reports the `/function` command to
  run. A bare `.mcfunction` is still available via a checkbox.
- Namespace and function name are configurable and sanitised to Minecraft's allowed characters.
- The generated command is previewed live in the settings panel.
- Particles needing options the tool cannot derive from an image — block states, item ids, the
  fade colour of `dust_color_transition`, raw SNBT for `shriek` and friends — get an input field
  labelled for that particle.

### Fixes

- Three-channel images (every `.jpg`) were read four bytes per pixel from a three-byte-per-pixel
  buffer, so colours were wrong throughout and the final row overran its allocation. Images are
  now normalised to 8-bit BGRA on load, which also fixes greyscale and 16-bit PNGs.
- Opening **More Settings** threw `ArgumentException: An item with the same key has already been
  added` and terminated the app. WPF re-raises `Loaded` when the Expander re-attaches its content;
  the handlers are now idempotent.
- An unhandled UI exception reported itself and then killed the process. A failed preview or an
  unreadable image called `Close()` on the main window. Both are now non-fatal.
- Coordinates were formatted with `"R"`, which switches to exponential notation for small
  magnitudes — `1E-07` is a parse error as a coordinate. Formatting is now fixed-point.
- Colour channels were rounded to two decimals, collapsing 256 levels to about 101. Now four.
- Number formatting is explicitly invariant, so a machine with a comma decimal separator cannot
  emit `~0,5`.
- The language selection persists between runs.

### Build

- Targets `net10.0-windows`.
- OpenCvSharp comes from NuGet. The 51 MB `OpenCvSharpExtern.dll` is out of git, along with a
  `<Reference>` whose `HintPath` pointed into the original author's Desktop.
- Assembly signing against a `.pfx` that was never committed is removed, so the project builds
  from a clean clone.
- `WindowsAPICodePack-Shell` is dropped in favour of WPF's built-in `OpenFolderDialog`.
- MaterialDesignThemes 5.x and HelixToolkit 2.27. MDIX 5 removed
  `MaterialDesignTheme.Defaults.xaml`; the `MaterialDesign2` variant is used to keep the original
  look.
- User settings moved from `ApplicationSettingsBase` to JSON under
  `%APPDATA%\ParticleConverter\settings.json`.
- Added `ParticleConverter.Tests` — 86 tests over command generation, the particle registry, the
  datapack layout, and an image-to-datapack round trip.
