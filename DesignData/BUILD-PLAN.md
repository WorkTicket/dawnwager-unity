# Dawnwager — fresh start status

**Client UI has been cleared.** Play Mode shows a blank shell only.

## What remains (rules wired)

| Path | Role |
| --- | --- |
| `Assets/Scripts/Core/Match.cs` | Match state, rounds, seals |
| `Assets/Scripts/Core/Actions.cs` | Legal moves, AI, `Apply` |
| `Assets/Scripts/Core/Abilities.cs` | Card abilities |
| `Assets/Scripts/Core/Catalog.Generated.cs` | Card definitions |
| `Assets/Scripts/Core/RulesText.cs` | Rules copy |
| `Assets/Scripts/Core/Campaign.cs` | Chapters, decks, unlocks |
| `Assets/Scripts/Core/CampaignMeta.cs` | Encounters, passives, relics |
| `Assets/Scripts/Core/CampaignHooks.cs` | Campaign combat hooks |
| `Assets/Scripts/Core/Ladder.cs` | Ranked ladder |
| `Assets/Scripts/Core/ViewportLayout.cs` | Viewport math |
| `Assets/Scripts/Net/*` | Matchmaker / duel wrappers |
| `Assets/Resources/Art|Audio|Data` | Assets on disk (unused by UI) |
| `DesignData/*` | Manifest, borders, CSVs, instructions |

## What was gutted

All client screens (Home, Battle, Collection, Campaign map, Play hub, Skills, Outcome, motion, drag, feel) are empty stubs. `Game.Render()` draws a blank ink screen stating core-only.

## Next rebuild

Build UI from scratch when ready. Use `DesignData/BUILD-INSTRUCTIONS.md` and `Asset-Manifest.csv` for assets. Do not restore the old overlapping chrome.
