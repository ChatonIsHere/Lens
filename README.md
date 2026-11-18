# Lens

A MelonLoader mod for playing A Township Tale on PC VR at a higher frame rate, by having the game draw less. Every
setting starts at the game's own, so installing Lens changes nothing you can see; you choose what to trade. It works
on clients only and does nothing on a server or a client without graphics.

## Settings

Under `[Lens]` in `UserData/MelonPreferences.cfg`. A change made while the game runs (in a mods menu, or by
reloading the file) applies straight away, so you can compare in the headset.

### Resolution and anti-aliasing

| Setting | What it does | To try |
| --- | --- | --- |
| `RenderScale` | The resolution each eye renders at, as a share of the headset's. The biggest saving there is, and the most visible. SteamVR's own resolution setting multiplies with it. | `0.8` |
| `Msaa` | Anti-aliasing samples. The game renders the world deferred, which can't use MSAA, so this may only change the hands, items and UI; check the frame timing. | `0` |
| `Anisotropic` | Keeps ground and walls seen at a shallow angle sharp: `Off`, `On` (where a texture asks) or `Forced`. Looks better, for almost nothing on current graphics cards. | `Forced` |
| `DesktopView` | What the game's desktop window shows: `OneEye` or `Off`. The headset is unaffected; streamers will want to keep it. | `OneEye` |

### Shadows

| Setting | What it does | To try |
| --- | --- | --- |
| `FireShadows` | Fires and torches cast shadows, and each shadowed one renders the scene six more times. `Hard` drops the soft edges, `Off` the shadows. Towns full of torches gain the most. | `Off` |
| `FireShadowDistance` | Fires farther away than this, in metres, cast no shadows. Their shadows are small by then; it's much cheaper than `FireShadows = Off` for most of the look. | `20` |
| `FireShadowResolution` | Fire and torch shadow resolution: `Low`, `Medium`, `High` or `VeryHigh`. | `Low` |
| `SmallShadowSize` | Props in the world smaller than this, in metres across, cast no shadows: grass tufts, pebbles and the like. | `0.5` |
| `ShadowDistance` | How far away shadows are drawn. The game moves it with the frame rate; Lens holds it. | `40` |
| `ShadowCascades` | Sun shadow cascades. Fewer is faster, with blurrier shadows near you. | `2` |
| `ShadowResolution` | Shadow map resolution: `Low`, `Medium`, `High` or `VeryHigh`. | `Medium` |
| `SunShadows` | The sun's and moon's shadows: `Hard` or `Off`. | `Hard` |
| `PixelLights` | How many lights are drawn fully at once (the game's 5); the rest are drawn the cheap way. | `3` |

### Distance

| Setting | What it does | To try |
| --- | --- | --- |
| `TreeDistance` | How far trees keep their detailed models, as a multiple. | `0.75` |
| `LodBias` | The same for grass and props. | `0.7` |
| `ViewDistance` | How far you can see, in metres. Nothing past it is drawn. | `600` |
| `CullDistance` | How far small things on the default layer are drawn, in metres (the game's 100). | `60` |

### Extras

| Setting | What it does |
| --- | --- |
| `OtherPlayersCameras` | `false` hides the viewfinder of a camera another player holds; each one shown renders the whole scene again. |
| `OverviewCamera` | `false` skips the scene's "Overview Camera", which FastTale found rendering in VR. The render report shows what it draws to. |
| `Bloom` | `false` turns off the glow around bright things. |
| `Grass` | `false` hides the small grass tufts. |
| `WaterReflections` | `false` stops water mirroring the scene, which renders it again for each such water. The render report shows whether any does. |
| `OffscreenSkinning` | `false` stops animating the skin of characters out of view where the game asks to keep it. Their skeletons still move. |
| `SkinWeights` | Bones that move each point of a character's skin: `1`, `2` or `4`. The Quest build uses `2`. |
| `TerrainInstancing` | `true` draws the terrain with GPU instancing, in fewer draw calls. Same look where its shader supports it; check the terrain still shows. |
| `UploadMs`, `UploadBufferMb` | Time a frame and buffer size for sending loaded textures and meshes to the graphics card. More means fewer hitches walking into new areas; try `4` and `64`. |
| `WarmShaders` | `true` prepares every shader variant the game ships while it loads, so materials don't stutter the first time they're seen. Loading takes longer, once. |

## On the tablet

With [TabletUILib](https://github.com/SmoothieBrain/TabletUILib) installed too, Lens is on the tablet's wheel. Each
row shows a setting; press it to step to the next value. A change applies at once and the tablet asks to keep it:
unless you press Keep within 15 seconds of the last change, everything goes back to how it was. Nothing is saved
until you keep it. Lens works the same without TabletUILib, with the settings in the file only.

## What costs the most

With `RenderReport` on (the default), Lens logs what the game is rendering once each scene has settled: every
camera and what it draws to, the lights and which cast shadows, post-processing effects, reflection probes,
terrain, water reflections and the quality settings. Much of that is set in the game's scenes, not its code, so
this is the place to see it.

To measure a change, use SteamVR's frame timing graph (in SteamVR's settings, under Developer): it shows the GPU's
and CPU's time for each frame. Change one setting at a time.

## Install

Put `Lens.dll` from the latest release in the game's `Mods` folder.

## Building

`dotnet build -c Release`, with `GameDir` in `Local.props` set to a client install with MelonLoader and TabletUILib's `TabletUILib-PC.dll` saved as `lib/TabletUILib.dll`. CI builds
against [att-stubs](https://github.com/ChatonIsHere/att-stubs) and releases each push to main as YY.M.N.

## Credits

Lens started as a fork of [FastTale](https://github.com/CircuitLord/FastTale) by CircuitLord: the Overview Camera,
MSAA, bloom and grass settings are from it. MIT licensed; see `LICENSE`.

Lens is an unofficial fan-made mod, not made or endorsed by Alta. It contains no game files.
