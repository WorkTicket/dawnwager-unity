using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>
    /// The battle table. Interaction is click-to-select: pick a hand card, unit or hero power, then click a highlighted
    /// lane header, unit, site or Bastion. Every legal choice comes straight from Match.Legal.
    /// </summary>
    public sealed partial class Game
    {
        private static readonly Color Slate = new Color32(17, 24, 35, 247);
        private static readonly Color Edge = new Color32(117, 96, 78, 220);
        private static readonly Color Brass = new Color32(232, 188, 112, 255);
        private static readonly Color Parchment = new Color32(230, 221, 199, 255);
        private static readonly Color DarkInk = new Color32(47, 40, 32, 255);
        private static readonly Color EnemyCopper = new Color32(225, 151, 129, 255);
        private static readonly Color AllySage = new Color32(180, 211, 193, 255);
        private static readonly Color Quiet = new Color32(179, 188, 180, 255);

        private static readonly int[] LaneX = { 290, 990 };
        private readonly Dictionary<int, RectTransform> battlePieces = new Dictionary<int, RectTransform>();
        private readonly RectTransform[] heroBadges = new RectTransform[2];
        private bool showRules, presenting, reduceMotion, canAct;
        private const int TableY = 160;
        private const int LaneWidth = 620;
        private const float HandBaseY = 815f;
        private const int HandCardW = 180;
        private const int HandCardH = 255;
        private const int TileW = 176, TileH = 220;
        private const float EnemyRowY = 250f, AllyRowY = 482f, EnemySiteY = 206f, AllySiteY = 708f;

        private static float RowY(int side) => side == 1 ? EnemyRowY : AllyRowY;
        private static float SlotX(int lane, int index) => LaneX[lane] + 32 + index * (TileW + 14);
        private Vector2 LanePoint(int side, int lane) => new Vector2(LaneX[lane] + LaneWidth / 2f, -(RowY(side) + TileH / 2f));

        private void Battle()
        {
            if (match == null) { page = Page.Home; UpdateBackdrop(); Home(); return; }
            battlePieces.Clear();
            bool live = match.Winner < 0 && !match.Opening;
            canAct = live && match.Turn == 0 && !ai && !presenting;
            var legal = live && match.Turn == 0 ? match.Legal(0) : new List<Move>();
            if (!canAct && !match.Opening) { choosing = false; selected = ActionKind.Pass; cursor = -1; }
            var choices = PresentMoves(legal);

            Gradient(root, "Left shade", 0, 0, 300, 1080, new Color(0, 0, 0, .55f), new Color(0, 0, 0, 0f), false);
            Gradient(root, "Right shade", 1620, 0, 300, 1080, new Color(0, 0, 0, 0f), new Color(0, 0, 0, .55f), false);
            Pic(root, Foundry + "HandTray", 0, 800, 1920, 280);

            DrawEnemyHand();
            DrawPiles();
            DrawForecast();
            BoardLane(0, choices);
            BoardLane(1, choices);
            PlayerBadge(1, 20, Hero.Info(match.Players[1].Hero).Name, choices);
            PlayerBadge(0, 536, Hero.Info(match.Players[0].Hero).Name, choices);
            DrawHand(legal);
            DrawControls(legal, choices);
            if (match.Opening) OpeningPanel();
            if (tutorialMatch) DrawTutorialCoach();
        }

        // ---------------------------------------------------------------- right rail

        private void DrawForecast()
        {
            var fresh = match.Prophecies.FirstOrDefault(p => !p.Spent);
            var plaque = Box(root, "Prophecy", 1650, 190, 240, 120);
            Pic(plaque, Foundry + "ProphecyPlaque", 0, 0, 240, 120);
            if (fresh != null)
            {
                bool due = match.Round == fresh.Round;
                if (due) Pic(plaque, Foundry + "ProphecyDue", 0, 0, 240, 120);
                SafeText(plaque, due ? "Prophecy now" : "Prophecy · round " + fresh.Round, 28, 18, 184, 26, 17, Gold, true, TextAnchor.MiddleCenter, 11);
                SafeText(plaque, fresh.Card.Name, 28, 46, 184, 40, 22, White, true, TextAnchor.MiddleCenter, 12);
                string id = fresh.Card.Id;
                Click(plaque, () => { inspected = id; UiSound("open"); Render(); });
                BindPreview(plaque, id, null);
            }
            else SafeText(plaque, "All prophecies spent", 28, 30, 184, 60, 20, Muted, false, TextAnchor.MiddleCenter, 12);

            string contested = match.Contested < 0 ? "No contested lane" : Lane(match.Contested) + " lane contested";
            OutlinedText(root, "Round " + Mathf.Max(1, match.Round), 1650, 320, 240, 40, 32, Gold, true, TextAnchor.MiddleCenter, 18);
            SafeText(root, contested, 1650, 358, 240, 28, 19, White, false, TextAnchor.MiddleCenter, 12);
            if (match.Winner < 0 && !match.Opening)
            {
                Pic(root, Foundry + (match.Turn == 0 ? "TurnGemAlly" : "TurnGemEnemy"), 1660, 392, 30, 30);
                Label(root, match.Turn == 0 ? "Your turn" : "Enemy turn", 1698, 390, 190, 34, 22,
                    match.Turn == 0 ? AllySage : EnemyCopper, true);
            }
            var rail = ChromePanel(root, "Log", 1650, 432, 240, 220, "HistoryRail");
            var lines = match.Log.Skip(Mathf.Max(0, match.Log.Count - 6)).Select(b => b.Text).ToArray();
            for (int i = 0; i < lines.Length; i++)
                SafeText(rail, lines[i], 22, 28 + i * 28, 200, 28, 15, i == lines.Length - 1 ? White : Muted, false, TextAnchor.MiddleLeft, 10);
        }

        // ---------------------------------------------------------------- lanes

        private void BoardLane(int lane, Move[] choices)
        {
            int lx = LaneX[lane];
            bool contested = match.Contested == lane;
            ColorBox(root, "Lane tint " + lane, lx, 204, LaneWidth, 548, new Color(0, 0, 0, contested ? .26f : .18f));
            ChromePanel(root, "Lane header", lx, TableY, LaneWidth, 44, contested ? "PanelGold" : "Panel");
            int mine = match.SealPower(0, lane), theirs = match.SealPower(1, lane);
            SafeText(root, Lane(lane) + (contested ? " · Contested" : ""), lx + 20, TableY, 330, 44, 24, contested ? Gold : White, true, TextAnchor.MiddleLeft, 14);
            SafeText(root, "Seal Power  You " + mine + "  ·  Foe " + theirs, lx + 330, TableY, 270, 44, 18,
                mine > theirs ? AllySage : mine < theirs ? EnemyCopper : Muted, false, TextAnchor.MiddleRight, 11);
            var zone = choices.FirstOrDefault(m => m.Target == 0 && m.Lane == lane && m.Kind != ActionKind.Attack);
            if (zone != null)
            {
                string verb = zone.Kind == ActionKind.March ? "March to " : zone.Kind == ActionKind.Power ? "Invoke in " : "Play in ";
                TableButton("▶ " + verb + Lane(lane), lx + 2, TableY - 2, LaneWidth - 4, 48, () => CommitMove(zone), true, true);
            }
            for (int side = 1; side >= 0; side--)
            {
                var units = match.Units(side, lane).ToList();
                for (int i = 0; i < units.Count && i < 3; i++)
                    UnitTile(root, units[i], SlotX(lane, i), RowY(side), choices);
                var site = match.Board.FirstOrDefault(p => p.Card.Kind == Kind.Site && p.Side == side && p.Lane == lane);
                DrawSite(site, lx + 32, side == 1 ? EnemySiteY : AllySiteY, choices);
            }
        }

        private void DrawSite(Piece site, float x, float y, Move[] choices)
        {
            const float w = TileW * 3 + 28, h = 40;
            if (site == null)
            {
                ColorBox(root, "Empty site", x, y + 14, w, 2, new Color(1, 1, 1, .08f));
                return;
            }
            bool target = choices.Any(m => m.Target == site.Key);
            var strip = ChromePanel(root, site.Card.Name, x, y, w, h, target ? "PanelGold" : "Panel");
            battlePieces[site.Key] = strip;
            CirclePortrait(strip, "Places/" + site.Card.Id, 6, 3, 34);
            SafeText(strip, "Site · " + site.Card.Name, 48, 0, 220, h, 19, target ? Gold : White, true, TextAnchor.MiddleLeft, 11);
            SafeText(strip, CompactRules(RulesText.Describe(site.Card)), 270, 0, w - 280, h, 14, Muted, false, TextAnchor.MiddleLeft, 9);
            CardTarget(strip, site.Card.Id, target ? (Action)(() => CommitMove(choices.First(m => m.Target == site.Key))) : null, false, site);
        }

        private string UnitArt(string id) => Hero.All.Any(hero => hero.Id == id) ? HeroArt(id) : "Units/" + (id == "X11" ? "X02" : id);

        private void UnitTile(Transform parent, Piece unit, float x, float y, Move[] choices)
        {
            var tile = Box(parent, unit.Card.Name + " " + unit.Key, x, y, TileW, TileH);
            battlePieces[unit.Key] = tile;
            bool mine = unit.Side == 0;
            bool target = choices.Any(m => m.Target == unit.Key);
            bool picked = choosing && selected == ActionKind.Attack && cursor == unit.Key;
            bool ready = mine && canAct && !unit.Spent && !choosing;
            Color tint = unit.Spent ? new Color(.62f, .62f, .68f, 1f) : Color.white;
            if (picked || target || ready)
            {
                var glow = Pic(tile, Foundry + "UnitTokenGlow", -14, -14, TileW + 28, TileH + 28,
                    target ? (mine ? AllySage : EnemyCopper) : picked ? Gold : new Color(Brass.r, Brass.g, Brass.b, .55f));
            }
            var disc = Box(tile, "Portrait", TileW * .15f, TileH * .1f, TileW * .70f, TileH * .78f);
            var backing = disc.gameObject.AddComponent<Image>();
            var circle = CircleSprite();
            if (circle != null) backing.sprite = circle;
            backing.color = new Color(.05f, .07f, .1f, 1f);
            backing.raycastTarget = false;
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            FitSafe(disc, UnitArt(unit.Card.Id), -8, -4, disc.sizeDelta.x + 16, disc.sizeDelta.y + 8, tint);
            Pic(tile, Foundry + "UnitToken", 0, 0, TileW, TileH, tint);
            int medal = 30;
            Medal(tile, "Attack", unit.Attack.ToString(), -6, TileH - 62, 62, medal);
            Medal(tile, "Health", unit.Hp.ToString(), TileW - 56, TileH - 62, 62, medal);
            OutlinedText(tile, unit.Card.Name, 52, TileH - 40, TileW - 104, 24, 15, unit.Card.Legendary ? Gold : White, true, TextAnchor.MiddleCenter, 8);
            if (unit.Shield) Pic(tile, Foundry + "Shield", TileW - 44, 0, 44, 44);
            if (unit.Guard) OutlinedText(tile, "Guard", 2, 2, 62, 22, 14, Frost, true, TextAnchor.MiddleLeft, 9);
            else if (unit.Card.Rush) OutlinedText(tile, "Rush", 2, 2, 62, 22, 14, Gold, true, TextAnchor.MiddleLeft, 9);
            if (unit.Spent) OutlinedText(tile, "zzz", TileW / 2f - 30, 4, 60, 22, 15, Muted, false, TextAnchor.MiddleCenter, 9);
            if (target) Pic(tile, Foundry + "TargetNode", TileW / 2f - 36, TileH / 2f - 36, 72, 72);
            Action act = null;
            if (target) act = () => CommitMove(choices.First(m => m.Target == unit.Key));
            else if (mine) act = () => SelectPiece(unit);
            CardTarget(tile, unit.Card.Id, act, false, unit);
            if (mine && ready) BindUnitDrag(tile, unit);
        }

        // ---------------------------------------------------------------- heroes

        private void PlayerBadge(int side, float y, string name, Move[] choices)
        {
            var pilot = match.Players[side];
            var badge = Box(root, "Hero " + side, 30, y, 240, 330);
            heroBadges[side] = badge;
            bool strike = side == 1 && choices.Any(m => m.Kind == ActionKind.Attack && m.Target == 0);
            var portrait = CirclePortrait(badge, HeroArt(pilot.Hero), 35, 0, 170, Foundry + "HeroFrame");
            if (strike)
            {
                Pic(badge, Foundry + "TargetNode", 70, 35, 100, 100);
                var move = choices.First(m => m.Kind == ActionKind.Attack && m.Target == 0);
                Click(portrait, () => CommitMove(move)).transition = Selectable.Transition.None;
                SafeText(badge, "Strike the Bastion", 0, -22, 240, 24, 19, EnemyCopper, true, TextAnchor.MiddleCenter, 11);
            }
            else if (side == 1)
            {
                Click(portrait, null).transition = Selectable.Transition.None;
            }
            Medal(badge, "Health", pilot.Hp.ToString(), 148, 108, 70, 30);
            Pic(badge, Foundry + "Nameplate", 10, 172, 220, 32);
            OutlinedText(badge, name, 14, 172, 212, 32, 20, White, true, TextAnchor.MiddleCenter, 11);
            int cap = Mathf.Min(10, match.Round);
            for (int i = 0; i < cap; i++)
                Pic(badge, Foundry + (i < pilot.Charge ? "ChargePipOn" : "ChargePipOff"), 18 + i * 20.5f, 212, 20, 20);
            Label(badge, "Charge " + pilot.Charge + "/" + cap, 0, 234, 240, 24, 17, Frost, false, TextAnchor.MiddleCenter);
            for (int i = 0; i < 6; i++)
                Pic(badge, Foundry + (i < pilot.Seals ? "SealLit" : "SealDim"), 18 + i * 35, 262, 33, 33);
            if (side == 0) DrawHeroPower(badge, 0, 300);
        }

        private void DrawHeroPower(RectTransform badge, float x, float y)
        {
            var info = Hero.Info(match.Players[0].Hero);
            bool usable = canAct && match.Legal(0).Any(m => m.Kind == ActionKind.Power);
            bool picked = choosing && selected == ActionKind.Power;
            var rect = Box(root, "Hero power", 30 + x, 536 + y + 4, 86, 86);
            var image = rect.gameObject.AddComponent<RawImage>();
            string baseArt = usable ? "HeroPower" : "HeroPowerDisabled";
            image.texture = Texture(Foundry + baseArt);
            if (image.texture == null) image.color = usable ? Brass : Quiet;
            Click(rect, () => SelectPower(), true, image).transition = Selectable.Transition.None;
            var relay = rect.gameObject.AddComponent<PointerRelay>();
            relay.Enter = () =>
            {
                if (!usable) return;
                var hover = Texture(Foundry + "HeroPowerHover");
                if (hover != null) image.texture = hover;
                UiSound("hover");
            };
            relay.Exit = () => { var t = Texture(Foundry + baseArt); if (t != null) image.texture = t; };
            if (picked) Pic(root, Foundry + "UnitTokenGlow", 20 + x, 536 + y - 6, 106, 106, Gold);
            Pic(root, Foundry + "HeroPowerCost", 30 + x + 52, 536 + y + 56, 40, 40);
            Label(root, "2", 30 + x + 52, 536 + y + 56, 40, 40, 20, White, true, TextAnchor.MiddleCenter);
            SafeText(root, info.Power, 126 + x, 536 + y + 4, 140, 40, 18, usable ? Gold : Quiet, true, TextAnchor.MiddleLeft, 10);
            SafeText(root, info.Description, 126 + x, 536 + y + 42, 150, 52, 13, Muted, false, TextAnchor.UpperLeft, 9);
        }

        // ---------------------------------------------------------------- selection

        private Move[] PresentMoves(List<Move> legal)
        {
            if (!choosing) return Array.Empty<Move>();
            return legal.Where(move => selected == ActionKind.Attack
                ? (move.Kind == ActionKind.Attack || move.Kind == ActionKind.March) && move.Slot == cursor
                : move.Kind == selected && (selected == ActionKind.Power || move.Slot == cursor)).ToArray();
        }

        private bool IsOmenTimingMove(Move move) => move.Kind == ActionKind.Play && match.Players[0].Hand[move.Slot] == "L11";
        private string MoveLabel(Move move) => move.Kind.ToString();

        private void CancelSelection(bool sound = true)
        {
            if (!choosing) return;
            choosing = false; selected = ActionKind.Pass; cursor = -1;
            if (sound) UiSound("close");
            Render();
        }

        private void CommitMove(Move move)
        {
            choosing = false; selected = ActionKind.Pass; cursor = -1; status = "";
            Command(move);
        }

        private void SelectHand(int slot)
        {
            if (!canAct) return;
            var options = match.Legal(0).Where(m => m.Kind == ActionKind.Play && m.Slot == slot).ToArray();
            if (options.Length == 0) { UiSound("deny"); status = "Not enough Charge or no legal target."; Render(); return; }
            if (choosing && selected == ActionKind.Play && cursor == slot) { CancelSelection(); return; }
            if (options.Length == 1 && options[0].Lane < 0 && options[0].Target == 0) { CommitMove(options[0]); return; }
            selected = ActionKind.Play; cursor = slot; choosing = true;
            UiSound("card-select");
            Render();
        }

        private void SelectPower()
        {
            if (!canAct) return;
            if (choosing && selected == ActionKind.Power) { CancelSelection(); return; }
            if (!match.Legal(0).Any(m => m.Kind == ActionKind.Power)) { UiSound("deny"); return; }
            selected = ActionKind.Power; cursor = -1; choosing = true;
            UiSound("card-select");
            Render();
        }

        private void SelectPiece(Piece piece)
        {
            if (piece == null || piece.Side != 0 || !canAct) return;
            if (choosing && selected == ActionKind.Attack && cursor == piece.Key) { CancelSelection(); return; }
            bool any = match.Legal(0).Any(m => (m.Kind == ActionKind.Attack || m.Kind == ActionKind.March) && m.Slot == piece.Key);
            if (!any) { UiSound("deny"); return; }
            selected = ActionKind.Attack; cursor = piece.Key; choosing = true;
            UiSound("card-select");
            Render();
        }

        private void SelectInfo(string id) { inspected = id; UiSound("open"); Render(); }

        /// <summary>Marks an area as a card: hover preview, right-click inspect and an optional click action.</summary>
        private void CardTarget(RectTransform area, string id, Action click, bool fromHand = false, Piece piece = null, bool canPlay = false)
        {
            var button = Click(area, click);
            button.transition = Selectable.Transition.None;
            BindPreview(area, id, piece);
        }

        // ---------------------------------------------------------------- hand

        private void DrawEnemyHand()
        {
            int n = Mathf.Min(8, match.Players[1].Hand.Count);
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0 : i - (n - 1) / 2f;
                var back = Pic(root, CardBackArt, 960 - 40 + t * 46, -26 + Mathf.Abs(t) * 3f, 80, 113);
                back.rectTransform.pivot = new Vector2(.5f, .5f);
                back.rectTransform.anchoredPosition = new Vector2(960 + t * 46, -(-26 + Mathf.Abs(t) * 3f + 56f));
                back.rectTransform.localRotation = Quaternion.Euler(0, 0, 180 + t * 3f);
            }
        }

        private void DrawHand(List<Move> legal)
        {
            var hand = match.Players[0].Hand;
            int n = hand.Count;
            handPoints.Clear();
            float step = n <= 1 ? 0 : Mathf.Min(150f, (1160f - HandCardW) / (n - 1));
            float x0 = 960 - (HandCardW + step * Mathf.Max(0, n - 1)) / 2f;
            for (int i = 0; i < n; i++)
            {
                int slot = i;
                var card = Catalog.Get(hand[i]);
                bool playable = canAct && legal.Any(m => m.Kind == ActionKind.Play && m.Slot == slot);
                bool picked = choosing && selected == ActionKind.Play && cursor == slot;
                float t = n == 1 ? 0 : i - (n - 1) / 2f;
                float tilt = -t * 2.3f;
                float y = HandBaseY + t * t * 3.2f - (picked ? 46 : 0);
                var rect = PaperCard(card, x0 + i * step, y, HandCardW, HandCardH, match.Cost(0, card), playable || picked, root, false);
                if (!playable && canAct) ColorBox(rect, "Unaffordable", 0, 0, rect.sizeDelta.x, rect.sizeDelta.y, new Color(0, 0, 0, .38f));
                var size = rect.sizeDelta;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(x0 + i * step + size.x / 2f, -(y + size.y / 2f));
                rect.localRotation = Quaternion.Euler(0, 0, picked ? 0 : tilt);
                handPoints[slot] = rect;
                Click(rect, () => SelectHand(slot)).transition = Selectable.Transition.None;
                BindHandHover(rect, card.Id, tilt, picked);
                if (playable) BindCardDrag(rect, slot);
            }
        }

        private void BindHandHover(RectTransform card, string id, float restTilt, bool pinned)
        {
            var relay = card.gameObject.GetComponent<PointerRelay>() ?? card.gameObject.AddComponent<PointerRelay>();
            Vector2 rest = card.anchoredPosition;
            float height = card.sizeDelta.y;
            relay.Enter = () =>
            {
                UiSound("hover");
                if (pinned || dragLayer != null) return;
                card.SetAsLastSibling();
                card.localRotation = Quaternion.identity;
                card.localScale = Vector3.one * 1.55f;
                card.anchoredPosition = new Vector2(rest.x, -(1076 - height * 1.55f / 2f));
            };
            relay.Exit = () =>
            {
                if (pinned) return;
                card.localScale = Vector3.one;
                card.anchoredPosition = rest;
                card.localRotation = Quaternion.Euler(0, 0, restTilt);
            };
            relay.RightClick = _ => { inspected = id; UiSound("open"); Render(); };
        }

        private void BindCardHover(RectTransform card, string id) => BindHandHover(card, id, 0, false);

        // ---------------------------------------------------------------- controls

        private void DrawControls(List<Move> legal, Move[] choices)
        {
            TableButton("Menu", 1650, 16, 240, 54, () => { settings = true; UiSound("open"); Render(); });
            TableButton("?", 1580, 16, 56, 54, () => OpenRules());
            if (choosing)
            {
                string hint = selected == ActionKind.Play ? "Choose where to play it" : selected == ActionKind.Power ? "Choose a target for your hero power" : "Choose a target, or a lane to march to";
                ChromePanel(root, "Hint", 560, 100, 800, 44, "Panel");
                SafeText(root, hint + " · right-click to cancel", 560, 100, 800, 44, 22, Gold, true, TextAnchor.MiddleCenter, 13);
                TableButton("Cancel", 1650, 664, 240, 54, () => CancelSelection());
                if (choices.Any(m => m.Target == 0 && m.Lane < 0 && m.Kind != ActionKind.Attack))
                {
                    var confirm = choices.First(m => m.Target == 0 && m.Lane < 0 && m.Kind != ActionKind.Attack);
                    TableButton(selected == ActionKind.Power ? "Invoke now" : "Play without target", 760, 740, 400, 58, () => CommitMove(confirm), true, true);
                }
            }
            else if (!string.IsNullOrEmpty(status) && canAct)
                SafeText(root, status, 560, 100, 800, 44, 20, Muted, false, TextAnchor.MiddleCenter, 12);
            bool idle = canAct && !legal.Any(m => m.Kind != ActionKind.Pass);
            string label = !canAct ? (match.Winner >= 0 ? "Finished" : "Enemy Turn") : match.AwaitingSecondPass ? "End Round" : (idle ? "End Turn" : "Pass");
            if (canAct) SafeText(root, "Space pass · 1-8 cards · Q power · R rules", 1640, 806, 260, 22, 13, Quiet, false, TextAnchor.MiddleCenter, 9);
            EndTurnButton(label, 1650, 726, 240, 75, () => { if (choosing) CancelSelection(false); Command(new Move(ActionKind.Pass)); }, canAct, idle, root);
        }

        private void EndTurnWithAutoCombat()
        {
            var attack = BestAutoAttack(match.Legal(0));
            Command(attack != null ? attack : new Move(ActionKind.Pass));
        }

        private static Move BestAutoAttack(List<Move> legal) =>
            legal.Where(m => m.Kind == ActionKind.Attack).OrderByDescending(m => m.Target != 0).FirstOrDefault();

        private int Measure(string value, int fontSize, int width) => 40;

        private static void CardSize(ref int w, ref int h)
        {
            w = Mathf.Max(48, w);
            h = Mathf.RoundToInt(w * (590f / 416f));
        }

        private static float CardX(float logical, float w) => logical * (w / 416f);
        private static float CardY(float logical, float h) => logical * (h / 590f);

        private string OrderArt(string id) => "Orders/" + id;
        private string PlaceArt(string id) => "Places/" + id;

        // ---------------------------------------------------------------- opening hand

        private void OpeningPanel()
        {
            ViewportShade("Opening shade", new Color(0, 0, 0, .72f));
            OutlinedText(root, "Your opening hand", 360, 120, 1200, 70, 54, Gold, true, TextAnchor.MiddleCenter, 28);
            SafeText(root, "Click any cards you want to replace, then keep the rest.", 360, 190, 1200, 40, 26, White, false, TextAnchor.MiddleCenter, 16);
            var hand = match.Players[0].Hand;
            const int w = 250;
            int h = Mathf.RoundToInt(w * 590f / 416f);
            float total = hand.Count * (w + 28) - 28;
            float x0 = 960 - total / 2f;
            for (int i = 0; i < hand.Count; i++)
            {
                int slot = i;
                bool swap = mulligan.Contains(i);
                var rect = PaperCard(Catalog.Get(hand[i]), x0 + i * (w + 28), swap ? 270 : 250, w, h, Catalog.Get(hand[i]).Cost, !swap, root, false, null, swap);
                Click(rect, () =>
                {
                    UiSound("card-select");
                    if (!mulligan.Remove(slot)) mulligan.Add(slot);
                    Render();
                }).transition = Selectable.Transition.None;
                BindPreview(rect, hand[i], null);
            }
            string keep = mulligan.Count == 0 ? "Keep Hand" : "Replace " + mulligan.Count + (mulligan.Count == 1 ? " Card" : " Cards");
            TableButton(keep, 760, 900, 400, 80, () => FinishOpening(), true, true);
        }

        private void FinishOpening()
        {
            var replace = mulligan.ToList();
            mulligan.Clear();
            UiSound("card-shuffle");
            if (!match.ChooseOpeningHand(0, replace)) return;
            if (!match.ChooseOpeningHand(1, Array.Empty<int>()) && match.Opening) return;
            Render();
            ShowPhaseBanner(CardAnimationLayer(), "Round 1", match.Turn == 0 ? "You move first" : "The enemy moves first", Brass, 1.0f);
            announcedRound = match.Round;
            Schedule();
        }

        /// <summary>Keyboard play: 1-8 pick a hand card, Q the hero power, Space or Enter passes, R opens the rules.</summary>
        private void BattleKeys(UnityEngine.InputSystem.Keyboard keyboard)
        {
            if (page != Page.Battle || match == null || settings || showRules || inspected != null || presenting) return;
            if (match.Opening)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) FinishOpening();
                return;
            }
            if (keyboard.rKey.wasPressedThisFrame) { OpenRules(); return; }
            if (!canAct) return;
            var digits = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key,
                keyboard.digit5Key, keyboard.digit6Key, keyboard.digit7Key, keyboard.digit8Key };
            for (int i = 0; i < digits.Length; i++)
                if (digits[i].wasPressedThisFrame && i < match.Players[0].Hand.Count) { SelectHand(i); return; }
            if (keyboard.qKey.wasPressedThisFrame) { SelectPower(); return; }
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                if (choosing) CancelSelection(false);
                Command(new Move(ActionKind.Pass));
            }
        }
    }
}
