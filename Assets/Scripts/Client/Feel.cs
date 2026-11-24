using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Sound cues, pulsing highlights and the round/turn banners.</summary>
    public sealed partial class Game
    {
        private float lastUiSound = -1f;
        private string lastUiKind = "";
        private int announcedRound = -1, announcedTurn = -1;
        private AudioSource menuAmbience;

        /// <summary>Maps a UI intent to an audio cue and plays it, with a little rate limiting so hover never machine-guns.</summary>
        private void UiSound(string kind)
        {
            if (string.IsNullOrEmpty(kind) || sfx == null) return;
            string cue;
            switch (kind)
            {
                case "click": cue = "ui-click"; break;
                case "hover": cue = "ui-hover"; break;
                case "open": cue = "ui-open"; break;
                case "close": cue = "ui-close"; break;
                case "deny": cue = "ui-deny"; break;
                case "tab": cue = "ui-tab"; break;
                default: cue = kind; break;
            }
            if (cue == "ui-hover" && Time.unscaledTime - lastUiSound < .06f) return;
            if (cue == lastUiKind && Time.unscaledTime - lastUiSound < .03f) return;
            lastUiSound = Time.unscaledTime;
            lastUiKind = cue;
            var clip = Clip(cue);
            if (clip) sfx.PlayOneShot(clip, cue == "ui-hover" ? .35f : .6f);
        }

        private void PolishButton(RectTransform box, Button button, bool enabled, bool primary, bool pulse)
        {
            if (button == null) return;
            button.interactable = enabled;
            HoverCue(box, () => enabled);
            if (pulse && enabled) PulseEndTurn(box, true);
        }

        /// <summary>Gently breathes the alpha of a graphic on the given rect (static when reduce-motion is on).</summary>
        private void PulseEndTurn(RectTransform box, bool active)
        {
            if (box == null || !active) return;
            var graphic = box.GetComponent<Graphic>();
            if (graphic == null || reduceMotion) return;
            StartCoroutine(Pulse(graphic));
        }

        private IEnumerator Pulse(Graphic graphic)
        {
            while (graphic != null)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * .5f;
                var c = graphic.color;
                // Only glow overlays pulse; opaque faces keep their alpha.
                if (graphic.gameObject.name == "glow") { c.a = .25f + t * .75f; graphic.color = c; }
                yield return null;
            }
        }

        private void ShowPhaseBanner(RectTransform layer, string title, string subtitle, Color accent, float hold = .78f)
        {
            if (layer == null) return;
            StartCoroutine(PhaseBanner(layer, title, subtitle, accent, hold));
        }

        private IEnumerator PhaseBanner(RectTransform layer, string title, string subtitle, Color accent, float hold)
        {
            var group = Box(layer, "Phase banner", 0, 440, 1920, 200);
            var canvasGroup = group.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0;
            ColorBox(group, "band-fill", 0, 40, 1920, 120, new Color(.02f, .035f, .055f, .8f));
            ColorBox(group, "rule-top", 0, 40, 1920, 2, accent);
            ColorBox(group, "rule-bottom", 0, 158, 1920, 2, accent);
            OutlinedText(group, title, 0, 52, 1920, 64, 52, accent, true, TextAnchor.MiddleCenter, 24);
            if (!string.IsNullOrEmpty(subtitle))
                SafeText(group, subtitle, 0, 112, 1920, 36, 22, White, false, TextAnchor.MiddleCenter, 14);
            float fade = reduceMotion ? 0.001f : .18f;
            for (float t = 0; t < fade && group; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = t / fade;
                yield return null;
            }
            if (!group) yield break;
            canvasGroup.alpha = 1;
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0; t < fade && group; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = 1 - t / fade;
                yield return null;
            }
            if (group) Destroy(group.gameObject);
        }

        /// <summary>Announces a new round and whose turn it is after a move resolved.</summary>
        private void QueuePhaseFeedback(RectTransform layer, int oldRound, int oldTurn, Move move)
        {
            if (match == null || layer == null) return;
            if (match.Round != oldRound && match.Round > 0 && match.Round != announcedRound)
            {
                announcedRound = match.Round;
                string sub = match.Contested >= 0 ? Lane(match.Contested) + " lane is contested" : "No lane contested yet";
                ShowPhaseBanner(layer, "Round " + match.Round, sub, Brass, .9f);
                UiSound("round-start");
            }
            else if (match.Turn != oldTurn && match.Winner < 0 && move != null && move.Kind != ActionKind.Pass)
            {
                // Quiet: turn changes are shown on the End Turn plate instead.
            }
        }

        private void ResetPhaseAnnouncements()
        {
            announcedRound = -1;
            announcedTurn = -1;
        }

        private void TryMenuAmbience()
        {
            if (menuAmbience == null)
            {
                menuAmbience = gameObject.AddComponent<AudioSource>();
                menuAmbience.loop = true;
                menuAmbience.spatialBlend = 0;
            }
            var clip = Clip("ambience-menu");
            if (clip == null) return;
            menuAmbience.clip = clip;
            menuAmbience.volume = .22f * (score != null ? Mathf.Clamp01(score.volume * 1.6f) : 1f);
            if (!menuAmbience.isPlaying) menuAmbience.Play();
        }
    }
}
