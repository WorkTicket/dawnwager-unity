using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private readonly RectTransform[] drawPiles = new RectTransform[2];
        private readonly List<RectTransform> enemyHandPoints = new List<RectTransform>();
        private string CardBackArt => string.IsNullOrEmpty(profile.cardBack) ? Foundry + "CardBack" : profile.cardBack;

        private void DrawPiles()
        {
            DrawPile(1, 1652, 40, 40, 57);
            DrawPile(0, 1652, 480, 40, 57);
        }

        private void DrawPile(int side, int x, int y, int w, int h)
        {
            var pilot = match.Players[side];
            int count = pilot.Pile.Count;
            var pile = Box(root, "Draw pile " + side, x, y, w, h);
            drawPiles[side] = pile;
            if (count > 0)
            {
                Pic(pile, CardBackArt, 4, 4, w, h, new Color(1, 1, 1, .7f));
                Pic(pile, CardBackArt, 0, 0, w, h);
            }
            else ColorBox(pile, "Empty", 0, 0, w, h, new Color(0, 0, 0, .35f));
            if (pilot.Fatigue > 0) Label(root, "Fatigue " + pilot.Fatigue, x - 40, y + h + 2, 120, 22, 14, EnemyCopper, false, TextAnchor.MiddleCenter);
        }

        private static CanvasGroup HideUntilLanding(RectTransform destination)
        {
            if (!destination) return null;
            var group = destination.GetComponent<CanvasGroup>() ?? destination.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            return group;
        }

        private IEnumerator CardFlight(RectTransform layer, string id, Vector2 from, Vector2 to,
            float duration, float delay, float startScale, float endScale, bool reveal, bool dealSound,
            CanvasGroup destination = null, bool fadeOut = false, bool turnOver = false)
        {
            yield break;
        }
    }
}
