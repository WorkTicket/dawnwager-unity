using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Battle UI cleared. Match rules live in Core/Match.cs.</summary>
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
        private bool showRules, presenting, reduceMotion;
        private const int TableY = 160;
        private const int LaneWidth = 620;
        private const float EnemyHeroY = 48f;
        private const float AllyHeroY = 600f;
        private const float HandBaseY = 860f;
        private const int HandCardW = 180;
        private const int HandCardH = 255;

        private void Battle() { }

        private void DrawForecast() { }
        private void BoardLane(int lane, Move[] choices) { }
        private string UnitArt(string id) => Hero.All.Any(hero => hero.Id == id) ? HeroArt(id) : "Units/" + (id == "X11" ? "X02" : id);
        private void UnitTile(Transform parent, Piece unit, int x, int y, Move[] choices) { }
        private void PlayerBadge(int side, float y, string name, Move[] choices) { }

        private Move[] PresentMoves(List<Move> legal)
        {
            if (!choosing) return Array.Empty<Move>();
            return legal.Where(move => selected == ActionKind.Attack
                ? (move.Kind == ActionKind.Attack || move.Kind == ActionKind.March) && move.Slot == cursor
                : move.Kind == selected && (selected == ActionKind.Power || move.Slot == cursor)).ToArray();
        }

        private bool IsOmenTimingMove(Move move) => move.Kind == ActionKind.Play && match.Players[0].Hand[move.Slot] == "L11";
        private string MoveLabel(Move move) => move.Kind.ToString();
        private void SelectInfo(string id) { }
        private void SelectPiece(Piece piece) { }
        private void DrawControls(List<Move> legal, Move[] choices) { }
        private void EndTurnWithAutoCombat() { }

        private static Move BestAutoAttack(List<Move> legal) =>
            legal.Where(m => m.Kind == ActionKind.Attack).OrderByDescending(m => m.Target != 0).FirstOrDefault();

        private RectTransform ScrollPanel(Transform parent, int x, int y, int w, int h, int contentHeight) =>
            Box(parent, "Scroll", x, y, w, Mathf.Max(h, contentHeight));

        private int Measure(string value, int fontSize, int width) => 40;

        private void CardTarget(RectTransform area, string id, Action click, bool fromHand = false, Piece piece = null, bool canPlay = false) { }

        private void DrawHand(List<Move> legal) { }
        private void BindHandHover(RectTransform card, float restTilt, bool pinned) { }

        private static void CardSize(ref int w, ref int h)
        {
            w = Mathf.Max(48, w);
            h = Mathf.RoundToInt(w * (590f / 416f));
        }

        private static float CardX(float logical, float w) => logical * (w / 416f);
        private static float CardY(float logical, float h) => logical * (h / 590f);

        private string OrderArt(string id) => "Orders/" + id;
        private string PlaceArt(string id) => "Places/" + id;

        /// <summary>Kept for Core-adjacent callers; draws nothing interactive.</summary>
        private RectTransform PaperCard(Card card, float x, float y, int w, int h, int cost, bool highlighted, Transform parent, bool rules, Piece piece = null, bool mulliganSelect = false)
        {
            CardSize(ref w, ref h);
            return Box(parent, card.Name, x, y, w, h);
        }

        private void OpeningPanel() { }
        private void OpenRules() { showRules = false; }
        private void RulesOverlay() { }
    }
}
