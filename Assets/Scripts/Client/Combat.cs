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
        private IEnumerator StrikeLunge(RectTransform layer, string art, Vector2 from, Vector2 to, RectTransform realTile, Color accent)
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
            CutFit(body, art, TileW, 184, Color.white);
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
            StartCoroutine(Burst(layer, to, accent, 9, 260f));
            StartCoroutine(Shake(7f));
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
            StartCoroutine(Burst(layer, to, accent, 8, 220f));
            StartCoroutine(Shake(4f));
            UiSound("card-slam");
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
    }
}
