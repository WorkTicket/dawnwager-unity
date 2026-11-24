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
            UiSound("card-select");
        }

        private void ContinueHandDrag(PointerEventData e)
        {
            if (dragLayer == null || carriedCard == null || e.pointerId != dragPointer) return;
            var at = PointerInRoot(root, e);
            carriedCard.anchoredPosition = new Vector2(at.x, -at.y);
            var hit = DropTargetAt(at);
            foreach (var target in dragTargets)
                if (target.Glow) target.Glow.color = hit != null && target.Move == hit
                    ? new Color(1f, .85f, .4f, .42f)
                    : (target.Move.Target != 0 ? new Color(1, .8f, .4f, .22f) : new Color(.7f, .9f, .75f, .16f));
        }

        private Move DropTargetAt(Vector2 at)
        {
            // Units and sites win over lane zones so targeted cards can be dropped on a creature in a lane.
            foreach (var target in dragTargets.Where(t => t.Move.Target != 0))
                if (Contains(target.Area, at)) return target.Move;
            foreach (var target in dragTargets.Where(t => t.Move.Target == 0))
                if (Contains(target.Area, at)) return target.Move;
            if (dropMove != null && at.y < 780 && at.y > 150 && at.x > 280 && at.x < 1640) return dropMove;
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
    }
}
