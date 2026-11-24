using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Minimal stubs so the project compiles. No real UI.</summary>
    public sealed partial class Game
    {
        private static readonly Vector4 PanelBorder = new Vector4(24, 24, 24, 24);
        private static readonly Vector4 ButtonBorder = new Vector4(20, 20, 20, 20);
        private static readonly Vector4 ModalBorder = new Vector4(28, 28, 28, 28);
        private static readonly Vector4 EndTurnBorder = new Vector4(24, 16, 24, 16);
        private static readonly Color SoftPlate = new Color32(14, 20, 30, 248);

        private readonly Dictionary<string, Sprite> tintedSprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> buttonFaces = new Dictionary<string, Sprite>();

        private RectTransform ChromePanel(Transform parent, string name, float x, float y, float w, float h,
            string art = "Panel", bool hit = false) => Box(parent ?? root, name, x, y, w, h);

        private RectTransform ChromeModal(Transform parent, string name, float x, float y, float w, float h) =>
            Box(parent ?? root, name, x, y, w, h);

        private Text SafeText(Transform parent, string words, float x, float y, float w, float h, int size, Color color,
            bool formal = false, TextAnchor anchor = TextAnchor.MiddleLeft, int minSize = 11) =>
            Label(parent, words, x, y, w, h, size, color, formal, anchor);

        private void ChromeButton(string text, float x, float y, float w, float h, Action click,
            bool enabled = true, bool primary = false, Transform parent = null, bool pulse = false) { }

        private RectTransform MaskedContent(Transform parent, string name, float x, float y, float w, float h) =>
            Box(parent, name, x, y, w, h);

        private RectTransform CirclePortrait(Transform parent, string artPath, float x, float y, float size,
            string frameArt = null, Color? spentTint = null) =>
            Box(parent, "portrait", x, y, size, size);

        private Sprite ButtonFace(string name) => null;

        private void TableButton(string text, float x, float y, float w, float h, Action click,
            bool enabled = true, bool primary = false, Transform parent = null, bool pulse = false) { }

        private void EndTurnButton(string text, float x, float y, float w, float h, Action click, bool enabled, bool pulse, Transform parent) { }

        private void SlicePanel(Transform parent, string art, float x, float y, float w, float h, bool hit = false) { }
    }
}
