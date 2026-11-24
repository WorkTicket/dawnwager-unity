using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Shared screens: navigation, home, settings, card inspect, rules and the battle outcome.</summary>
    public sealed partial class Game
    {
        private Page returnPage = Page.Home;

        // ---------------------------------------------------------------- navigation

        private void Nav()
        {
            Pic(root, "Foundry/NavBar", 0, 0, 1920, 128);
            var home = Box(root, "Home", 28, 22, 84, 84);
            FitSafe(home, Identity + "Emblem", 0, 0, 84, 84);
            Click(home, () => { UiSound("tab"); Go(Page.Home); });
            HoverCue(home);
            (string label, Page target)[] tabs =
            {
                ("Play", Page.Play), ("Campaign", Page.Story), ("Collection", Page.Collection),
                ("Career", Page.Career), ("Skills", Page.Skills)
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                bool current = page == tab.target;
                TableButton(tab.label, 140 + i * 214, 30, 204, 68, () => { if (!current) { UiSound("tab"); Go(tab.target); } }, true, current);
            }
            Label(root, profile.embers + " Embers", 1290, 30, 250, 68, 26, Gold, true, TextAnchor.MiddleRight);
            TableButton("Options", 1566, 30, 190, 68, () => { settings = true; UiSound("open"); Render(); });
            TableButton("Menu", 1766, 30, 126, 68, () => Go(Page.Home));
        }

        /// <summary>Page heading with an ornamental rule, drawn just below the navigation bar.</summary>
        private void Banner(string title, string subtitle)
        {
            OutlinedText(root, title, 120, 140, 1100, 64, 52, Gold, true, TextAnchor.MiddleLeft, 28);
            if (!string.IsNullOrEmpty(subtitle))
                SafeText(root, subtitle, 124, 204, 1500, 34, 22, Muted, false, TextAnchor.MiddleLeft, 14);
            FitSafe(root, "Foundry/DividerOrnate", 120, 240, 1680, 24);
        }

        // ---------------------------------------------------------------- home

        private void ModeTile(string art, string title, string sub, float x, float y, Action click)
        {
            const float w = 400, h = 225;
            var tile = Box(root, title, x, y, w, h);
            var image = tile.gameObject.AddComponent<RawImage>();
            image.texture = Texture(Foundry + "Mode-" + art);
            if (image.texture == null) image.color = Slate;
            Click(tile, () => { UiSound("click"); click(); }, true, image).transition = Selectable.Transition.None;
            var relay = tile.gameObject.AddComponent<PointerRelay>();
            relay.Enter = () =>
            {
                var hover = Texture(Foundry + "Mode-" + art + "Hover");
                if (hover != null) image.texture = hover;
                UiSound("hover");
            };
            relay.Exit = () =>
            {
                var normal = Texture(Foundry + "Mode-" + art);
                if (normal != null) image.texture = normal;
            };
            OutlinedText(root, title, x, y + h + 6, w, 40, 30, Gold, true, TextAnchor.MiddleCenter, 18);
            SafeText(root, sub, x + 8, y + h + 46, w - 16, 30, 19, Muted, false, TextAnchor.MiddleCenter, 12);
        }

        private void Home()
        {
            ColorBox(root, "Vignette", 0, 0, 1920, 1080, new Color(0, 0, 0, .28f));
            Gradient(root, "Bottom fade", 0, 480, 1920, 600, new Color(.04f, .06f, .09f, 0f), new Color(.04f, .06f, .09f, .92f), true);
            FitSafe(root, Identity + "Wordmark", 360, 56, 1200, 111);
            SafeText(root, "A card game of two lanes at the end of the world", 360, 172, 1200, 40, 26, White, false, TextAnchor.MiddleCenter, 16);
            var foe = Campaign.Chapters[Campaign.ChapterAtProgress(profile.progress)];
            bool finished = profile.progress >= Campaign.Chapters.Length;
            string story = finished ? "All seven chapters cleared" : (profile.progress == 0 ? "Begin the story" : "Next: " + foe.Name);
            const float x0 = 115, gap = 30;
            ModeTile("Campaign", "Campaign", story, x0, 520, () => { chapter = Campaign.ChapterAtProgress(profile.progress); Go(Page.Story); });
            ModeTile("Casual", "Casual Duel", "Unranked match against a rival", x0 + 430, 520, () => { pendingQueue = Net.QueueMode.Casual; Go(Page.Play); });
            ModeTile("Ranked", "Ranked Ladder", Ladder.DisplayName(profile.ladderAbsolute, profile.legendPlace), x0 + 860, 520, () => { pendingQueue = Net.QueueMode.Ranked; Go(Page.Play); });
            ModeTile("Collection", "Collection", owned.Count + " cards · build your deck", x0 + 1290, 520, () => Go(Page.Collection));
            bool learn = !profile.tutorialDone;
            TableButton(learn ? "Learn to Play" : "Practice Basics", 330, 940, 300, 72, () => StartTutorial(), true, learn);
            TableButton("Options", 660, 940, 300, 72, () => { settings = true; UiSound("open"); Render(); });
            TableButton("How It Works", 990, 940, 300, 72, () => OpenRules());
            TableButton("Quit", 1320, 940, 270, 72, () => Application.Quit());
            var hero = Hero.Info(profile.hero);
            Label(root, hero.Name + " · " + profile.wins + " wins · " + profile.losses + " losses", 40, 1030, 900, 40, 20, Quiet);
            if (!string.IsNullOrEmpty(status)) Label(root, status, 960, 1030, 900, 40, 20, Gold, false, TextAnchor.MiddleRight);
        }

        // ---------------------------------------------------------------- settings

        private void VolumeControl(string name, AudioSource source, string key, int y)
        {
            Label(root, name, 740, y, 220, 56, 26, White, true);
            float value = source != null ? source.volume : 0;
            TableButton("−", 980, y, 64, 56, () => AdjustVolume(source, key, -.1f));
            ColorBox(root, name + " track", 1056, y + 20, 220, 16, new Color(0, 0, 0, .55f));
            ColorBox(root, name + " fill", 1056, y + 20, 220 * Mathf.Clamp01(value), 16, Brass);
            TableButton("+", 1288, y, 64, 56, () => AdjustVolume(source, key, .1f));
        }

        private void AdjustVolume(AudioSource source, string key, float delta)
        {
            if (source == null) return;
            source.volume = Mathf.Clamp01(Mathf.Round((source.volume + delta) * 10f) / 10f);
            PlayerPrefs.SetFloat(key, source.volume);
            PlayerPrefs.Save();
            UiSound("click");
            Render();
        }

        private void Settings()
        {
            ViewportShade("Settings shade", new Color(0, 0, 0, .66f));
            ChromeModal(root, "Settings", 660, 90, 600, 900);
            OutlinedText(root, "Options", 660, 112, 600, 60, 44, Gold, true, TextAnchor.MiddleCenter, 24);
            VolumeControl("Music", score, "wolfsun.music", 200);
            VolumeControl("Effects", sfx, "wolfsun.effects", 276);
            Label(root, "Reduce motion", 740, 352, 300, 56, 26, White, true);
            TableButton(reduceMotion ? "On" : "Off", 1056, 352, 150, 56, () =>
            {
                reduceMotion = !reduceMotion;
                PlayerPrefs.SetInt("wolfsun.reduceMotion", reduceMotion ? 1 : 0);
                Render();
            }, true, reduceMotion);
            Label(root, "Fullscreen", 740, 428, 300, 56, 26, White, true);
            TableButton(Screen.fullScreen ? "On" : "Off", 1056, 428, 150, 56, () => { ToggleFullscreen(); Render(); }, !Application.isEditor, Screen.fullScreen);
            Label(root, "Opponent skill", 740, 504, 300, 56, 26, White, true);
            string[] levels = { "Easy", "Normal", "Hard" };
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                TableButton(levels[i], 740 + i * 150, 570, 140, 56, () => { profile.aiDifficulty = level; Save(); Render(); }, true, profile.aiDifficulty == level);
            }
            SafeText(root, "Applies to practice and campaign battles. The tutorial always uses Easy.", 740, 632, 440, 54, 17, Quiet, false, TextAnchor.UpperLeft, 12);
            TableButton("How It Works", 740, 704, 440, 60, () => { settings = false; OpenRules(); });
            bool inMatch = page == Page.Battle && match != null && match.Winner < 0;
            if (inMatch)
                TableButton("Concede Match", 740, 778, 440, 60, () =>
                {
                    settings = false;
                    match.Surrender(0);
                    Schedule();
                    Render();
                });
            else if (page != Page.Home)
                TableButton("Main Menu", 740, 778, 440, 60, () => Go(Page.Home));
            TableButton("Close", 740, 850, 440, 70, () => { settings = false; PlayerPrefs.Save(); UiSound("close"); Render(); }, true, true);
        }

        // ---------------------------------------------------------------- inspect

        private void Inspect()
        {
            if (inspected == null) return;
            Card card;
            try { card = Catalog.Get(inspected); } catch (InvalidOperationException) { inspected = null; return; }
            var shade = ViewportShade("Inspect shade", new Color(0, 0, 0, .74f));
            Click(shade, () => { inspected = null; UiSound("close"); Render(); }, true).transition = Selectable.Transition.None;
            const int w = 480, h = 680;
            PaperCard(card, 380, 190, w, h, card.Cost, false, root, true);
            ChromePanel(root, "Details", 900, 190, 640, 680, "Panel");
            OutlinedText(root, card.Name, 930, 210, 580, 56, 40, card.Legendary ? Gold : White, true, TextAnchor.MiddleLeft, 22);
            string stats = card.Kind == Kind.Unit
                ? card.Cost + " Charge · " + card.Attack + " Attack · " + card.Health + " Health"
                : card.Kind == Kind.Prophecy ? "Prophecy" : card.Cost + " Charge";
            SafeText(root, FactionName(card.Faction) + " " + KindName(card) + " · " + stats, 930, 266, 580, 34, 22, Gold, false, TextAnchor.MiddleLeft, 14);
            SafeText(root, RulesText.Describe(card), 930, 310, 580, 150, 24, White, false, TextAnchor.UpperLeft, 14);
            int row = 0;
            foreach (var term in RulesText.Terms(card).Take(5))
            {
                SafeText(root, term.Key, 930, 470 + row * 70, 580, 26, 21, Gold, true, TextAnchor.MiddleLeft, 13);
                SafeText(root, term.Value, 930, 496 + row * 70, 580, 42, 17, Muted, false, TextAnchor.UpperLeft, 11);
                row++;
            }
            TableButton("Close", 930, 800, 580, 56, () => { inspected = null; UiSound("close"); Render(); }, true, true);
        }

        // ---------------------------------------------------------------- rules

        private static readonly string[] HowToPlay =
        {
            "Two lanes, West and East. Each lane holds up to three Units and one Site per side.",
            "From round 2 one lane is contested, and the contested lane flips every round.",
            "Charge is your mana. It rises by one each round up to ten and refills at round start.",
            "On your turn play a card, march a Unit to the other lane, attack, use your hero power (2 Charge) or pass.",
            "Attack an enemy Unit, or strike the Bastion if the lane has none. Guards must be struck first.",
            "When both sides pass the round ends. The side with more Seal Power in the contested lane claims a Seal.",
            "Claim six Seals, or break the enemy Bastion (24 health), to win. Your hand holds at most eight cards.",
            "Prophecies fire on rounds three and five and change the board for everyone.",
            "Right-click or hold the pointer on any card to read its full rules."
        };

        private void OpenRules()
        {
            showRules = true;
            UiSound("open");
            Render();
        }

        private void RulesOverlay()
        {
            ViewportShade("Rules shade", new Color(0, 0, 0, .72f));
            ChromeModal(root, "Rules", 260, 70, 1400, 940);
            OutlinedText(root, "How It Works", 260, 92, 1400, 60, 44, Gold, true, TextAnchor.MiddleCenter, 24);
            var scroll = ScrollPanel(root, 320, 170, 1280, 740, 140 + HowToPlay.Length * 70 + RulesText.Keywords.Count * 86 + 80);
            float y = 0;
            foreach (string line in HowToPlay)
            {
                SafeText(scroll, "• " + line, 8, y, 1240, 66, 25, White, false, TextAnchor.MiddleLeft, 15);
                y += 70;
            }
            y += 20;
            Label(scroll, "Keywords", 8, y, 600, 50, 34, Gold, true);
            y += 56;
            foreach (var pair in RulesText.Keywords)
            {
                Label(scroll, pair.Key, 8, y, 280, 36, 24, Gold, true);
                SafeText(scroll, pair.Value, 300, y, 940, 80, 20, Muted, false, TextAnchor.UpperLeft, 13);
                y += 86;
            }
            TableButton("Close", 760, 928, 400, 64, () => { showRules = false; UiSound("close"); Render(); }, true, true);
        }

        // ---------------------------------------------------------------- outcome

        private void Outcome()
        {
            bool won = result == "Victory";
            Gradient(root, "Fade", 0, 0, 1920, 1080, new Color(0, 0, 0, .1f), new Color(0, 0, 0, .55f), true);
            OutlinedText(root, result.ToUpperInvariant(), 360, 60, 1200, 120, 100, won ? Gold : result == "Draw" ? White : EnemyCopper, true, TextAnchor.MiddleCenter, 48);
            if (match != null)
            {
                string line = "Seals " + match.Players[0].Seals + " – " + match.Players[1].Seals + " · Round " + match.Round;
                SafeText(root, line, 360, 190, 1200, 40, 28, White, false, TextAnchor.MiddleCenter, 16);
            }
            float y = 260;
            if (!string.IsNullOrEmpty(status))
            {
                SafeText(root, status, 360, y, 1200, 36, 24, Gold, false, TextAnchor.MiddleCenter, 14);
                y += 44;
            }
            if (won && rewardCards.Length > 0)
            {
                ChromePanel(root, "Reward panel", 260, y, 1400, 420, "Panel");
                OutlinedText(root, "New cards unlocked", 260, y + 12, 1400, 50, 34, Gold, true, TextAnchor.MiddleCenter, 18);
                int shown = Mathf.Min(rewardCards.Length, 7);
                float cw = 190, ch = cw * 590f / 416f;
                float start = 960 - (shown * (cw + 14) - 14) / 2f;
                for (int i = 0; i < shown; i++) FullCard(rewardCards[i], start + i * (cw + 14), y + 72, cw, ch, null);
                if (rewardCards.Length > shown)
                    Label(root, "+" + (rewardCards.Length - shown) + " more in your Collection", 260, y + 360, 1400, 40, 22, Muted, false, TextAnchor.MiddleCenter);
                y += 440;
            }
            else if (won && !duel && !tutorialMatch)
            {
                SafeText(root, "Chapter replayed. First clears earn new cards, skill points and relics.", 360, y, 1200, 40, 24, Muted, false, TextAnchor.MiddleCenter, 14);
                y += 50;
            }
            if (won && pendingRelicChoices != null && pendingRelicChoices.Length > 0)
            {
                ChromePanel(root, "Relic panel", 260, y, 1400, 250, "Panel");
                OutlinedText(root, "Choose a relic", 260, y + 8, 1400, 44, 32, Gold, true, TextAnchor.MiddleCenter, 18);
                int n = pendingRelicChoices.Length;
                for (int i = 0; i < n; i++)
                {
                    var relic = CampaignMeta.Relic(pendingRelicChoices[i]);
                    if (relic == null) continue;
                    string id = relic.Id;
                    float rx = 960 - (n * 420 - 20) / 2f + i * 420;
                    int index = Array.FindIndex(CampaignMeta.Relics, r => r.Id == id);
                    FitSafe(root, "Campaign/Relic" + (index + 1), rx, y + 56, 120, 120);
                    Label(root, relic.Name, rx + 128, y + 58, 270, 36, 24, Gold, true);
                    SafeText(root, relic.Summary, rx + 128, y + 94, 270, 70, 18, White, false, TextAnchor.UpperLeft, 12);
                    TableButton("Take", rx + 128, y + 174, 150, 50, () => ChoosePendingRelic(id), true, true);
                }
                TableButton("Skip", 1500, y + 190, 130, 46, () => SkipPendingRelic());
                y += 270;
            }
            float by = Mathf.Min(y + 20, 920);
            TableButton("Continue", 560, by, 380, 80, () => LeaveOutcome(), true, true);
            if (duel && !tutorialMatch) TableButton("Play Again", 980, by, 380, 80, () => StartMatch(true));
            else if (!won && !duel && !tutorialMatch) TableButton("Try Again", 980, by, 380, 80, () => StartMatch(false));
            else if (tutorialMatch) TableButton("Replay Tutorial", 980, by, 380, 80, () => StartTutorial());
        }

        private void LeaveOutcome()
        {
            var target = tutorialMatch ? Page.Home : returnPage;
            if (target == Page.Story && result == "Victory") chapter = Campaign.ChapterAtProgress(profile.progress);
            Go(target);
        }
    }
}
