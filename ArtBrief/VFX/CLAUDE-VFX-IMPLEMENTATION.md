# Dawnwager VFX integration handoff

Implement and finish the mapped card VFX atlases in the Unity project. Keep the established ink-slate, brass, frost, and ember palette; the effects should read clearly over the 1920×1080 screen-space battle board without obscuring card art, unit names, stats, targeting, or rules. Do not replace the game’s existing bespoke fallback animations.

## Runtime already wired

- `Assets/Resources/Data/VfxByCard.json` maps all 161 card IDs to a primary and secondary effect key (49 unique keys).
- `Assets/Scripts/Client/CardVfx.cs` plays each imported 8×8 atlas on the existing effects canvas. It uses top-left row order at 30 fps and falls back cleanly when an atlas is missing.
- Orders use the effect mapped to that exact Order. Units and Sites use their own card IDs when they enter play. Summoned Units use the summoned Unit ID, not the Order that created them. Prophecies retain their tailored sequence and receive their mapped overlay.
- `Assets/Scripts/Editor/VfxAtlasImporter.cs` applies sRGB, alpha, clamp, bilinear, no mipmaps, and high-quality compression to atlases placed under `Assets/Resources/Art/VFX/`.
- Additive sheets use `UI/Foundry VFX Additive`; alpha-blended smoke, dust, water, and frost use standard UI alpha blending.
- Options already shows the required Vinish attribution. Keep it visible while any of these sheets ship.

## Source asset import

Use the original Vinish.dev sheets listed below, under the publisher’s CC BY 4.0 terms. For each effect, open its category page, choose **View and download**, select the **2048 px** PNG sheet (8×8 cells, transparent, 30 fps), then save it under the exact `Assets/Resources/Art/VFX/<key>.png` path below. Do not trim, recolor, or flatten the alpha unless the change is recorded in the credits. Preset JSON files are optional; runtime uses the game manifest and fixed 8×8 / 30 fps timing.

| Effect | Unity file key | Publisher page |
|---|---|---|
| Arched Door Portal Loop | `arched_door_portal_loop.png` | https://vinish.dev/free-vfx/portals-and-vortex |
| Blizzard Overlay Loop | `blizzard_overlay_loop.png` | https://vinish.dev/free-vfx/rain-and-snow-overlays |
| Boat Wake Loop | `boat_wake_loop.png` | https://vinish.dev/free-vfx/water-splashes-and-ripples |
| Campfire Loop with Embers | `campfire_loop_with_embers.png` | https://vinish.dev/free-vfx/fire-and-flames |
| Candle Flame Loop | `candle_flame_loop.png` | https://vinish.dev/free-vfx/fire-and-flames |
| Claw Scratch | `claw_scratch.png` | https://vinish.dev/free-vfx/sword-slashes-and-trails |
| Cold Mist Loop | `cold_mist_loop.png` | https://vinish.dev/free-vfx/ice-and-frost |
| Curse Aura Loop | `curse_aura_loop.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Dash Speed Streaks | `dash_speed_streaks.png` | https://vinish.dev/free-vfx/sword-slashes-and-trails |
| Drifting Embers Loop | `drifting_embers_loop.png` | https://vinish.dev/free-vfx/sparks-and-embers |
| Falling Ash Overlay Loop | `falling_ash_overlay_loop.png` | https://vinish.dev/free-vfx/rain-and-snow-overlays |
| Feather Burst | `feather_burst.png` | https://vinish.dev/free-vfx/teleport-and-spawn |
| Fire Burst | `fire_burst.png` | https://vinish.dev/free-vfx/fire-and-flames |
| Frost Spreading | `frost_spreading.png` | https://vinish.dev/free-vfx/ice-and-frost |
| Frozen Screen Edges | `frozen_screen_edges.png` | https://vinish.dev/free-vfx/screen-overlays |
| Ground Fog Loop | `ground_fog_loop.png` | https://vinish.dev/free-vfx/smoke-and-dust |
| Ground Halo Pulse Loop | `ground_halo_pulse_loop.png` | https://vinish.dev/free-vfx/pickup-glows-and-shines |
| Ground Slam Shockwave | `ground_slam_shockwave.png` | https://vinish.dev/free-vfx/hit-flashes-and-impacts |
| Guard Block Flash | `guard_block_flash.png` | https://vinish.dev/free-vfx/hit-flashes-and-impacts |
| Healing Aura Loop | `healing_aura_loop.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Heavy Landing Crack | `heavy_landing_crack.png` | https://vinish.dev/free-vfx/footsteps-and-ground |
| Hit Flash and Impact Ring | `hit_flash_and_impact_ring.png` | https://vinish.dev/free-vfx/hit-flashes-and-impacts |
| Ice Shatter | `ice_shatter.png` | https://vinish.dev/free-vfx/ice-and-frost |
| Ice Spikes Eruption | `ice_spikes_eruption.png` | https://vinish.dev/free-vfx/ice-and-frost |
| Landing Dust Puffs | `landing_dust_puffs.png` | https://vinish.dev/free-vfx/smoke-and-dust |
| Level-Up Light Pillar | `level_up_light_pillar.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Light Arrow Loop | `light_arrow_loop.png` | https://vinish.dev/free-vfx/energy-orbs-and-projectiles |
| Lightning Strike | `lightning_strike.png` | https://vinish.dev/free-vfx/lightning-and-electricity |
| Magic Sparkle Burst | `magic_sparkle_burst.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Metal Hit Sparks | `metal_hit_sparks.png` | https://vinish.dev/free-vfx/sparks-and-embers |
| Rune Glyph Burst | `rune_glyph_burst.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Rune Ward Loop | `rune_ward_loop.png` | https://vinish.dev/free-vfx/shields-and-force-fields |
| Shadow Step | `shadow_step.png` | https://vinish.dev/free-vfx/dark-and-shadow |
| Shallow Water Step Splash | `shallow_water_step_splash.png` | https://vinish.dev/free-vfx/footsteps-and-ground |
| Shield Activate | `shield_activate.png` | https://vinish.dev/free-vfx/shields-and-force-fields |
| Snow Kick | `snow_kick.png` | https://vinish.dev/free-vfx/footsteps-and-ground |
| Steam Burst | `steam_burst.png` | https://vinish.dev/free-vfx/smoke-and-dust |
| Summoning Circle Spawn | `summoning_circle_spawn.png` | https://vinish.dev/free-vfx/teleport-and-spawn |
| Sword Slash Arc | `sword_slash_arc.png` | https://vinish.dev/free-vfx/sword-slashes-and-trails |
| Time Stop Clock Ring | `time_stop_clock_ring.png` | https://vinish.dev/free-vfx/magic-and-spells |
| Torch Flame Loop | `torch_flame_loop.png` | https://vinish.dev/free-vfx/fire-and-flames |
| Void Crack Burst | `void_crack_burst.png` | https://vinish.dev/free-vfx/dark-and-shadow |
| Wall of Fire Loop | `wall_of_fire_loop.png` | https://vinish.dev/free-vfx/fire-and-flames |
| Warp-In Ripple | `warp_in_ripple.png` | https://vinish.dev/free-vfx/teleport-and-spawn |
| Water Ripple Rings Loop | `water_ripple_rings_loop.png` | https://vinish.dev/free-vfx/water-splashes-and-ripples |
| Wave Crash | `wave_crash.png` | https://vinish.dev/free-vfx/water-splashes-and-ripples |
| Wind Wisps Loop | `wind_wisps_loop.png` | https://vinish.dev/free-vfx/nature-and-ambient |
| Wraith Smoke Loop | `wraith_smoke_loop.png` | https://vinish.dev/free-vfx/dark-and-shadow |
| XP Orbs Collect | `xp_orbs_collect.png` | https://vinish.dev/free-vfx/ui-and-reward-fx |

## Required Unity review before handoff

1. Open the project in the Unity version named by `ProjectSettings/ProjectVersion.txt`; allow imports to finish and resolve any compile errors introduced by the VFX files.
2. Confirm every one of the 49 atlas files exists and is imported. Inspect alpha and frame order for one each of fire, frost, water, smoke, rune, slash, shield, spawn, and lightning.
3. Play an Order, a Unit, a Site, a summoned Unit, and all six Prophecies. Confirm that the card-specific VFX follows the correct target/arrival, secondary effects layer at reduced size, no effect intercepts pointer input, and existing motion/sound timing remains sensible.
4. Test Reduce Motion on and off and verify missing sheets still use the existing procedural effects. Confirm normal and additive sheets both render in the actual screen-space overlay canvas.
5. Add no effect when its card failed validation or play. Keep each VFX lifetime bounded and destroy its temporary UI object after playback.
6. Report any effect that clips, covers rules/stat UI, or reads poorly at game scale. Fix its placement/size in the runtime map rather than editing the source atlas.

## Attribution

Visible in Options: `VFX: vinish.dev (https://vinish.dev/free-vfx), CC BY 4.0`

If any effect is modified, add a plain description of that change after its credit in the in-game attribution.
