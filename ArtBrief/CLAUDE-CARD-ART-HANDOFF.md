# Dawnwager card art handoff

Use [Hel-Art-Direction.png](Hel-Art-Direction.png) as a **technique reference only**: opaque matte gouache on toned paper, broad painted forms, tactile wool and iron, natural anatomy, charcoal shadows, and restrained color. Do not copy Hel's face, mask, hair, silhouette, clothing, or plum palette onto another character. Prefer written style direction over passing Hel's portrait as an image reference for unrelated cards; that caused unrelated characters to inherit her mask and wardrobe in early drafts. Give every card a specific identity, one readable action, and a distinct silhouette. Keep the sharpest edges near the face or defining prop.

The 161-card catalog is **not yet art-complete**. Redrawn entries currently include A01–A10, A13, A17 and A19. The Unit/Site pairs were checked side by side for matching identity and distinct action. The other catalog entries remain pending; do not describe them as final or mark their checklist rows complete. N06 Fisher of the Sound remains the regression check for the earlier male-card/female-board mismatch.

Review the [first four paired illustrations](Art-Pair-Review-A01-A02-A05-A06.jpg) and the [full catalog](Dawnwager-Full-Card-Catalog.jpg) alongside the reference. The paired review is a static preview; it does not replace checking the sprites in Unity. Refresh both full-catalog previews only after the complete art pass.

## Character consistency protocol

1. Write a short identity line for the character before drawing either view: age, face, hair, skin tone, clothing palette, and one signature object.
2. Design the card illustration as an environmental scene whose action communicates the card. Keep it in the card art well's landscape crop.
3. Design the board portrait as a separate pose of the same individual. Preserve face, age, hair, clothing, and props; change camera angle and action. Keep the complete silhouette inside the transparent canvas.
4. Compare the card and board images at full size and at board scale. Reject identity drift, repeated poses, clipped hands/weapons, and details that disappear at board scale.
5. Preserve each Unity `.meta` file. Update the matching checklist row only after both views have passed inspection. Human art-direction approval remains a separate status from identity/pose checks.

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
