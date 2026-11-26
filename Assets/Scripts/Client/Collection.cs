using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Collection and deck builder: filter and browse every card, click to add to the 30-card deck.</summary>
    public sealed partial class Game
    {
        private const string FilterLetters = "AMJHLN";
        private const int GridCols = 6, GridRows = 2, GridW = 205, GridH = 290;

        private static bool CardMatchesQuery(Card card, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            string hay = (card.Name + " " + RulesText.Describe(card) + " " + KindName(card) + " " + FactionName(card.Faction) + " cost" + card.Cost).ToLowerInvariant();
            foreach (var word in query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (!hay.Contains(word)) return false;
            return true;
        }

        private IEnumerable<Card> VisibleCards()
        {
            var cards = Catalog.All.Where(c => c.Faction != 'X' && c.Faction != 'P');
            if (filter > 0 && filter <= FilterLetters.Length) cards = cards.Where(c => c.Faction == FilterLetters[filter - 1]);
            if (ownedOnly) cards = cards.Where(c => owned.Contains(c.Id));
            string q = profile.collectionQuery;
            cards = cards.Where(c => CardMatchesQuery(c, q));
            return cards.OrderBy(c => c.Cost).ThenBy(c => c.Name);
        }

        private bool DeckLegal => Campaign.LegalDeck(build, owned);

        private string DeckProblem()
        {
            if (build.Count != 30) return build.Count + " / 30 cards";
            if (!DeckLegal) return "Illegal deck (max two factions plus Neutral)";
            return "Ready · 30 / 30";
        }

        private void CommitDeckIfLegal()
        {
            if (!DeckLegal) return;
            profile.deck = build.ToArray();
            StoreDeck();
            Save();
        }

        private void AddDeck(string id)
        {
            Card card;
            try { card = Catalog.Get(id); } catch (InvalidOperationException) { return; }
            string why = null;
            if (!owned.Contains(id)) why = "You do not own that card yet.";
            else if (build.Count >= 30) why = "Your deck is full.";
            else if (build.Count(x => x == id) >= (card.Legendary ? 1 : 2)) why = card.Legendary ? "Only one copy of a Mythic card." : "Two copies maximum.";
            else if (card.Faction != 'N')
            {
                var factions = build.Select(x => Catalog.Get(x).Faction).Where(f => f != 'N').Distinct().ToList();
                if (!factions.Contains(card.Faction) && factions.Count >= 2) why = "A deck can use only two factions plus Neutral.";
            }
            if (why != null) { UiSound("deny"); status = why; Render(); return; }
            build.Add(id);
            UiSound("click");
            status = "";
            CommitDeckIfLegal();
            Render();
        }

        private void RemoveDeck(string id)
        {
            if (build.Remove(id)) { UiSound("click"); CommitDeckIfLegal(); status = ""; Render(); }
        }

        private void Collection()
        {
            Banner("Collection", owned.Count(id => Catalog.All.Any(c => c.Id == id && c.Faction != 'X' && c.Faction != 'P')) + " cards owned · click a card to add it, click a deck row to remove it");
            // filters
            string[] labels = { "All", "Asgard", "Midgard", "Jötunheim", "Hel", "Loki", "Neutral" };
            for (int i = 0; i < labels.Length; i++)
            {
                int f = i;
                float tx = 50 + i * 150;
                TextLink(labels[i], tx, 262, 140, 48, 25, filter == f ? Gold : Muted, () => { filter = f; collectionPage = 0; UiSound("tab"); Render(); });
                if (filter == f) ColorBox(root, "Tab line", tx, 310, 100, 3, Gold);
            }
            TableButton(ownedOnly ? "Owned only" : "Show all", 1090, 266, 140, 44, () => { ownedOnly = !ownedOnly; collectionPage = 0; Render(); }, true, ownedOnly);
            DrawSearchField(1244, 264, 130, 48);

            var cards = VisibleCards().ToList();
            int perPage = GridCols * GridRows;
            int pages = Mathf.Max(1, (cards.Count + perPage - 1) / perPage);
            collectionPage = Mathf.Clamp(collectionPage, 0, pages - 1);
            for (int i = 0; i < perPage; i++)
            {
                int index = collectionPage * perPage + i;
                if (index >= cards.Count) break;
                var card = cards[index];
                int col = i % GridCols, row = i / GridCols;
                float x = 50 + col * (GridW + 14), y = 346 + row * (GridH + 30);
                bool have = owned.Contains(card.Id);
                string id = card.Id;
                FullCard(id, x, y, GridW, GridH, () => AddDeck(id), have ? (Color?)null : new Color(0, 0, 0, .62f));
                int inDeck = build.Count(b => b == id);
                if (inDeck > 0)
                {
                    ColorBox(root, "count plate", x + GridW - 44, y - 6, 44, 30, new Color(.05f, .07f, .1f, .95f));
                    Label(root, "×" + inDeck, x + GridW - 44, y - 6, 44, 30, 20, Gold, true, TextAnchor.MiddleCenter);
                }
                if (!have) Label(root, "Locked", x, y + GridH / 2f - 14, GridW, 28, 20, White, true, TextAnchor.MiddleCenter);
            }
            if (cards.Count == 0) Label(root, "No cards match.", 50, 600, 1300, 60, 30, Muted, false, TextAnchor.MiddleCenter);
            TableButton("◀", 480, 1000, 90, 52, () => { collectionPage = Mathf.Max(0, collectionPage - 1); UiSound("tab"); Render(); }, collectionPage > 0);
            Label(root, "Page " + (collectionPage + 1) + " / " + pages, 580, 1000, 200, 52, 22, White, true, TextAnchor.MiddleCenter);
            TableButton("▶", 790, 1000, 90, 52, () => { collectionPage = Mathf.Min(pages - 1, collectionPage + 1); UiSound("tab"); Render(); }, collectionPage < pages - 1);
            DrawDeckPanel();
        }

        private void DrawSearchField(float x, float y, float w, float h)
        {
            var box = Box(root, "Search", x, y, w, h);
            var back = box.gameObject.AddComponent<Image>();
            var sprite = GeneratedSlice("Panel");
            if (sprite != null) { back.sprite = sprite; back.type = Image.Type.Sliced; } else back.color = SoftPlate;
            back.raycastTarget = true;
            var input = box.gameObject.AddComponent<InputField>();
            var text = Label(box, "", 14, 0, w - 28, h, 20, White);
            text.supportRichText = false;
            var hint = Label(box, "Search cards…", 14, 0, w - 28, h, 20, Quiet);
            hint.fontStyle = FontStyle.Italic;
            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = back;
            input.text = profile.collectionQuery ?? "";
            input.characterLimit = 30;
            input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(value =>
            {
                if (value == profile.collectionQuery) return;
                profile.collectionQuery = value;
                collectionPage = 0;
                Render();
            });
        }

        private void DrawDeckPanel()
        {
            ChromePanel(root, "Deck panel", 1396, 272, 500, 790, "Panel");
            // heroes
            for (int i = 0; i < Hero.All.Length; i++)
            {
                var hero = Hero.All[i];
                bool unlocked = UnlockedHero(hero.Id);
                bool current = profile.hero == hero.Id;
                string id = hero.Id;
                float hx = 1416 + i * 94;
                var portrait = CirclePortrait(root, HeroArt(hero.Id), hx, 286, 84, Foundry + "HeroFrame", unlocked ? Color.white : new Color(.3f, .3f, .35f, 1f));
                Click(portrait, () => ChooseHero(id), unlocked).transition = Selectable.Transition.None;
                if (current) Pic(root, Foundry + "UnitTokenGlow", hx - 6, 280, 96, 96, Gold);
            }
            var info = Hero.Info(profile.hero);
            SafeText(root, info.Name + " · " + info.Power, 1416, 376, 460, 30, 22, Gold, true, TextAnchor.MiddleLeft, 13);
            SafeText(root, DeckProblem(), 1416, 406, 460, 28, 20, DeckLegal ? AllySage : EnemyCopper, false, TextAnchor.MiddleLeft, 12);
            if (!string.IsNullOrEmpty(status)) SafeText(root, status, 1416, 432, 460, 26, 16, Muted, false, TextAnchor.MiddleLeft, 10);
            // mana curve
            int[] curve = new int[9];
            foreach (var id in build) curve[Mathf.Min(8, Catalog.Get(id).Cost)]++;
            int tall = Mathf.Max(1, curve.Max());
            for (int i = 0; i < 9; i++)
            {
                float bh = 56f * curve[i] / tall;
                ColorBox(root, "curve " + i, 1420 + i * 50, 520 - bh, 40, bh, new Color(Brass.r, Brass.g, Brass.b, .8f));
                Label(root, i == 8 ? "8+" : i.ToString(), 1420 + i * 50, 520, 40, 22, 16, Muted, false, TextAnchor.MiddleCenter);
                if (curve[i] > 0) Label(root, curve[i].ToString(), 1420 + i * 50, 520 - bh - 20, 40, 20, 15, White, false, TextAnchor.MiddleCenter);
            }
            // list
            var rows = build.GroupBy(x => x).Select(g => new { Card = Catalog.Get(g.Key), Count = g.Count() })
                .OrderBy(r => r.Card.Cost).ThenBy(r => r.Card.Name).ToList();
            var scroll = ScrollPanel(root, 1410, 552, 472, 430, rows.Count * 34 + 8);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string id = row.Card.Id;
                var line = Box(scroll, row.Card.Name, 0, i * 34, 472, 32);
                ColorBox(line, "bg", 0, 0, 472, 32, new Color(0, 0, 0, i % 2 == 0 ? .28f : .16f));
                Label(line, row.Card.Cost.ToString(), 6, 0, 32, 32, 20, Frost, true, TextAnchor.MiddleCenter);
                SafeText(line, row.Card.Name, 46, 0, 340, 32, 20, row.Card.Legendary ? Gold : White, false, TextAnchor.MiddleLeft, 12);
                Label(line, "×" + row.Count, 400, 0, 60, 32, 20, Gold, true, TextAnchor.MiddleRight);
                Click(line, () => RemoveDeck(id)).transition = Selectable.Transition.None;
                BindPreview(line, id, null);
            }
            TableButton("Recommended", 1416, 996, 150, 52, () =>
            {
                build = Campaign.Recommend(owned, profile.hero).ToList();
                CommitDeckIfLegal();
                status = "Built a deck from your cards.";
                Render();
            });
            TableButton("Clear", 1576, 996, 120, 52, () => { build.Clear(); status = ""; Render(); });
            TableButton("Revert", 1706, 996, 170, 52, () => { build = profile.deck.ToList(); status = "Reverted to your saved deck."; Render(); });
        }
    }
}
