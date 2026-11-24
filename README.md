# Dawnwager

An offline tactical card game aimed at Steam Early Access. Build a deck, cross the frozen bridge, and win back the dawn. Includes a guided tutorial, Road to Ragnarök campaign, Beacon Ladder (Casual/Ranked find-match with Ranked-sim AI fill), daily quests, and a 144-card collection. See `Steam/EA-ROADMAP.md`.

## Play and build

1. Extract the archive and add the **Dawnwager-Unity** folder in Unity Hub with Unity **6000.0.61f1**.
2. Open `Assets/Scenes/TheLastDawn.unity` and press **Play**. For a larger in-editor preview, maximize the Game view.
3. With Windows Build Support installed, select **Dawnwager → Build Windows Player**. Launch `Builds/Dawnwager-Windows/Dawnwager.exe`. Keep the executable and the rest of that folder together.

The standalone player defaults to borderless fullscreen at the desktop resolution. **F11** and **Options → Display** switch between fullscreen and a resizable window. The backdrop covers the entire screen while the game keeps the controls inside a centered safe area. The Windows player checks for a save from the former WOLFSUN build if Dawnwager has no profile yet.

## Campaign and battle

Choose an available location on the campaign map and win its encounter to unlock the next stop. The route draws its story beats from the Eddas, with original encounters and a fictional map. See `Credits/Ragnarok.md`.

The opening hand deals from the deck. Select cards to replace, then confirm to watch those cards turn over and fly back. Replacements deal into your hand, and normal draws and played cards travel to their on-board destinations. Reduced motion skips these sequences.

Hover a hand card to see its full card face. Drag it into a highlighted crossing or onto a valid target. You can also click a card and choose an action. Hover a unit already on the board to read its card and current Attack and Health. The hero health medals stay visible during targeting. Win by taking six Seals or reducing the enemy hero to zero health.

**Dawnwager → Check Campaign in Play Mode** verifies campaign navigation from the title screen. See `QA/REPORT.md` for results and `Steam/README.md` for Windows depot packaging.
