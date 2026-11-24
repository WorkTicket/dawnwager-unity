using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using QueueMode = Wolfsun.Net.QueueMode;
using MatchPair = Wolfsun.Net.MatchPair;
using QueueTicket = Wolfsun.Net.QueueTicket;
using AuthoritativeDuel = Wolfsun.Net.AuthoritativeDuel;
using Matchmaker = Wolfsun.Net.Matchmaker;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private static readonly string[] CardBackCatalog =
        {
            "Foundry/CardBack",
            "Foundry/CardBack-Ember",
            "Foundry/CardBack-Frost",
            "Foundry/CardBack-Legend"
        };
        private static readonly int[] CardBackCost = { 0, 150, 250, 500 };
        private static readonly string[] CardBackName = { "Dawnwager", "Ember", "Frost", "Legend" };

        private Text queueLabel;
        private RectTransform queueRing;
        private const string LocalPlayer = "local";

        private void PlayHub()
        {
            bool ranked = pendingQueue == QueueMode.Ranked;
            Banner("Play", "Duel a rival, climb the Beacon Ladder and finish daily quests");

            // --- mode + ladder
            TableButton("Casual", 60, 280, 280, 60, () => { pendingQueue = QueueMode.Casual; UiSound("tab"); Render(); }, true, !ranked);
            TableButton("Ranked", 360, 280, 280, 60, () => { pendingQueue = QueueMode.Ranked; UiSound("tab"); Render(); }, true, ranked);
            ChromePanel(root, "Ladder", 60, 356, 580, 520, ranked ? "PanelGold" : "Panel");
            if (ranked)
            {
                FitSafe(root, Foundry + RankMedalPath(profile.ladderAbsolute), 190, 372, 280, 280);
                OutlinedText(root, Ladder.DisplayName(profile.ladderAbsolute, profile.legendPlace), 60, 650, 580, 50, 38, Gold, true, TextAnchor.MiddleCenter, 20);
                if (profile.ladderAbsolute < Ladder.LegendOrdinal)
                    DrawRankStars(root, 60 + (580 - Ladder.StarsPerRank * 48) / 2f, 704, profile.ladderStars, Ladder.StarsPerRank);
                Label(root, "Win streak " + profile.winStreak + " · Stars per win " + Ladder.StarsForWin(profile.starBonus, profile.winStreak, profile.ladderAbsolute),
                    60, 760, 580, 30, 20, White, false, TextAnchor.MiddleCenter);
                Label(root, "Rating " + Mathf.RoundToInt((float)profile.rankedMmr) + " · Peak " + Ladder.DisplayName(profile.peakAbsolute),
                    60, 790, 580, 30, 18, Muted, false, TextAnchor.MiddleCenter);
                if (profile.apprenticeLeft > 0)
                    Label(root, profile.apprenticeLeft + " apprentice games: losses cost no stars", 60, 822, 580, 30, 18, Frost, false, TextAnchor.MiddleCenter);
            }
            else
            {
                FitSafe(root, Foundry + "Icon-Casual", 220, 392, 200, 200);
                OutlinedText(root, "Casual Duel", 60, 610, 580, 50, 38, Gold, true, TextAnchor.MiddleCenter, 20);
                SafeText(root, "Unranked games against rivals of similar rating. No stars to lose.", 100, 670, 500, 60, 22, White, false, TextAnchor.MiddleCenter, 14);
                Label(root, "Rating " + Mathf.RoundToInt((float)profile.casualMmr), 60, 750, 580, 30, 20, Muted, false, TextAnchor.MiddleCenter);
            }

            // --- deck + find match
            ChromePanel(root, "Deck", 680, 280, 560, 596, "Panel");
            var hero = Hero.Info(profile.hero);
            CirclePortrait(root, HeroArt(profile.hero), 850, 300, 220, Foundry + "HeroFrame");
            OutlinedText(root, hero.Name + " · " + hero.Title, 680, 530, 560, 44, 30, Gold, true, TextAnchor.MiddleCenter, 16);
            SafeText(root, hero.Power + ": " + hero.Description, 710, 576, 500, 56, 19, White, false, TextAnchor.MiddleCenter, 12);
            bool legal = Campaign.LegalDeck(profile.deck, owned);
            Label(root, legal ? "Deck ready · 30 cards" : "Your deck is not legal", 680, 640, 560, 34, 22, legal ? AllySage : EnemyCopper, true, TextAnchor.MiddleCenter);
            TableButton("Edit Deck", 740, 688, 200, 56, () => Go(Page.Collection));
            TableButton("Change Hero", 980, 688, 200, 56, () => Go(Page.Collection));
            TableButton(ranked ? "Find Ranked Match" : "Find Casual Match", 720, 770, 480, 84, () => BeginQueue(pendingQueue), legal && !queueSearching, true);
            TableButton("Practice Skirmish", 680, 890, 560, 62, () => StartMatch(true), legal);
            if (!string.IsNullOrEmpty(status)) SafeText(root, status, 680, 960, 560, 40, 20, Gold, false, TextAnchor.MiddleCenter, 12);

            // --- daily quests + card backs
            RefreshDailies();
            ChromePanel(root, "Dailies", 1280, 280, 580, 330, "Panel");
            OutlinedText(root, "Daily quests", 1280, 292, 580, 44, 30, Gold, true, TextAnchor.MiddleCenter, 18);
            for (int i = 0; i < profile.dailies.Count && i < 3; i++)
            {
                var quest = profile.dailies[i];
                float y = 346 + i * 84;
                SafeText(root, quest.label, 1306, y, 530, 30, 20, quest.progress >= quest.target ? AllySage : White, false, TextAnchor.MiddleLeft, 12);
                ColorBox(root, "bar", 1306, y + 38, 400, 14, new Color(0, 0, 0, .55f));
                ColorBox(root, "bar fill", 1306, y + 38, 400 * Mathf.Clamp01((float)quest.progress / Mathf.Max(1, quest.target)), 14, quest.claimed ? AllySage : Brass);
                Label(root, quest.progress + "/" + quest.target, 1716, y + 30, 70, 28, 18, Muted, false, TextAnchor.MiddleLeft);
                Label(root, "+" + quest.rewardEmbers, 1760, y + 30, 90, 28, 18, Gold, true, TextAnchor.MiddleRight);
            }
            ChromePanel(root, "Backs", 1280, 626, 580, 250, "Panel");
            OutlinedText(root, "Card backs · " + profile.embers + " Embers", 1280, 636, 580, 40, 26, Gold, true, TextAnchor.MiddleCenter, 16);
            for (int i = 0; i < CardBackCatalog.Length; i++)
            {
                string path = CardBackCatalog[i];
                int index = i;
                bool have = profile.unlockedCardBacks.Contains(path);
                bool equipped = CardBackArt == path;
                float x = 1304 + i * 140;
                var back = Pic(root, path, x, 686, 84, 118);
                if (!have) back.color = new Color(.5f, .5f, .5f, 1f);
                if (equipped) Pic(root, Foundry + "CardPlayableGlow", x - 8, 680, 100, 130, Gold);
                string label = equipped ? "Equipped" : have ? "Equip" : CardBackCost[index] + " Embers";
                TableButton(label, x - 12, 812, 124, 44, () => BuyCardBack(path), !equipped && (have || profile.embers >= CardBackCost[index]));
            }
            if (queueSearching) DrawQueueOverlay();
        }

        private void DrawRankStars(Transform parent, float x, float y, int filled, int total)
        {
            for (int i = 0; i < total; i++)
                Pic(parent, Foundry + (i < filled ? "Rank-Star" : "Rank-StarEmpty"), x + i * 48, y, 44, 44);
        }

        private void BuyCardBack(string path = null)
        {
            if (string.IsNullOrEmpty(path)) return;
            int index = Array.IndexOf(CardBackCatalog, path);
            if (index < 0) return;
            if (!profile.unlockedCardBacks.Contains(path))
            {
                if (profile.embers < CardBackCost[index]) { UiSound("deny"); return; }
                profile.embers -= CardBackCost[index];
                profile.unlockedCardBacks = profile.unlockedCardBacks.Concat(new[] { path }).ToArray();
                Sound("skill-unlock");
            }
            profile.cardBack = path;
            Save();
            Render();
        }

        // ---------------------------------------------------------------- queue

        private void DrawQueueOverlay()
        {
            ViewportShade("Queue shade", new Color(0, 0, 0, .7f));
            queueRing = Box(root, "Queue ring", 832, 300, 256, 256);
            var ring = queueRing.gameObject.AddComponent<RawImage>();
            ring.texture = Texture(Foundry + "QueueRing");
            if (ring.texture == null) ring.color = Color.clear;
            ring.raycastTarget = false;
            queueRing.pivot = new Vector2(.5f, .5f);
            queueRing.anchoredPosition = new Vector2(960, -428);
            OutlinedText(root, pendingQueue == QueueMode.Ranked ? "Searching for a ranked rival" : "Searching for a rival", 360, 600, 1200, 60, 44, Gold, true, TextAnchor.MiddleCenter, 24);
            queueLabel = Label(root, "0:00", 360, 664, 1200, 44, 30, White, false, TextAnchor.MiddleCenter);
            TableButton("Cancel", 760, 740, 400, 70, () => { CancelQueue(); UiSound("close"); Render(); });
        }

        private QueueTicket LocalTicket(QueueMode mode)
        {
            bool ranked = mode == QueueMode.Ranked;
            return new QueueTicket
            {
                PlayerId = LocalPlayer,
                Mode = mode,
                Mmr = ranked ? profile.rankedMmr : profile.casualMmr,
                MmrRd = ranked ? profile.rankedRd : profile.casualRd,
                DeckHash = string.Join(",", profile.deck).GetHashCode().ToString(),
                AllowAiFill = true
            };
        }

        private void BeginQueue(QueueMode mode)
        {
            if (queueSearching) return;
            if (!Campaign.LegalDeck(profile.deck, owned)) { status = "Fix your deck first."; Render(); return; }
            pendingQueue = mode;
            queueSearching = true;
            queueStarted = Time.unscaledTime;
            queueStatus = "Searching";
            matchmaker.Enqueue(LocalTicket(mode));
            UiSound("queue-tick");
            StartCoroutine(QueuePump());
            Render();
        }

        private void CancelQueue()
        {
            matchmaker.Cancel(LocalPlayer, pendingQueue);
            queueSearching = false;
            queueStatus = "";
        }

        private IEnumerator QueuePump()
        {
            while (queueSearching && page == Page.Play)
            {
                yield return null;
                float waited = Time.unscaledTime - queueStarted;
                if (queueLabel != null)
                {
                    int seconds = Mathf.FloorToInt(waited);
                    queueLabel.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
                }
                if (queueRing != null && !reduceMotion) queueRing.localRotation = Quaternion.Euler(0, 0, -waited * 140f);
                // Offline there is nobody to pair with, so a rival is filled in after a few seconds.
                var pair = matchmaker.TryPair(DateTime.UtcNow, 3.5);
                if (pair != null)
                {
                    queueSearching = false;
                    ShowMatchFound(pair);
                    yield break;
                }
            }
        }

        private void ShowMatchFound(MatchPair pair)
        {
            Sound("match-found");
            StartCoroutine(MatchFoundRoutine(pair));
        }

        private IEnumerator MatchFoundRoutine(MatchPair pair)
        {
            var layer = CardAnimationLayer();
            var banner = Box(layer, "Match found", 0, 340, 1920, 400);
            Pic(banner, Foundry + "MatchFound", 0, 0, 1920, 400);
            OutlinedText(banner, "MATCH FOUND", 0, 130, 1920, 90, 80, Gold, true, TextAnchor.MiddleCenter, 40);
            SafeText(banner, "Rival rating " + Mathf.RoundToInt((float)pair.B.Mmr), 0, 230, 1920, 44, 28, White, false, TextAnchor.MiddleCenter, 16);
            yield return new WaitForSecondsRealtime(1.5f);
            if (banner) Destroy(banner.gameObject);
            if (page == Page.Play) StartRankedSim(pair);
        }

        private void StartRankedSim(MatchPair pair)
        {
            var random = new System.Random();
            var themes = Catalog.ThemedDecks.Keys.ToArray();
            string theme = themes[random.Next(themes.Length)];
            var foeHero = Hero.All[random.Next(Hero.All.Length)];
            int first = random.Next(1, 7), second;
            do { second = random.Next(1, 7); } while (second == first);
            StopAllCoroutines();
            ResetMatchState();
            returnPage = Page.Play;
            tutorialMatch = false;
            duel = true;
            activeEncounterId = "";
            match = new Match(profile.deck, Catalog.Deck(Catalog.ThemedDecks[theme]), profile.hero, foeHero.Id,
                "P0" + first, "P0" + second, random.Next(), pair.AiFill ? 22 : 24);
            PlayerPrefs.SetInt("dawnwager.rated", 1);
            PlayerPrefs.SetInt("dawnwager.lastMode", (int)pair.A.Mode);
            PlayerPrefs.SetFloat("dawnwager.lastOppMmr", (float)pair.B.Mmr);
            PlayerPrefs.SetFloat("dawnwager.lastOppRd", (float)pair.B.MmrRd);
            Go(Page.Battle);
            DealOpeningHand();
        }

        // ---------------------------------------------------------------- career

        private static readonly (string id, string name, string text)[] AchievementList =
        {
            ("tutorial_clear", "Apprentice", "Win the tutorial match."),
            ("first_chapter", "First Light", "Clear the first campaign chapter."),
            ("first_seal", "Seal Bearer", "Claim your first Seal."),
            ("all_heroes", "Five Heroes", "Unlock every hero."),
            ("daily_win_practice", "Duelist", "Finish the daily duel quest."),
            ("daily_claim_seals", "Beacon Keeper", "Finish the daily Seals quest."),
            ("daily_play_chapter", "Pathwalker", "Finish the daily campaign quest.")
        };

        private void Career()
        {
            Banner("Career", "Your record, heroes and achievements");
            int games = profile.wins + profile.losses;
            ChromePanel(root, "Record", 60, 280, 560, 440, "Panel");
            OutlinedText(root, "Record", 60, 292, 560, 44, 32, Gold, true, TextAnchor.MiddleCenter, 18);
            string[] left = { "Wins", "Losses", "Win rate", "Seals claimed", "Campaign", "Ranked games", "Peak rank", "Embers" };
            string[] right =
            {
                profile.wins.ToString(), profile.losses.ToString(), games == 0 ? "—" : Mathf.RoundToInt(100f * profile.wins / games) + "%",
                profile.sealsClaimed.ToString(), Mathf.Min(profile.progress, Campaign.Chapters.Length) + " / " + Campaign.Chapters.Length + " chapters",
                profile.rankedGames.ToString(), Ladder.DisplayName(profile.peakAbsolute), profile.embers.ToString()
            };
            for (int i = 0; i < left.Length; i++)
            {
                Label(root, left[i], 96, 350 + i * 44, 240, 40, 24, Muted, false);
                Label(root, right[i], 340, 350 + i * 44, 250, 40, 24, White, true, TextAnchor.MiddleRight);
            }
            ChromePanel(root, "Heroes", 640, 280, 1220, 440, "Panel");
            OutlinedText(root, "Heroes", 640, 292, 1220, 44, 32, Gold, true, TextAnchor.MiddleCenter, 18);
            for (int i = 0; i < Hero.All.Length; i++)
            {
                var hero = Hero.All[i];
                bool unlocked = UnlockedHero(hero.Id);
                string id = hero.Id;
                float x = 668 + i * 236;
                var portrait = CirclePortrait(root, HeroArt(id), x + 14, 350, 180, Foundry + "HeroFrame", unlocked ? Color.white : new Color(.28f, .28f, .32f, 1f));
                Click(portrait, () => { ChooseHero(id); }, unlocked).transition = Selectable.Transition.None;
                if (profile.hero == id) Pic(root, Foundry + "UnitTokenGlow", x + 4, 340, 200, 200, Gold);
                OutlinedText(root, hero.Name, x, 540, 208, 36, 26, unlocked ? Gold : Quiet, true, TextAnchor.MiddleCenter, 14);
                SafeText(root, unlocked ? hero.Title : "Locked", x, 576, 208, 28, 18, Muted, false, TextAnchor.MiddleCenter, 11);
                SafeText(root, unlocked ? hero.Power : "Beat the matching chapter", x, 606, 208, 30, 17, unlocked ? White : Quiet, false, TextAnchor.MiddleCenter, 10);
                if (unlocked && profile.hero != id) TableButton("Play as", x + 34, 646, 140, 44, () => ChooseHero(id));
                else if (profile.hero == id) Label(root, "Selected", x, 646, 208, 44, 20, AllySage, true, TextAnchor.MiddleCenter);
            }
            ChromePanel(root, "Achievements", 60, 740, 1800, 300, "Panel");
            OutlinedText(root, "Achievements", 60, 750, 1800, 44, 30, Gold, true, TextAnchor.MiddleCenter, 18);
            for (int i = 0; i < AchievementList.Length; i++)
            {
                var a = AchievementList[i];
                bool got = profile.unlockedAchievements.Contains(a.id);
                float x = 90 + (i % 4) * 440, y = 806 + (i / 4) * 110;
                ColorBox(root, "badge", x, y, 420, 96, new Color(0, 0, 0, got ? .32f : .16f));
                Pic(root, Foundry + (got ? "SealLit" : "SealDim"), x + 12, y + 24, 48, 48);
                SafeText(root, a.name, x + 70, y + 8, 340, 34, 24, got ? Gold : Muted, true, TextAnchor.MiddleLeft, 14);
                SafeText(root, a.text, x + 70, y + 44, 340, 44, 17, got ? White : Quiet, false, TextAnchor.UpperLeft, 11);
            }
        }
    }
}
