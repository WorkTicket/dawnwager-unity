using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        [Serializable] private sealed class HeroSkillBuild
        {
            public string hero;
            public string[] nodes = Array.Empty<string>();
        }

        private string skillFocusId;
        private string activeEncounterId = "";
        private string[] pendingRelicChoices = Array.Empty<string>();
        private string pendingRelicEncounter = "";

        private void EnsureCampaignProfile()
        {
            profile.completedEncounters = profile.completedEncounters ?? Array.Empty<string>();
            profile.ownedRelics = profile.ownedRelics ?? Array.Empty<string>();
            profile.equippedRelics = profile.equippedRelics ?? Array.Empty<string>();
            profile.skillBuilds = profile.skillBuilds ?? new List<HeroSkillBuild>();
            MigrateCampaignFromProgress();
        }

        private void MigrateCampaignFromProgress()
        {
            // Derive completed main encounters from legacy chapter progress without replaying rewards.
            var mains = CampaignMeta.Encounters.Where(e => !e.IsOptional).ToArray();
            var done = new HashSet<string>(profile.completedEncounters ?? Array.Empty<string>());
            for (int i = 0; i < profile.progress && i < mains.Length; i++)
                done.Add(mains[i].Id);
            profile.completedEncounters = done.ToArray();
        }

        private bool EncounterCleared(string id) =>
            profile.completedEncounters != null && profile.completedEncounters.Contains(id);

        private int EarnedSkillPoints()
        {
            EnsureCampaignProfile();
            return profile.completedEncounters.Sum(id =>
            {
                var enc = CampaignMeta.Encounters.FirstOrDefault(e => e.Id == id);
                return enc?.FirstWinSkillPoints ?? 0;
            });
        }

        private string[] NodesForHero(string hero)
        {
            EnsureCampaignProfile();
            var build = profile.skillBuilds.FirstOrDefault(b => b.hero == hero);
            return build?.nodes ?? Array.Empty<string>();
        }

        private void SetNodesForHero(string hero, string[] nodes)
        {
            EnsureCampaignProfile();
            var build = profile.skillBuilds.FirstOrDefault(b => b.hero == hero);
            if (build == null)
            {
                build = new HeroSkillBuild { hero = hero };
                profile.skillBuilds.Add(build);
            }
            build.nodes = nodes ?? Array.Empty<string>();
        }

        private int SpentSkillPoints(string hero) =>
            NodesForHero(hero).Sum(id => CampaignMeta.Passive(id)?.Cost ?? 0);

        private int UnspentSkillPoints(string hero) =>
            Math.Max(0, EarnedSkillPoints() - SpentSkillPoints(hero));

        private bool PrerequisitesMet(CampaignPassive node, string hero)
        {
            var owned = new HashSet<string>(NodesForHero(hero));
            return node.Prerequisites.All(owned.Contains);
        }

        private bool CanBuyNode(CampaignPassive node, string hero) =>
            UnlockedHero(hero)
            && !NodesForHero(hero).Contains(node.Id)
            && PrerequisitesMet(node, hero)
            && UnspentSkillPoints(hero) >= node.Cost;

        private void BuyNode(string id)
        {
            var node = CampaignMeta.Passive(id);
            if (node == null || !CanBuyNode(node, profile.hero)) return;
            var list = NodesForHero(profile.hero).ToList();
            list.Add(id);
            SetNodesForHero(profile.hero, list.ToArray());
            Sound("skill-unlock");
            Save();
            Render();
        }

        private void RefundHeroSkills()
        {
            SetNodesForHero(profile.hero, Array.Empty<string>());
            Save();
            Render();
        }

        private void EquipRelic(string relicId)
        {
            EnsureCampaignProfile();
            if (string.IsNullOrEmpty(relicId) || !profile.ownedRelics.Contains(relicId)) return;
            var eq = profile.equippedRelics.ToList();
            if (eq.Contains(relicId)) return;
            if (eq.Count >= 2) eq.RemoveAt(0);
            eq.Add(relicId);
            profile.equippedRelics = eq.ToArray();
            Save();
        }

        private CampaignRules SnapshotCampaignRules(string encounterId)
        {
            EnsureCampaignProfile();
            if (duel || tutorialMatch) return CampaignRules.Empty;
            return new CampaignRules(NodesForHero(profile.hero), profile.equippedRelics, encounterId ?? "");
        }

        private string[] BuildRelicOffer(string featuredName)
        {
            EnsureCampaignProfile();
            var owned = new HashSet<string>(profile.ownedRelics);
            var all = CampaignMeta.Relics.Select(r => r.Id).ToList();
            if (owned.Count >= all.Count) return Array.Empty<string>();
            var offer = new List<string>();
            string featured = CampaignMeta.RelicIdFromName(featuredName);
            if (!string.IsNullOrEmpty(featured) && !owned.Contains(featured)) offer.Add(featured);
            foreach (var id in all.Where(id => !owned.Contains(id) && !offer.Contains(id)))
            {
                offer.Add(id);
                if (offer.Count >= 3) break;
            }
            return offer.ToArray();
        }

        private void ClaimFirstClear(string encounterId, string featuredRelicName)
        {
            EnsureCampaignProfile();
            if (string.IsNullOrEmpty(encounterId) || EncounterCleared(encounterId)) return;
            var list = profile.completedEncounters.ToList();
            list.Add(encounterId);
            profile.completedEncounters = list.ToArray();
            pendingRelicEncounter = encounterId;
            pendingRelicChoices = BuildRelicOffer(featuredRelicName);
            if (pendingRelicChoices.Length == 0)
            {
                pendingRelicEncounter = "";
                pendingRelicChoices = Array.Empty<string>();
            }
            Save();
        }

        private void ChoosePendingRelic(string relicId)
        {
            EnsureCampaignProfile();
            if (pendingRelicChoices == null || !pendingRelicChoices.Contains(relicId)) return;
            if (!profile.ownedRelics.Contains(relicId))
            {
                var owned = profile.ownedRelics.ToList();
                owned.Add(relicId);
                profile.ownedRelics = owned.ToArray();
            }
            EquipRelic(relicId);
            pendingRelicChoices = Array.Empty<string>();
            pendingRelicEncounter = "";
            Sound("relic-reveal");
            Save();
            Render();
        }

        private void SkipPendingRelic()
        {
            pendingRelicChoices = Array.Empty<string>();
            pendingRelicEncounter = "";
            Save();
            Render();
        }
    }
}
