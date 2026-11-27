# Dawnwager card art handoff

Use [Hel-Art-Direction.png](Hel-Art-Direction.png) as the visual reference for new illustrations. The target is opaque matte gouache on toned paper, with broad painted forms, wool and iron that feel tactile, natural anatomy, charcoal shadows, and restrained color. Give each card a single recognizable subject and keep the sharpest edges near the face or defining prop. Hel's plum palette belongs to Hel. Use the same painting technique across factions while retaining their individual colors.

The card redesign applies the same shared frame system and finish across the 161-card catalog. The Legendary portraits and visibly mismatched card art were replaced or recomposed. The remaining illustrations use reworked tonal and brush-texture treatment on the existing artwork.

Review [assembled cards](Dawnwager-Final-Card-Design.jpg) and the [full catalog](Dawnwager-Full-Card-Catalog.jpg) alongside the reference. These are static previews assembled from game textures.

## Runtime paths

All paths below are relative to `Assets/Resources/Art/`. Preserve the existing Unity `.meta` files when replacing images.

| Asset | Path | Export |
|---|---|---|
| Full card illustration | `CardFaces/<ID>.png` | 696 × 592, RGB or RGBA |
| Played Unit and Site | `BoardPortraits/<ID>.png` | 320 × 320, RGBA |
| Battle hero | `HeroPortraits/<ID>.png` | 320 × 320, RGBA |
| Hero menu bust | `HeroBusts/<ID>.png` | 512 × 512, RGBA |
| Card foil | `Foil/Faces/<ID>.png` | 696 × 592, RGBA |
| Card frame | `Foundry/Card-<faction>-<kind>.png` or `-Mythic.png` | 416 × 590, RGBA |
| Shared nameplate | `Foundry/Nameplate.png` | 336 × 44, RGBA |
| Rules panel | `Foundry/RulesParchment.png` | 332 × 120, RGBA |

`PaperCard` in `Assets/Scripts/Client/Cards.cs` draws the frame, card name, rules, stats, and foil. Keep the frame art well fully transparent at x=34, y=56, width=348, height=296. Preserve the existing live text and socket coordinates. Use sRGB, Clamp, Bilinear, and no mipmaps for the card art sprites. Treat rules text and the title as live UI. Keep the rules panel light enough for long effects and the stat sockets clear of decorative lines.

## Legendary foil motifs

| ID | Illustration | Foil |
|---|---|---|
| A23 | Thor, Last Light | Broken lightning branches |
| A24 | Týr, the Open Hand | Paired oath arcs |
| M23 | Runa, the Ice Ferrier | Ice wake |
| M24 | Eydis, Beacon Keeper | Beacon rays |
| J23 | Fenrir, Chainbreaker | Split chain links |
| J24 | Surtr, the Coming Fire | Ember fissures |
| H23 | Hel, Keeper of the Names | Ledger clasps |
| H24 | Garmr, Door of the Dead | Crypt arch |
| L23 | Loki, Oathbreaker | Split paths |
| L24 | Angrboða, Who Warned Us | Rowan branches |

Position foil details around the focal area with transparent space above faces and props. Transparent Legendary source portraits are in `ArtBrief/LegendaryCutouts/`. Composite these into their card faces over a subdued painted scene. Separate played art fills the shield to its lower rim so the character feels grounded. Preserve Thor’s one-handed Mjölnir grip and Hel’s attached reference portrait for the card face.

## Unity review

1. Inspect an Asgard Unit, Midgard Site, Order, and Prophecy at full size and in hand. Check the portrait aperture, stat sockets, and rules panel.
2. Inspect all ten Legendary cards. Compare the foil motifs with Reduce Motion on and off. Check that no overlay obscures a face.
3. Play Thor, Hel, and Basalt Whelp. Check the full-bleed framing, then attack and defeat animations. Check the hero selection busts separately.
4. Check the project in the Unity version listed in `ProjectSettings/ProjectVersion.txt` before marking the art ready for a build.
