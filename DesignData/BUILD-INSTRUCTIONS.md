# Dawnwager build instructions

Open the existing Dawnwager Unity project in **Unity 6000.0.61f1**. This download contains art, audio, editable third-party source assets, and campaign design data. The campaign progression and new visual and audio hooks below are implementation instructions for the existing project.

## 1. Import the assets

1. Back up or commit the Unity project. Merge this archive's top-level `Assets/Resources` into the existing project and keep the existing project scripts, scenes, and Unity metadata. Preserve the project’s installed `Art/Cards` folder until Step 2. Its legacy contents are included under `Reference/Legacy-Card-Composites` for recovery.
2. Copy this archive's `Credits` into the project root. Keep `Third-Party`, `Reference`, and `Steam` outside Unity's `Assets` folder. Amompharetus’s CC0 sources are included. [Cinder UI](https://ashbloomgames.itch.io/cinder-ui) is an optional direct download for the game project under the author’s own terms. To use it, replace the equivalent control states listed below, preserve its author-provided slice margins when using native exports, and retain its license. It is not needed to build from this ZIP.
3. The `BOM-Upgrade/Assets/Resources` folder mirrors the import paths for the revised renderer. Apply its `Art` folder after Step 2 and its fonts after renaming or removing the existing `Display.otf` and `Body.otf` from Resources. Duplicate extensions with the same resource name conflict.
4. For the current `Resources.Load<Texture2D>` and `RawImage` paths, import UI PNGs as **Default**, sRGB on, Alpha Is Transparency on, mipmaps off, Clamp, Bilinear, Max Size 2048, and uncompressed when edge fidelity matters. Keep this basis for full-screen overlays. For assets drawn with sliced `Image`, use Sprite (2D and UI), Full Rect, and the authored border values in `BOM-Upgrade/Sprite-Borders.json`. That file records left, top, right, bottom. Unity's `Sprite.border` Vector4 order is left, bottom, right, top. A `RawImage` stretched across a panel cannot render a 9-slice border.
5. Import one-shot WAVs with Decompress on Load, PCM, Preserve Sample Rate, Preload Audio Data, and Force To Mono off if using the stereo layer. Keep music and ambience on separate mixer groups with streaming or compressed-in-memory imports as appropriate.

| Cinder optional source family | Dawnwager targets |
| --- | --- |
| Ember and iron buttons with matched states | `Foundry/Primary` and `Foundry/Secondary` family |
| Framed panels and dialogs | `Foundry/Panel`, `PanelGold`, `ModalFrame` |
| Pips, targeting glyphs, small utility icons | `Foundry/ChargePipOn`, `ChargePipOff`, `TargetArrow`, `TargetNode` |

## 2. Install the card renderer upgrade

Start in `Assets/Scripts/Client/BattleTable.cs`, method `PaperCard`. Use this same rendering path for hand, inspect, collection, opening mulligan and drag previews. Preserve current hit regions and animation anchors.

- Render on a **416 × 590** logical canvas. Multiply all X coordinates and widths by visible width / 416, and all Y coordinates and heights by visible height / 590.
- Draw shadow and playable glow first. Add a dark art backing, then mask the portrait to the art well. Use aspect-fit for alpha cutouts in `Art/Units/{id}` and aspect-cover for `Art/Orders/{id}` and `Art/Places/{id}`. Prophecy art also lives in Places. The installed code has an `X11 → X02` art alias. Keep it where the existing catalog uses it.
- Draw the appropriate transparent `Foundry/Card-{Faction}-{Kind}` frame over that portrait. Append `-Mythic` only for legendary Units. Draw Nameplate, RulesParchment, gems, and text after the frame. The title plate and cost gem overlap the well by design. No paint sits inside the transparent well in the frame PNG.
- Render card names, type, current cost, current Attack and Health, and rules as live text. Use `RulesText.Describe` and existing query methods, and omit cost for Prophecies and Attack/Health for non-Units. Use dark ink on the parchment. Keep the compact hand face concise and show the full rules on deliberate inspect or hover. Test expanded localization strings and three-digit stat stress cases, the longest English rules, and live board stats. Keep legacy composites outside runtime lookup after migration.
- `HeroPower` states are the button backing, and the hero’s existing ability name and description remain live text. Put unit cutouts behind `UnitToken`, and render its glow and the drag ring only during valid interactions. Use the same controls and keyboard focus cues across campaign and battle.
- Keep `CardBack` and the existing foil and Impact art. Crop foils to the intended card region. Disable motion on request and preserve a static highlight.

| Layer | Top-left x, y, width, height |
| --- | --- |
| Art well | 34, 56, 348, 296 |
| Nameplate | 40, 330, 336, 44 |
| Type line | 50, 374, 316, 28 |
| Rules parchment | 42, 410, 332, 120 |
| Cost | 8, 8, 72, 72 |
| Attack | 8, 510, 72, 72 |
| Health | 336, 510, 72, 72 |

The explicit BOM names define **29 frames**, including Neutral Mythic. The exports follow those names. Portrait normalization reuses the existing artwork and resamples it. The higher-dimensional exports have the same underlying detail as their sources.

## 3. Build the campaign RPG layer

Keep the existing seven Ragnarök chapters, card stack, bosses, unlocks and two-lane rules. Add a campaign map state that links to an encounter, a skill-tree screen, and a post-battle reward screen. `Campaign-Encounters.csv` indexes the seven boss beats and three optional locations. Main encounters remain playable without taking a detour. Use plot copy and existing chapter decks for the main route. The optional encounters are new content and need their own encounter definitions.

**Player loop:** Choose a route → fight a card encounter → claim earned cards and, when offered, pick one relic → spend skill points → adjust deck → unlock the next route segment.

1. **Core progression.** Add a pure C# versioned campaign save record with completed encounter IDs, chosen relics, equipped relic IDs, and purchased node IDs keyed by hero. Award one point for each first-time victory in the seven main chapters and three optional encounters. The first point should open a build decision. Import older saves by deriving completed main encounters from existing `profile.progress`. Do not replay old reward payouts as a side effect of loading.
2. **Skill tree.** Import `Campaign-Passives.csv` as data. Use stable IDs and prereqs in the rules layer. Three six-node branches cover defensive Bastion play, aggressive Warcraft and tempo Runecraft. Spend is shared across branches for the current hero. Each unlocked hero can allocate the earned point budget independently. Refunds restore the build for that hero between encounters. Only unlocked heroes can spend, and saves must preserve each hero’s allocation. Recompute unspent points from earned points minus validated purchase costs.
3. **Screen wiring.** Add a Skill Tree button to `CampaignMap.cs` and a page in `Game.cs`. Use `Art/Campaign/NodeLocked`, `NodeAvailable`, `NodeNormal`, and `NodePurchased`, with `LinkLit` and `LinkDormant`. Position using the CSV grid coordinates, for example x = 400 + column × 140 and y = 265 + row × 115 on a 1920 × 1080 display. Place links behind nodes. Color, label, and outline all states. Selecting any node reveals its effect, point cost, prerequisites, and the reason for a locked state. Buying and refunding are separate actions, with keyboard/gamepad navigation.
4. **Combat integration.** In `BattleScreen.StartMatch`, resolve a read-only snapshot of the equipped hero’s perks, relics, and encounter modifiers only for campaign. Pass the snapshot into a neutral-by-default `Match` constructor overload. Add explicit max hero health to `Pilot` before using health bonuses and update the current 24-HP heal cap in `Core/Match.cs`. Wire effects into successful plays, paid powers, Seal award, and round resolution. Use per-match and per-round guard flags. Use the same charged cost in move legality, previews, AI evaluation, and execution. Refunds, loadout edits, and previews must never consume match effects. Casual and PvP should pass the empty rules object.
5. **Reward integration.** The current `ShowResult` increments sequential chapter progress and unlocks cards. Add idempotent first-clear tracking. Save encounter completion and a pending reward choice in one write. Resolve relic choice and clear pending state in one save so restarting cannot duplicate rewards or lose them. Replay victories grant no progression points or duplicate relics. Show what was earned on the result panel. Each relic milestone in the CSV suggests a featured relic. Offer it if unowned alongside up to two other unowned relics and skip the choice once all four are owned. Start with these four test relics: Ferry Bell (heal 2 on your first march), Frost Nail (your first Site gives its lane +1 Seal Power this round), Charred Coin (gain 1 Charge after your first paid hero power), and Last Lantern (the first time you fall to 6 health or below and survive, heal 2). Limit to two equipped relics and no duplicates. Snapshot them when the fight starts.
6. **Balance targets.** Test the starter with all three branches and an unspent build. Verify no mandatory node, no infinite discount loops, no repeated cooldown abuse, and no progression path bottleneck caused by an optional encounter. Existing base decks and damage numbers are the baseline. Tune new encounters against the weakest viable campaign build, then test the strongest allocation against the same encounter.

## 4. Wire the audio to actions

- Existing cue paths continue to resolve. `SFX-CUE-MAP.csv` lists the new files, what should trigger them, and the original recordings used.
- In opening/round draw animation, play draw or deal when the card leaves the deck. On successful play, choose by card kind and keep a separate card performance layer. On attacks, start the movement cue on lunge and play the strike sound on contact. Shield break plays only when a shield is consumed. Empty-seat clicks and rejected plays use deny. Do not play confirm on canceled drags.
- The 39 new cues use 48 kHz stereo 16-bit PCM. Use the audition reel in `Preview` to compare paper, wood, steel, shield crack and reward cues before testing them in context.
- `MixLog` currently selects one sound for a batch. Play important combat events in their animation callbacks rather than triggering twice from `MixLog`. Keep one voice per action, delay it slightly after the physical card cue, and duck music gently while speaking. Limit repeated draw/UI sounds to 3 in 150 ms and avoid repeating a variation back-to-back. Start mixer tuning around Music −16 dB, Foley −6 dB, UI −12 dB and card voices −4 dB. Adjust from the actual build and listen over headphones and speakers.
- Preserve the four existing music tracks and the 160 existing card performance cues. Individual performances still need an audition if the release requires replacement voice acting for specific cards.

## 5. Build and verify

1. In Unity Hub install the editor version above and Windows Build Support. Open the project's `Assets/Scenes/TheLastDawn.unity` through **Dawnwager → Open Game Scene**, then enter Play Mode. Clear compile errors before export. The extracted project already uses a Windows registry migration implementation based on native calls. An older project reporting a missing `Registry` identifier needs that source updated before building.
2. Use the project’s **Dawnwager → Build Windows Player** menu. Run `Builds/Dawnwager-Windows/Dawnwager.exe` with its accompanying files. Keep the entire output directory together when zipping or staging a depot.
3. Run the project's existing `dotnet run --project QA/Wolfsun.QA.csproj`. Add tests for old saves, first-clear replay, pending rewards across restart, hero-specific allocation, prerequisite rejection, reset, and campaign-hook isolation from casual.
4. On Windows validate 1080p and an ultrawide viewport. Start a fresh save, complete a fight, claim a relic, spend a point, restart and confirm it persists. Replay the same fight and check the reward cannot be duplicated. Check playable card borders, card draw count, mulligan, Order art, targeting and typography. Verify one sound per contact and check loudness across a complete fight. Export a Windows build and smoke-test it outside the editor.
5. For Steam, upload the entire validated Windows build to your configured SteamPipe depot and test it on a private branch. The supplied capsule art and trailer are media assets. Steam app, depot and release configuration are separate.
