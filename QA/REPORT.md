# Dawnwager 0.6 verification

| Area | Result |
|---|---|
| Rules and campaign | 168 seeded campaign simulations completed. Chapter gating, victories, prophecies, draw counts, full-hand burns, and all 16 starting-hand replacement patterns were checked. |
| Windows compilation | Windows-only migration and gameplay client were compiled with `UNITY_STANDALONE_WIN` against a .NET Standard 2.1 reference surface and available Unity engine and UI references. The newer object lookup API was mapped to its older equivalent in the temporary QA copy only. |
| Packaging | The campaign map, card animation code, updated README, build paths, and archive integrity were checked. |

Native Unity 6 Windows export, reading an old Windows save, and visual checks remain to be run on Windows. The updated **Dawnwager → Check Campaign in Play Mode** helper exercises the opening deal, replacing a card, and loading the battle board. Normal card draws and plays can be checked by passing into round two and playing a card onto the board.
