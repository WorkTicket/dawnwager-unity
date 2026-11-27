using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Fades the bottom of a cut-out bust to transparent so it stands on the board instead of ending in a hard edge.</summary>
    public sealed class BottomFade : BaseMeshEffect
    {
        public float From = .84f, To = .99f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount < 4) return;
            var list = new List<UIVertex>();
            UIVertex v = default;
            for (int i = 0; i < vh.currentVertCount; i++) { vh.PopulateUIVertex(ref v, i); list.Add(v); }
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            UIVertex bl = list[0], tr = list[0];
            foreach (var p in list)
            {
                if (p.position.x < minX) minX = p.position.x;
                if (p.position.x > maxX) maxX = p.position.x;
                if (p.position.y < minY) { minY = p.position.y; bl = p; }
                if (p.position.y > maxY) { maxY = p.position.y; tr = p; }
            }
            Vector2 uvBottom = bl.uv0, uvTop = tr.uv0;
            // Horizontal uv range from the extreme vertices.
            float uMin = float.MaxValue, uMax = float.MinValue;
            foreach (var p in list) { uMin = Mathf.Min(uMin, p.uv0.x); uMax = Mathf.Max(uMax, p.uv0.x); }
            Color32 baseColor = list[0].color;
            vh.Clear();
            float[] ts = { 0f, From, To, 1f };
            float[] alpha = { 1f, 1f, 0f, 0f };
            for (int row = 0; row < ts.Length; row++)
            {
                float y = Mathf.Lerp(maxY, minY, ts[row]);
                float vv = Mathf.Lerp(uvTop.y, uvBottom.y, ts[row]);
                var c = baseColor; c.a = (byte)(baseColor.a * alpha[row]);
                vh.AddVert(new Vector3(minX, y, 0), c, new Vector4(uMin, vv, 0, 0));
                vh.AddVert(new Vector3(maxX, y, 0), c, new Vector4(uMax, vv, 0, 0));
            }
            for (int row = 0; row < ts.Length - 1; row++)
            {
                int a = row * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }
    }

    /// <summary>Procedurally drawn stat badges (gold attack coin, crimson health drop, blue crystal) plus soft shapes.</summary>
    public sealed partial class Game
    {
        private bool badgesBuilt;

        private void BuildBadgeTextures()
        {
            if (badgesBuilt) return;
            badgesBuilt = true;
            // Replace the old medal art everywhere Medal() and the charge pips use it.
            textures[Foundry + "Attack"] = MakeBadge(0, true);
            textures[Foundry + "Health"] = MakeBadge(1, true);
            textures[Foundry + "Cost"] = MakeBadge(2, true);
            textures[Foundry + "ChargePipOn"] = MakeBadge(2, true);
            textures[Foundry + "ChargePipOff"] = MakeBadge(2, false);
            textures["Gen/Shadow"] = MakeShadow();
            textures["Gen/Pill"] = MakePill(true);
            textures["Gen/PillOff"] = MakePill(false);
        }

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            return tex;
        }

        private static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));
        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        private static Color Ramp(float t, Color c0, Color c1, Color c2, float mid)
        {
            t = Mathf.Clamp01(t);
            return t < mid ? Mix(c0, c1, t / mid) : Mix(c1, c2, (t - mid) / (1f - mid));
        }

        private static Color Over(Color under, Color over)
        {
            float a = over.a + under.a * (1 - over.a);
            if (a <= 0) return Color.clear;
            var rgb = (over * over.a + under * under.a * (1 - over.a)) / a;
            return new Color(rgb.r, rgb.g, rgb.b, a);
        }

        private static bool InEllipse(float u, float v, float cx, float cy, float rx, float ry, float rotDeg)
        {
            float r = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            float dx = u - cx, dy = v - cy;
            float x = dx * c + dy * s, y = -dx * s + dy * c;
            return x * x / (rx * rx) + y * y / (ry * ry) <= 1f;
        }

        private static bool InDrop(float u, float v, float k)
        {
            // Teardrop: apex on top, round belly. k shrinks the shape about its centre for rim detection.
            const float cx = .5f, cy = .56f;
            u = cx + (u - cx) / k; v = cy + (v - cy) / k;
            const float bx = .5f, by = .625f, br = .375f, ay = .04f;
            float dx = u - bx, dy = v - by;
            if (dx * dx + dy * dy <= br * br) return true;
            if (v >= ay && v <= by) return Mathf.Abs(u - bx) <= br * Mathf.Pow((v - ay) / (by - ay), 1.15f);
            return false;
        }

        private Texture2D MakeBadge(int kind, bool on)
        {
            const int N = 160, SS = 2;
            var tex = NewTex(N, N);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Color sum = Color.clear;
                    float aSum = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            float u = (x + (sx + .5f) / SS) / N;
                            float v = 1f - (y + (sy + .5f) / SS) / N; // v = 0 at top
                            var c = kind == 0 ? ShadeCoin(u, v) : kind == 1 ? ShadeDrop(u, v) : ShadeGem(u, v, on);
                            sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, 0);
                            aSum += c.a;
                        }
                    float n = SS * SS;
                    float a = aSum / n;
                    px[y * N + x] = a <= 0 ? Color.clear : new Color(sum.r / n / a, sum.g / n / a, sum.b / n / a, a);
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        private static Color ShadeCoin(float u, float v)
        {
            float dx = u - .5f, dy = v - .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > .485f) return Color.clear;
            if (d > .44f) return Hex("#2b1c08");
            float t = Mathf.Sqrt((u - .35f) * (u - .35f) + (v - .28f) * (v - .28f)) / .85f;
            var c = Ramp(t, Hex("#fff0b0"), Hex("#e5a933"), Hex("#8b5a14"), .45f);
            if (Mathf.Abs(d - .375f) < .012f) c = Over(c, new Color(1, 1, 1, .45f));
            if (Mathf.Abs(d - .414f) < .012f) c = Over(c, new Color(.24f, .12f, 0, .55f));
            if (InEllipse(u, v, .39f, .234f, .22f, .11f, 18f)) c = Over(c, new Color(1, 1, 1, .4f));
            return c;
        }

        private static Color ShadeDrop(float u, float v)
        {
            if (!InDrop(u, v, 1f)) return Color.clear;
            if (!InDrop(u, v, .93f)) return Hex("#2c0710");
            float t = Mathf.Sqrt((u - .38f) * (u - .38f) + (v - .4f) * (v - .4f)) / .8f;
            var c = Ramp(t, Hex("#ff9a86"), Hex("#d12c3d"), Hex("#6e0c20"), .5f);
            if (InEllipse(u, v, .34f, .53f, .075f, .15f, -14f)) c = Over(c, new Color(1, 1, 1, .28f));
            return c;
        }

        private static float EdgeDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }

        private static bool InPoly(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        private static readonly Vector2[] HexPoly =
        {
            new Vector2(.5f, .047f), new Vector2(.89f, .266f), new Vector2(.89f, .734f),
            new Vector2(.5f, .953f), new Vector2(.11f, .734f), new Vector2(.11f, .266f)
        };
        private static readonly Vector2[] HexTop = { new Vector2(.5f, .047f), new Vector2(.89f, .266f), new Vector2(.5f, .47f), new Vector2(.11f, .266f) };

        private static Color ShadeGem(float u, float v, bool on)
        {
            var p = new Vector2(u, v);
            if (!InPoly(p, HexPoly)) return Color.clear;
            var inner = new Vector2[HexPoly.Length];
            for (int i = 0; i < HexPoly.Length; i++) inner[i] = new Vector2(.5f, .5f) + (HexPoly[i] - new Vector2(.5f, .5f)) * .9f;
            if (!InPoly(p, inner)) return on ? Hex("#0d2a66") : Hex("#0c1017");
            float t = (u + v) / 2f;
            var c = on ? Ramp(t, Hex("#b8f1ff"), Hex("#3a95e8"), Hex("#1b45a6"), .5f) : Ramp(t, Hex("#4a5667"), Hex("#2c3644"), Hex("#171d27"), .5f);
            if (InPoly(p, HexTop)) c = Over(c, new Color(1, 1, 1, on ? .38f : .12f));
            float line = Mathf.Min(EdgeDist(p, new Vector2(.11f, .266f), new Vector2(.5f, .47f)),
                Mathf.Min(EdgeDist(p, new Vector2(.5f, .47f), new Vector2(.89f, .266f)), EdgeDist(p, new Vector2(.5f, .47f), new Vector2(.5f, .953f))));
            if (line < .008f) c = Over(c, new Color(1, 1, 1, on ? .4f : .12f));
            return c;
        }

        private Texture2D MakeShadow()
        {
            const int N = 64;
            var tex = NewTex(N, N);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = new Vector2((x + .5f) / N - .5f, (y + .5f) / N - .5f).magnitude * 2f;
                    float a = Mathf.Clamp01(1f - d / .7f);
                    px[y * N + x] = new Color(1, 1, 1, a * a * (3 - 2 * a));
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        private Texture2D MakePill(bool on)
        {
            const int W = 256, H = 64;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            float r = H / 2f;
            Color top = on ? Hex("#ffe9a8") : Hex("#6b747c"), mid = on ? Hex("#e0b24a") : Hex("#4d555c"), bot = on ? Hex("#b98a2c") : Hex("#353b41");
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, r, W - r), dist = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, r));
                    if (dist > r) { px[y * W + x] = Color.clear; continue; }
                    float a = Mathf.Clamp01(r - dist + .5f);
                    var c = Ramp(1f - (y + .5f) / H, top, mid, bot, .55f);
                    if (dist > r - 2.5f) c = Over(c, new Color(1, .93f, .67f, on ? .55f : .25f));
                    c.a = a;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        // ------------------------------------------------------------ cut-out helpers

        /// <summary>Places a transparent cut-out bust so its opaque pixels fill W x H, centred, with a soft bottom fade.</summary>
        private RawImage CutFit(Transform parent, string key, float W, float H, Color tint, bool fade = true)
        {
            if (key.StartsWith("Heroes/", System.StringComparison.Ordinal))
            {
                string bust = "HeroBusts/" + key.Substring("Heroes/".Length);
                if (Texture(bust) != null)
                {
                    key = bust;
                    fade = false;
                }
            }
            var tex = Texture(key);
            if (tex == null) return FitSafe(parent, key, 0, 0, W, H, tint);
            int[] b;
            if (!ArtBounds.TryGetValue(key, out b)) b = new[] { 0, 0, tex.width - 1, tex.height - 1 };
            float bw = b[2] - b[0], bh = b[3] - b[1];
            float sc = Mathf.Min((W - 12f) / bw, H / bh);
            float w = tex.width * sc, h = tex.height * sc;
            float left = W / 2f - (b[0] + b[2]) / 2f * sc;
            float top = -b[1] * sc + 4f;
            var img = Picture(parent, key, left, top, w, h, tint);
            if (fade) img.gameObject.AddComponent<BottomFade>();
            return img;
        }

        private RawImage Shade(Transform parent, float x, float y, float w, float h, Color color) =>
            Picture(parent, "Gen/Shadow", x, y, w, h, color);

        private void TextLink(string text, float x, float y, float w, float h, int size, Color color, Action click, bool formal = true)
        {
            var label = OutlinedText(root, text, x, y, w, h, size, color, formal, TextAnchor.MiddleLeft, Mathf.Max(8, size - 4));
            var button = Click(label.rectTransform, click, true, label);
            button.transition = Selectable.Transition.None;
            HoverCue(label.rectTransform);
        }

        private void GoldPill(string text, float x, float y, float w, float h, Action click, bool enabled, bool pulse)
        {
            if (enabled)
            {
                var glow = Shade(root, x - 36, y - 30, w + 72, h + 60, new Color(1f, .85f, .38f, .55f));
                glow.gameObject.name = "glow";
                if (pulse) PulseEndTurn(glow.rectTransform, true);
            }
            var rect = Box(root, "End turn", x, y, w, h);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = Texture(enabled ? "Gen/Pill" : "Gen/PillOff");
            var button = Click(rect, () => { UiSound("click"); click?.Invoke(); }, enabled, image);
            button.transition = Selectable.Transition.None;
            HoverCue(rect, () => enabled);
            var label = Label(rect, text, 0, 0, w, h, Mathf.Clamp(Mathf.RoundToInt(h * .46f), 16, 34), enabled ? new Color32(42, 30, 8, 255) : Quiet, true, TextAnchor.MiddleCenter);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = label.fontSize;
        }
    }
}
