using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private void StartTutorial()
        {
            StopAllCoroutines();
            ResetMatchState();
            returnPage = Page.Home;
            tutorialMatch = true;
            duel = true;
            match = new Match(Campaign.Starter, Campaign.EnemyDeck(0), profile.hero, "M23", "P01", "P02", 42, 14);
            Go(Page.Battle);
            DealOpeningHand();
        }

        /// <summary>A short, state-driven coaching panel for the first match.</summary>
        private void DrawTutorialCoach()
        {
            if (match == null || match.Winner >= 0) return;
            string title, body;
            if (match.Opening)
            {
                title = "Your opening hand";
                body = "Cards you click are swapped for new ones. Keep the cheap ones you can play early.";
            }
            else if (match.Turn != 0)
            {
                title = "Enemy turn";
                body = "Wait for the rival to act. Each action passes the turn back and forth.";
            }
            else if (choosing)
            {
                title = "Choose where it goes";
                body = "Click a glowing lane bar or target. Right-click anywhere to cancel.";
            }
            else
            {
                var legal = match.Legal(0);
                bool canPlay = legal.Any(m => m.Kind == ActionKind.Play);
                bool canAttack = legal.Any(m => m.Kind == ActionKind.Attack);
                if (canPlay)
                {
                    title = "Play a card";
                    body = "Click a bright card in your hand. Units fight, Orders cast once, Sites stay in their lane. Charge is your mana.";
                }
                else if (canAttack)
                {
                    title = "Attack";
                    body = "Click one of your ready units, then an enemy or the enemy Bastion. Guards must be struck first.";
                }
                else
                {
                    title = "End your turn";
                    body = "Nothing left to do. Press Pass. When both sides pass, the round ends and the contested lane awards a Seal.";
                }
            }
            ChromePanel(root, "Coach", 300, 14, 340, 136, "PanelGold");
            SafeText(root, title, 320, 20, 300, 30, 24, Gold, true, TextAnchor.MiddleLeft, 14);
            SafeText(root, body, 320, 52, 300, 92, 16, White, false, TextAnchor.UpperLeft, 11);
        }
    }
}
