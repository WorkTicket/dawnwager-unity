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

        private void DrawPiles() { }

        private void DrawPile(int side, int x, int y, int w, int h) { }

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
