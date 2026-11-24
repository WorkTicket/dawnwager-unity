using System.Collections.Generic;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private readonly List<RectTransform> openingCards = new List<RectTransform>();

        private void DealOpeningHand() { }

        private void EnsureOpeningDeckAnchor() { }

        private Vector2 OpeningDeckPoint() => Vector2.zero;
    }
}
