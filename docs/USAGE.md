# Using Particle Converter on Minecraft 26.2

Most tutorials for this tool were made around 1.16-1.19. The tool still works the same way, but
three things in that workflow are now wrong. This page is the current version.

## What changed since those tutorials

| The tutorial says | What to do now |
|---|---|
| Put the `.mcfunction` in your datapack's `functions` folder | The folder is `function`, singular, since 1.21. Tick **Export as a datapack** and the tool builds the whole tree for you. |
| Set size to `1.0`, export, then find-and-replace it with `5` in a text editor | Don't. Just type up to `4.00` in the box. On 26.2 a scale above 4 is a parse error, so a hand-edited `5` means the command never runs at all. |
| Pick any particle from the dropdown | The dropdown is now filtered to the version you selected, so removed particles like `barrier` aren't offered. |

---

## 1. Convert the image

Load a `.png` or `.jpg` with the button at the top, then work down the settings.

**Coordinate mode** — `Relative Local` (`^`) for almost everything. It makes the image face
whichever way the entity running the function is facing, which is what lets you rotate it.
`Relative World` (`~`) locks it to one compass direction.

**Coordinate axis** — which plane the image is drawn on. `X-Y` stands it up like a picture,
`Z-X` lays it flat on the ground.

**Alignment** — where the image sits relative to the entity running it. `Bottom` puts the entity
at the base of the image, `Center` puts it in the middle. Set this to match where your marker
entity will be.

**Particle size** — `0.75` is a good default. Below about `0.5` particles can expire before the
function runs again, which makes the image look gappy. The maximum is `4.00`.

**Density** — particles per block. `8` means each block of the image is 8 particles across.
Higher density looks solid but multiplies the particle count fast.

**Resolution** (under More Settings) — the image is resampled to this before conversion. This is
the main dial for keeping the particle count sane. If you want 16 particles per block over 5
blocks, set the width to 80.

Watch the **Particles** counter in the top right. Minecraft renders roughly 1600 particles at
once, and the counter turns red at 2000.

## 2. Export

Pick your **Minecraft version** — this changes the command syntax, the datapack directory name and
the pack format together, so it has to match the world you're loading into.

Leave **Export as a datapack** ticked. Set a **namespace** (the part before the colon; all your
images can share one) and a **function name**, then point **ExportFolder** at your world's
`datapacks` directory:

```
.minecraft/saves/<world>/datapacks
```

You get:

```
datapacks/
  particles/
    pack.mcmeta
    data/particles/function/my_image.mcfunction
```

The line under the Export button tells you exactly what to run. Every image exported with the same
namespace lands in the same pack, so you can build up a library.

Untick the checkbox if you'd rather have a bare `.mcfunction` to drop into a pack you already have.

## 3. Run it in game

`/reload`, then:

```
/function particles:my_image
```

That draws the image once, at you, facing the way you're facing. To pin it in the world you need
something to run it *at*.

### Anchor it to an entity

Summon an invisible armour stand where you want the image, tagged so you can find it again:

```
/summon armor_stand ~ ~ ~ {Invisible:1b,Marker:1b,Tags:["img1"]}
```

Then run the function at it, every tick, from a repeating command block:

```
/execute at @e[tag=img1] run function particles:my_image
```

`Marker:1b` removes the hitbox so you can't bump into it. A `marker` entity is lighter still if
you want to experiment — it doesn't tick or render at all — though armour stands are the
well-trodden path.

### Make it move

Because the image was exported with local (`^`) coordinates, it follows the anchor's rotation.
Rotating the anchor rotates the image:

```
/execute as @e[tag=img1] at @s run tp @s ~ ~ ~ ~3 ~
```

Three degrees per tick. Run that from a repeating command block for a continuous spin, or gate it
behind a button — a wooden button stays pressed for 30 ticks, which is exactly 90 degrees. That's
the trick behind the smooth particle doors: two anchors, one turning `~3` and one turning `~-3`.

For a flat spinning ring, export with the coordinate axis set to `Z-X` and both alignments set to
`Center`, then spin the anchor the same way.

## Troubleshooting

**Nothing appears at all.** Check the pack loaded: `/datapack list` should show it. If it doesn't,
the directory name is almost always the cause — it must be `function`, not `functions`, for any
version from 1.21 onward. Re-export with the correct version selected.

**The function isn't in the `/function` autocomplete.** Same cause, or you forgot `/reload`.

**Some particles show, most don't.** You're over the render limit. Lower the resolution or the
density, not the size.

**The image looks gappy or flickery.** Raise the particle size toward `0.75`-`1.0`, and make sure
the function is running every tick rather than every few ticks.

**Colours look wrong on a `.jpg`.** Fixed in this fork. Older builds read JPEG pixels incorrectly.

**The image faces the wrong way or is upside down.** Coordinate axis and the `Flip` checkbox.
Use the 3D preview to get it right before exporting.
