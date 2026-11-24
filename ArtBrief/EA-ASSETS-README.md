# Dawnwager Early Access Asset Pack

## Art remaster

The menu icons, rank crests, selected HUD icons, and achievements use Ravenmore’s hand-painted Fantasy Icon Pack. End Turn, hero framing, prophecy plaques, medal hardware, stars, and Match Found received a material pass. The reusable panel borders use adapted Kenney geometry. See `ATTRIBUTION.md` for credits.

The archive contains 117 visual Resources assets, 11 WAV cues, six achievement icons, refreshed Steam Library Hero art, and a 36-second cinematic art trailer. Copy the `Assets/Resources/` contents into the project using the included directory structure. The assets are ready for UI hookup.

| Deliverable | Import or usage |
|---|---|
| `TitleKey.jpg` | 3840×2160 delivery upscale. `TitleKey-1080.jpg` is the 1920×1080 alternative. |
| Mode tiles | 1280×720 idle and hover pairs. The hover art uses the same cutout and layout, with a brighter rim and about 6.5% brighter interiors. |
| Panel and button edges | Default border inset 17 px. Wide banner inset 24 px left/right and 16 px top/bottom. Render End Turn at its native 3.2:1 aspect ratio to preserve the lever and lantern silhouettes. Suggested label box at source size is x=108–436, y=53–134. Its four button states share the same silhouette. EndTurnGlow is a separate overlay. |
| Prophecy plaques and hero frame | Composite over card art and portrait art. The center is transparent. |
| Rank | Ember, Rime, Brass, Iron and Mythic ranks 10 through 1, plus Legend. Numbers are the only type on medal art. |
| VFX and float font | Star Burst is a 4×4 atlas. Slash is a 2×2 atlas. Read atlases left to right, top to bottom. FloatFont is 6 columns by 2 rows with glyphs `0123456789+−` in row-major order. |
| Queue and targeting | Rotate the static QueueRing at runtime. DropRing is a static overlay. |
| Audio | 44.1 kHz stereo WAV. All 11 cues meet the brief’s duration ranges and stay at or below −1.5 dBTP. The 28-second ambience measures −18 LUFS integrated and loops at the file seam. |
| Steam | Six 256×256 achievement icons are named by achievement ID. The trailer is 1920×1080 at 30 fps with an original score. Capture gameplay screenshots from a running client. |

Title and mode illustrations were generated and composited. The soundtrack was produced for this drop. The installed wordmark, bitten-sun emblem, and hero art were used as the visual references and for the cinematic art trailer.
