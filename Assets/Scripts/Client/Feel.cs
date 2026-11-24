using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private void UiSound(string kind) { }

        private void PolishButton(RectTransform box, Button button, bool enabled, bool primary, bool pulse) { }

        private void PulseEndTurn(RectTransform box, bool active) { }

        private void ShowPhaseBanner(RectTransform layer, string title, string subtitle, Color accent, float hold = .78f) { }

        private void QueuePhaseFeedback(RectTransform layer, int oldRound, int oldTurn, Move move) { }

        private void ResetPhaseAnnouncements() { }
    }
}
