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
                case Kind.Unit: return "Units/" + (card.Id == "X11" ? "X02" : card.Id);
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
            var well = Box(rect, "Art well", 34 * sx, 56 * sy, 348 * sx, 296 * sy);
            var backing = Fill(well, new Color(.035f, .045f, .06f, 1f));
            backing.raycastTarget = false;
            var mask = MaskedContent(well, "Portrait", 0, 0, 348 * sx, 296 * sy);
            string art = CardArt(card);
            if (card.Kind == Kind.Unit)
            {
                Backdrop(mask, 0, 0, 348 * sx, 296 * sy, CardAccent(card));
                float ua = 348 * sx * 1.04f;
                FitSafe(mask, art, (348 * sx - ua) / 2f, 4 * sy, ua, ua);
            }
            else CoverSafe(mask, art, 0, 0, 348 * sx, 296 * sy, Color.white);
            var foil = Pic(mask, Foundry + "Foil-" + card.Faction, 0, 0, 88 * sx, 296 * sy);
            if (foil.texture != null && !reduceMotion) StartCoroutine(FoilSweep(foil, card, 348 * sx, 0));
            else foil.color = Color.clear;
            Pic(rect, FramePath(card), 0, 0, w, h);
            Pic(rect, Foundry + "Nameplate", 40 * sx, 330 * sy, 336 * sx, 44 * sy);
            var name = OutlinedText(rect, card.Name, 58 * sx, 330 * sy, 300 * sx, 44 * sy, Mathf.Max(11, Mathf.RoundToInt(26 * sx)),
                card.Legendary ? Gold : White, true, TextAnchor.MiddleCenter, 8);
            string keywords = card.Kind == Kind.Unit ? (card.Guard ? " · Guard" : card.Rush ? " · Rush" : card.Unchain > 0 ? " · Unchain" : "") : "";
            SafeText(rect, FactionName(card.Faction) + " " + KindName(card) + keywords, 50 * sx, 374 * sy, 316 * sx, 28 * sy,
                Mathf.Max(9, Mathf.RoundToInt(17 * sx)), new Color32(222, 205, 168, 255), false, TextAnchor.MiddleCenter, 7);
            Pic(rect, Foundry + "RulesParchment", 42 * sx, 410 * sy, 332 * sx, 120 * sy);
            string text = RulesText.Describe(card);
            SafeText(rect, rules ? text : CompactRules(text), 56 * sx, 416 * sy, 304 * sx, 108 * sy,
                Mathf.Max(9, Mathf.RoundToInt((rules ? 19 : 20) * sx)), DarkInk, false, TextAnchor.MiddleCenter, 7);
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
