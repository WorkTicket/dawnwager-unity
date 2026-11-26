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
            var foe = Campaign.Chapters[Campaign.ChapterAtProgress(profile.progress)];
            bool finished = profile.progress >= Campaign.Chapters.Length;
            (string art, string title, string sub, string meta, bool on, Action go)[] tiles =
            {
                ("Campaign", "Campaign", "Seven chapters of Ragnarök. Earn relics and grow your hero.", finished ? "All chapters cleared" : "Next: " + foe.Name, false,
                    () => { chapter = Campaign.ChapterAtProgress(profile.progress); Go(Page.Story); }),
                ("Ranked", "Ranked", "Climb ten ranks in six leagues. Stars are earned, not given.", Ladder.DisplayName(profile.ladderAbsolute, profile.legendPlace), ranked,
                    () => { pendingQueue = QueueMode.Ranked; UiSound("tab"); Render(); }),
                ("Casual", "Casual", "Unranked duels against players near your skill.", "No rank at stake", !ranked,
                    () => { pendingQueue = QueueMode.Casual; UiSound("tab"); Render(); }),
                ("Collection", "Collection", "Build decks and review every card you own.", owned.Count + " cards owned", false, () => Go(Page.Collection))
            };
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                float x = 110 + i * 430, y = 140 - (t.on ? 10 : 0);
                var panel = ChromePanel(root, t.title, x, y, 400, 540, t.on ? "PanelGold" : "Panel", true);
                Cover(root, Foundry + "Mode-" + t.art, x + 4, y + 4, 392, 330, Color.white);
                Gradient(root, "Tile fade", x + 4, y + 250, 392, 84, new Color(.03f, .043f, .065f, 0f), new Color(.03f, .043f, .065f, .95f), true);
                OutlinedText(root, t.title, x + 24, y + 336, 352, 50, 40, t.on ? Gold : new Color32(244, 227, 180, 255), true, TextAnchor.MiddleLeft, 24);
                SafeText(root, t.sub, x + 24, y + 392, 352, 64, 19, new Color32(207, 214, 218, 255), false, TextAnchor.UpperLeft, 13);
                Label(root, t.meta.ToUpperInvariant(), x + 24, y + 490, 352, 28, 15, Brass, false);
                var go = t.go;
                var button = panel.gameObject.AddComponent<Button>();
                button.targetGraphic = panel.GetComponent<Graphic>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { UiSound("click"); go(); });
                HoverCue(panel);
            }

            // --- deck bar
            ChromePanel(root, "Deck bar", 110, 700, 1700, 190, "Panel");
            var hero = Hero.Info(profile.hero);
            CirclePortrait(root, HeroArt(profile.hero), 130, 716, 150);
            OutlinedText(root, hero.Name + " · " + hero.Title, 300, 716, 600, 44, 30, Gold, true, TextAnchor.MiddleLeft, 16);
            bool legal = Campaign.LegalDeck(profile.deck, owned);
            Label(root, legal ? "Deck ready · 30 cards" : "Your deck is not legal", 300, 762, 500, 30, 20, legal ? AllySage : EnemyCopper, false);
            SafeText(root, hero.Power + ": " + hero.Description, 300, 794, 520, 44, 17, Muted, false, TextAnchor.UpperLeft, 12);
            TableButton("Edit deck", 300, 842, 170, 36, () => Go(Page.Collection));
            TableButton("Change hero", 486, 842, 190, 36, () => Go(Page.Collection));
            if (ranked)
            {
                FitSafe(root, Foundry + RankMedalPath(profile.ladderAbsolute), 860, 712, 130, 130);
                OutlinedText(root, Ladder.DisplayName(profile.ladderAbsolute, profile.legendPlace), 1000, 724, 420, 44, 32, Gold, true, TextAnchor.MiddleLeft, 18);
                if (profile.ladderAbsolute < Ladder.LegendOrdinal)
                    DrawRankStars(root, 1000, 770, profile.ladderStars, Ladder.StarsPerRank);
                Label(root, "Streak " + profile.winStreak + " · Rating " + Mathf.RoundToInt((float)profile.rankedMmr), 1000, 820, 420, 26, 17, Muted, false);
                if (profile.apprenticeLeft > 0)
                    Label(root, profile.apprenticeLeft + " apprentice games: no star loss", 860, 850, 560, 26, 16, Frost, false);
            }
            else
            {
                OutlinedText(root, "Casual duel", 860, 730, 420, 44, 32, Gold, true, TextAnchor.MiddleLeft, 18);
                Label(root, "Rating " + Mathf.RoundToInt((float)profile.casualMmr), 860, 780, 420, 26, 18, Muted, false);
            }
            TableButton(ranked ? "Play Ranked" : "Play Casual", 1450, 722, 330, 86, () => BeginQueue(pendingQueue), legal && !queueSearching, true);
            TableButton("Practice skirmish", 1450, 828, 330, 44, () => StartMatch(true), legal);
            if (!string.IsNullOrEmpty(status)) SafeText(root, status, 860, 868, 560, 22, 16, Gold, false, TextAnchor.MiddleLeft, 11);

            // --- daily quests + card backs
            RefreshDailies();
            ChromePanel(root, "Dailies", 110, 910, 860, 150, "Panel");
            OutlinedText(root, "Daily quests", 136, 916, 300, 34, 22, Gold, true, TextAnchor.MiddleLeft, 14);
            for (int i = 0; i < profile.dailies.Count && i < 3; i++)
            {
                var quest = profile.dailies[i];
                float qx = 136 + i * 280;
                SafeText(root, quest.label, qx, 956, 262, 26, 17, quest.progress >= quest.target ? AllySage : White, false, TextAnchor.MiddleLeft, 11);
                ColorBox(root, "bar", qx, 992, 200, 10, new Color(1, 1, 1, .12f));
                ColorBox(root, "bar fill", qx, 992, 200 * Mathf.Clamp01((float)quest.progress / Mathf.Max(1, quest.target)), 10, quest.claimed ? AllySage : Brass);
                Label(root, quest.progress + "/" + quest.target + "  +" + quest.rewardEmbers, qx, 1006, 262, 24, 15, Muted, false);
            }
            ChromePanel(root, "Backs", 1000, 910, 810, 150, "Panel");
            OutlinedText(root, "Card backs · " + profile.embers + " Embers", 1026, 916, 500, 34, 22, Gold, true, TextAnchor.MiddleLeft, 14);
            for (int i = 0; i < CardBackCatalog.Length; i++)
            {
                string path = CardBackCatalog[i];
                int index = i;
                bool have = profile.unlockedCardBacks.Contains(path);
                bool equipped = CardBackArt == path;
                float x = 1026 + i * 195;
                var back = Pic(root, path, x, 956, 70, 98);
                if (!have) back.color = new Color(.5f, .5f, .5f, 1f);
                if (equipped) Pic(root, Foundry + "CardPlayableGlow", x - 6, 952, 82, 106, Gold);
                string label = equipped ? "Equipped" : have ? "Equip" : CardBackCost[index] + " Embers";
                TableButton(label, x + 78, 988, 106, 40, () => BuyCardBack(path), !equipped && (have || profile.embers >= CardBackCost[index]));
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
            Gradient(banner, "Band", 0, 0, 1920, 400, new Color(.016f, .024f, .04f, .85f), new Color(.016f, .024f, .04f, .85f), true);
            var heroHolder = Box(banner, "Hero", 240, -40, 420, 440);
            Shade(heroHolder, 40, 360, 340, 50, new Color(0, 0, 0, .8f));
            CutFit(heroHolder, "Heroes/" + profile.hero, 420, 400, Color.white);
            OutlinedText(banner, "VS", 1060, 120, 200, 150, 110, new Color32(244, 227, 180, 255), true, TextAnchor.MiddleCenter, 60);
            OutlinedText(banner, "MATCH FOUND", 0, 20, 1920, 90, 72, Gold, true, TextAnchor.MiddleCenter, 40);
            SafeText(banner, "Rival rating " + Mathf.RoundToInt((float)pair.B.Mmr), 1000, 280, 920, 44, 28, White, false, TextAnchor.MiddleCenter, 16);
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
