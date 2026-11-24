using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    public sealed class CampaignEncounter
    {
        public readonly string Id, Route, Name, BossOrRule, UnlockAfter, Relic;
        public readonly int FirstWinSkillPoints;
        public bool IsOptional => Route.Equals("Optional", StringComparison.OrdinalIgnoreCase);
        public CampaignEncounter(string id, string route, string name, string bossOrRule, string unlockAfter, int points, string relic)
        {
            Id = id; Route = route; Name = name; BossOrRule = bossOrRule;
            UnlockAfter = unlockAfter ?? ""; Relic = relic ?? ""; FirstWinSkillPoints = points;
        }
    }

    public sealed class CampaignPassive
    {
        public readonly string Id, Branch, DisplayName, Prerequisite, Trigger, Effect;
        public readonly int Column, Row, Cost;
        public CampaignPassive(string id, string branch, string name, int col, int row, int cost, string prereq, string trigger, string effect)
        {
            Id = id; Branch = branch; DisplayName = name; Column = col; Row = row; Cost = cost;
            Prerequisite = prereq ?? ""; Trigger = trigger ?? ""; Effect = effect ?? "";
        }
        public string[] Prerequisites => string.IsNullOrWhiteSpace(Prerequisite)
            ? Array.Empty<string>()
            : Prerequisite.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public sealed class RelicDef
    {
        public readonly string Id, Name, Trigger, Summary;
        public RelicDef(string id, string name, string trigger, string summary)
        {
            Id = id; Name = name; Trigger = trigger; Summary = summary;
        }
    }

    /// <summary>Read-only combat modifiers snapped at match start (campaign only).</summary>
    public sealed class CampaignRules
    {
        public static readonly CampaignRules Empty = new CampaignRules(Array.Empty<string>(), Array.Empty<string>(), "");
        public readonly string[] PassiveIds;
        public readonly string[] RelicIds;
        public readonly string EncounterId;
        public readonly HashSet<string> Fired = new HashSet<string>();
        public int MaxHeroHealth = 24;
        public int OrderDiscount;
        public bool FirstUnitBuffed, FirstMarchBuffed, FirstGuardBuffed, FirstRushBuffed;
        public bool UsedFerryBell, UsedFrostNail, UsedCharredCoin, UsedLastLantern;
        public bool AttackedThisRound;
        public bool PlayedOrderThisMatch;
        public bool SealAwardedThisMatch;

        public CampaignRules(string[] passives, string[] relics, string encounterId)
        {
            PassiveIds = passives ?? Array.Empty<string>();
            RelicIds = relics ?? Array.Empty<string>();
            EncounterId = encounterId ?? "";
            foreach (var id in PassiveIds)
            {
                if (id == "B1") MaxHeroHealth += 2;
                if (id == "B5") MaxHeroHealth += 3;
            }
        }

        public bool Has(string id) => PassiveIds.Contains(id) || RelicIds.Contains(id);
        public bool Once(string key) => Fired.Add(key);
    }

    public static class CampaignMeta
    {
        public static readonly RelicDef[] Relics =
        {
            new RelicDef("ferry-bell", "Ferry Bell", "FirstMarch", "Heal 2 on your first march."),
            new RelicDef("frost-nail", "Frost Nail", "FirstSite", "Your first Site gives its lane +1 Seal Power this round."),
            new RelicDef("charred-coin", "Charred Coin", "FirstHeroPower", "Gain 1 Charge after your first paid hero power."),
            new RelicDef("last-lantern", "Last Lantern", "LowHealthSurvive", "The first time you fall to 6 health or below and survive, heal 2."),
        };

        private static CampaignEncounter[] encounters;
        private static CampaignPassive[] passives;

        public static CampaignEncounter[] Encounters
        {
            get { EnsureLoaded(); return encounters; }
        }

        public static CampaignPassive[] Passives
        {
            get { EnsureLoaded(); return passives; }
        }

        public static CampaignEncounter MainAt(int chapterIndex)
        {
            var mains = Encounters.Where(e => !e.IsOptional).ToArray();
            if (chapterIndex < 0 || chapterIndex >= mains.Length) return null;
            return mains[chapterIndex];
        }

        public static CampaignPassive Passive(string id) => Passives.FirstOrDefault(p => p.Id == id);

        public static RelicDef Relic(string id) => Relics.FirstOrDefault(r =>
            r.Id == id || r.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

        public static string RelicIdFromName(string name)
        {
            var r = Relics.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return r?.Id ?? "";
        }

        private static void EnsureLoaded()
        {
            if (encounters != null && passives != null) return;
            encounters = ParseEncounters(LoadCsv("Data/Campaign-Encounters"));
            passives = ParsePassives(LoadCsv("Data/Campaign-Passives"));
            if (encounters.Length == 0) encounters = FallbackEncounters();
            if (passives.Length == 0) passives = FallbackPassives();
        }

        private static string LoadCsv(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            return asset != null ? asset.text : "";
        }

        private static CampaignEncounter[] ParseEncounters(string csv)
        {
            var list = new List<CampaignEncounter>();
            if (string.IsNullOrWhiteSpace(csv)) return list.ToArray();
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                var c = SplitCsv(lines[i]);
                if (c.Length < 6) continue;
                int.TryParse(c[5], out int pts);
                list.Add(new CampaignEncounter(c[0], c[1], c[2], c[3], c[4], pts, c.Length > 6 ? c[6] : ""));
            }
            return list.ToArray();
        }

        private static CampaignPassive[] ParsePassives(string csv)
        {
            var list = new List<CampaignPassive>();
            if (string.IsNullOrWhiteSpace(csv)) return list.ToArray();
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                var c = SplitCsv(lines[i]);
                if (c.Length < 9) continue;
                int.TryParse(c[3], out int col);
                int.TryParse(c[4], out int row);
                int.TryParse(c[5], out int cost);
                list.Add(new CampaignPassive(c[0], c[1], c[2], col, row, cost, c[6], c[7], c[8]));
            }
            return list.ToArray();
        }

        private static string[] SplitCsv(string line)
        {
            var parts = new List<string>();
            bool quoted = false;
            var cur = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"') { quoted = !quoted; continue; }
                if (ch == ',' && !quoted) { parts.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(ch);
            }
            parts.Add(cur.ToString());
            return parts.ToArray();
        }

        private static CampaignEncounter[] FallbackEncounters() => new[]
        {
            new CampaignEncounter("C01", "Main", "Fimbulwinter", "Runa Ice Ferrier", "", 1, "Ferry Bell"),
            new CampaignEncounter("C02", "Main", "Heimdalls Horn", "The Oathguard", "C01", 1, ""),
            new CampaignEncounter("C03", "Main", "The Road to Hel", "Hel Keeper of the Names", "C02", 1, ""),
            new CampaignEncounter("C04", "Main", "Naglfar Sets Sail", "The Mountain Host", "C03", 1, "Charred Coin"),
            new CampaignEncounter("C05", "Main", "Fenrir at Vigridr", "Fenrir Chainbreaker", "C04", 1, ""),
            new CampaignEncounter("C06", "Main", "Loki at the Broken Bridge", "Loki Oathbreaker", "C05", 1, ""),
            new CampaignEncounter("C07", "Main", "Surtrs Fire", "Surtr the Coming Fire", "C06", 1, "Last Lantern"),
            new CampaignEncounter("O01", "Optional", "The Frost Ford", "Start with a quiet Site in each lane", "C01", 1, "Frost Nail"),
            new CampaignEncounter("O02", "Optional", "The Toll at Gjoll", "Enemy starts with one Guard in each lane", "C03", 1, "Ferry Bell"),
            new CampaignEncounter("O03", "Optional", "The Buried Forge", "First Site played by each side gains one Shield", "C05", 1, "Charred Coin"),
        };

        private static CampaignPassive[] FallbackPassives() => new[]
        {
            new CampaignPassive("B1", "Bastion", "Set the Rampart", 1, 0, 1, "", "MatchStart", "Start with +2 maximum and current hero health."),
            new CampaignPassive("B2", "Bastion", "Keep the Watch", 0, 1, 1, "B1", "FirstGuardFromHand", "The first Guard Unit you play from hand gains +1 Health."),
            new CampaignPassive("B3", "Bastion", "Field Dressing", 2, 1, 1, "B1", "FirstAllyDeath", "Heal your hero for 2 the first time an allied Unit dies."),
            new CampaignPassive("B4", "Bastion", "Hold This Ground", 0, 2, 2, "B2", "FirstEnemySeal", "The first time the enemy claims a Seal heal your hero for 2."),
            new CampaignPassive("B5", "Bastion", "Iron Reserve", 2, 2, 2, "B3", "MatchStart", "Start with +3 maximum and current hero health."),
            new CampaignPassive("B6", "Bastion", "Stand at Dawn", 1, 3, 2, "B4+B5", "FirstRoundEndAtLowHealth", "Once per match, if your hero has 6 or less health and the match is ongoing at round end, heal 3."),
            new CampaignPassive("W1", "Warcraft", "First Through", 4, 0, 1, "", "FirstUnitFromHand", "The first Unit you play from hand gains +1 Attack this round."),
            new CampaignPassive("W2", "Warcraft", "Marching Cadence", 3, 1, 1, "W1", "FirstMarch", "The first Unit you march gets +1 Attack this round."),
            new CampaignPassive("W3", "Warcraft", "Press the Opening", 5, 1, 1, "W1", "FirstContestedUnitPlay", "The first Unit you play from hand into the contested lane gets +1 Attack this round."),
            new CampaignPassive("W4", "Warcraft", "Steel Followthrough", 3, 2, 2, "W2", "FirstOrderAfterAttack", "Your first Order played after an allied Unit attacks costs 1 less Charge this round."),
            new CampaignPassive("W5", "Warcraft", "Oath of the Front", 5, 2, 2, "W3", "FirstSealWon", "When you claim your first Seal heal your hero for 2."),
            new CampaignPassive("W6", "Warcraft", "Break the Line", 4, 3, 2, "W4+W5", "FirstRushFromHand", "The first Rush Unit you play from hand gains +1 permanent Attack."),
            new CampaignPassive("R1", "Runecraft", "Measured Reading", 7, 0, 1, "", "FirstOrderFromHand", "The first Order you play from hand costs 1 less Charge."),
            new CampaignPassive("R2", "Runecraft", "Ink Before Iron", 6, 1, 1, "R1", "FirstAlliedSiteFromHand", "Heal your hero 1 when you first play a Site from hand."),
            new CampaignPassive("R3", "Runecraft", "Sigil Keeper", 8, 1, 1, "R1", "FirstHeroPower", "Heal your hero 1 after the first hero power you activate."),
            new CampaignPassive("R4", "Runecraft", "Quiet Rune", 6, 2, 2, "R2", "FirstAlliedSeal", "After your first Seal award your first Order in the following round costs 1 less Charge."),
            new CampaignPassive("R5", "Runecraft", "Last Page", 8, 2, 2, "R3", "FirstProphecyResolved", "After the first prophecy resolves, draw one card if the match is ongoing."),
            new CampaignPassive("R6", "Runecraft", "Living Spellbook", 7, 3, 2, "R4+R5", "FirstSiteAfterOrder", "The first Site you play after an Order this match costs 1 less Charge."),
        };
    }
}
