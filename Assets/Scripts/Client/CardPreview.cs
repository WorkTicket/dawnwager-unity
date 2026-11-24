using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Hover preview: a large copy of a card with full rules and keyword help, drawn above the screen.</summary>
    public sealed partial class Game
    {
        private RectTransform previewCard;
        private RectTransform previewSource;

        private void ClearCardPreview()
        {
            if (previewCard) Destroy(previewCard.gameObject);
            previewCard = null;
            previewSource = null;
        }

        private Vector2 RootCenter(RectTransform rect)
        {
            if (rect == null || root == null) return new Vector2(960, 540);
            Vector3 local = root.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return new Vector2(local.x, -local.y);
        }

        /// <summary>Shows or hides the preview as the pointer moves on and off the source. Right-click also pins it open as Inspect.</summary>
        private void BindPreview(RectTransform source, string id, Piece piece, bool grid = false)
        {
            var relay = source.gameObject.GetComponent<PointerRelay>() ?? source.gameObject.AddComponent<PointerRelay>();
            var prior = relay.Enter;
            relay.Enter = () => { if (prior != null) prior(); ShowCardPreview(source, id, piece); };
            relay.Exit = () => { if (previewSource == source) ClearCardPreview(); };
            relay.RightClick = _ => { ClearCardPreview(); inspected = id; inspectBack = true; Render(); };
        }

        private void ShowCardPreview(RectTransform source, string id, Piece piece)
        {
            if (presenting || settings || showRules || inspected != null || dragLayer != null) return;
            ClearCardPreview();
            Card card;
            try { card = Catalog.Get(id); } catch (System.InvalidOperationException) { return; }
            const int w = 340, h = 482;
            var at = RootCenter(source);
            float px = at.x < 960 ? at.x + source.rect.width * .5f + 24 : at.x - source.rect.width * .5f - 24 - w;
            if (px < 16 || px + w > 1904) px = Mathf.Clamp(px, 16, 1904 - w);
            float py = Mathf.Clamp(at.y - h * .5f, 16, 1080 - h - 16);
            var layer = Box(root, "Card preview", px, py, w + 20, h);
            previewCard = layer;
            previewSource = source;
            PaperCard(card, 0, 0, w, h, piece != null ? card.Cost : card.Cost, false, layer, true, piece);
            var terms = RulesText.Terms(card).Take(3).ToArray();
            if (terms.Length > 0)
            {
                float th = 28 + terms.Length * 64;
                float tx = at.x < 960 ? w + 14 : -304;
                var tip = Box(layer, "Terms", tx, 0, 290, th);
                ChromePanel(tip, "plate", 0, 0, 290, th, "Panel");
                for (int i = 0; i < terms.Length; i++)
                {
                    SafeText(tip, terms[i].Key, 16, 12 + i * 64, 258, 22, 19, Gold, true, TextAnchor.MiddleLeft, 12);
                    SafeText(tip, terms[i].Value, 16, 33 + i * 64, 258, 42, 15, White, false, TextAnchor.UpperLeft, 10);
                }
            }
        }
    }
}
