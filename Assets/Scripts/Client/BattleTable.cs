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

        private static readonly int[] LaneX = { 270, 990 };
        private readonly Dictionary<int, RectTransform> battlePieces = new Dictionary<int, RectTransform>();
        private readonly RectTransform[] heroBadges = new RectTransform[2];
        private bool showRules, presenting, reduceMotion, canAct;
        private const int TableY = 158;
        private const int LaneWidth = 660;
        private const float HandBaseY = 777f;
        private const int HandCardW = 204;
        private const int HandCardH = 289;
        private const int TileW = 176, TileH = 220;
        private const float EnemyRowY = 302f, AllyRowY = 530f, EnemySiteY = 262f, AllySiteY = 744f;
        private const float EnemyFoot = 478f, AllyFoot = 742f, EnemyScale = .9f, AllyScale = 1f;

        private static float RowY(int side) => side == 1 ? EnemyRowY : AllyRowY;
        private static float SlotCx(int lane, int index) => LaneX[lane] + 26 + (LaneWidth - 52) / 3f * (index + .5f);
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

            Gradient(root, "Vignette top", 0, 0, 1920, 220, new Color(.016f, .024f, .04f, .55f), new Color(.016f, .024f, .04f, 0f), true);
            Gradient(root, "Tray fade", 0, 760, 1920, 320, new Color(.016f, .024f, .04f, 0f), new Color(.016f, .024f, .04f, .9f), true);

            DrawEnemyHand();
            DrawPiles();
            BoardLane(0, choices);
            BoardLane(1, choices);
            PlayerBadge(1, 16, Hero.Info(match.Players[1].Hero).Name, choices);
            PlayerBadge(0, 452, Hero.Info(match.Players[0].Hero).Name, choices);
            DrawChargeRow();
            DrawHeroPower();
            DrawLeftHud();
            DrawHand(legal);
            DrawControls(legal, choices);
            if (match.Opening) OpeningPanel();
            if (tutorialMatch) DrawTutorialCoach();
        }

        // ---------------------------------------------------------------- left HUD (text only)

        private void DrawLeftHud()
        {
            TextLink("Menu", 40, 26, 190, 30, 22, new Color32(207, 213, 216, 255), () => { settings = true; UiSound("open"); Render(); });
            TextLink("Rules", 40, 60, 190, 26, 17, Muted, () => OpenRules(), false);
            OutlinedText(root, "Round " + Mathf.Max(1, match.Round), 40, 350, 260, 50, 40, Gold, true, TextAnchor.MiddleLeft, 24);
            var fresh = match.Prophecies.FirstOrDefault(p => !p.Spent);
            string line = fresh == null ? "All prophecies spent" : (match.Round == fresh.Round ? "Prophecy now" : "Prophecy, round " + fresh.Round);
            var note = SafeText(root, line + (fresh != null ? "\n" + fresh.Card.Name : "") +
                "\n" + (match.Contested < 0 ? "No contested lane" : Lane(match.Contested) + " lane contested"), 40, 402, 270, 80, 16, Muted, false, TextAnchor.UpperLeft, 11);
            if (fresh != null)
            {
                string id = fresh.Card.Id;
                Click(note.rectTransform, () => { inspected = id; UiSound("open"); Render(); }, true, note).transition = Selectable.Transition.None;
                BindPreview(note.rectTransform, id, null);
            }
            if (match.Winner < 0 && !match.Opening)
                OutlinedText(root, match.Turn == 0 ? "Your turn" : "Enemy turn", 40, 496, 260, 34, 24, match.Turn == 0 ? AllySage : EnemyCopper, true, TextAnchor.MiddleLeft, 14);
            var lines = match.Log.Skip(Mathf.Max(0, match.Log.Count - 4)).Select(b => b.Text).ToArray();
            for (int i = 0; i < lines.Length; i++)
                SafeText(root, lines[i], 40, 826 + i * 24, 290, 24, 15, i == lines.Length - 1 ? White : Muted, false, TextAnchor.MiddleLeft, 10);
        }

        // ---------------------------------------------------------------- lanes (text labels only)

        private void BoardLane(int lane, Move[] choices)
        {
            int lx = LaneX[lane];
            bool contested = match.Contested == lane;
            int mine = match.SealPower(0, lane), theirs = match.SealPower(1, lane);
            var lead = mine > theirs ? AllySage : mine < theirs ? EnemyCopper : Muted;
            OutlinedText(root, Lane(lane).ToUpperInvariant(), lx, 170, LaneWidth, 42, 32, contested ? Gold : new Color32(228, 232, 234, 255), true, TextAnchor.MiddleCenter, 20);
            var site0 = match.Board.FirstOrDefault(p => p.Card.Kind == Kind.Site && p.Lane == lane && p.Side == 0);
            var site1 = match.Board.FirstOrDefault(p => p.Card.Kind == Kind.Site && p.Lane == lane && p.Side == 1);
            string extra = (contested ? " · Contested" : "");
            var power = OutlinedText(root, "Seal power " + mine + " – " + theirs + extra, lx, 212, LaneWidth, 26, 17, lead, false, TextAnchor.MiddleCenter, 12);
            var zone = choices.FirstOrDefault(m => m.Target == 0 && m.Lane == lane && m.Kind != ActionKind.Attack);
            if (zone != null)
            {
                string verb = zone.Kind == ActionKind.March ? "March to " : zone.Kind == ActionKind.Power ? "Invoke in " : "Play in ";
                TableButton("▶ " + verb + Lane(lane), lx + (LaneWidth - 340) / 2f, 118, 340, 46, () => CommitMove(zone), true, true);
            }
            for (int side = 1; side >= 0; side--)
            {
                float foot = side == 1 ? EnemyFoot : AllyFoot, sc = side == 1 ? EnemyScale : AllyScale;
                var units = match.Units(side, lane).ToList();
                for (int i = 0; i < units.Count && i < 3; i++)
                    UnitTile(root, units[i], SlotCx(lane, i), foot, sc, choices);
                DrawSite(side == 0 ? site0 : site1, lx + (LaneWidth - 360) / 2f, side == 1 ? EnemySiteY : AllySiteY, choices);
            }
        }

        private void DrawSite(Piece site, float x, float y, Move[] choices)
        {
            if (site == null) return;
            const float w = 360, h = 34;
            bool target = choices.Any(m => m.Target == site.Key);
            var strip = ChromePanel(root, site.Card.Name, x, y, w, h, target ? "PanelGold" : "Panel");
            battlePieces[site.Key] = strip;
            SafeText(strip, "Site · " + site.Card.Name + " — " + CompactRules(RulesText.Describe(site.Card)), 14, 0, w - 28, h, 15, target ? Gold : White, false, TextAnchor.MiddleLeft, 9);
            CardTarget(strip, site.Card.Id, target ? (Action)(() => CommitMove(choices.First(m => m.Target == site.Key))) : null, false, site);
        }

        private string UnitArt(string id) => Hero.All.Any(hero => hero.Id == id) ? "Heroes/" + id : "Units/" + (id == "X11" ? "X02" : id);

        private void UnitTile(Transform parent, Piece unit, float cx, float foot, float sc, Move[] choices)
        {
            float tx = cx - 88 * sc, ty = foot - 200 * sc + 4;
            var tile = Box(parent, unit.Card.Name + " " + unit.Key, tx, ty, TileW * sc, TileH * sc);
            battlePieces[unit.Key] = tile;
            bool mine = unit.Side == 0;
            bool target = choices.Any(m => m.Target == unit.Key);
            bool picked = choosing && selected == ActionKind.Attack && cursor == unit.Key;
            bool ready = mine && canAct && !unit.Spent && !choosing;
            Color tint = unit.Spent ? new Color(.62f, .62f, .68f, 1f) : Color.white;
            Color glow = target ? (mine ? AllySage : EnemyCopper) : picked ? Gold : new Color(1f, .88f, .5f, 1f);
            // Soft ground shadow, warmed when the unit can act.
            Shade(tile, -12 * sc, 176 * sc - 4, 200 * sc, 46 * sc, new Color(0, 0, 0, .75f));
            if (picked || target || ready) Shade(tile, -4 * sc, 150 * sc, 184 * sc, 52 * sc, new Color(glow.r, glow.g, glow.b, target || picked ? .85f : .5f));
            var art = Box(tile, "Art", 0, 0, TileW * sc, 184 * sc);
            CutFit(art, UnitArt(unit.Card.Id), TileW * sc, 184 * sc, tint);
            Medal(tile, "Attack", unit.Attack.ToString(), -2 * sc, 156 * sc, 58 * sc, Mathf.RoundToInt(28 * sc));
            Medal(tile, "Health", unit.Hp.ToString(), 120 * sc, 156 * sc, 58 * sc, Mathf.RoundToInt(28 * sc));
            Pic(tile, Foundry + "UnitPlate", 44 * sc, 180 * sc, 88 * sc, 30 * sc, new Color(1, 1, 1, .92f));
            OutlinedText(tile, unit.Card.Name, 50 * sc, 182 * sc, 76 * sc, 26 * sc, Mathf.RoundToInt(14 * sc), unit.Card.Legendary ? Gold : White, true, TextAnchor.MiddleCenter, 8);
            if (unit.Shield) Pic(tile, Foundry + "Shield", (TileW - 44) * sc, 0, 44 * sc, 44 * sc);
            if (unit.Guard) OutlinedText(tile, "Guard", 2, 2, 62, 22, 14, Frost, true, TextAnchor.MiddleLeft, 9);
            else if (unit.Card.Rush) OutlinedText(tile, "Rush", 2, 2, 62, 22, 14, Gold, true, TextAnchor.MiddleLeft, 9);
            if (unit.Spent) OutlinedText(tile, "zzz", TileW * sc / 2f - 30, 4, 60, 22, 15, Muted, false, TextAnchor.MiddleCenter, 9);
            if (target) Pic(tile, Foundry + "TargetNode", TileW * sc / 2f - 36, TileH * sc / 2f - 56, 72, 72);
            Action act = null;
            if (target) act = () => CommitMove(choices.First(m => m.Target == unit.Key));
            else if (mine) act = () => SelectPiece(unit);
            CardTarget(tile, unit.Card.Id, act, false, unit);
            if (mine && ready) BindUnitDrag(tile, unit);
        }

        // ---------------------------------------------------------------- heroes (cut-outs on the right)

        private void PlayerBadge(int side, float y, string name, Move[] choices)
        {
            var pilot = match.Players[side];
            var badge = Box(root, "Hero " + side, 1700, y, 210, 270);
            heroBadges[side] = badge;
            bool strike = side == 1 && choices.Any(m => m.Kind == ActionKind.Attack && m.Target == 0);
            Shade(badge, 10, 176, 170, 34, new Color(0, 0, 0, .8f));
            var portrait = Box(badge, "Portrait", 0, 0, 190, 190);
            CutFit(portrait, "Heroes/" + pilot.Hero, 190, 190, Color.white);
            if (!reduceMotion) StartCoroutine(Breathe(portrait));
            if (strike)
            {
                Pic(badge, Foundry + "TargetNode", 45, 45, 100, 100);
                var move = choices.First(m => m.Kind == ActionKind.Attack && m.Target == 0);
                Click(portrait, () => CommitMove(move)).transition = Selectable.Transition.None;
                SafeText(badge, "Strike the Bastion", 0, 150, 190, 24, 19, EnemyCopper, true, TextAnchor.MiddleCenter, 11);
            }
            else if (side == 1) Click(portrait, null).transition = Selectable.Transition.None;
            Medal(badge, "Health", pilot.Hp.ToString(), 122, 126, 78, 38);
            OutlinedText(badge, name, 0, 204, 190, 32, 24, White, true, TextAnchor.MiddleCenter, 14);
            for (int i = 0; i < 6; i++)
            {
                var d = ColorBox(badge, "Seal", 52 + i * 17 + (i * 0), 245, 10, 10, i < pilot.Seals ? Gold : new Color(1, 1, 1, .18f));
                d.pivot = new Vector2(.5f, .5f);
                d.localRotation = Quaternion.Euler(0, 0, 45);
            }
            SafeText(badge, "Deck " + pilot.Pile.Count + " · Hand " + pilot.Hand.Count, 0, 256, 190, 20, 14, Muted, false, TextAnchor.MiddleCenter, 10);
        }

        private void DrawChargeRow()
        {
            var pilot = match.Players[0];
            int cap = Mathf.Max(1, Mathf.Min(10, match.Round));
            float size = Mathf.Min(34f, (210f - 6f * (cap - 1)) / cap);
            for (int i = 0; i < cap; i++)
                Pic(root, Foundry + (i < pilot.Charge ? "ChargePipOn" : "ChargePipOff"), 1700 + i * (size + 6), 764, size, size);
            SafeText(root, "CHARGE  " + pilot.Charge + " / " + cap, 1700, 764 + size + 6, 220, 24, 17, Frost, true, TextAnchor.MiddleLeft, 11);
        }

        private void DrawHeroPower()
        {
            var info = Hero.Info(match.Players[0].Hero);
            bool usable = canAct && match.Legal(0).Any(m => m.Kind == ActionKind.Power);
            bool picked = choosing && selected == ActionKind.Power;
            var rect = Box(root, "Hero power", 1760, 860, 74, 74);
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
            if (picked) Pic(root, Foundry + "UnitTokenGlow", 1750, 850, 94, 94, Gold);
            Medal(root, "Cost", "2", 1810, 910, 34, 18);
            SafeText(root, info.Power, 1700, 938, 210, 26, 19, usable ? Gold : Quiet, true, TextAnchor.MiddleCenter, 11);
            SafeText(root, info.Description, 1700, 964, 210, 52, 13, Muted, false, TextAnchor.UpperCenter, 9);
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
            float step = n <= 1 ? 0 : Mathf.Min(212f, 1060f / (n - 1));
            float x0 = 960 - (HandCardW + step * Mathf.Max(0, n - 1)) / 2f;
            for (int i = 0; i < n; i++)
            {
                int slot = i;
                var card = Catalog.Get(hand[i]);
                bool playable = canAct && legal.Any(m => m.Kind == ActionKind.Play && m.Slot == slot);
                bool picked = choosing && selected == ActionKind.Play && cursor == slot;
                float t = n == 1 ? 0 : i - (n - 1) / 2f;
                float tilt = -t * 2.2f;
                float y = HandBaseY + t * t * 5f - (picked ? 46 : 0);
                var rect = PaperCard(card, x0 + i * step, y, HandCardW, HandCardH, match.Cost(0, card), playable || picked, root, false);
                if (!playable && canAct) Pic(rect, Foundry + "CardShadow", 0, 0, rect.sizeDelta.x, rect.sizeDelta.y, new Color(0, 0, 0, .45f));
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
            relay.Enter = () =>
            {
                UiSound("hover");
                if (pinned || dragLayer != null) return;
                card.SetAsLastSibling();
                card.localRotation = Quaternion.identity;
                card.localScale = Vector3.one * 1.18f;
                card.anchoredPosition = rest + new Vector2(0, 34);
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
            if (choosing)
            {
                string hint = selected == ActionKind.Play ? "Choose where to play it" : selected == ActionKind.Power ? "Choose a target for your hero power" : "Choose a target, or a lane to march to";
                SafeText(root, hint + " · right-click to cancel", 40, 534, 270, 56, 18, Gold, true, TextAnchor.UpperLeft, 12);
                TableButton("Cancel", 40, 728, 200, 44, () => CancelSelection());
                if (choices.Any(m => m.Target == 0 && m.Lane < 0 && m.Kind != ActionKind.Attack))
                {
                    var confirm = choices.First(m => m.Target == 0 && m.Lane < 0 && m.Kind != ActionKind.Attack);
                    TableButton(selected == ActionKind.Power ? "Invoke now" : "Play without target", 40, 598, 260, 46, () => CommitMove(confirm), true, true);
                }
            }
            else if (!string.IsNullOrEmpty(status) && canAct)
                SafeText(root, status, 40, 534, 270, 56, 17, Muted, false, TextAnchor.UpperLeft, 12);
            bool idle = canAct && !legal.Any(m => m.Kind != ActionKind.Pass);
            string label = !canAct ? (match.Winner >= 0 ? "Finished" : "Enemy Turn") : match.AwaitingSecondPass ? "End Round" : (idle ? "End Turn" : "Pass");
            GoldPill(label, 40, 660, 200, 58, () => { if (choosing) CancelSelection(false); Command(new Move(ActionKind.Pass)); }, canAct, idle);
            if (canAct) SafeText(root, "Space pass · 1-8 cards · Q power · R rules", 40, 786, 320, 22, 13, Quiet, false, TextAnchor.MiddleLeft, 9);
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
            const int w = 330;
            int h = Mathf.RoundToInt(w * 590f / 416f);
            float total = hand.Count * (w + 40) - 40;
            float x0 = 960 - total / 2f;
            for (int i = 0; i < hand.Count; i++)
            {
                int slot = i;
                bool swap = mulligan.Contains(i);
                var rect = PaperCard(Catalog.Get(hand[i]), x0 + i * (w + 40), swap ? 250 : 220, w, h, Catalog.Get(hand[i]).Cost, !swap, root, false, null, swap);
                Click(rect, () =>
                {
                    UiSound("card-select");
                    if (!mulligan.Remove(slot)) mulligan.Add(slot);
                    Render();
                }).transition = Selectable.Transition.None;
                BindPreview(rect, hand[i], null);
            }
            string keep = mulligan.Count == 0 ? "Keep Hand" : "Replace " + mulligan.Count + (mulligan.Count == 1 ? " Card" : " Cards");
            TableButton(keep, 760, 940, 400, 80, () => FinishOpening(), true, true);
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
