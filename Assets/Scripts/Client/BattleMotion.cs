using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                if (victim != null)
                    impactAt = battlePieces.TryGetValue(victim.Key, out var tile) && tile
                        ? PieceEffectPoint(victim, tile) : LanePoint(victim.Side, victim.Lane);
                else if (move.Lane >= 0) impactAt = LanePoint(side, move.Lane);
            }
            var snap = TakeSnap();
            var previousPieces = new HashSet<int>(match.Board.Select(piece => piece.Key));
            Vector2? actorFrom = null;
            string actorArt = null;
            if (strike && actor != null)
            {
                if (battlePieces.TryGetValue(actor.Key, out var actorTile) && actorTile) actorFrom = CenterAnchored(actorTile);
                actorArt = UnitArt(actor.Card.Id);
                if (victim == null && heroBadges[1 - side]) impactAt = CenterAnchored(heroBadges[1 - side]) + new Vector2(-20, 40);
            }
            Vector2? handFrom = null;
            if (move.Kind == ActionKind.Play && side == 0 && handPoints.TryGetValue(move.Slot, out var handRect) && handRect) handFrom = CenterAnchored(handRect);
            else if (move.Kind == ActionKind.Play && side == 1) handFrom = new Vector2(960, -60);
            var pendingProphecies = match.Prophecies.Where(prophecy => !prophecy.Spent).ToArray();
            if (!match.Apply(side, move))
            {
                UiSound("deny");
                return false;
            }
            if (side == 1)
            {
                string line = null;
                if (played != null) line = "The enemy plays " + played.Name;
                else if (actor != null && strike) line = actor.Card.Name + " attacks " + (victim != null ? victim.Card.Name : "your Bastion");
                else if (actor != null) line = actor.Card.Name + " moves lanes";
                else if (move.Kind == ActionKind.Power) line = "The enemy uses their hero power";
                else if (move.Kind == ActionKind.Pass) line = "The enemy passes";
                if (line != null) Callout(line, EnemyCopper);
            }
            MixLog();
            choosing = false; selected = ActionKind.Pass; cursor = -1;
            Render();
            var layer = CardAnimationLayer();
            QueuePhaseFeedback(layer, oldRound, oldTurn, move);
            bool prophecyResolved = false;
            foreach (var prophecy in pendingProphecies)
                if (prophecy.Spent)
                {
                    prophecyResolved = true;
                    StartCoroutine(ProphecyFx(layer, prophecy.Card.Id));
                    TryStartCardVfx(layer, prophecy.Card.Id, new Vector2(960f, -540f), new Vector2(1020f, 640f), opacity: .48f);
                    StartCoroutine(DelayedBanner(layer, "Prophecy", prophecy.Card.Name, Gold, 1.4f, 0f));
                    StartCoroutine(ResolveArt(layer, prophecy.Card.Id, new Vector2(960, -520), 650));
                }
            Color accent = side == 0 ? Gold : EnemyCopper;
            float feedbackDelay = 0f;
            if (played != null)
            {
                if (played.Kind == Kind.Order)
                {
                    var kind = PickFx(played);
                    Vector2 spellAt = impactAt;
                    if (victim != null && battlePieces.TryGetValue(victim.Key, out var targetTile) && targetTile)
                        spellAt = PieceEffectPoint(victim, targetTile);
                    else if (move.Target == 0 && move.Lane < 0)
                    {
                        int owner = WantsOwnHero(kind) ? side : 1 - side;
                        if (heroBadges[owner]) spellAt = CenterAnchored(heroBadges[owner]);
                    }
                    if (reduceMotion)
                        StartCoroutine(ResolveArt(layer, played.Id, spellAt, 290));
                    else
                    {
                        float castDelay = handFrom.HasValue ? .46f : 0f;
                        if (handFrom.HasValue)
                            StartCoroutine(CardSlam(layer, played, handFrom.Value, spellAt, accent));
                        Vector2 caster = heroBadges[side] ? CenterAnchored(heroBadges[side]) : handFrom ?? spellAt;
                        StartCoroutine(DelayedSpell(layer, played, caster, spellAt, castDelay));
                        feedbackDelay = castDelay + ImpactDelay(kind) + .06f;
                    }
                }
                else if (!reduceMotion && handFrom.HasValue)
                {
                    var arrival = match.Board.FirstOrDefault(piece => piece.Side == side && piece.Card.Id == played.Id &&
                        !previousPieces.Contains(piece.Key));
                    if (arrival != null && battlePieces.TryGetValue(arrival.Key, out var arrivalTile) && arrivalTile)
                        impactAt = PieceEffectPoint(arrival, arrivalTile);
                    StartCoroutine(CardSlam(layer, played, handFrom.Value, impactAt, accent));
                    feedbackDelay = .66f;
                }
            }
            if (!reduceMotion)
            {
                float arrivalDelay = prophecyResolved ? 1.05f : feedbackDelay;
                foreach (var piece in match.Board)
                {
                    if (previousPieces.Contains(piece.Key) || !battlePieces.TryGetValue(piece.Key, out var tile) || !tile)
                        continue;
                    if (piece.Card.Kind == Kind.Unit)
                        StartCoroutine(SpawnReveal(tile, arrivalDelay));
                    StartCoroutine(DelayedEntrance(layer, piece.Card, PieceEffectPoint(piece, tile), arrivalDelay));
                }
            }
            if (strike && actorFrom.HasValue && actorArt != null && !reduceMotion)
            {
                battlePieces.TryGetValue(actor.Key, out var realTile);
                RectTransform victimTile = null;
                if (victim != null) battlePieces.TryGetValue(victim.Key, out victimTile);
                StartCoroutine(StrikeLunge(layer, actorArt, actorFrom.Value, impactAt, realTile, Pal(actor.Card.Faction)[0], actor.Card, victimTile));
                feedbackDelay = .21f;
            }
            else if (impactCard != null && played == null) StartCoroutine(CardImpact(layer, impactCard, impactAt, strike));
            if (feedbackDelay > 0f) StartCoroutine(DelayedFeedback(layer, snap, feedbackDelay));
            else PlayFeedback(layer, snap);
            return true;
        }

        /// <summary>A layer above the screen that survives Render(), so banners and impacts are not wiped by redraws.</summary>
        private RectTransform CardAnimationLayer()
        {
            if (fxLayer == null) fxLayer = Box(stage, "Effects", 0, 0, 1920, 1080);
            fxLayer.SetAsLastSibling();
            return fxLayer;
        }

        private Vector2 PieceEffectPoint(Piece piece, RectTransform tile)
        {
            var center = CenterAnchored(tile);
            // Sites draw their portrait at the left edge of the strip. Unit stats sit below the art.
            return piece.Card.Kind == Kind.Site
                ? center + new Vector2(21f - tile.rect.width * .5f, 0f)
                : center + new Vector2(0f, 24f);
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

        // ---------------------------------------------------------------- combat feedback

        private sealed class BoardSnap
        {
            public readonly Dictionary<int, int> Hp = new Dictionary<int, int>();
            public readonly Dictionary<int, Vector2> At = new Dictionary<int, Vector2>();
            public readonly Dictionary<int, string> Art = new Dictionary<int, string>();
            public readonly int[] HeroHp = new int[2];
            public readonly int[] Seals = new int[2];
            public int Contested, Round;
        }

        private BoardSnap TakeSnap()
        {
            var snap = new BoardSnap();
            foreach (var unit in match.Units())
            {
                snap.Hp[unit.Key] = unit.Hp;
                snap.Art[unit.Key] = UnitArt(unit.Card.Id);
                if (battlePieces.TryGetValue(unit.Key, out var rect) && rect) snap.At[unit.Key] = CenterAnchored(rect);
            }
            snap.Contested = match.Contested;
            snap.Round = match.Round;
            for (int i = 0; i < 2; i++) { snap.HeroHp[i] = match.Players[i].Hp; snap.Seals[i] = match.Players[i].Seals; }
            return snap;
        }

        /// <summary>Floating damage and healing numbers, fading ghosts for fallen units, and a banner when a Seal is claimed.</summary>
        private void PlayFeedback(RectTransform layer, BoardSnap before)
        {
            foreach (var unit in match.Units())
            {
                if (!before.Hp.TryGetValue(unit.Key, out int old) || unit.Hp == old) continue;
                if (!battlePieces.TryGetValue(unit.Key, out var rect) || !rect) continue;
                int delta = unit.Hp - old;
                StartCoroutine(FloatText(layer, (delta > 0 ? "+" : "−") + Mathf.Abs(delta), delta > 0 ? AllySage : EnemyCopper, CenterAnchored(rect)));
            }
            var alive = new HashSet<int>(match.Units().Select(u => u.Key));
            foreach (var pair in before.At)
            {
                if (alive.Contains(pair.Key) || !before.Art.TryGetValue(pair.Key, out var art)) continue;
                StartCoroutine(Shake(7f));
                StartCoroutine(FallenGhost(layer, art, pair.Value));
            }
            for (int side = 0; side < 2; side++)
            {
                int delta = match.Players[side].Hp - before.HeroHp[side];
                if (delta != 0 && heroBadges[side])
                {
                    var at = CenterAnchored(heroBadges[side]) + new Vector2(70, side == 0 ? 20 : 40);
                    StartCoroutine(FloatText(layer, (delta > 0 ? "+" : "−") + Mathf.Abs(delta), delta > 0 ? AllySage : EnemyCopper, at));
                    if (delta < 0) { UiSound("hero-hit"); StartCoroutine(Shake(Mathf.Min(16f, 5f + -delta * 1.5f))); }
                }
                if (match.Players[side].Seals > before.Seals[side])
                    StartCoroutine(DelayedBanner(layer, "Seal claimed", (side == 0 ? "You hold" : "The enemy holds") + " the " + Lane(Mathf.Max(0, before.Contested)) + " lane",
                        side == 0 ? AllySage : EnemyCopper, 1.0f, match.Round != before.Round ? 1.2f : 0f));
            }
        }

        /// <summary>Short decaying screen shake; skipped when reduce-motion is on.</summary>
        private float shakeEnergy;
        private bool shaking;
        private float shakeTick;

        private IEnumerator Shake(float magnitude)
        {
            if (reduceMotion || stage == null) yield break;
            // One shaker at a time, always returning to the true origin. Overlapping shakes used to
            // capture a displaced "home" and leave the whole screen permanently offset.
            shakeEnergy = Mathf.Max(shakeEnergy, magnitude);
            if (shaking && Time.unscaledTime - shakeTick < .25f) yield break; // a stopped coroutine leaves the flag stale
            shaking = true;
            float t = 0f;
            while (stage != null && shakeEnergy > .25f)
            {
                t += Time.unscaledDeltaTime;
                shakeTick = Time.unscaledTime;
                stage.anchoredPosition = new Vector2(Mathf.Sin(t * 90f), Mathf.Cos(t * 77f)) * shakeEnergy;
                shakeEnergy *= Mathf.Pow(.0004f, Time.unscaledDeltaTime);
                yield return null;
            }
            shakeEnergy = 0f;
            shaking = false;
            if (stage != null) stage.anchoredPosition = Vector2.zero;
        }

        private IEnumerator FloatText(RectTransform layer, string text, Color color, Vector2 at)
        {
            if (!layer) yield break;
            var holder = Box(layer, "Float", at.x - 90, -at.y - 36, 180, 72);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            OutlinedText(holder, text, 0, 0, 180, 72, 56, color, true, TextAnchor.MiddleCenter, 30);
            float duration = reduceMotion ? .9f : 1.05f;
            for (float t = 0; t < duration && holder; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                if (!reduceMotion)
                {
                    holder.anchoredPosition = new Vector2(at.x - 90, at.y + 36 + 70 * Mathf.Sin(k * Mathf.PI * .5f));
                    holder.localScale = Vector3.one * (k < .12f ? .6f + k / .12f * .55f : 1.15f - Mathf.Min(.15f, (k - .12f)));
                }
                group.alpha = k < .6f ? 1 : 1 - (k - .6f) / .4f;
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
        }

        private IEnumerator FallenGhost(RectTransform layer, string art, Vector2 at)
        {
            if (!layer) yield break;
            var holder = Box(layer, "Fallen", at.x - TileW / 2f, -at.y - TileH / 2f, TileW, TileH);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            DrawBoardUnit(holder, art, 1f, Color.white);
            float duration = reduceMotion ? .4f : .65f;
            for (float t = 0; t < duration && holder; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                group.alpha = 1 - k;
                if (!reduceMotion)
                {
                    holder.localScale = Vector3.one * (1f + .12f * k);
                    group.alpha = 1 - k * k;
                }
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
        }

        /// <summary>A copy of a played card that glides from the hand (or the enemy) to its lane and fades.</summary>
        private IEnumerator GhostFlight(RectTransform layer, Card card, Vector2 from, Vector2 to)
        {
            if (!layer || reduceMotion) yield break;
            const int w = 170;
            var ghost = PaperCard(card, 0, 0, w, 240, card.Cost, false, layer, false);
            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            ghost.pivot = new Vector2(.5f, .5f);
            const float duration = .38f;
            for (float t = 0; t < duration && ghost; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duration);
                ghost.anchoredPosition = Vector2.Lerp(from, to, k);
                ghost.localScale = Vector3.one * Mathf.Lerp(1.15f, .62f, k);
                ghost.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI) * (from.x < to.x ? -6f : 6f));
                group.alpha = k < .7f ? 1 : 1 - (k - .7f) / .3f;
                yield return null;
            }
            if (ghost) Destroy(ghost.gameObject);
        }

        private IEnumerator DelayedBanner(RectTransform layer, string title, string subtitle, Color accent, float hold, float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            if (layer) ShowPhaseBanner(layer, title, subtitle, accent, hold);
        }

        /// <summary>Removes any floating text, ghosts or banners left on the effects layer (their coroutines were stopped).</summary>
        private void ClearEffects()
        {
            if (!fxLayer) return;
            for (int i = fxLayer.childCount - 1; i >= 0; i--) Destroy(fxLayer.GetChild(i).gameObject);
        }
    }
}
