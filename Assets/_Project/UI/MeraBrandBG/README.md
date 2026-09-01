# Mera Brand Pakistan — animated background loop (Unity)

Animates the existing `MeraBrandPakistan.png` in place. Nothing is baked to video, no
layers need slicing in Photoshop, and the artwork stays a single texture.

The effects find their own targets by colour-keying the image: the light ribbons and the
network map light up because they are green-over-blue against the navy field, while lit
stone and snow are rejected by a red-channel test. If the artwork is ever re-exported with
the same palette, the animation keeps working.

## Contents

```
MeraBrandBG/
  Shaders/MBPBackgroundCore.cginc      the animation maths, shared
  Shaders/MBPBackgroundLoop.shader     world / SpriteRenderer / quad
  Shaders/MBPBackgroundLoopUI.shader   Canvas UI (Image, RawImage)
  Scripts/MBPBackgroundLoop.cs         drives the loop clock and _UVRect
  README.md
```

## Which shader

| Rendering the background as | Shader |
|---|---|
| UI `RawImage` or `Image` on a Canvas | **BackgroundLoopUI** |
| `SpriteRenderer` | BackgroundLoop |
| Quad / mesh in front of the camera | BackgroundLoop |

The UI variant is a real UI shader: stencil block, `RectMask2D` clipping, `CanvasGroup`
alpha and `Graphic` colour tint all behave. The world variant is opaque and skips that
plumbing. Both compile under Built-in RP and URP — neither carries a `RenderPipeline` tag,
which would otherwise make one pipeline silently skip the SubShader and fall back to a
static image.

## Setup

1. Drop the `MeraBrandBG` folder anywhere under `Assets/`.
2. Import `MeraBrandPakistan.png`. Set **Wrap Mode → Clamp** and **Filter Mode → Bilinear**.
   Leave **Generate Mip Maps** on if the background is ever scaled down.
3. Create a material, assign shader **MeraBrandPakistan/BackgroundLoop**, and set its
   texture to the background PNG.
4. Put the material on whichever object renders the background.
5. Add the **MBPBackgroundLoop** component to that same object. Done — it animates in play
   mode, and in the editor too if `animateInEditMode` is on.

## Sprites and atlases

The effect rects are authored in 0..1 across the artwork. A sprite packed into an atlas has
UVs in *atlas* space, so without a remap the flag ripple would land on whatever else shares
the atlas. `MBPBackgroundLoop.cs` handles this: it reads `Sprite.textureRect` (or
`RawImage.uvRect`) and pushes `_UVRect`, and the shader maps back to 0..1 before doing
anything.

Two constraints remain:

- **`Image` must be Simple + Full Rect.** Sliced, Tiled and Filled rewrite the UVs per
  quad; Tight mesh type crops them. The component logs a warning if it sees one.
- **No rotation in the atlas.** Turn rotation off for this sprite, or exclude it from
  packing. Also warned at runtime.

`RawImage` avoids all of this — it takes a `Texture` directly with a plain 0..1 `uvRect`.
For a full-screen background that is the simpler choice.

## Component settings

| Field | What it does |
|---|---|
| `loopDuration` | Seconds per cycle. 18 s is calm; 10 s is noticeably livelier. Any value loops cleanly. |
| `startOffset` | Phase offset 0–1. Set different values on multiple screens so they don't move in lockstep. |
| `useUnscaledTime` | Keeps the background alive while `Time.timeScale = 0`. Leave on for menus and pause screens. |
| `animateInEditMode` | Editor preview without pressing play. |

## What moves

| Effect | Where | Read |
|---|---|---|
| Breath zoom + pan | whole frame | slow push in and back out, ~1.6% |
| Flag ripple | top-left rect | UV wave, strongest at the fly, still at the pole |
| Ribbon flow | bottom band | light travelling along the existing green streaks |
| Node pulse | map rect | dotted map breathes |
| Data sweep | map rect | one diagonal pass across the network per cycle |
| Logo shimmer | logo rect | single highlight pass across the star and wordmark |
| Sky twinkle | upper dark area | sparse stars fading in and out |
| Horizon haze | mid band | soft green drift behind the skyline |
| Vignette pulse | edges | barely-there breathing, ties the rest together |

Every rect is a material property in normalised UV (`x0, y0, x1, y1`, **y = 0 at the
bottom**). If the artwork gets recomposed, drag these in the inspector rather than editing
the shader.

## Keeping the loop seamless if you retune

The loop is exact because every term is either `sin`/`cos` of `TAU * t * N` with **N a whole
number**, or a sweep that begins and ends fully off-screen. Two rules:

- Keep `_FlagCycles`, `_RibbonCycles` and `_MapCycles` at integers. `1.5` will visibly jump
  at the wrap.
- Don't add anything driven by `_Time`. Use `_LoopT` only.

Verified: the frame at `t = 1` is byte-identical to the frame at `t = 0`.

## Performance

One texture fetch and some arithmetic per pixel, no extra draw calls, no render textures.
It is fill-rate bound, which matters only on mobile. If you are targeting a low-end Android
kiosk tablet and see a dip, set `_Twinkle` to 0 first (it is the only branchy part), then
drop `_Haze`. On desktop or a PC-driven expo screen this is free.

## Notes

- Unlit, ignores lighting and fog, so it looks identical in Built-in RP and URP. Neither
  variant is SRP-batcher compatible; with one background quad that is irrelevant.
- For renderers the loop is driven through a `MaterialPropertyBlock`, so several
  backgrounds can share one material at different phases with no extra instances.
- For UI there is no `MaterialPropertyBlock` path — `CanvasRenderer` ignores them. The
  component instances the material in `OnEnable` and destroys it in `OnDisable` instead, so
  the shared material asset is never written to.
- If you would rather ship a prerendered clip than a shader, say so and I can export a
  frame sequence or a ProRes/H.264 loop at your target resolution instead.
