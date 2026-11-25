using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>
    /// Shared chrome: sliced panels, buttons with per-state sprites, End Turn, masks, safe text.
    /// Slice borders follow DesignData/Sprite-Borders.json (stored here as Unity order: left, bottom, right, top).
    /// </summary>
    public sealed partial class Game
    {
        private static readonly Vector4 PanelBorder = new Vector4(24, 24, 24, 24);
        private static readonly Vector4 ButtonBorder = new Vector4(20, 20, 20, 20);
        private static readonly Vector4 ModalBorder = new Vector4(28, 28, 28, 28);
        private static readonly Vector4 EndTurnBorder = new Vector4(24, 16, 24, 16);
        private static readonly Vector4 RailBorder = new Vector4(24, 26, 24, 26);
        private static readonly Color SoftPlate = new Color32(14, 20, 30, 248);

        private readonly Dictionary<string, Sprite> tintedSprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> buttonFaces = new Dictionary<string, Sprite>();
        private Sprite circleSprite;

        private static Vector4 BorderFor(string art)
        {
            if (art.StartsWith("EndTurn", StringComparison.Ordinal)) return EndTurnBorder;
            if (art == "ModalFrame") return ModalBorder;
            if (art == "HistoryRail") return RailBorder;
            if (art.StartsWith("Primary", StringComparison.Ordinal) || art.StartsWith("Secondary", StringComparison.Ordinal)) return ButtonBorder;
            return PanelBorder;
        }

        /// <summary>A 9-sliced sprite from Art/Foundry, or null if the art is missing.</summary>
        private Sprite Slice(string art) => FoundrySprite(art, BorderFor(art));

        /// <summary>Like Picture, but a missing texture draws nothing instead of a white box.</summary>
        private Texture2D gradientTex;

        /// <summary>Vertical accent gradient used behind cut-out busts so transparent art never shows a black hole.</summary>
        private RawImage Backdrop(Transform parent, float x, float y, float w, float h, Color accent)
        {
            if (gradientTex == null)
            {
                gradientTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int i = 0; i < 64; i++)
                {
                    float t = i / 63f; // 0 = bottom
                    float v = Mathf.Lerp(.28f, 1f, Mathf.SmoothStep(0, 1, t));
                    gradientTex.SetPixel(0, i, new Color(v, v, v, 1));
                }
                gradientTex.Apply();
            }
            var rect = Box(parent, "backdrop", x, y, w, h);
            var view = rect.gameObject.AddComponent<RawImage>();
            view.texture = gradientTex;
            view.color = Color.Lerp(accent, new Color(.12f, .16f, .24f, 1f), .45f);
            view.raycastTarget = false;
            return view;
        }

        private RawImage Pic(Transform parent, string name, float x, float y, float w, float h, Color? tint = null)
        {
            var view = Picture(parent, name, x, y, w, h, tint);
            if (view.texture == null) view.color = Color.clear;
            return view;
        }

        private RawImage CoverSafe(Transform parent, string name, float x, float y, float w, float h, Color tint)
        {
            var view = Cover(parent, name, x, y, w, h, tint);
            if (view.texture == null) view.color = Color.clear;
            return view;
        }

        private RawImage FitSafe(Transform parent, string name, float x, float y, float w, float h, Color? tint = null)
        {
            var view = Fit(parent, name, x, y, w, h);
            if (view.texture == null) { view.color = Color.clear; return view; }
            if (tint.HasValue) view.color = tint.Value;
            return view;
        }

        private RectTransform ChromePanel(Transform parent, string name, float x, float y, float w, float h,
            string art = "Panel", bool hit = false)
        {
            var rect = Box(parent != null ? parent : root, name, x, y, w, h);
            var sprite = Slice(art);
            var image = rect.gameObject.AddComponent<Image>();
            if (sprite != null) { image.sprite = sprite; image.type = Image.Type.Sliced; }
            else image.color = SoftPlate;
            image.raycastTarget = hit;
            return rect;
        }

        private RectTransform ChromeModal(Transform parent, string name, float x, float y, float w, float h) =>
            ChromePanel(parent, name, x, y, w, h, "ModalFrame", true);

        private void SlicePanel(Transform parent, string art, float x, float y, float w, float h, bool hit = false) =>
            ChromePanel(parent, art, x, y, w, h, art, hit);

        private Text SafeText(Transform parent, string words, float x, float y, float w, float h, int size, Color color,
            bool formal = false, TextAnchor anchor = TextAnchor.MiddleLeft, int minSize = 11)
        {
            var text = Label(parent, words, x, y, w, h, size, color, formal, anchor);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(minSize, size);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        /// <summary>Text with a dark outline so numerals and names stay readable on busy art.</summary>
        private Text OutlinedText(Transform parent, string words, float x, float y, float w, float h, int size, Color color,
            bool formal = true, TextAnchor anchor = TextAnchor.MiddleCenter, int minSize = 9)
        {
            var text = SafeText(parent, words, x, y, w, h, size, color, formal, anchor, minSize);
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(8, 6, 4, 230);
            float d = Mathf.Max(1f, size * .06f);
            outline.effectDistance = new Vector2(d, -d);
            return text;
        }

        private void ChromeButton(string text, float x, float y, float w, float h, Action click,
            bool enabled = true, bool primary = false, Transform parent = null, bool pulse = false) =>
            TableButton(text, x, y, w, h, click, enabled, primary, parent, pulse);

        private RectTransform MaskedContent(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Box(parent, name, x, y, w, h);
            rect.gameObject.AddComponent<RectMask2D>();
            return rect;
        }

        private Sprite CircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            var tex = Texture(Foundry + "CircleMask");
            if (tex == null) return null;
            circleSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
            return circleSprite;
        }

        private RectTransform CirclePortrait(Transform parent, string artPath, float x, float y, float size,
            string frameArt = null, Color? spentTint = null)
        {
            var holder = Box(parent, "portrait", x, y, size, size);
            bool framed = !string.IsNullOrEmpty(frameArt);
            // HeroFrame/UnitToken openings measure about 0.67 of the frame; tuck the disc just under the ring.
            float d = framed ? size * .70f : size * .92f;
            float off = (size - d) / 2f;
            var disc = Box(holder, "disc", off, off, d, d);
            var maskImage = disc.gameObject.AddComponent<Image>();
            var sprite = CircleSprite();
            if (sprite != null) maskImage.sprite = sprite;
            maskImage.color = framed ? new Color(.05f, .07f, .1f, 1f) : Color.white;
            maskImage.raycastTarget = false;
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = framed;
            if (framed)
            {
                // Busts have the face near the top: scale down and sit lower so the circle never clips it.
                Backdrop(disc, 0, 0, d, d, new Color(.35f, .5f, .75f, 1f));
                float s = d * 1.22f;
                CoverSafe(disc, artPath, (d - s) / 2f, d * .03f, s, s, spentTint ?? Color.white);
                Pic(holder, frameArt, 0, 0, size, size);
            }
            else CoverSafe(disc, artPath, 0, 0, d, d, spentTint ?? Color.white);
            return holder;
        }

        private Sprite ButtonFace(string name) => Slice(name);

        private void HoverCue(RectTransform rect, Func<bool> active = null)
        {
            var relay = rect.gameObject.GetComponent<PointerRelay>();
            if (relay == null) relay = rect.gameObject.AddComponent<PointerRelay>();
            relay.Enter = () => { if (active == null || active()) UiSound("hover"); };
        }

        private void TableButton(string text, float x, float y, float w, float h, Action click,
            bool enabled = true, bool primary = false, Transform parent = null, bool pulse = false)
        {
            string family = primary ? "Primary" : "Secondary";
            var rect = Box(parent != null ? parent : root, text, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            var face = Slice(enabled ? family : family + "Disabled");
            if (face != null)
            {
                image.sprite = face;
                image.type = Image.Type.Sliced;
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = Slice(family + "Hover"),
                    selectedSprite = Slice(family + "Hover"),
                    pressedSprite = Slice(family + "Pressed"),
                    disabledSprite = Slice(family + "Disabled")
                };
            }
            else image.color = primary ? Brass : Slate;
            if (click != null)
                button.onClick.AddListener(() => { UiSound("click"); click(); });
            HoverCue(rect, () => enabled);
            int size = Mathf.Clamp(Mathf.RoundToInt(h * .36f), 15, 30);
            Color ink = enabled ? (primary ? Gold : White) : Quiet;
            SafeText(rect, text, 16, 0, w - 32, h, size, ink, true, TextAnchor.MiddleCenter, 11);
            if (pulse && enabled) PulseEndTurn(rect, true);
        }

        private void EndTurnButton(string text, float x, float y, float w, float h, Action click, bool enabled, bool pulse, Transform parent)
        {
            // The art is 3.2:1; keep that aspect so the lever and lantern silhouettes survive.
            h = Mathf.Min(h, w / 3.2f);
            var rect = Box(parent != null ? parent : root, "End turn", x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            var face = Slice(enabled ? "EndTurn" : "EndTurnDisabled");
            if (face != null)
            {
                image.sprite = face;
                image.type = Image.Type.Sliced;
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = Slice("EndTurnHover"),
                    selectedSprite = Slice("EndTurnHover"),
                    pressedSprite = Slice("EndTurnPressed"),
                    disabledSprite = Slice("EndTurnDisabled")
                };
            }
            else image.color = Slate;
            if (click != null)
                button.onClick.AddListener(() => { UiSound("click"); click(); });
            HoverCue(rect, () => enabled);
            if (enabled && pulse)
            {
                var glow = Box(rect, "glow", 0, 0, w, h);
                var glowImage = glow.gameObject.AddComponent<Image>();
                var glowSprite = Slice("EndTurnGlow");
                if (glowSprite != null) { glowImage.sprite = glowSprite; glowImage.type = Image.Type.Sliced; }
                else glowImage.color = Color.clear;
                glowImage.raycastTarget = false;
                PulseEndTurn(glow, true);
            }
            // Label box measured on the source art (512x160): x 108-436, y 53-134.
            float lx = w * (108f / 512f), ly = h * (53f / 160f), lw = w * (328f / 512f), lh = h * (81f / 160f);
            SafeText(rect, text, lx, ly, lw, lh, Mathf.Clamp(Mathf.RoundToInt(h * .3f), 14, 26),
                enabled ? Gold : Quiet, true, TextAnchor.MiddleCenter, 11);
        }

        /// <summary>A clipped, wheel- and drag-scrollable area. Returns the content rect to add children to.</summary>
        private RectTransform ScrollPanel(Transform parent, int x, int y, int w, int h, int contentHeight)
        {
            var view = MaskedContent(parent, "Scroll", x, y, w, h);
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);
            catcher.raycastTarget = true;
            var content = Box(view, "Content", 0, 0, w, Mathf.Max(h, contentHeight));
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            return content;
        }
    }
}
