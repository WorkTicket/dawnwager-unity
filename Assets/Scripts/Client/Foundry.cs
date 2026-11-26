using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private const string Foundry = "Foundry/";
        private readonly System.Collections.Generic.Dictionary<(Color, Color, bool), Texture2D> gradients = new System.Collections.Generic.Dictionary<(Color, Color, bool), Texture2D>();
        private string HeroArt(string id)
        {
            var path = Foundry + "Hero-" + id;
            return Texture(path) != null ? path : "Units/" + id;
        }

        private void Gradient(Transform parent, string name, float x, float y, float w, float h, Color from, Color to, bool vertical)
        {
            var key = (from, to, vertical);
            if (!gradients.TryGetValue(key, out var texture))
            {
                texture = new Texture2D(vertical ? 1 : 2, vertical ? 2 : 1, TextureFormat.RGBA32, false);
                texture.SetPixels(vertical ? new[] { to, from } : new[] { from, to });
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply();
                texture.hideFlags = HideFlags.DontSave;
                gradients.Add(key, texture);
            }
            var graphic = Box(parent, name, x, y, w, h).gameObject.AddComponent<RawImage>();
            graphic.texture = texture;
            graphic.raycastTarget = false;
        }

        private void Medal(Transform parent, string type, string value, float x, float y, float size, int fontSize)
        {
            Picture(parent, Foundry + type, x, y, size, size);
            // Dead-center the numeral in the gem — outlined for HS readability.
            var num = Label(parent, value, x, y + (type == "Health" ? size * .08f : 0f), size, size, fontSize, White, false, TextAnchor.MiddleCenter);
            num.resizeTextForBestFit = true;
            num.resizeTextMinSize = Mathf.Max(8, fontSize - 8);
            num.resizeTextMaxSize = fontSize;
            num.horizontalOverflow = HorizontalWrapMode.Overflow;
            num.verticalOverflow = VerticalWrapMode.Truncate;
            num.fontStyle = FontStyle.Bold;
            num.alignByGeometry = true;
            var outline = num.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(10, 8, 6, 220);
            outline.effectDistance = new Vector2(Mathf.Max(1f, size * .04f), -Mathf.Max(1f, size * .04f));
            var shade = num.gameObject.AddComponent<Shadow>();
            shade.effectColor = new Color32(0, 0, 0, 160);
            shade.effectDistance = new Vector2(0, -Mathf.Max(1f, size * .03f));
        }

        // Stable per card, so the finish and the motion stay recognizable across sessions.
        private static int Motif(string id) => (id[0] * 31 + id[1]) * 31 + id[2];

        private Color CardAccent(Card card)
        {
            if (Finishes.TryGetValue(card.Id, out var code) && ColorUtility.TryParseHtmlString(code, out var color)) return color;
            return Brass;
        }

        private static string EffectArt(Card card)
        {
            switch(card.Faction)
            {
                case 'A': return "Thunder";
                case 'M': return "Ferry";
                case 'J': return "Chain";
                case 'H': return "Wake";
                case 'L': return "Trick";
                default: return "Steel";
            }
        }

        private IEnumerator FoilSweep(RawImage sweep, Card card, float w, float end)
        {
            int motif = Motif(card.Id);
            float duration = 2.05f + motif % 10 * .13f;
            var accent = CardAccent(card);
            while (sweep && !reduceMotion)
            {
                float phase = Mathf.Repeat(Time.unscaledTime + motif % 24 * .23f, duration * 2) / duration;
                if (phase <= 1)
                {
                    float drift = Mathf.Sin(phase * Mathf.PI) * (motif % 8 - 4);
                    sweep.rectTransform.anchoredPosition = new Vector2(w * (.14f + .72f * phase) - sweep.rectTransform.sizeDelta.x / 2, -end + drift);
                    accent.a = Mathf.Sin(phase * Mathf.PI) * (card.Legendary ? .61f : .30f);
                    sweep.color = accent;
                }
                else sweep.color = Color.clear;
                yield return null;
            }
            if (sweep) sweep.color = Color.clear;
        }

        private IEnumerator CardImpact(RectTransform layer, Card card, Vector2 position, bool strike)
        {
            if (!layer || reduceMotion) yield break;
            Color color = CardAccent(card);
            int motif = Motif(card.Id);
            int count = 4 + motif % 5;
            float spread = (strike ? 91 : 65) + motif % 26;
            int width = card.Legendary ? 272 : 190;
            var flash = Picture(layer, Foundry + "Impact-" + EffectArt(card), position.x - width / 2, -position.y - width / 2, width, width);
            flash.rectTransform.pivot = new Vector2(.5f,.5f);
            flash.rectTransform.anchoredPosition = position;
            var rays = new RawImage[count];
            var angles = new Vector2[count];
            float angleStep = Mathf.PI * 2 / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep + motif % 59 * .11f;
                var ray = Picture(layer, Foundry + "Hit", 0, 0, 34, 34, color);
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                rays[i] = ray;
                angles[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            for (float time = 0; time < .30f && layer; time += Time.unscaledDeltaTime)
            {
                float t = time / .30f;
                flash.rectTransform.localScale = Vector3.one * (.5f + t * .87f);
                flash.rectTransform.localRotation = Quaternion.Euler(0,0,motif % 180 + t*(motif % 60 - 30));
                color.a = Mathf.Clamp01(1 - t * 1.7f);
                flash.color = color;
                for (int i = 0; i < count; i++)
                {
                    var ray = rays[i];
                    var direction = angles[i];
                    float travel = card.Kind == Kind.Order ? Mathf.Sin(t*Mathf.PI*.5f) : t;
                    var at = position + direction * (14 + spread * travel) + new Vector2(-17, 17 - 23 * t * t);
                    ray.rectTransform.anchoredPosition = at;
                    ray.color = color;
                }
                yield return null;
            }
            if (flash) Destroy(flash.gameObject);
            foreach (var ray in rays) if (ray) Destroy(ray.gameObject);
        }
    }
}
