using System;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    /// <summary>The Ragnarök map: seven main encounters along the river and three optional detours.</summary>
    public sealed partial class Game
    {
        private string mapFocus = "";

        private static readonly Vector2[] MainNodes =
        {
            new Vector2(1260, 860), new Vector2(1060, 740), new Vector2(880, 630), new Vector2(860, 470),
            new Vector2(1040, 350), new Vector2(1280, 300), new Vector2(1620, 230)
        };

        private static Vector2 OptionalNode(string id)
        {
            switch (id)
            {
                case "O01": return new Vector2(1520, 740);
                case "O02": return new Vector2(690, 540);
                default: return new Vector2(1240, 470);
            }
        }

        private bool EncounterAvailable(CampaignEncounter enc)
        {
            if (!enc.IsOptional)
            {
                int index = Array.FindIndex(CampaignMeta.Encounters.Where(e => !e.IsOptional).ToArray(), e => e.Id == enc.Id);
                return Campaign.CanPlayChapter(index, profile.progress);
            }
            return string.IsNullOrEmpty(enc.UnlockAfter) || EncounterCleared(enc.UnlockAfter);
        }

        private int MainIndex(CampaignEncounter enc) =>
            Array.FindIndex(CampaignMeta.Encounters.Where(e => !e.IsOptional).ToArray(), e => e.Id == enc.Id);

        private void MapLine(Vector2 a, Vector2 b, bool lit)
        {
            Vector2 d = b - a;
            var line = Box(root, "Route", a.x, a.y, d.magnitude, lit ? 7 : 4);
            line.pivot = new Vector2(0, .5f);
            line.anchoredPosition = new Vector2(a.x, -a.y);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg);
            var image = line.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = lit ? new Color(Brass.r, Brass.g, Brass.b, .9f) : new Color(1, 1, 1, .22f);
            image.raycastTarget = false;
        }

        private void Story()
        {
            var mains = CampaignMeta.Encounters.Where(e => !e.IsOptional).ToArray();
            var all = CampaignMeta.Encounters;
            ColorBox(root, "Map shade", 0, 128, 1920, 952, new Color(0, 0, 0, .18f));
            Banner("Ragnarök", "Choose your route. Main battles unlock in order; detours are optional.");
            if (string.IsNullOrEmpty(mapFocus) || all.All(e => e.Id != mapFocus))
                mapFocus = (mains.FirstOrDefault(e => !EncounterCleared(e.Id)) ?? mains.Last()).Id;

            for (int i = 0; i + 1 < mains.Length && i + 1 < MainNodes.Length; i++)
                MapLine(MainNodes[i], MainNodes[i + 1], EncounterCleared(mains[i].Id));
            foreach (var opt in all.Where(e => e.IsOptional))
            {
                var parent = all.FirstOrDefault(e => e.Id == opt.UnlockAfter);
                int pi = parent != null ? MainIndex(parent) : -1;
                if (pi >= 0 && pi < MainNodes.Length) MapLine(MainNodes[pi], OptionalNode(opt.Id), EncounterAvailable(opt));
            }
            foreach (var enc in all)
            {
                Vector2 at;
                if (enc.IsOptional) at = OptionalNode(enc.Id);
                else { int i = MainIndex(enc); if (i < 0 || i >= MainNodes.Length) continue; at = MainNodes[i]; }
                DrawMapNode(enc, at);
            }
            DrawEncounterPanel(all.First(e => e.Id == mapFocus));
        }

        private void DrawMapNode(CampaignEncounter enc, Vector2 at)
        {
            bool open = EncounterAvailable(enc);
            bool cleared = EncounterCleared(enc.Id);
            bool focus = mapFocus == enc.Id;
            float size = enc.IsOptional ? 78 : 96;
            var node = Box(root, enc.Id, at.x - size / 2, at.y - size / 2, size, size);
            if (focus) Pic(node, Foundry + "UnitTokenGlow", -size * .22f, -size * .22f, size * 1.44f, size * 1.44f, Gold);
            string art = cleared ? "Campaign/NodePurchased" : open ? "Campaign/NodeAvailable" : "Campaign/NodeLocked";
            Pic(node, art, 0, 0, size, size);
            string icon = enc.IsOptional ? "Campaign/EncounterShrine" : "Campaign/EncounterBoss";
            Pic(node, icon, size * .25f, size * .25f, size * .5f, size * .5f, open ? Color.white : new Color(.6f, .6f, .6f, 1));
            if (cleared) Pic(node, Foundry + "SealLit", size * .58f, -8, size * .46f, size * .46f);
            string name = enc.Name;
            ColorBox(root, "Label back", at.x - 118, at.y + size / 2 + 3, 236, 28, new Color(.016f, .024f, .04f, .6f));
            OutlinedText(root, name, at.x - 130, at.y + size / 2 + 2, 260, 30, 20, open ? White : Quiet, true, TextAnchor.MiddleCenter, 11);
            string id = enc.Id;
            Click(node, () => { mapFocus = id; UiSound("tab"); Render(); }).transition = UnityEngine.UI.Selectable.Transition.None;
            HoverCue(node);
        }

        private void DrawEncounterPanel(CampaignEncounter enc)
        {
            ChromePanel(root, "Encounter", 40, 280, 500, 770, "PanelGold");
            bool open = EncounterAvailable(enc);
            bool cleared = EncounterCleared(enc.Id);
            OutlinedText(root, enc.Name, 60, 296, 460, 54, 36, Gold, true, TextAnchor.MiddleLeft, 18);
            SafeText(root, enc.IsOptional ? "Optional detour" : "Main route · chapter " + (MainIndex(enc) + 1), 62, 350, 460, 28, 20, Muted, false, TextAnchor.MiddleLeft, 12);
            string story;
            if (!enc.IsOptional)
            {
                int index = MainIndex(enc);
                var ch = Campaign.Chapters[Mathf.Clamp(index, 0, Campaign.Chapters.Length - 1)];
                story = cleared ? ch.After : ch.Before;
                SafeText(root, "Foe: " + ch.Boss + " · " + ch.Health + " health", 62, 380, 460, 30, 21, EnemyCopper, true, TextAnchor.MiddleLeft, 12);
                var hero = Hero.Info(ch.Hero);
                CirclePortrait(root, HeroArt(ch.Hero), 380, 400, 130, HeroFrameArt(ch.Hero));
            }
            else story = "Special rule: " + enc.BossOrRule + ".";
            SafeText(root, story, 62, 420, enc.IsOptional ? 440 : 310, 300, 21, White, false, TextAnchor.UpperLeft, 13);
            float y = 740;
            Label(root, "First clear rewards", 62, y, 440, 30, 22, Gold, true);
            string reward = "+" + enc.FirstWinSkillPoints + " skill point" + (enc.FirstWinSkillPoints == 1 ? "" : "s");
            if (!string.IsNullOrEmpty(enc.Relic)) reward += " · relic: " + enc.Relic;
            if (!enc.IsOptional && MainIndex(enc) == profile.progress) reward += " · new cards";
            SafeText(root, cleared ? "Claimed — replays give no repeat rewards." : reward, 62, y + 32, 440, 56, 19, cleared ? Muted : White, false, TextAnchor.UpperLeft, 12);
            Label(root, "Skill points to spend: " + UnspentSkillPoints(profile.hero), 62, y + 90, 440, 28, 19, Frost);
            if (!open)
            {
                string needs = enc.IsOptional ? "Clear " + enc.UnlockAfter + " first." : "Clear the previous chapter first.";
                Label(root, needs, 62, 960, 440, 30, 20, EnemyCopper, false, TextAnchor.MiddleCenter);
                TableButton("Locked", 60, 988, 220, 52, null, false);
            }
            else
            {
                string id = enc.Id;
                TableButton(cleared ? "Replay" : "Begin Battle", 60, 988, 250, 52, () =>
                {
                    if (enc.IsOptional) StartOptionalEncounter(id);
                    else { chapter = MainIndex(enc); StartMatch(false); }
                }, true, true);
            }
            TableButton("Skills", 326, 988, 100, 52, () => Go(Page.Skills));
            TableButton("Deck", 436, 988, 96, 52, () => Go(Page.Collection));
        }

        private void StartOptionalEncounter(string encounterId)
        {
            var enc = CampaignMeta.Encounters.FirstOrDefault(e => e.Id == encounterId);
            if (enc == null || !enc.IsOptional) return;
            if (!string.IsNullOrEmpty(enc.UnlockAfter) && !EncounterCleared(enc.UnlockAfter)) return;
            StopAllCoroutines();
            if (!Campaign.LegalDeck(profile.deck, owned))
            {
                profile.deck = Campaign.Starter;
                Save();
            }
            ResetMatchState();
            returnPage = Page.Story;
            duel = false;
            tutorialMatch = false;
            activeEncounterId = enc.Id;
            chapter = Mathf.Clamp(chapter, 0, Campaign.Chapters.Length - 1);
            var foe = Campaign.Chapters[chapter];
            var rules = SnapshotCampaignRules(enc.Id);
            match = new Match(profile.deck, Campaign.EnemyDeck(chapter), profile.hero, foe.Hero, foe.First, foe.Second,
                Environment.TickCount, foe.Health, rules);
            Go(Page.Battle);
            DealOpeningHand();
        }
    }
}
