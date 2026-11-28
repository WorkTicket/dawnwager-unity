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
            Gradient(root, "Nav shade", 0, 0, 1920, 130, new Color(.016f, .024f, .04f, .92f), new Color(.016f, .024f, .04f, 0f), true);
            TableButton("‹  Home", 40, 24, 160, 52, () => { UiSound("tab"); Go(Page.Home); });
            (string label, Page target)[] tabs =
            {
                ("Play", Page.Play), ("Campaign", Page.Story), ("Collection", Page.Collection),
                ("Career", Page.Career), ("Skills", Page.Skills)
            };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                bool current = page == tab.target;
                float x = 250 + i * 170;
                TextLink(tab.label, x, 26, 160, 52, 28, current ? Gold : Muted, () => { if (!current) { UiSound("tab"); Go(tab.target); } });
                if (current) ColorBox(root, "Tab line", x, 78, 130, 3, Gold);
            }
            ChromePanel(root, "Wallet", 1500, 22, 260, 56, "Panel");
            Pic(root, Foundry + "Attack", 1514, 30, 40, 40);
            Label(root, profile.embers + " Embers", 1562, 22, 190, 56, 24, Gold, true, TextAnchor.MiddleLeft);
            TextLink("Options", 1780, 26, 120, 52, 24, new Color32(207, 213, 216, 255), () => { settings = true; UiSound("open"); Render(); });
        }

        /// <summary>Page heading with a soft gold rule, drawn just below the navigation bar.</summary>
        private void Banner(string title, string subtitle)
        {
            OutlinedText(root, title, 120, 116, 1100, 60, 48, Gold, true, TextAnchor.MiddleLeft, 28);
            if (!string.IsNullOrEmpty(subtitle))
                SafeText(root, subtitle, 124, 176, 1500, 30, 21, Muted, false, TextAnchor.MiddleLeft, 14);
            Gradient(root, "Banner rule", 120, 222, 1100, 2, new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, .7f), new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, 0f), false);
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
            Gradient(root, "Left shade", 0, 0, 1100, 1080, new Color(.016f, .024f, .04f, .9f), new Color(.016f, .024f, .04f, 0f), false);
            Gradient(root, "Bottom shade", 0, 800, 1920, 280, new Color(.016f, .024f, .04f, 0f), new Color(.016f, .024f, .04f, .7f), true);
            FitSafe(root, Identity + "Emblem", 110, 86, 92, 92);
            FitSafe(root, Identity + "Wordmark", 216, 108, 560, 52);
            Label(root, "RAGNARÖK · EARLY ACCESS", 220, 166, 560, 24, 15, Brass, false);
            bool finished = profile.progress >= Campaign.Chapters.Length;
            TableButton("Play", 110, 290, 360, 86, () => Go(Page.Play), true, true);
            string[] names = { "Collection", "Campaign", "Career", "Options", "Quit" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 408 + i * 76;
                string n = names[i];
                Action act = n == "Collection" ? (Action)(() => Go(Page.Collection))
                    : n == "Campaign" ? () => { chapter = Campaign.ChapterAtProgress(profile.progress); Go(Page.Story); }
                    : n == "Career" ? () => Go(Page.Career)
                    : n == "Options" ? () => { settings = true; UiSound("open"); Render(); }
                    : (Action)(() => Application.Quit());
                TextLink(n, 120, y, 300, 56, 36, new Color32(232, 219, 182, 255), act);
                Gradient(root, "Rule", 120, y + 58, 300, 2, new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, .6f), new Color(GoldEdge.r, GoldEdge.g, GoldEdge.b, 0f), false);
                if (n == "Campaign")
                {
                    ColorBox(root, "Chapter tag", 330, y + 14, 110, 30, Gold);
                    Label(root, finished ? "Cleared" : "Ch. " + (Campaign.ChapterAtProgress(profile.progress) + 1), 330, y + 14, 110, 30, 16, InkOnGold, true, TextAnchor.MiddleCenter);
                }
            }
            bool learn = !profile.tutorialDone;
            TextLink(learn ? "Learn to Play" : "Practice Basics", 120, 810, 240, 36, 20, learn ? Gold : Muted, () => StartTutorial(), false);
            TextLink("How It Works", 370, 810, 200, 36, 20, Muted, () => OpenRules(), false);

            // player plate and wallet (top right)
            var hero = Hero.Info(profile.hero);
            ChromePanel(root, "Wallet", 1500, 22, 380, 56, "Panel");
            Pic(root, Foundry + "Attack", 1516, 30, 40, 40);
            Label(root, profile.embers + " Embers", 1564, 22, 300, 56, 24, Gold, true, TextAnchor.MiddleLeft);
            CirclePortrait(root, HeroArt(profile.hero), 1418, 18, 64, HeroFrameArt(profile.hero));
            Label(root, hero.Name, 1170, 24, 232, 28, 21, White, true, TextAnchor.MiddleRight);
            Label(root, Ladder.DisplayName(profile.ladderAbsolute, profile.legendPlace), 1170, 52, 232, 24, 15, Muted, false, TextAnchor.MiddleRight);

            // daily quests (bottom right)
            RefreshDailies();
            ChromePanel(root, "Dailies", 1380, 640, 490, 300, "Panel");
            OutlinedText(root, "Daily quests", 1408, 652, 300, 40, 26, Gold, true, TextAnchor.MiddleLeft, 16);
            for (int i = 0; i < profile.dailies.Count && i < 3; i++)
            {
                var quest = profile.dailies[i];
                float y = 704 + i * 70;
                SafeText(root, quest.label, 1408, y, 330, 28, 20, quest.progress >= quest.target ? AllySage : White, false, TextAnchor.MiddleLeft, 12);
                ColorBox(root, "bar", 1408, y + 38, 300, 10, new Color(1, 1, 1, .12f));
                ColorBox(root, "bar fill", 1408, y + 38, 300 * Mathf.Clamp01((float)quest.progress / Mathf.Max(1, quest.target)), 10, quest.claimed ? AllySage : Brass);
                Label(root, quest.progress + "/" + quest.target, 1722, y + 28, 60, 26, 16, Muted, false, TextAnchor.MiddleLeft);
                Label(root, "+" + quest.rewardEmbers, 1766, y + 2, 90, 26, 20, Gold, true, TextAnchor.MiddleRight);
            }
            Label(root, profile.wins + " wins · " + profile.losses + " losses", 1380, 960, 490, 30, 18, Muted, false, TextAnchor.MiddleRight);
            Label(root, "v" + Application.version + " early access", 40, 1040, 300, 24, 14, Quiet);
            if (!string.IsNullOrEmpty(status)) Label(root, status, 120, 1000, 900, 40, 20, Gold, false, TextAnchor.MiddleLeft);
        }

        // ---------------------------------------------------------------- settings

        private void VolumeControl(string name, AudioSource source, string key, int y)
        {
            // Everything sits inside the modal frame (x 660-1260): label, minus, ten clickable notches, plus.
            Label(root, name, 740, y, 130, 56, 26, White, true);
            int level = source != null ? Mathf.RoundToInt(Mathf.Clamp01(source.volume) * 10f) : 0;
            TableButton("−", 872, y, 56, 56, () => AdjustVolume(source, key, -.1f));
            for (int i = 0; i < 10; i++)
            {
                int target = i + 1;
                var notch = Box(root, name + " notch " + target, 940 + i * 19, y + 12, 17, 32);
                ColorBox(notch, "bar", 0, 30 - (10 + i * 2), 17, 10 + i * 2, i < level ? Gold : new Color(1, 1, 1, .16f));
                Click(notch, () => SetVolume(source, key, target / 10f)).transition = Selectable.Transition.None;
            }
            TableButton("+", 1140, y, 66, 56, () => AdjustVolume(source, key, .1f));
        }

        private void SetVolume(AudioSource source, string key, float value)
        {
            if (source == null) return;
            source.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, source.volume);
            PlayerPrefs.Save();
            UiSound("click");
            Render();
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

        private bool concedeArmed, refundArmed;

        private void Settings()
        {
            ViewportShade("Settings shade", new Color(0, 0, 0, .66f));
            ChromeModal(root, "Settings", 660, 90, 600, 900);
            OutlinedText(root, page == Page.Battle ? "Paused" : "Options", 660, 112, 600, 60, 44, Gold, true, TextAnchor.MiddleCenter, 24);
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
            Label(root, "Fast enemy turns", 740, 504, 300, 56, 26, White, true);
            TableButton(fastEnemy ? "On" : "Off", 1056, 504, 150, 56, () =>
            {
                fastEnemy = !fastEnemy;
                PlayerPrefs.SetInt("wolfsun.fastEnemy", fastEnemy ? 1 : 0);
                Render();
            }, true, fastEnemy);
            Label(root, "Opponent skill", 740, 576, 300, 44, 26, White, true);
            string[] levels = { "Easy", "Normal", "Hard" };
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                TableButton(levels[i], 740 + i * 150, 628, 140, 52, () => { profile.aiDifficulty = level; Save(); Render(); }, true, profile.aiDifficulty == level);
            }
            SafeText(root, "Practice and campaign. The tutorial always uses Easy.", 740, 686, 440, 28, 15, Quiet, false, TextAnchor.UpperLeft, 12);
            TableButton("How It Works", 740, 728, 440, 56, () => { settings = false; OpenRules(); });
            bool inMatch = page == Page.Battle && match != null && match.Winner < 0;
            if (inMatch)
                TableButton(concedeArmed ? "Really concede?" : "Concede Match", 740, 794, 440, 56, () =>
                {
                    if (!concedeArmed) { concedeArmed = true; UiSound("deny"); Render(); return; }
                    concedeArmed = false;
                    settings = false;
                    match.Surrender(0);
                    Schedule();
                    Render();
                });
            else if (page != Page.Home)
                TableButton("Main Menu", 740, 794, 440, 56, () => Go(Page.Home));
            TableButton(page == Page.Battle ? "Resume" : "Close", 740, 860, 440, 66, () => { settings = false; concedeArmed = false; PlayerPrefs.Save(); UiSound("close"); Render(); }, true, true);
            SafeText(root, "VFX: vinish.dev (https://vinish.dev/free-vfx), CC BY 4.0", 740, 936, 440, 28, 13, Quiet, false, TextAnchor.MiddleCenter, 10);
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
                Label(scroll, (y / 70 + 1).ToString(), 8, y + 8, 46, 46, 28, Gold, true, TextAnchor.MiddleCenter);
                SafeText(scroll, line, 70, y, 1180, 66, 25, White, false, TextAnchor.MiddleLeft, 15);
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
            Gradient(root, "Fade", 0, 0, 1920, 1080, new Color(.016f, .024f, .04f, .1f), new Color(.016f, .024f, .04f, .45f), true);
            Gradient(root, "Right shade", 700, 0, 1220, 1080, new Color(.016f, .024f, .04f, 0f), new Color(.016f, .024f, .04f, .85f), false);
            if (match != null)
            {
                var holder = Box(root, "Hero", 140, 220, 560, 700);
                Shade(holder, 40, 640, 480, 70, new Color(0, 0, 0, .8f));
                CutFit(holder, "Heroes/" + match.Players[0].Hero, 560, 660, won ? Color.white : new Color(.6f, .6f, .66f, 1f));
            }
            OutlinedText(root, result.ToUpperInvariant(), 900, 70, 920, 150, 120, won ? Gold : result == "Draw" ? White : EnemyCopper, true, TextAnchor.MiddleLeft, 60);
            if (match != null)
                SafeText(root, "Seals " + match.Players[0].Seals + " – " + match.Players[1].Seals + " · Round " + match.Round, 906, 224, 900, 36, 26, White, false, TextAnchor.MiddleLeft, 16);
            float y = 280;
            if (!string.IsNullOrEmpty(status))
            {
                SafeText(root, status, 906, y, 900, 36, 22, Gold, false, TextAnchor.MiddleLeft, 14);
                y += 44;
            }
            if (won && rewardCards.Length > 0)
            {
                ChromePanel(root, "Reward panel", 900, y, 920, 330, "Panel");
                OutlinedText(root, "New cards unlocked", 930, y + 10, 600, 44, 30, Gold, true, TextAnchor.MiddleLeft, 18);
                int shown = Mathf.Min(rewardCards.Length, 7);
                float cw = 112, ch = cw * 590f / 416f;
                for (int i = 0; i < shown; i++) FullCard(rewardCards[i], 926 + i * (cw + 10), y + 62, cw, ch, null);
                if (rewardCards.Length > shown)
                    Label(root, "+" + (rewardCards.Length - shown) + " more in your Collection", 930, y + 286, 860, 32, 19, Muted, false, TextAnchor.MiddleLeft);
                y += 350;
            }
            else if (won && !duel && !tutorialMatch)
            {
                SafeText(root, "Chapter replayed. First clears earn new cards, skill points and relics.", 906, y, 900, 40, 22, Muted, false, TextAnchor.MiddleLeft, 14);
                y += 50;
            }
            if (won && pendingRelicChoices != null && pendingRelicChoices.Length > 0)
            {
                ChromePanel(root, "Relic panel", 900, y, 920, 260, "Panel");
                OutlinedText(root, "Choose a relic", 930, y + 8, 600, 44, 30, Gold, true, TextAnchor.MiddleLeft, 18);
                int n = pendingRelicChoices.Length;
                for (int i = 0; i < n; i++)
                {
                    var relic = CampaignMeta.Relic(pendingRelicChoices[i]);
                    if (relic == null) continue;
                    string id = relic.Id;
                    float rx = 930 + i * 290;
                    int index = Array.FindIndex(CampaignMeta.Relics, r => r.Id == id);
                    FitSafe(root, "Campaign/Relic" + (index + 1), rx, y + 60, 90, 90);
                    Label(root, relic.Name, rx + 96, y + 58, 190, 32, 21, Gold, true);
                    SafeText(root, relic.Summary, rx + 96, y + 90, 190, 70, 16, White, false, TextAnchor.UpperLeft, 11);
                    TableButton("Take", rx + 96, y + 190, 130, 46, () => ChoosePendingRelic(id), true, true);
                }
                TableButton("Skip", 1700, y + 200, 100, 42, () => SkipPendingRelic());
                y += 280;
            }
            float by = Mathf.Min(y + 20, 900);
            TableButton("Continue", 900, by, 360, 84, () => LeaveOutcome(), true, true);
            if (duel && !tutorialMatch) TableButton("Play Again", 1290, by, 270, 84, () => StartMatch(true));
            else if (!won && !duel && !tutorialMatch) TableButton("Try Again", 1290, by, 270, 84, () => StartMatch(false));
            else if (tutorialMatch) TableButton("Replay Tutorial", 1290, by, 270, 84, () => StartTutorial());
        }

        private void LeaveOutcome()
        {
            var target = tutorialMatch ? Page.Home : returnPage;
            if (target == Page.Story && result == "Victory") chapter = Campaign.ChapterAtProgress(profile.progress);
            Go(target);
        }
    }
}
