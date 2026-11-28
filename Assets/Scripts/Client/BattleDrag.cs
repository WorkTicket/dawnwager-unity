using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Drag a hand card onto a highlighted lane or target. Click-to-select still works; this is the faster path.</summary>
    public sealed partial class Game
    {
        private readonly RectTransform[] laneZones = new RectTransform[2];
        private readonly Dictionary<int, RectTransform> handPoints = new Dictionary<int, RectTransform>();
        private readonly List<(Move Move, RectTransform Area, Image Glow)> dragTargets = new List<(Move, RectTransform, Image)>();
        private RectTransform dragLayer, carriedCard, dragArrow;
        private int dragPointer = int.MinValue;
        private Move dropMove;
        private CanvasGroup dragSource;

        private static Vector2 PointerInRoot(RectTransform root, PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, e.position, null, out Vector2 local);
            return new Vector2(local.x, -local.y); // x right, y down: matches Box coordinates
        }

        private void BindCardDrag(RectTransform area, int slot)
        {
            var relay = area.gameObject.GetComponent<PointerRelay>() ?? area.gameObject.AddComponent<PointerRelay>();
            relay.BeginDrag = e => BeginHandDrag(area, slot, e);
            relay.Drag = e => ContinueHandDrag(e);
            relay.EndDrag = e => EndHandDrag(e);
        }

        private void BeginHandDrag(RectTransform source, int slot, PointerEventData e)
        {
            if (!canAct || presenting || dragLayer != null) return;
            var options = match.Legal(0).Where(m => m.Kind == ActionKind.Play && m.Slot == slot).ToArray();
            if (options.Length == 0) { UiSound("deny"); return; }
            ClearCardPreview();
            dragPointer = e.pointerId;
            dragTargets.Clear();
            dragLayer = Box(root, "Drag layer", 0, 0, 1920, 1080);
            dragSource = source.GetComponent<CanvasGroup>() ?? source.gameObject.AddComponent<CanvasGroup>();
            dragSource.alpha = .25f;
            foreach (var move in options)
            {
                if (move.Target != 0 && battlePieces.TryGetValue(move.Target, out var tile) && tile)
                {
                    var c = RootCenter(tile);
                    var box = ColorBox(dragLayer, "Target", c.x - tile.rect.width / 2f, c.y - tile.rect.height / 2f, tile.rect.width, tile.rect.height, new Color(1, .8f, .4f, .22f));
                    Pic(box, Foundry + "TargetNode", tile.rect.width / 2f - 30, tile.rect.height / 2f - 30, 60, 60);
                    dragTargets.Add((move, box, box.GetComponent<Image>()));
                }
                else if (move.Target == 0 && move.Lane >= 0)
                {
                    var box = ColorBox(dragLayer, "Lane", LaneX[move.Lane], AllyRowY - 8, LaneWidth, AllySiteY + 48 - AllyRowY, new Color(.7f, .9f, .75f, .16f));
                    Pic(box, Foundry + "DropRing", LaneWidth / 2f - 60, 90, 120, 120);
                    dragTargets.Add((move, box, box.GetComponent<Image>()));
                }
                else if (move.Target == 0 && move.Lane < 0)
                {
                    // Untargeted: dropping anywhere on the table plays it.
                    dropMove = move;
                }
            }
            var card = Catalog.Get(match.Players[0].Hand[slot]);
            carriedCard = PaperCard(card, 0, 0, 190, 270, match.Cost(0, card), true, dragLayer, false);
            carriedCard.pivot = new Vector2(.5f, .5f);
            var at = PointerInRoot(root, e);
            carriedCard.anchoredPosition = new Vector2(at.x, -at.y);
            lastDragAt = at;
            UiSound("card-select");
        }

        private Vector2 lastDragAt;

        private void ContinueHandDrag(PointerEventData e)
        {
            if (dragLayer == null || e.pointerId != dragPointer) return;
            lastDragAt = PointerInRoot(root, e);
            RefreshDrag();
        }

        private static Color BaseGlow(Move move) =>
            move.Kind == ActionKind.Attack ? new Color(1f, .4f, .32f, .26f)
            : move.Kind == ActionKind.March ? new Color(.6f, .8f, 1f, .16f)
            : move.Target != 0 ? new Color(1f, .8f, .4f, .24f) : new Color(.7f, .9f, .75f, .16f);

        /// <summary>Runs every frame while dragging: moves the carried card or aim chain and pulses every legal target.</summary>
        private void RefreshDrag()
        {
            if (dragLayer == null) return;
            var at = lastDragAt;
            if (carriedCard != null) carriedCard.anchoredPosition = new Vector2(at.x, -at.y);
            var hit = DropTargetAt(at);
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var target in dragTargets)
            {
                if (!target.Glow) continue;
                var c = BaseGlow(target.Move);
                bool hovered = hit != null && target.Move == hit;
                c.a = hovered ? .5f : c.a * (.75f + .5f * pulse);
                target.Glow.color = c;
            }
            if (dragArrow != null) AimArrow(at, hit);
        }

        private Move DropTargetAt(Vector2 at)
        {
            // Units and sites win over lane zones so targeted cards can be dropped on a creature in a lane.
            foreach (var target in dragTargets.Where(t => t.Move.Target != 0))
                if (Contains(target.Area, at)) return target.Move;
            foreach (var target in dragTargets.Where(t => t.Move.Target == 0))
                if (Contains(target.Area, at)) return target.Move;
            if (dropMove != null && at.y < 780 && at.y > 150 && at.x > 290 && at.x < 1630) return dropMove;
            return null;
        }

        private bool Contains(RectTransform rect, Vector2 at)
        {
            if (!rect) return false;
            var c = RootCenter(rect);
            return Mathf.Abs(at.x - c.x) <= rect.rect.width / 2f && Mathf.Abs(at.y - c.y) <= rect.rect.height / 2f;
        }

        private void EndHandDrag(PointerEventData e)
        {
            if (dragLayer == null || e.pointerId != dragPointer) return;
            var at = PointerInRoot(root, e);
            var move = DropTargetAt(at);
            if (dragSource) dragSource.alpha = 1f;
            CancelDrag();
            if (move != null) CommitMove(move);
            else UiSound("deny");
        }

        private void CancelDrag()
        {
            if (dragSource) dragSource.alpha = 1f;
            dragSource = null;
            if (dragLayer) { Destroy(dragLayer.gameObject); dragLayer = null; }
            carriedCard = null;
            dragArrow = null;
            dragPointer = int.MinValue;
            dropMove = null;
            dragTargets.Clear();
        }

        // ---------------------------------------------------------------- unit attack / march

        private Vector2 arrowFrom;

        private void BindUnitDrag(RectTransform tile, Piece unit)
        {
            var relay = tile.gameObject.GetComponent<PointerRelay>() ?? tile.gameObject.AddComponent<PointerRelay>();
            relay.BeginDrag = e => BeginUnitDrag(tile, unit, e);
            relay.Drag = e => ContinueHandDrag(e);
            relay.EndDrag = e => EndHandDrag(e);
        }

        private void BeginUnitDrag(RectTransform tile, Piece unit, PointerEventData e)
        {
            if (!canAct || presenting || dragLayer != null) return;
            var options = match.Legal(0).Where(m => (m.Kind == ActionKind.Attack || m.Kind == ActionKind.March) && m.Slot == unit.Key).ToArray();
            if (options.Length == 0) { UiSound("deny"); return; }
            ClearCardPreview();
            dragPointer = e.pointerId;
            dragTargets.Clear();
            dropMove = null;
            dragLayer = Box(root, "Drag layer", 0, 0, 1920, 1080);
            foreach (var move in options)
            {
                if (move.Target != 0 && battlePieces.TryGetValue(move.Target, out var victim) && victim)
                {
                    var c = RootCenter(victim);
                    var box = ColorBox(dragLayer, "Target", c.x - victim.rect.width / 2f, c.y - victim.rect.height / 2f, victim.rect.width, victim.rect.height, new Color(1, .45f, .35f, .24f));
                    Pic(box, Foundry + "TargetNode", victim.rect.width / 2f - 30, victim.rect.height / 2f - 30, 60, 60);
                    dragTargets.Add((move, box, box.GetComponent<Image>()));
                }
                else if (move.Kind == ActionKind.Attack && move.Target == 0 && heroBadges[1])
                {
                    var c = RootCenter(heroBadges[1]);
                    var box = ColorBox(dragLayer, "Bastion", c.x - 100, c.y - 120, 200, 200, new Color(1, .45f, .35f, .2f));
                    dragTargets.Add((move, box, box.GetComponent<Image>()));
                }
                else if (move.Kind == ActionKind.March && move.Lane >= 0)
                {
                    var box = ColorBox(dragLayer, "March", LaneX[move.Lane], AllyRowY - 8, LaneWidth, AllySiteY + 48 - AllyRowY, new Color(.6f, .8f, 1f, .14f));
                    dragTargets.Add((move, box, box.GetComponent<Image>()));
                }
            }
            arrowFrom = RootCenter(tile);
            var ring = Pic(dragLayer, Foundry + "UnitTokenGlow", arrowFrom.x - tile.rect.width / 2f - 18, arrowFrom.y - tile.rect.height / 2f - 18, tile.rect.width + 36, tile.rect.height + 36, Gold);
            ring.raycastTarget = false;
            dragArrow = Box(dragLayer, "Aim", 0, 0, 1, 1);
            aimDots.Clear();
            for (int i = 0; i < AimDotCount; i++)
            {
                var dot = Pic(dragArrow, "Gen/Shadow", 0, 0, 20, 20, Gold);
                dot.raycastTarget = false;
                dot.rectTransform.pivot = new Vector2(.5f, .5f);
                aimDots.Add(dot.rectTransform);
            }
            // A clean chevron arrowhead (two bars) and a quiet target ring.
            aimWings[0] = Bar(dragArrow, Vector2.zero, new Vector2(40f, 0f), 10f, Gold);
            aimWings[1] = Bar(dragArrow, Vector2.zero, new Vector2(40f, 0f), 10f, Gold);
            aimHead = Pic(dragArrow, Foundry + "TargetNode", 0, 0, 96, 96, Gold).rectTransform;
            aimHead.pivot = new Vector2(.5f, .5f);
            aimHead.GetComponent<RawImage>().raycastTarget = false;
            lastDragAt = PointerInRoot(root, e);
            RefreshDrag();
            UiSound("card-select");
        }

        private const int AimDotCount = 14;
        private readonly List<RectTransform> aimDots = new List<RectTransform>();
        private readonly RectTransform[] aimWings = new RectTransform[2];
        private RectTransform aimHead;

        /// <summary>
        /// A single clean arc of evenly spaced beads ending in a chevron. It snaps its tip to a valid target, which
        /// gets a soft pulsing ring; the colour reads red for an attack, blue for a march and gold otherwise.
        /// </summary>
        private void AimArrow(Vector2 at, Move hit)
        {
            if (!dragArrow || aimHead == null) return;
            Vector2 end = at;
            if (hit != null)
                foreach (var target in dragTargets)
                    if (target.Move == hit && target.Area) { end = RootCenter(target.Area); break; }
            Vector2 start = arrowFrom;
            float distance = (end - start).magnitude;
            Vector2 control = (start + end) * .5f + new Vector2(0, -Mathf.Min(120f, distance * .22f));
            Color tint = hit == null ? new Color(Gold.r, Gold.g, Gold.b, .9f)
                : hit.Kind == ActionKind.Attack ? new Color(1f, .36f, .3f, 1f) : new Color(.6f, .9f, 1f, 1f);
            Vector2 last = start;
            for (int i = 0; i < aimDots.Count; i++)
            {
                float t = (i + 1f) / (aimDots.Count + 1f);
                Vector2 p = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end;
                var dot = aimDots[i];
                dot.anchoredPosition = new Vector2(p.x, -p.y);
                dot.sizeDelta = Vector2.one * Mathf.Lerp(14f, 22f, t);
                var image = dot.GetComponent<RawImage>();
                if (image) image.color = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(.3f, .95f, t));
                last = p;
            }
            Vector2 dir = (end - last).sqrMagnitude > .01f ? (end - last).normalized : Vector2.up;
            float baseAngle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg; // anchored space is y-up, root space is y-down
            for (int i = 0; i < 2; i++)
            {
                var wing = aimWings[i];
                if (!wing) continue;
                wing.anchoredPosition = new Vector2(end.x, -end.y);
                wing.localRotation = Quaternion.Euler(0, 0, baseAngle + 180f + (i == 0 ? 32f : -32f));
                var image = wing.GetComponent<Image>();
                if (image) image.color = tint;
            }
            bool locked = hit != null;
            aimHead.anchoredPosition = new Vector2(end.x, -end.y);
            aimHead.localScale = Vector3.one * (locked ? 1f + .05f * Mathf.Sin(Time.unscaledTime * 7f) : .0f);
            var headImage = aimHead.GetComponent<RawImage>();
            if (headImage) headImage.color = new Color(tint.r, tint.g, tint.b, locked ? .9f : 0f);
        }
    }
}
