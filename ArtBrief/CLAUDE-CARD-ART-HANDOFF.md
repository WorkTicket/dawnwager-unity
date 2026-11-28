# Dawnwager card art handoff

Use [Hel-Art-Direction.png](Hel-Art-Direction.png) as a **technique reference only**: opaque matte gouache on toned paper, broad painted forms, tactile wool and iron, natural anatomy, charcoal shadows, and restrained color. The setting is Norse mythology: ground the world in saga-era Scandinavia, its gods and creatures, carved runes, longhouses, fjords, ships, barrows, and mythic places such as Bifröst and Élivágar. Use historically grounded wool, linen, leather, timber, iron, and bronze; fantasy elements should grow from Norse cosmology and folklore. Do not introduce modern objects, generic high-fantasy armor, or unrelated visual motifs. Do not copy Hel's face, mask, hair, silhouette, clothing, or plum palette onto another character. Prefer written style direction over passing Hel's portrait as an image reference for unrelated cards; that caused unrelated characters to inherit her mask and wardrobe in early drafts. Give every card a specific identity, one readable action, and a distinct silhouette. Keep the sharpest edges near the face or defining prop.

The 161-card catalog is in an active art pass; use `Card-Art-Consistency-Checklist.csv` as the source of truth. Installed/generated artwork is still draft art until a human art director reviews it. Do not call the full set final while any checklist rows remain pending. N06 Fisher of the Sound remains the regression check for the earlier male-card/female-board mismatch.

Review the [first four paired illustrations](Art-Pair-Review-A01-A02-A05-A06.jpg) and the [full catalog](Dawnwager-Full-Card-Catalog.jpg) alongside the reference. The paired review is a static preview; it does not replace checking the sprites in Unity. Refresh both full-catalog previews only after the complete art pass.

## Character consistency protocol

1. Write a short identity line for the character before drawing either view: age, face, hair, skin tone, clothing palette, and one signature object.
2. Design the card illustration as an environmental scene whose action communicates the card. Keep it in the card art well's landscape crop.
3. Design the played humanoid portrait as a separate **chest/waist-up bust** of the same individual. Match the established cutout scale: face, shoulders, upper torso, and only the hands/prop needed for the action. Do not show human legs or a full body. Keep arms anatomically connected, both hands intentional, and the cutout cleanly transparent with no platform, pedestal, or fake ground shadow. Animal portraits may remain full-body when that is the established design.
4. Compare the card and board images at full size and inside the actual board tile. Reject identity drift, repeated poses, malformed/extra limbs, awkward arm joins, clipped hands/weapons, human full-body cutouts, and details that disappear at board scale. Keep card scene views and played busts compositionally distinct.
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
