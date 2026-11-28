# Card art consistency and identity rework

This brief supersedes the earlier assumption that tonal grading and texture treatment alone are enough. The shipped gallery still has many near-identical portrait angles, generic centered stances, and character mismatches between a card and its board art. Treat the 161-row `Card-Art-Consistency-Checklist.csv` as the coverage list; do not call the art pass complete while any row is pending.

## Shared visual direction

Use the attached `Hel-Art-Direction.png` as a technique reference only: matte gouache on toned paper, visibly hand-shaped brushwork, tactile wool, wood, iron and weathered skin; broad value masses; natural anatomy; restrained cold slate, brass, ember, and faction colors. The world is Norse mythology rooted in saga-era Scandinavia: use runes, longhouses, fjords, ships, barrows, and the established gods, creatures, and mythic places. Build fantasy details from Norse cosmology; avoid modern objects, generic high-fantasy armor, and unrelated motifs. Maintain legibility at the card art well (348 × 296 logical pixels) and board portrait display (about 164 × 176). Keep all titles, card frames, stats, type and rules as live UI.

The art should feel selected and composed by a senior fantasy illustrator. Avoid the recurring AI look: same head-and-shoulders crop, same three-quarter face turned left, same centered frontal stance, repeated shoulder silhouette, generic decorative details, overly polished skin, hyper-detailed noise everywhere, accidental text, and excessive uniform fog. Do not apply a global filter or reuse one pose across a faction.

## Identity and pose rules

- For every Unit with both `CardFaces/<ID>.png` and `BoardPortraits/<ID>.png`, those images must depict the same named individual. Match apparent age, gender, face, hair, scars, skin tone, defining clothing, weapon and signature prop. The board art may change the action and camera angle, but must preserve the character identity.
- Give each character a short visual identity line and one signature anchor before painting either view. Record it in the checklist or a companion art bible. Do not infer identity from faction alone.
- Assign every Unit card scene a distinct silhouette, action and camera distance. Rotate among full-body motion, profile, overhead/low angle, seated work, defensive brace, leaping attack, mounted travel, group command, ritual, and quiet narrative beat. Played humanoid art is always a chest/waist-up bust; full-body compositions are reserved for animals when they suit the established cutout style. Avoid repeating the same pose in neighboring cards or between Legendary characters.
- Derive the action from the card's name, rules, role and equipment. Guard should communicate its own action; Rush should have a legible path of motion; healers should show an intentional healing interaction; a fisherman should perform a believable fishing task. Generic idle poses are not acceptable for the whole catalog.
- For Orders, show the event or action being cast, not a lineup of characters. For Sites, establish a distinctive place and architecture that can be recognized again when it remains on the board. For Prophecies, use a strong readable omen image with a clear focal shape.
- For tokens and summoned cards, use the exact token's identity art. A summon animation must never substitute the summoning Order's artwork.

## Proven mismatch to preserve as a regression check

`N06 — Fisher of the Sound` previously showed a man on the card face and a woman when played. The updated pair in `Assets/Resources/Art/CardFaces/N06.png` and `Assets/Resources/Art/BoardPortraits/N06.png` now depicts the same weathered male fisherman in related, distinct actions. Check both together in the game; do not change one side without rechecking the pair.

## File and review requirements

Keep the current runtime paths and GUID `.meta` files. Card-face artwork belongs in `Assets/Resources/Art/CardFaces/<ID>.png`; played Unit/Site artwork belongs in `Assets/Resources/Art/BoardPortraits/<ID>.png`. Do not bake card borders, names, costs, rules, stats, or foil into these images. Keep card art full-bleed in its well and board art readable at the small board size.

Review the checklist row by row against the actual card name/rules and both runtime views. Mark identity, unique pose, and style QA only after visually checking the art in the card and on the board. Fix face/board mismatches, anatomy problems, clipped hands/weapons, repeated compositions, and details that vanish at game scale. Refresh `Dawnwager-Full-Card-Catalog.jpg` and `Dawnwager-Final-Card-Design.jpg` after the full pass so previews match the shipped source images.
