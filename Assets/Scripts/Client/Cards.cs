using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>
    /// The single card renderer (hand, inspect, collection, mulligan, preview). Follows the 416x590 layer table in
    /// DesignData/BUILD-INSTRUCTIONS.md section 2. All text is live.
    /// </summary>
    public sealed partial class Game
    {
        private static string KindName(Card card) =>
            card.Kind == Kind.Unit ? "Unit" : card.Kind == Kind.Order ? "Order" : card.Kind == Kind.Site ? "Site" : "Prophecy";

        private static string FactionName(char faction)
        {
            switch (faction)
            {
                case 'A': return "Asgard";
                case 'M': return "Midgard";
                case 'J': return "Jötunheim";
                case 'H': return "Hel";
                case 'L': return "Loki";
                case 'N': return "Neutral";
                case 'X': return "Token";
                default: return "Prophecy";
            }
        }

        private string CardArt(Card card)
        {
            switch (card.Kind)
            {
                case Kind.Unit: return "Units/" + card.Id;
                case Kind.Order: return "Orders/" + card.Id;
                default: return "Places/" + card.Id;
            }
        }

        private string FramePath(Card card)
        {
            string kind = KindName(card);
            string path = "Card-" + card.Faction + "-" + kind;
            if (card.Kind == Kind.Unit && card.Legendary) path += "-Mythic";
            return Foundry + path;
        }

        private static string CompactRules(string rules)
        {
            if (string.IsNullOrEmpty(rules)) return "";
            int stop = rules.IndexOf(". ", StringComparison.Ordinal);
            string first = stop > 0 ? rules.Substring(0, stop + 1) : rules;
            if (first.Length > 74) first = first.Substring(0, 71).TrimEnd() + "…";
            return first;
        }

        /// <summary>
        /// Draws one card. cost is the live cost to show (pass card.Cost for base). When piece is given, live board stats are shown.
        /// rules=true draws the complete rules text; false gives the concise hand face.
        /// </summary>
        private RectTransform PaperCard(Card card, float x, float y, int w, int h, int cost, bool highlighted, Transform parent,
            bool rules, Piece piece = null, bool mulliganSelect = false)
        {
            CardSize(ref w, ref h);
            var rect = Box(parent != null ? parent : root, card.Name, x, y, w, h);
            float sx = w / 416f, sy = h / 590f;
            // CardShadow and CardPlayableGlow are authored at exactly card size: draw them 1:1, never enlarged.
            Pic(rect, Foundry + "CardShadow", w * .012f, h * .014f, w, h, new Color(1, 1, 1, .4f));
            if (highlighted) Pic(rect, Foundry + "CardPlayableGlow", 0, 0, w, h);
            // Layout (card space 416x590): a shorter art window on top, then name, type, and a tall rules panel
            // so every card can explain its full rules. The unused lower part of the frame opening is filled with the
            // frame's own metal tone behind the nameplate.
            const float ArtH = 212f;
            var well = Box(rect, "Art well", 34 * sx, 56 * sy, 348 * sx, 296 * sy);
            var backing = Fill(well, new Color32(66, 57, 47, 255));
            backing.raycastTarget = false;
            var portrait = Box(well, "Portrait", 0, 0, 348 * sx, ArtH * sy); // no RectMask2D: it clips wrongly on tilted cards
            string art = CardArt(card);
            string facePath = "CardFaces/" + card.Id;
            bool hasFace = Texture(facePath) != null;
            string foilPath = "Foil/Faces/" + card.Id;
            if (hasFace)
                TopCover(portrait, facePath, 348 * sx, ArtH * sy, Color.white);
            else if (card.Kind == Kind.Unit)
            {
                Backdrop(portrait, 0, 0, 348 * sx, ArtH * sy, CardAccent(card));
                float ua = Mathf.Min(348 * sx, ArtH * sy) * 1.02f;
                FitSafe(portrait, art, (348 * sx - ua) / 2f, 2 * sy, ua, ua);
            }
            else TopCover(portrait, art, 348 * sx, ArtH * sy, Color.white);
            if (hasFace && card.Kind == Kind.Unit && Texture(foilPath) != null)
            {
                var layer = TopCover(portrait, foilPath, 348 * sx, ArtH * sy, new Color(1f, 1f, 1f, .8f));
                if (!reduceMotion) StartCoroutine(PortraitFoil(layer, card));
            }
            else if (!hasFace || card.Kind != Kind.Unit)
            {
                var foil = Pic(portrait, Foundry + "Foil-" + card.Faction, 0, 0, 88 * sx, ArtH * sy, Color.clear);
                if (foil.texture != null && !reduceMotion) StartCoroutine(FoilSweep(foil, card, 348 * sx, 0));
            }
            Pic(rect, FramePath(card), 0, 0, w, h);
            Pic(rect, Foundry + "Nameplate", 40 * sx, 248 * sy, 336 * sx, 44 * sy);
            var name = OutlinedText(rect, card.Name, 58 * sx, 248 * sy, 300 * sx, 44 * sy, Mathf.Max(11, Mathf.RoundToInt(26 * sx)),
                card.Legendary ? Gold : White, true, TextAnchor.MiddleCenter, 8);
            string keywords = card.Kind == Kind.Unit ? (card.Guard ? " · Guard" : card.Rush ? " · Rush" : card.Unchain > 0 ? " · Unchain" : "") : "";
            SafeText(rect, FactionName(card.Faction) + " " + KindName(card) + keywords, 50 * sx, 292 * sy, 316 * sx, 26 * sy,
                Mathf.Max(9, Mathf.RoundToInt(17 * sx)), new Color32(222, 205, 168, 255), false, TextAnchor.MiddleCenter, 7);
            Pic(rect, Foundry + "RulesParchment", 40 * sx, 320 * sy, 336 * sx, 214 * sy);
            // Always the complete rules; the box shrinks the type to fit rather than cutting words.
            SafeText(rect, RulesText.Describe(card), 54 * sx, 328 * sy, 308 * sx, 176 * sy,
                Mathf.Max(8, Mathf.RoundToInt(23 * sx)), DarkInk, false, TextAnchor.MiddleCenter, 6);
            int medal = Mathf.Max(10, Mathf.RoundToInt(38 * sx));
            if (card.Kind != Kind.Prophecy)
                Medal(rect, "Cost", Mathf.Max(0, cost).ToString(), 8 * sx, 8 * sy, 72 * sx, medal);
            if (card.Kind == Kind.Unit)
            {
                int attack = piece != null ? piece.Attack : card.Attack;
                int health = piece != null ? piece.Hp : card.Health;
                Medal(rect, "Attack", attack.ToString(), 8 * sx, 510 * sy, 72 * sx, medal);
                Medal(rect, "Health", health.ToString(), 336 * sx, 510 * sy, 72 * sx, medal);
            }
            if (piece != null && piece.Shield)
                Pic(rect, Foundry + "Shield", 168 * sx, 20 * sy, 80 * sx, 80 * sy);
            if (mulliganSelect)
            {
                Pic(rect, Foundry + "CardShadow", 0, 0, w, h, new Color(0, 0, 0, .6f));
                Pic(rect, Foundry + "DenyX", w * .25f, h * .3f, w * .5f, w * .5f);
            }
            return rect;
        }

        /// <summary>Cover-fits a texture into a w x h window, keeping the upper part (faces and focal props) when it must crop.</summary>
        private RawImage TopCover(Transform parent, string path, float w, float h, Color tint)
        {
            var view = Cover(parent, path, 0, 0, w, h, tint);
            if (view.texture == null) { view.color = Color.clear; return view; }
            var uv = view.uvRect;
            if (uv.height < 1f) { uv.y = Mathf.Max(0f, 1f - uv.height - .06f); view.uvRect = uv; }
            return view;
        }

        private System.Collections.IEnumerator PortraitFoil(RawImage layer, Card card)
        {
            float offset = Motif(card.Id) % 31 * .19f;
            float period = card.Legendary ? 3.6f : 4.6f;
            while (layer && !reduceMotion)
            {
                float phase = (Time.unscaledTime + offset) * (2f * Mathf.PI / period);
                layer.color = new Color(1f, 1f, 1f, .72f + .27f * Mathf.Cos(phase));
                yield return null;
            }
            if (layer) layer.color = new Color(1f, 1f, 1f, .80f);
        }

        /// <summary>Full-size card as a clickable control (collection grid, rewards, inspect).</summary>
        private void FullCard(string id, float x, float y, float w, float h, Action click, Color? tint = null, Transform parent = null)
        {
            var card = Catalog.Get(id);
            var rect = PaperCard(card, x, y, Mathf.RoundToInt(w), Mathf.RoundToInt(h), card.Cost, false, parent, false);
            if (tint.HasValue) Pic(rect, Foundry + "CardShadow", 0, 0, rect.sizeDelta.x, rect.sizeDelta.y, tint.Value);
            if (click != null)
            {
                Click(rect, click);
                BindPreview(rect, id, null, true);
            }
        }
    }
}
