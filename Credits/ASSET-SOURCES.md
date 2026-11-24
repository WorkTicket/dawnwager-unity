# Third-party UI sources

| Shipped sprites | Source | License | Adaptation |
|---|---|---|---|
| `ui/hud/iron-border.png`, `frost-border.png`, `gold-border.png` | [Kenney · Fantasy UI Borders](https://kenney.nl/assets/fantasy-ui-borders), Double Border 028, 010 and 014 | CC0 1.0 | Recolored; original alpha and 96 × 96 dimensions retained |
| `ui/hud/divider.png`, `divider-ornate.png` | [Kenney · Fantasy UI Borders](https://kenney.nl/assets/fantasy-ui-borders), Double Divider 001 and 005 | CC0 1.0 | Recolored warm bronze |
| `ui/hud/button-normal.png`, `button-hover.png`, `button-pressed.png`, `button-disabled.png` | [pzUH · Free Fantasy Game GUI](https://opengameart.org/content/free-fantasy-game-gui), four blank wide buttons | CC0 1.0 | Extracted at native size and recolored to WOLFSUN’s palette |

The other HUD sprites were created for WOLFSUN. The source ZIPs contain additional art and sample graphics. The shipped files above are the particular sprites used or supplied for integration.

## Frozen Crossing tabletop

`Assets/Resources/Art/FrozenCrossing.png` is a color-graded composite of photographic materials with a hand-laid inset, trim and fasteners. Source textures are CC0.

- [Slate Floor](https://polyhaven.com/a/slate_floor), Dimitrios Savva.
- [Dark Wood](https://polyhaven.com/a/dark_wood), photography by Dimitrios Savva, baking by Dario Barresi, tiling by Rico Cilliers.

## Emberbridge update

Dawn Pass, Emberbridge, and the five Foundry hero portraits are generated illustrations selected and integrated for this project. Faction card frames, controls, medallions, the card back, foil textures and impact sprites were constructed for WOLFSUN. Existing unit cutouts, site art, audio and the wordmark remain in use.

## Dawnwager identity

The Dawnwager emblem is a refined silhouette from a generated concept. The wordmark is set with the project's licensed display face and custom optical spacing. The Windows icon, card back, and storefront artwork use the same bitten-sun symbol. The new button artwork was built as reusable nine-slice textures.

## Interface polish audio (Hearthstone-feel pass)

| Shipped cue | Source | License | Adaptation |
|---|---|---|---|
| `Audio/ui-click.wav` | [Kenney · UI Audio](https://kenney.nl/assets/ui-audio) / [OpenGameArt mirror](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks), `click1.wav` | CC0 1.0 | Renamed for Resources load path |
| `Audio/ui-hover.wav` | Kenney UI Audio, `rollover2.wav` | CC0 1.0 | Renamed |
| `Audio/enemy-turn.wav` | Kenney UI Audio, `switch26.wav` | CC0 1.0 | Renamed as enemy-action stinger |
| `Audio/ui-deny.wav`, `Audio/defeat.wav` | [HaelDB · Card Game sounds](https://opengameart.org/content/card-game-sounds), `error.wav` | CC0 1.0 | Copied for deny / defeat feedback |
| `Audio/your-turn.wav` | HaelDB Card Game sounds, `stagechangeoldnotification.wav` | CC0 1.0 | Renamed as your-action banner cue |
| `Audio/round-start.wav` | HaelDB Card Game sounds, `cuckoo.wav` (game start) | CC0 1.0 | Renamed as round banner cue |

Existing battle cues (`pass-turn`, `prophecy-reveal`, `card-deal`, `card-select`, `unit-hit`, `shield-break`, `beacon-claim`, `victory`) remain the project mixes; this pass wires the previously unused `pass-turn` and `prophecy-reveal` files into match resolution.

## Phase banner chrome

| Shipped sprite | Source | License | Adaptation |
|---|---|---|---|
| `Art/Foundry/BannerFrame.png` | Derived from Foundry `Contested.png` plate (project art) with Brass / Frost / Slate recolor | Project | 512×128 plaque for Your/Enemy/Round banners; frost inner rule and brass outer edge |

SFXMint Card Game downloads were planned but require an API key for automated fetch; Kenney + HaelDB CC0 packs cover the same roles.

## EA remaster pack (Dawnwager-EA-Assets)

See `Credits/EA-ASSETS-ATTRIBUTION.md` for the file-by-file Ravenmore / Kenney map.

### Ravenmore · Fantasy Icon Pack 2.0

- Artist: [Ravenmore](https://ravenmore.itch.io/).
- Source: [Fantasy Icon Pack by Ravenmore, OpenGameArt](https://opengameart.org/content/fantasy-icon-pack-by-ravenmore-20).
- License: [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).
- Used (adapted) for menu icons, rank crest centers, selected HUD gems, and Steam achievement icons. Preserve attribution in-game and on redistribution.

### Kenney · Fantasy UI Borders (EA chrome)

- License: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- Adapted into Panel, PanelGold, ModalFrame, Tooltip, SeasonBadge, NavBar, BannerFrame, LadderPlate, SearchField, MatchFound.

### Project-authored EA chrome

End Turn lantern states, prophecy plaque frames, forged-metal rank medal frames, Outcome plates, mode tiles, TitleKey, SealLit/SealDim, and cosmetic card backs were built for this remaster and wired to existing `Resources/Art/Foundry` paths.
