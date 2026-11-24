using System;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    /// <summary>UI cleared — fresh start. Core rules remain in Assets/Scripts/Core.</summary>
    public sealed partial class Game
    {
        private void Banner(string title, string subtitle) { }

        private void Home() { }

        private void TryMenuAmbience() { }

        private void Collection() { }

        private static bool CardMatchesQuery(Card card, string query) => true;

        private void AddDeck(string id) { }

        private void FullCard(string id, float x, float y, float w, float h, Action click, Color? tint = null, Transform parent = null) { }

        private void Inspect() { inspected = null; }

        private void Settings() { settings = false; }

        private void VolumeControl(string name, AudioSource source, string key, int y) { }

        private void Outcome() { }
    }
}
