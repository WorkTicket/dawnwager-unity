using System.Collections;
using UnityEngine;

namespace Wolfsun
{
    /// <summary>Applies a move to the match, then plays its sound, redraws the table and layers feedback on top.</summary>
    public sealed partial class Game
    {
        private RectTransform fxLayer;

        private bool ResolveWithMotion(int side, Move move, Vector2? dragOrigin = null)
        {
            if (match == null || move == null) return false;
            int oldRound = match.Round, oldTurn = match.Turn;
            Card played = null;
            var hand = match.Players[side].Hand;
            if (move.Kind == ActionKind.Play && move.Slot >= 0 && move.Slot < hand.Count) played = Catalog.Get(hand[move.Slot]);
            Piece actor = (move.Kind == ActionKind.Attack || move.Kind == ActionKind.March) ? match.Find(move.Slot) : null;
            Piece victim = move.Target != 0 ? match.Find(move.Target) : null;
            Vector2 impactAt = new Vector2(960, -540);
            Card impactCard = played;
            bool strike = false;
            if (move.Kind == ActionKind.Attack && actor != null)
            {
                impactCard = actor.Card;
                strike = true;
                impactAt = victim != null ? LanePoint(victim.Side, victim.Lane) : new Vector2(150, side == 0 ? -105f : -625f);
                if (victim != null && battlePieces.TryGetValue(victim.Key, out var tile)) impactAt = CenterAnchored(tile);
            }
            else if (played != null)
            {
                if (victim != null && battlePieces.TryGetValue(victim.Key, out var tile)) impactAt = CenterAnchored(tile);
                else if (move.Lane >= 0) impactAt = LanePoint(side, move.Lane);
            }
            if (!match.Apply(side, move))
            {
                UiSound("deny");
                return false;
            }
            MixLog();
            choosing = false; selected = ActionKind.Pass; cursor = -1;
            Render();
            var layer = CardAnimationLayer();
            QueuePhaseFeedback(layer, oldRound, oldTurn, move);
            if (impactCard != null) StartCoroutine(CardImpact(layer, impactCard, impactAt, strike));
            return true;
        }

        /// <summary>A layer above the screen that survives Render(), so banners and impacts are not wiped by redraws.</summary>
        private RectTransform CardAnimationLayer()
        {
            if (fxLayer == null) fxLayer = Box(stage, "Effects", 0, 0, 1920, 1080);
            fxLayer.SetAsLastSibling();
            return fxLayer;
        }

        private Vector2 CenterPoint(RectTransform rect)
        {
            if (!rect) return Vector2.zero;
            return rect.anchoredPosition + new Vector2(rect.sizeDelta.x * .5f, -rect.sizeDelta.y * .5f);
        }

        private Vector2 CenterAnchored(RectTransform rect)
        {
            var c = RootCenter(rect);
            return new Vector2(c.x, -c.y);
        }

        private IEnumerator FinishMotion(RectTransform layer, float delay)
        {
            presenting = false;
            yield break;
        }
    }
}
