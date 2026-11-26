using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>
    /// Generated 9-slice chrome for the approved look: dark glass panels with a thin gold edge and corner ticks,
    /// gold pill primary buttons and dark outlined secondary buttons. Slice() prefers these over the old Foundry art.
    /// </summary>
    public sealed partial class Game
    {
        private readonly Dictionary<string, Sprite> styleSprites = new Dictionary<string, Sprite>();
        private static readonly Color GoldEdge = new Color32(201, 165, 92, 255);
        private static readonly Color InkOnGold = new Color32(42, 30, 8, 255);

        private Sprite GeneratedSlice(string art)
        {
            if (styleSprites.TryGetValue(art, out var cached) && cached != null) return cached;
            Sprite made = null;
            switch (art)
            {
                case "Panel": case "HistoryRail": case "ModalFrame":
                    made = MakeSlice(96, 96, 8, new Color(.035f, .05f, .075f, .86f), new Color(.03f, .043f, .065f, .9f), new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, .55f), 2, true, new Vector4(20, 20, 20, 20)); break;
                case "PanelGold":
                    made = MakeSlice(96, 96, 8, new Color(.09f, .075f, .04f, .9f), new Color(.05f, .042f, .025f, .92f), new Color(1f, .85f, .45f, .95f), 2, true, new Vector4(20, 20, 20, 20)); break;
                case "Primary": made = MakePillSprite(Hex("#ffe9a8"), Hex("#e0b24a"), Hex("#b98a2c"), Hex("#5b3d0e")); break;
                case "PrimaryHover": made = MakePillSprite(Hex("#fff3c4"), Hex("#f0c65a"), Hex("#c99a38"), Hex("#6b4a12")); break;
                case "PrimaryPressed": made = MakePillSprite(Hex("#d9b968"), Hex("#b98a2c"), Hex("#8d6620"), Hex("#4a3009")); break;
                case "PrimaryDisabled": made = MakePillSprite(Hex("#6b747c"), Hex("#4d555c"), Hex("#353b41"), Hex("#22272c")); break;
                case "Secondary": made = MakeDarkPill(.85f, .5f); break;
                case "SecondaryHover": made = MakeDarkPill(.95f, .95f); break;
                case "SecondaryPressed": made = MakeDarkPill(.6f, .7f); break;
                case "SecondaryDisabled": made = MakeDarkPill(.5f, .2f); break;
            }
            if (made != null) styleSprites[art] = made;
            return made;
        }

        private Sprite MakeDarkPill(float fill, float edge) =>
            MakeSlice(256, 64, 32, new Color(.05f, .07f, .1f, fill), new Color(.03f, .045f, .065f, fill), new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, edge), 2, false, new Vector4(30, 30, 30, 30));

        private Sprite MakePillSprite(Color top, Color mid, Color bottom, Color outline)
        {
            const int W = 256, H = 64;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            float r = H / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, r, W - r);
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, r));
                    float a = Mathf.Clamp01(r - d + .5f);
                    if (a <= 0) { px[y * W + x] = Color.clear; continue; }
                    var c = Ramp(1f - (y + .5f) / H, top, mid, bottom, .55f);
                    if (d > r - 2f) c = outline;
                    else if (d > r - 4.5f) c = Over(c, new Color(1, .95f, .75f, .5f));
                    c.a = a;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(30, 30, 30, 30));
        }

        private Sprite MakeSlice(int W, int H, float radius, Color top, Color bottom, Color border, float bw, bool ticks, Vector4 slice)
        {
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            var half = new Vector2(W, H) / 2f;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var p = new Vector2(x + .5f, y + .5f);
                    var q = new Vector2(Mathf.Abs(p.x - half.x), Mathf.Abs(p.y - half.y)) - (half - Vector2.one * radius);
                    float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                    float a = Mathf.Clamp01(.5f - d);
                    if (a <= 0) { px[y * W + x] = Color.clear; continue; }
                    var c = Color.Lerp(bottom, top, (y + .5f) / H);
                    if (d > -bw) c = Over(c, border);
                    int tx = x, ty = H - 1 - y; // top-left based
                    if (ticks)
                    {
                        bool tl = tx < 14 && ty < 14 && (tx < 3 || ty < 3);
                        bool br = tx >= W - 14 && ty >= H - 14 && (tx >= W - 3 || ty >= H - 3);
                        if (tl || br) c = Over(c, new Color(1f, .85f, .45f, .95f));
                    }
                    c.a = Mathf.Max(c.a, 0) * a;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, slice);
        }
    }
}
