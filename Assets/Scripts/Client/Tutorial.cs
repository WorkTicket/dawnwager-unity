namespace Wolfsun
{
    public sealed partial class Game
    {
        private void StartTutorial()
        {
            // Rules-only: builds a Match; no UI.
            StopAllCoroutines();
            tutorialMatch = true;
            duel = true;
            match = new Match(Campaign.Starter, Campaign.EnemyDeck(0), profile.hero, "M23", "P01", "P02", 42, 14);
            Go(Page.Battle);
        }

        private void DrawTutorialCoach() { }
    }
}
