using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private readonly RectTransform[] laneZones = new RectTransform[2];
        private readonly Dictionary<int, RectTransform> handPoints = new Dictionary<int, RectTransform>();
        private readonly List<(Move Move, RectTransform Area, Image Glow)> dragTargets = new List<(Move, RectTransform, Image)>();
        private RectTransform dragLayer, carriedCard, dragArrow;
        private int dragPointer = int.MinValue;
        private Move dropMove;

        private void BindCardDrag(RectTransform area, int slot) { }

        private void CancelDrag()
        {
            if (dragLayer) { Destroy(dragLayer.gameObject); dragLayer = null; }
            carriedCard = null;
            dragArrow = null;
            dragPointer = int.MinValue;
            dropMove = null;
            dragTargets.Clear();
        }
    }
}
