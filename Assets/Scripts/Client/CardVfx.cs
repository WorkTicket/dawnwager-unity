using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Optional 8x8 transparent VFX sheets. Missing sheets keep the existing built-in effects.</summary>
    public sealed partial class Game
    {
        [Serializable]
        private sealed class VfxCardEntry
        {
            public string id;
            public string primary;
            public string secondary;
            public bool primaryLoop;
            public bool secondaryLoop;
            public bool primaryAdditive;
            public bool secondaryAdditive;
        }

        [Serializable]
        private sealed class VfxCardManifest
        {
            public VfxCardEntry[] cards;
        }

        private static Dictionary<string, VfxCardEntry> vfxByCard;
        private static Material additiveVfxMaterial;

        private static VfxCardEntry VfxFor(string cardId)
        {
            if (vfxByCard == null)
            {
                vfxByCard = new Dictionary<string, VfxCardEntry>(StringComparer.OrdinalIgnoreCase);
                var json = Resources.Load<TextAsset>("Data/VfxByCard");
                if (json != null)
                {
                    var manifest = JsonUtility.FromJson<VfxCardManifest>(json.text);
                    if (manifest != null && manifest.cards != null)
                        foreach (var entry in manifest.cards)
                            if (entry != null && !string.IsNullOrEmpty(entry.id)) vfxByCard[entry.id] = entry;
                }
            }
            return !string.IsNullOrEmpty(cardId) && vfxByCard.TryGetValue(cardId, out var found) ? found : null;
        }

        /// <summary>
        /// Starts a mapped card effect if its source atlas has been imported. Returns false when
        /// the asset is absent so callers can retain the game's existing procedural animation.
        /// </summary>
        private bool TryStartCardVfx(RectTransform layer, string cardId, Vector2 at, Vector2 size, float delay = 0f)
        {
            if (reduceMotion || !layer) return false;
            var entry = VfxFor(cardId);
            if (entry == null || string.IsNullOrEmpty(entry.primary)) return false;
            var primary = Resources.Load<Texture2D>("Art/VFX/" + entry.primary);
            if (primary == null) return false;

            StartCoroutine(PlayVfxAtlas(layer, primary, at, size, entry.primaryLoop, entry.primaryAdditive, delay, 1f));
            if (!string.IsNullOrEmpty(entry.secondary))
            {
                var secondary = Resources.Load<Texture2D>("Art/VFX/" + entry.secondary);
                if (secondary != null)
                    StartCoroutine(PlayVfxAtlas(layer, secondary, at, size * .66f, entry.secondaryLoop, entry.secondaryAdditive, delay + .1f, .82f));
            }
            return true;
        }

        private IEnumerator PlayVfxAtlas(RectTransform layer, Texture2D atlas, Vector2 at, Vector2 size,
            bool loop, bool additive, float delay, float alpha)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (!layer || !atlas || page != Page.Battle) yield break;

            var holder = new GameObject("VFX " + atlas.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            holder.transform.SetParent(layer, false);
            var rect = (RectTransform)holder.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = size;

            var image = holder.GetComponent<RawImage>();
            image.texture = atlas;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, alpha);
            if (additive)
            {
                if (additiveVfxMaterial == null)
                {
                    var shader = Resources.Load<Shader>("Shaders/FoundryVfxAdditiveUI");
                    if (shader == null) shader = Shader.Find("UI/Foundry VFX Additive");
                    if (shader != null) additiveVfxMaterial = new Material(shader) { name = "Foundry VFX Additive (runtime)" };
                }
                if (additiveVfxMaterial != null) image.material = additiveVfxMaterial;
            }

            const int grid = 8;
            const int frames = 64;
            const float fps = 30f;
            float duration = loop ? .9f : frames / fps;
            float elapsed = 0f;
            while (elapsed < duration && holder && layer && page == Page.Battle)
            {
                int frame = Mathf.FloorToInt(elapsed * fps);
                if (loop) frame %= frames;
                else frame = Mathf.Min(frame, frames - 1);
                int column = frame % grid;
                int row = frame / grid;
                // The publisher's atlas is row-major from the top-left; Unity UVs start bottom-left.
                const float inset = .00035f;
                float cell = 1f / grid;
                image.uvRect = new Rect(column * cell + inset, 1f - (row + 1) * cell + inset,
                    cell - inset * 2f, cell - inset * 2f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (holder) Destroy(holder);
        }
    }
}
