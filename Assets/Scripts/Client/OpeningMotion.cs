using System.Collections.Generic;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private readonly List<RectTransform> openingCards = new List<RectTransform>();

        /// <summary>The opening hand is chosen in OpeningPanel; this just resets selection state and announces the match.</summary>
        private void DealOpeningHand()
        {
            mulligan.Clear();
            UiSound("card-shuffle");
            UiSound("card-deal");
        }

        private void EnsureOpeningDeckAnchor() { }

        private Vector2 OpeningDeckPoint() => new Vector2(1688, -888);
    }
}
