using System.Collections;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private bool ResolveWithMotion(int side, Move move, Vector2? dragOrigin = null)
        {
            if (match == null) return false;
            return match.Apply(side, move);
        }

        private RectTransform CardAnimationLayer() => root;

        private Vector2 CenterPoint(RectTransform rect)
        {
            if (!rect) return Vector2.zero;
            return rect.anchoredPosition + new Vector2(rect.sizeDelta.x * .5f, -rect.sizeDelta.y * .5f);
        }

        private IEnumerator FinishMotion(RectTransform layer, float delay)
        {
            presenting = false;
            yield break;
        }
    }
}
