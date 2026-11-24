# Dawnwager — EA Art & Audio Brief  
**Goal:** Hearthstone-level menu / UI / play feel for Steam Early Access  
**Product:** Norse prophecy-first tactical CCG (two lanes, Seals, public omens)  
**Drop path:** `Assets/Resources/Art/...` and `Assets/Resources/Audio/...`  
**Engine:** Unity 6 · runtime UI is code-built; prefer **PNG with alpha**, **9-slice friendly** edges, **no baked text** on chrome (except logo/wordmark)

---

## 1. Design direction (north star)

### Fantasy
You are standing on a **frozen bridge at the end of the world**. The sun has been bitten. Embers still cling to the wood. Gods, ferries, and dead names fight over the last light. UI should feel like **forged metal, frost, and parchment** laid over that bridge—not a generic RPG inventory, not cartoon candy, not purple sci‑fi glow.

### Competitor bar (Hearthstone / LoR)
- **One hero image owns the first viewport**
- **Mode choice is pictorial**, not a stack of plain buttons
- **Rank climb is trophy-clear** (medals + stars)
- **End turn is unmistakable**
- Every important action has a **short, readable animation + sound**

### Dawnwager differentiators (must stay visible in art)
- **Two crossings** (West / East lanes)
- **Seals** (bitten-sun / beacon discs)
- **Prophecies** (omen plaques that feel dangerous and public)
- **Charge** (ember/crystal energy—not generic mana droplets if avoidable)

---

## 2. Visual system

### Palette (lock these)
| Role | Hex | Use |
|---|---|---|
| Ink / void | `#0E1723` | Deep panels, shade |
| Slate panel | `#111822`–`#15202B` | Cards of UI, rails |
| Brass / gold | `#E8BC70` / `#F2C37F` | Primary accents, stars, CTAs |
| Frost | `#ADDDFO` ≈ `#ADDDF0` | Secondary info, ally |
| Ally sage | `#B4D3C1` | Your turn, positive |
| Enemy copper | `#E19781` | Enemy turn, damage |
| Parchment | `#E6DDC7` | Rules text on cards only |
| Quiet muted | `#B3BCB4` | Disabled / secondary copy |
| Edge bronze | `#75604E` | Hairline borders |

**Avoid:** purple neon, pure white UI, thick black outlines comic style, flat Material Design, pixel art.

### Materials
- **Brass** = interactive / valuable  
- **Frost glass / ice rim** = information / ally  
- **Charred wood + iron** = frames and panels  
- **Ember dust** = VFX only (sparks, not full-screen fire)

### Lighting
Low-key. Key light cool from upper-left (dawn), rim warm embers from below. UI plates slightly lighter than the scene so controls read on dark bridges.

### Shape language
- Soft **rounded rectangles** with **beveled metal** (match existing Primary button corners ~16–20px on 9-slice)
- Rank medals: **circular discs** with rune ring
- Prophecy: **taller plaque / tablet**, not a unit card
- End Turn: **taller, chunkier** than Secondary buttons

### Typography (asset + ship check)
- Display: existing project display face (wordmark already set)—**do not redraw wordmark** unless replacing Identity assets as a set
- UI labels are runtime Text—**do not bake English into chrome** except logos and rank Roman/numerals if part of medal design
- Provide a **numeral atlas** optional for ATK/HP floaters (see §6)

### Technical delivery
- PNG-24 + alpha (or PNG-32)
- sRGB
- No compression artifacts on edges
- 9-slice: keep a **flat center** (no busy pattern in middle 40%)
- Name files **exactly** as listed (Unity `Resources.Load` paths)
- Prefer power-of-two where easy; not required for UI
- Trim transparent padding to ≤4px unless 9-slice margins need more

### 9-slice margins (default)
Buttons/panels: **17px** all sides (matches current Primary sprites).  
Wide banners: **24px** left/right, **16px** top/bottom.

---

## 3. Do / Don’t

**Do**
- Match existing Foundry hero portraits’ painterly realism
- Keep the **bitten-sun Seal** silhouette consistent with `Identity/Emblem` and `Foundry/Seal`
- Design for **1920×1080** safe area with 10% margin
- Provide **Idle + Hover** (and Pressed when interactive)

**Don’t**
- Replace faction card frames unless doing a full set refresh
- Use skull spam / gore for menus (mythic gravity, not horror wall)
- Add tiny illegible runes as the only contrast
- Ship AI mush: edges must be intentional; regenerate until metal reads as metal

---

## 4. Asset list — Menus & navigation

### 4.1 Title key art — `Art/Foundry/TitleKey.jpg` (or `.png`)
| | |
|---|---|
| Size | **3840×2160** (also deliver 1920×1080 crop) |
| Content | Full-bleed bridge / dawn / wolf-bitten sun. Heroes optional as silhouettes—not a character select collage |
| Safe zone | Left **35%** quieter for logo + CTAs; right richer |
| Mood | Cold air, warm ember cracks in wood, distant fire on horizon |
| Notes | Must still read if darkened 40% by UI gradient |

### 4.2 Play mode tiles — `Art/Foundry/Mode-*.png`
Deliver **Idle + Hover** for each:

| File stem | Subject |
|---|---|
| `Mode-Campaign` | Frozen road / map seal path |
| `Mode-Casual` | Two players at a lit table, softer light |
| `Mode-Ranked` | Brass ladder / medals / contested beacon |
| `Mode-Collection` | Fan of cards + bitten-sun back |

| | |
|---|---|
| Size | **1280×720** each (engine can scale); keep 16:9 |
| Hover | Brighter brass rim + +5–8% exposure, no layout shift |
| Text | **None** (labels are code) |

### 4.3 Mode icons — `Art/Foundry/Icon-*.png`
`Icon-Campaign`, `Icon-Casual`, `Icon-Ranked`, `Icon-Collection`, `Icon-Career`, `Icon-Options`  
Size **256×256**, centered glyph, transparent, readable at 48px.

### 4.4 Panel / modal 9-slices
| File | Size | Use |
|---|---|---|
| `Art/Foundry/Panel.png` | 256×256 | Generic dark panel |
| `Art/Foundry/PanelGold.png` | 256×256 | Highlighted / primary modal |
| `Art/Foundry/ModalFrame.png` | 512×512 | Options, inspect, dailies |
| `Art/Foundry/BannerFrame.png` | **Already exists — refine** to 640×144 ornate | Phase banners |

Refine BannerFrame: thicker brass outer, frost inner rule, optional tiny Seal corners (current file is a start).

### 4.5 Navigation chrome
| File | Size | Notes |
|---|---|---|
| `Art/Foundry/NavBar.png` | 1920×128 (9-slice horizontal) | Top bar behind emblem |
| `Art/Foundry/DividerOrnate.png` | 512×16 | Section rules |
| `Art/Foundry/Tooltip.png` | 256×128 9-slice | Keyword tips |

---

## 5. Asset list — Beacon Ladder (Ranked)

### 5.1 Rank medals — `Art/Foundry/Rank-{League}{N}.png`
Leagues: **Ember, Rime, Brass, Iron, Mythic** × ranks **10→1**  
Plus **`Rank-Legend.png`**

| | |
|---|---|
| Count | 50 + 1 Legend = **51** |
| Size | **256×256** |
| Design | Metal disc + rune ring + central motif per league |
| Ember | Charcoal + weak ember |
| Rime | Ice iron |
| Brass | Warm bronze (default climb) |
| Iron | Cold steel |
| Mythic | Gold + frost filigree |
| Legend | Bitten-sun full emblem, brightest |

Also deliver:
- `Rank-Star.png` / `Rank-StarEmpty.png` (64×64)
- `Rank-StarGlow.png` (128×128 soft add)
- `Rank-FloorShield.png` (256×256, used when at floor)

### 5.2 Ladder UI plates
| File | Size | Use |
|---|---|---|
| `Art/Foundry/LadderPlate.png` | 1024×512 | Play hub ladder card |
| `Art/Foundry/SeasonBadge.png` | 512×128 | “Season YYYY-MM” frame |
| `Art/Foundry/QueueRing.png` | 256×256 | Spinning search (or 8-frame sheet `QueueRing_01`…`_08`) |
| `Art/Foundry/MatchFound.png` | 1920×400 | Full-width splash (no baked text) |

### 5.3 Post-match star VFX
Sprite sheet **`Art/Foundry/Vfx-StarBurst.png`**  
- 1024×1024 atlas, **4×4** frames, center-weighted burst  
- Or separate `Vfx-StarFly_01`…`_12` (128×128)

---

## 6. Asset list — Battle HUD & play experience

### 6.1 End Turn / End Round — CRITICAL
| File | States |
|---|---|
| `Art/Foundry/EndTurn.png` | Idle |
| `Art/Foundry/EndTurnHover.png` | Hover |
| `Art/Foundry/EndTurnPressed.png` | Pressed |
| `Art/Foundry/EndTurnGlow.png` | Soft outer glow for “second pass / end round” pulse |
| `Art/Foundry/EndTurnDisabled.png` | Greyed |

| | |
|---|---|
| Size | **512×160** (wide) |
| Look | Distinct from Primary—more **lantern / torch / seal press** |
| Safe text area | Center 70% empty for runtime “Pass action” / “End round” |

### 6.2 Charge (mana) system
| File | Size |
|---|---|
| `Art/Foundry/ChargePipOn.png` | 64×64 |
| `Art/Foundry/ChargePipOff.png` | 64×64 |
| `Art/Foundry/ChargeCrystal.png` | 128×128 (hero rail decoration) |
| `Art/Foundry/ChargeSpend.png` | 128×128 flash (1–2 frames ok) |

Look: **ember crystal / beacon coal**, not blue mana gems (unless frost-tinged ember).

### 6.3 Turn ownership
| File | Size |
|---|---|
| `Art/Foundry/TurnGemAlly.png` | 64×64 |
| `Art/Foundry/TurnGemEnemy.png` | 64×64 |
| `Art/Foundry/TurnTorch.png` | 128×256 optional ornate marker |

### 6.4 Targeting & legality
| File | Size | Notes |
|---|---|---|
| `Art/Foundry/TargetArrow.png` | 128×256 | Point down/toward target; rotatable |
| `Art/Foundry/TargetNode.png` | 128×128 | End point on unit/hero |
| `Art/Foundry/DropRing.png` | 256×256 | Legal lane drop; deliver 8-frame `DropRing_01`… if animated |
| `Art/Foundry/DenyX.png` | 128×128 | Invalid invalid |

### 6.5 Prophecy & Seal emphasis
| File | Size | Notes |
|---|---|---|
| `Art/Foundry/ProphecyPlaque.png` | 512×256 | Left-rail omen frame (empty center for art) |
| `Art/Foundry/ProphecyDue.png` | 512×256 | “Resolves this round” variant (brass alarm) |
| `Art/Foundry/SealLit.png` | 128×128 | Brighter Seal for claimed |
| `Art/Foundry/SealDim.png` | 128×128 | Unclaimed slot |

### 6.6 Combat feedback
| File | Size |
|---|---|
| `Art/Foundry/FloatFont.png` | 512×256 bitmap digits `0-9` + `+` `−` (white + optional red/green duplicates) |
| `Art/Foundry/Vfx-Slash.png` | 512×512 atlas 2×2 or strip |
| `Art/Foundry/Vfx-ShieldBreak.png` | 256×256 |
| `Art/Foundry/Vfx-SealClaim.png` | 512×512 |
| `Art/Foundry/Vfx-CardTrail.png` | Soft streak 256×64 |

Existing `Impact-*` and `Hit` stay; new VFX **layer with** them, don’t replace unless upgrading the whole set.

### 6.7 Hero portrait frames
`Art/Foundry/HeroFrame.png` — 512×512 ornate frame, transparent center for `Hero-A23` etc.  
Optional per-faction tints later; one universal frame is enough for EA.

### 6.8 Outcome plates
| File | Size |
|---|---|
| `Art/Foundry/OutcomeVictory.png` | 1920×1080 overlay (transparent center for runtime title) |
| `Art/Foundry/OutcomeDefeat.png` | 1920×1080 |
| `Art/Foundry/OutcomeDraw.png` | 1920×1080 optional |

---

## 7. Asset list — Collection & cosmetics

| File | Size | Notes |
|---|---|---|
| `Art/Foundry/CardBack-Ember.png` | same as CardBack | Embers shop unlock |
| `Art/Foundry/CardBack-Frost.png` | | Second cosmetic |
| `Art/Foundry/CardBack-Legend.png` | | Rare / Legend reward |
| `Art/Foundry/CurveBar.png` | 32×128 | Mana curve fill (or solid code is fine) |
| `Art/Foundry/SearchField.png` | 512×64 9-slice | Collection search |

Keep master `CardBack.png` as default equipped back.

---

## 8. Asset list — Audio

Deliver **WAV 44.1kHz mono or stereo**, peak ≤ −1.5 dBTP, short tails trimmed.  
Path: `Assets/Resources/Audio/{name}.wav`

### Required new cues
| File | Length | Direction |
|---|---|---|
| `match-found.wav` | 1.0–1.8s | Triumphant brass + soft choir hit |
| `queue-tick.wav` | 0.05–0.12s | Soft metallic tick (loop-friendly) |
| `star-gain.wav` | 0.25–0.5s | Bright chime ascending |
| `star-loss.wav` | 0.25–0.5s | Descending muted brass |
| `rank-up.wav` | 1.2–2.0s | Short fanfare (Mythic/Legend heavier) |
| `ui-open.wav` | 0.15–0.3s | Panel slide / wood+metal |
| `ui-close.wav` | 0.15–0.3s | Inverse of open |
| `ui-tab.wav` | 0.08–0.15s | Light tap |
| `your-turn-fanfare.wav` | 0.9–1.4s | Clearer than current stub; frost+brass |
| `end-turn-ready.wav` | 0.3–0.6s | Soft pulse when End Round available |
| `ambience-menu.wav` | 20–40s loop | Wind, distant horn, low hall—under Menu.ogg at −18 LUFS |

### Upgrade existing (optional remasters)
`pass-turn`, `prophecy-reveal`, `beacon-claim`, `victory`, `defeat` — keep names; replace files in place if remastering.

### Music
Keep Menu / Campaign / Battle / Boss. Optional: `Ladder.ogg` (60–90s loop) for Play hub.

### Voice (optional EA+)
5 hero enter lines, ≤2s each: `Audio/Cards/A23` already exists as card cue—**do not overwrite**. New: `Audio/Voice/Thor-Enter.wav` etc. if adding a Voice folder later.

---

## 9. Steam store assets (marketing, not Resources)

Already have capsules in `Steam/Capsules/`. Still produce:

| Asset | Spec |
|---|---|
| Gameplay screenshots ×6–8 | 1920×1080+, show battle HUD + ladder + collection |
| Trailer | 30–90s, 1080p60 or 30, no copyrighted music |
| Achievement icons ×6 | 256×256: tutorial, first seal, first chapter, all heroes, daily, cosmetic |
| Library hero refinements | If title key art changes, refresh Library-Hero |

Achievement IDs already in code: `tutorial_clear`, `first_seal`, `first_chapter`, `all_heroes`, `daily_win_practice`, `cosmetic_back`.

---

## 10. Production priority (ship order)

### P0 — Blocks “HS menu feel”
1. Title key art  
2. 4 mode tiles (+ hover)  
3. End Turn button set  
4. Rank medals (at least Ember–Brass full, Mythic/Legend can follow)  
5. Match Found splash + `match-found.wav`

### P1 — Blocks “HS play feel”
6. Charge pips + turn gems  
7. Target arrow + drop ring  
8. Prophecy plaque  
9. Star gain/loss SFX + star VFX  
10. Victory/Defeat plates  

### P2 — Polish
11. Panel/modal 9-slices  
12. Queue ring + ambience  
13. Extra card backs  
14. Float font atlas  
15. Trailer + screenshots  

---

## 11. Reference checklist for artists

**Look at in-repo**
- `Assets/Resources/Art/Identity/*` — button language to match  
- `Assets/Resources/Art/Foundry/Hero-*.png` — character realism bar  
- `Assets/Resources/Art/Foundry/Seal.png` + `Identity/Emblem.png` — sun motif  
- `Assets/Resources/Art/Foundry/Emberbridge.jpg` — battle table mood  
- `Assets/Resources/Art/Foundry/BannerFrame.png` — start point for banners  

**External comps (feel only, do not copy)**
- Hearthstone: mode select, end turn, star pop, rank chest  
- LoR: board clarity, region identity without clutter  
- Poetic Edda mood: cold, fate, names—not Marvel comedy Norse  

---

## 12. Acceptance tests

An asset set is EA-ready when:
- [ ] Title screen reads as Dawnwager with logo removed from a screenshot crop of the right half  
- [ ] Ranked medal at 64px still identifiable by league color  
- [ ] End Turn cannot be confused with Collection/Secondary  
- [ ] Prophecy plaque ≠ unit plate at a glance  
- [ ] Match Found + star SFX playtests get “that felt expensive” from cold testers  
- [ ] All P0 filenames exist under `Resources/Art/Foundry` and `Resources/Audio`  
- [ ] No baked English on interactive chrome  

---

## 13. Filename master list (copy/paste)

```
Art/Foundry/TitleKey.jpg
Art/Foundry/Mode-Campaign.png
Art/Foundry/Mode-CampaignHover.png
Art/Foundry/Mode-Casual.png
Art/Foundry/Mode-CasualHover.png
Art/Foundry/Mode-Ranked.png
Art/Foundry/Mode-RankedHover.png
Art/Foundry/Mode-Collection.png
Art/Foundry/Mode-CollectionHover.png
Art/Foundry/Icon-Campaign.png
Art/Foundry/Icon-Casual.png
Art/Foundry/Icon-Ranked.png
Art/Foundry/Icon-Collection.png
Art/Foundry/Icon-Career.png
Art/Foundry/Icon-Options.png
Art/Foundry/Panel.png
Art/Foundry/PanelGold.png
Art/Foundry/ModalFrame.png
Art/Foundry/NavBar.png
Art/Foundry/DividerOrnate.png
Art/Foundry/Tooltip.png
Art/Foundry/LadderPlate.png
Art/Foundry/SeasonBadge.png
Art/Foundry/QueueRing.png
Art/Foundry/MatchFound.png
Art/Foundry/Rank-Star.png
Art/Foundry/Rank-StarEmpty.png
Art/Foundry/Rank-StarGlow.png
Art/Foundry/Rank-FloorShield.png
Art/Foundry/Rank-Legend.png
Art/Foundry/Rank-Ember10.png … Rank-Ember1.png
Art/Foundry/Rank-Rime10.png … Rank-Rime1.png
Art/Foundry/Rank-Brass10.png … Rank-Brass1.png
Art/Foundry/Rank-Iron10.png … Rank-Iron1.png
Art/Foundry/Rank-Mythic10.png … Rank-Mythic1.png
Art/Foundry/EndTurn.png
Art/Foundry/EndTurnHover.png
Art/Foundry/EndTurnPressed.png
Art/Foundry/EndTurnGlow.png
Art/Foundry/EndTurnDisabled.png
Art/Foundry/ChargePipOn.png
Art/Foundry/ChargePipOff.png
Art/Foundry/ChargeCrystal.png
Art/Foundry/ChargeSpend.png
Art/Foundry/TurnGemAlly.png
Art/Foundry/TurnGemEnemy.png
Art/Foundry/TargetArrow.png
Art/Foundry/TargetNode.png
Art/Foundry/DropRing.png
Art/Foundry/DenyX.png
Art/Foundry/ProphecyPlaque.png
Art/Foundry/ProphecyDue.png
Art/Foundry/SealLit.png
Art/Foundry/SealDim.png
Art/Foundry/FloatFont.png
Art/Foundry/Vfx-Slash.png
Art/Foundry/Vfx-ShieldBreak.png
Art/Foundry/Vfx-SealClaim.png
Art/Foundry/Vfx-CardTrail.png
Art/Foundry/Vfx-StarBurst.png
Art/Foundry/HeroFrame.png
Art/Foundry/OutcomeVictory.png
Art/Foundry/OutcomeDefeat.png
Art/Foundry/CardBack-Ember.png
Art/Foundry/CardBack-Frost.png
Art/Foundry/CardBack-Legend.png
Art/Foundry/SearchField.png
Audio/match-found.wav
Audio/queue-tick.wav
Audio/star-gain.wav
Audio/star-loss.wav
Audio/rank-up.wav
Audio/ui-open.wav
Audio/ui-close.wav
Audio/ui-tab.wav
Audio/your-turn-fanfare.wav
Audio/end-turn-ready.wav
Audio/ambience-menu.wav
```

When P0 files land in `Resources`, say so in chat—the client can be wired to prefer them over flat `ColorBox` / generic Primary without redesigning systems.
