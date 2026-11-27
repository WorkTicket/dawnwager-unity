using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wolfsun
{
    /// <summary>Small glass notifications in the top-right corner (quest complete, achievement unlocked).</summary>
    public sealed partial class Game
    {
        private int toastsShown;

        private void Toast(string title, string text, Color accent)
        {
            // Toasts live on the effects layer so a Render() does not wipe them.
            StartCoroutine(ToastRoutine(title, text, accent, toastsShown++ % 3));
        }

        private IEnumerator ToastRoutine(string title, string text, Color accent, int slot)
        {
            var layer = CardAnimationLayer();
            float y = 40 + slot * 104;
            var holder = Box(layer, "Toast", 1470, y, 420, 88);
            ChromePanel(holder, "Toast panel", 0, 0, 420, 88, "Panel");
            ColorBox(holder, "Accent", 0, 10, 4, 68, accent);
            OutlinedText(holder, title, 26, 8, 380, 34, 26, accent, true, TextAnchor.MiddleLeft, 14);
            SafeText(holder, text, 26, 44, 380, 34, 19, new Color32(207, 214, 218, 255), false, TextAnchor.MiddleLeft, 11);
            UiSound("open");
            float t = 0;
            var start = holder.anchoredPosition;
            while (t < .35f && holder != null)
            {
                t += Time.unscaledDeltaTime;
                holder.anchoredPosition = start + new Vector2(Mathf.Lerp(460, 0, Mathf.SmoothStep(0, 1, t / .35f)), 0);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3.2f);
            if (holder != null) Destroy(holder.gameObject);
        }
    }
}
