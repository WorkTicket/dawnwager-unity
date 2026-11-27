# VFX atlas import

Place the 49 8×8 transparent PNG atlases named in `ArtBrief/VFX/CLAUDE-VFX-IMPLEMENTATION.md` in this folder. The runtime uses these assets through `Resources.Load<Texture2D>("Art/VFX/<key>")` and `VfxByCard.json`.

Choose the publisher's 2048 px sheet for the 256 px animation cells. Keep the complete sheet intact: 8 columns × 8 rows, frames ordered left-to-right and top-to-bottom at 30 fps. Unity import settings are applied by `VfxAtlasImporter.cs`.

Source and card-by-card effect mapping: `ArtBrief/VFX/Dawnwager-Free-VFX-by-Card.csv`.
