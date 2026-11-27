using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Combat feel: attackers lunge into their target with a hit-stop and sparks, played cards fly in, hold, then slam into the lane.</summary>
    public sealed partial class Game
    {
        private static float EaseOut(float k) => 1f - (1f - k) * (1f - k) * (1f - k);
        private static float EaseIn(float k) => k * k * k;

        private static void Centered(RectTransform rect, Vector2 anchored)
        {
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = anchored;
        }

        /// <summary>Flash plus flying sparks at an anchored point on the effects layer.</summary>
        private IEnumerator Burst(RectTransform layer, Vector2 at, Color color, int count, float speed)
        {
            if (!layer || reduceMotion) yield break;
            var holder = Box(layer, "Burst", 0, 0, 10, 10);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var flash = Shade(holder, 0, 0, 260, 260, new Color(color.r, color.g, color.b, .95f));
            Centered(flash.rectTransform, at);
            var sparks = new List<RectTransform>();
            var velocity = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                var spark = ColorBox(holder, "Spark", 0, 0, 8, 22, Color.Lerp(color, Color.white, .55f));
                Centered(spark, at);
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.value * .5f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spark.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90f);
                sparks.Add(spark);
                velocity.Add(dir * speed * (.6f + Random.value * .8f));
            }
            const float duration = .45f;
            for (float t = 0; t < duration && holder; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                flash.rectTransform.localScale = Vector3.one * (.4f + k * 1.1f);
                flash.color = new Color(color.r, color.g, color.b, (1f - k) * .9f);
                for (int i = 0; i < sparks.Count; i++)
                    if (sparks[i]) sparks[i].anchoredPosition = at + velocity[i] * EaseOut(k) * .5f;
                group.alpha = k < .5f ? 1f : 1f - (k - .5f) / .5f;
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
        }

        /// <summary>The attacker leaves its tile, winds up, lunges at the target, freezes a beat on impact, then returns.</summary>
        private IEnumerator StrikeLunge(RectTransform layer, string art, Vector2 from, Vector2 to, RectTransform realTile, Color accent, Card striker = null, RectTransform victimTile = null)
        {
            if (!layer || reduceMotion) yield break;
            CanvasGroup realGroup = null;
            if (realTile)
            {
                realGroup = realTile.GetComponent<CanvasGroup>() ?? realTile.gameObject.AddComponent<CanvasGroup>();
                realGroup.alpha = 0f;
            }
            var holder = Box(layer, "Striker", 0, 0, TileW, TileH);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Shade(holder, -12, 176, 200, 46, new Color(0, 0, 0, .6f));
            var body = Box(holder, "Art", 0, 0, TileW, 184);
            DrawBoardUnit(body, art, 1f, Color.white);
            // Pivot at the centre so scale and tilt look natural; anchored y is negative downwards.
            holder.pivot = new Vector2(.5f, .5f);
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            dir = dist > 1f ? dir / dist : Vector2.up;
            Vector2 hit = from + dir * Mathf.Max(0f, dist - 120f);
            Vector2 wind = from - dir * 22f;
            holder.SetAsLastSibling();

            for (float t = 0; t < .1f && holder; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / .1f);
                holder.anchoredPosition = Vector2.Lerp(from, wind, k);
                holder.localScale = Vector3.one * (1f + .06f * k);
                yield return null;
            }
            for (float t = 0; t < .11f && holder; t += Time.unscaledDeltaTime)
            {
                float k = EaseIn(t / .11f);
                holder.anchoredPosition = Vector2.Lerp(wind, hit, k);
                holder.localScale = Vector3.one * Mathf.Lerp(1.06f, 1.16f, k);
                holder.localRotation = Quaternion.Euler(0, 0, -dir.x * 9f * k);
                yield return null;
            }
            StartCoroutine(StrikeImpact(layer, to, accent, striker, dir, victimTile));
            UiSound("unit-hit");
            yield return new WaitForSecondsRealtime(.06f); // hit-stop
            for (float t = 0; t < .26f && holder; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / .26f);
                holder.anchoredPosition = Vector2.Lerp(hit, from, k);
                holder.localScale = Vector3.one * Mathf.Lerp(1.16f, 1f, k);
                holder.localRotation = Quaternion.Euler(0, 0, -dir.x * 9f * (1f - k));
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
            if (realGroup) realGroup.alpha = 1f;
        }

        private IEnumerator DelayedFeedback(RectTransform layer, BoardSnap before, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (layer && match != null && page == Page.Battle) PlayFeedback(layer, before);
        }

        /// <summary>A played card flies from its owner, hangs in the air so it can be read, then slams into its lane.</summary>
        private IEnumerator CardSlam(RectTransform layer, Card card, Vector2 from, Vector2 to, Color accent)
        {
            if (!layer || reduceMotion) yield break;
            const int w = 230, h = 326;
            var ghost = PaperCard(card, 0, 0, w, h, card.Cost, false, layer, false);
            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            ghost.pivot = new Vector2(.5f, .5f);
            ghost.SetAsLastSibling();
            Vector2 stage1 = to + new Vector2(0, 40);
            for (float t = 0; t < .3f && ghost; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / .3f);
                ghost.anchoredPosition = Vector2.Lerp(from, stage1, k) + new Vector2(0, 70f * Mathf.Sin(k * Mathf.PI));
                ghost.localScale = Vector3.one * Mathf.Lerp(.7f, 1.05f, k);
                ghost.localRotation = Quaternion.Euler(0, 0, (1f - k) * (from.x < to.x ? -10f : 10f));
                yield return null;
            }
            for (float t = 0; t < .28f && ghost; t += Time.unscaledDeltaTime)
            {
                float k = t / .28f;
                ghost.anchoredPosition = stage1 + new Vector2(0, Mathf.Sin(k * Mathf.PI * 2f) * 4f);
                ghost.localScale = Vector3.one * (1.05f + .03f * k);
                yield return null;
            }
            for (float t = 0; t < .14f && ghost; t += Time.unscaledDeltaTime)
            {
                float k = EaseIn(t / .14f);
                ghost.anchoredPosition = Vector2.Lerp(stage1, to, k);
                ghost.localScale = Vector3.one * Mathf.Lerp(1.08f, .5f, k);
                group.alpha = 1f - k * .6f;
                yield return null;
            }
            if (card.Kind == Kind.Order && Texture("ResolveEffects/" + card.Id) != null)
                StartCoroutine(ResolveArt(layer, card.Id, to, 290));
            else StartCoroutine(Burst(layer, to, accent, 8, 220f));
            StartCoroutine(Shake(4f));
            UiSound("card-play-unit");
            for (float t = 0; t < .12f && ghost; t += Time.unscaledDeltaTime)
            {
                group.alpha = .4f * (1f - t / .12f);
                yield return null;
            }
            if (ghost) Destroy(ghost.gameObject);
        }

        /// <summary>Holds a freshly summoned tile invisible until the card lands, then pops it in.</summary>
        private IEnumerator SpawnReveal(RectTransform tile, float delay)
        {
            if (!tile) yield break;
            var group = tile.GetComponent<CanvasGroup>() ?? tile.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            yield return new WaitForSecondsRealtime(delay);
            if (!tile) yield break;
            tile.pivot = new Vector2(.5f, .5f);
            tile.anchoredPosition += new Vector2(tile.sizeDelta.x * .5f, -tile.sizeDelta.y * .5f);
            for (float t = 0; t < .22f && tile; t += Time.unscaledDeltaTime)
            {
                float k = EaseOut(t / .22f);
                group.alpha = Mathf.Clamp01(k * 2f);
                tile.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, k);
                yield return null;
            }
            if (tile) { group.alpha = 1f; tile.localScale = Vector3.one; }
        }

        /// <summary>A short caption under the lane labels that says what the enemy just did.</summary>
        private void Callout(string text, Color accent)
        {
            if (reduceMotion && false) return;
            StartCoroutine(CalloutRoutine(text, accent));
        }

        private IEnumerator CalloutRoutine(string text, Color accent)
        {
            var layer = CardAnimationLayer();
            var holder = Box(layer, "Callout", 560, 100, 800, 52);
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            ChromePanel(holder, "Callout panel", 0, 0, 800, 52, "Panel");
            ColorBox(holder, "Accent", 0, 8, 4, 36, accent);
            OutlinedText(holder, text, 20, 0, 760, 52, 26, White, true, TextAnchor.MiddleCenter, 14);
            for (float t = 0; t < 1.6f && holder; t += Time.unscaledDeltaTime)
            {
                group.alpha = t < .15f ? t / .15f : t > 1.3f ? 1f - (t - 1.3f) / .3f : 1f;
                yield return null;
            }
            if (holder) Destroy(holder.gameObject);
        }

        /// <summary>The moment of contact: a weapon slash across the target, a shockwave, faction sparks and a recoil on the victim.</summary>
        private IEnumerator StrikeImpact(RectTransform layer, Vector2 at, Color accent, Card striker, Vector2 dir, RectTransform victimTile)
        {
            if (!layer || reduceMotion) yield break;
            int power = striker != null ? Mathf.Clamp(striker.Attack, 1, 9) : 3;
            float weight = Mathf.Lerp(7f, 15f, power / 9f);
            StartCoroutine(Shake(weight));
            StartCoroutine(Burst(layer, at, accent, 8 + power, 240f + power * 25f));
            StartCoroutine(RingFx(layer, at, 30f, 180f + power * 14f, accent, .32f, 0f));
            StartCoroutine(Flash(layer, at, 240f + power * 18f, Color.white, .16f));
            // Two crossing slash arcs, angled along the line of attack.
            float baseAngle = Mathf.Atan2(dir.y, dir.x);
            var bars = new List<RectTransform>();
            for (int i = 0; i < 2; i++)
            {
                float a = baseAngle + (i == 0 ? .9f : -.9f);
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (110f + power * 8f);
                bars.Add(Bar(layer, at - d, at + d, 10f, new Color(accent.r, accent.g, accent.b, .75f)));
                bars.Add(Bar(layer, at - d, at + d, 3f, Color.white));
            }
            StartCoroutine(FadeBars(bars, .03f, .2f));
            if (victimTile) StartCoroutine(Recoil(victimTile, dir, weight));
            // Faction flavour on top of the generic spark burst.
            if (striker != null)
            {
                var list = new List<Spark>();
                StartCoroutine(RunSparks(list, .9f));
                var pal = Pal(striker.Faction);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 v = Random.insideUnitCircle * 260f + dir * 160f;
                    switch (striker.Faction)
                    {
                        case 'J': Emit(list, layer, at, v, .5f, 24f, 4f, pal[1], pal[0], -200f, 0f, true); break;
                        case 'H': Emit(list, layer, at, new Vector2(v.x * .3f, Mathf.Abs(v.y) * .6f + 40f), .8f, 34f, 8f, pal[0], new Color(pal[1].r, pal[1].g, pal[1].b, 0f)); break;
                        case 'L': Emit(list, layer, at, v, .55f, 26f, 5f, pal[0], pal[1], 0f, 0f, true); break;
                        case 'A': Emit(list, layer, at, v * 1.2f, .3f, 28f, 3f, Color.white, pal[1], 0f, 0f, true); break;
                        default: Emit(list, layer, at, v, .5f, 22f, 4f, pal[1], pal[0], -420f, 0f, true); break;
                    }
                }
            }
        }

        /// <summary>The struck tile is knocked back along the blow, flashes red, and settles.</summary>
        private IEnumerator Recoil(RectTransform tile, Vector2 dir, float weight)
        {
            if (!tile) yield break;
            Vector2 home = tile.anchoredPosition;
            var flash = ColorBox(tile, "Hit flash", 0, 0, tile.rect.width, tile.rect.height, new Color(1f, .15f, .1f, .45f));
            var image = flash.GetComponent<Image>();
            if (image) image.raycastTarget = false;
            const float duration = .3f;
            for (float t = 0; t < duration && tile; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float kick = Mathf.Sin(k * Mathf.PI) * (1f - k);
                tile.anchoredPosition = home + new Vector2(dir.x, dir.y) * weight * 2.4f * kick + new Vector2(Mathf.Sin(t * 70f), 0f) * weight * .35f * (1f - k);
                if (image) image.color = new Color(1f, .15f, .1f, .45f * (1f - k));
                yield return null;
            }
            if (tile) tile.anchoredPosition = home;
            if (flash) Destroy(flash.gameObject);
        }
    }
}
