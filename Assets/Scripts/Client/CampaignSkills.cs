using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    /// <summary>Skill tree (three branches of passives) and the two-slot relic loadout.</summary>
    public sealed partial class Game
    {
        private static Color BranchColor(string branch) =>
            branch == "Bastion" ? new Color32(150, 196, 232, 255) : branch == "Warcraft" ? new Color32(236, 150, 120, 255) : new Color32(190, 160, 236, 255);

        private static Vector2 NodeCenter(CampaignPassive node) => new Vector2(130 + node.Column * 190, 372 + node.Row * 118);

        private void SkillTree()
        {
            Banner("Skills", "Spend points earned by first-clearing encounters. Passives apply to campaign battles only.");
            string hero = profile.hero;
            int unspent = UnspentSkillPoints(hero);
            Pic(root, "Campaign/PointToken", 1360, 150, 48, 48);
            Label(root, unspent + " points · " + SpentSkillPoints(hero) + " spent", 1416, 146, 360, 56, 26, Gold, true);
            TableButton("Refund all", 1696, 146, 190, 56, () => { if (NodesForHero(hero).Length > 0) RefundHeroSkills(); }, NodesForHero(hero).Length > 0);
            SafeText(root, Hero.Info(hero).Name + "'s build", 700, 140, 600, 56, 24, White, true, TextAnchor.MiddleRight, 14);

            ChromePanel(root, "Tree", 40, 280, 1840, 500, "Panel");
            string[] branches = { "Bastion", "Warcraft", "Runecraft" };
            for (int i = 0; i < 3; i++)
                OutlinedText(root, branches[i], 40 + 130 + i * 570 - 95 + 95, 290, 380, 36, 28, BranchColor(branches[i]), true, TextAnchor.MiddleCenter, 16);
            var owned = NodesForHero(hero);
            var passives = CampaignMeta.Passives;
            foreach (var node in passives)
                foreach (var req in node.Prerequisites)
                {
                    var from = passives.FirstOrDefault(p => p.Id == req);
                    if (from != null) SkillLine(NodeCenter(from), NodeCenter(node), owned.Contains(req) && owned.Contains(node.Id));
                }
            foreach (var node in passives) DrawSkillNode(node, owned);
            DrawSkillDetail(hero);
            DrawRelics();
        }

        private void SkillLine(Vector2 a, Vector2 b, bool lit)
        {
            Vector2 d = b - a;
            var line = Box(root, "Link", a.x, a.y, d.magnitude, 6);
            line.pivot = new Vector2(0, .5f);
            line.anchoredPosition = new Vector2(a.x, -a.y);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg);
            var image = line.gameObject.AddComponent<Image>();
            image.color = lit ? Brass : new Color(1, 1, 1, .2f);
            image.raycastTarget = false;
        }

        private void DrawSkillNode(CampaignPassive node, string[] ownedNodes)
        {
            string hero = profile.hero;
            bool have = ownedNodes.Contains(node.Id);
            bool can = CanBuyNode(node, hero);
            bool focus = skillFocusId == node.Id;
            var at = NodeCenter(node);
            const float size = 84;
            var rect = Box(root, node.Id, at.x - size / 2, at.y - size / 2, size, size);
            if (focus) Pic(rect, Foundry + "UnitTokenGlow", -16, -16, size + 32, size + 32, Gold);
            Pic(rect, have ? "Campaign/NodePurchased" : can ? "Campaign/NodeAvailable" : PrerequisitesMet(node, hero) ? "Campaign/NodeNormal" : "Campaign/NodeLocked", 0, 0, size, size);
            string icon = node.Branch == "Bastion" ? "Campaign/IconBastion" : node.Branch == "Warcraft" ? "Campaign/IconWarcraft" : "Campaign/IconRunecraft";
            Pic(rect, icon, size * .22f, size * .22f, size * .56f, size * .56f, have || can ? Color.white : new Color(.55f, .55f, .55f, 1));
            Label(rect, node.Cost.ToString(), size * .62f, size * .62f, size * .4f, size * .4f, 20, have ? Gold : White, true, TextAnchor.MiddleCenter);
            SafeText(root, node.DisplayName, at.x - 92, at.y + 46, 184, 44, 16, have ? Gold : can ? White : Quiet, true, TextAnchor.UpperCenter, 10);
            string id = node.Id;
            Click(rect, () => { skillFocusId = id; UiSound("tab"); Render(); }).transition = Selectable.Transition.None;
            HoverCue(rect);
        }

        private void DrawSkillDetail(string hero)
        {
            ChromePanel(root, "Skill detail", 40, 800, 1080, 250, "PanelGold");
            var node = string.IsNullOrEmpty(skillFocusId) ? null : CampaignMeta.Passive(skillFocusId);
            if (node == null)
            {
                SafeText(root, "Select a passive to read what it does.", 70, 800, 1020, 250, 26, Muted, false, TextAnchor.MiddleCenter, 14);
                return;
            }
            bool have = NodesForHero(hero).Contains(node.Id);
            OutlinedText(root, node.DisplayName, 70, 812, 700, 48, 34, BranchColor(node.Branch), true, TextAnchor.MiddleLeft, 18);
            SafeText(root, node.Branch + " · cost " + node.Cost + (node.Prerequisites.Length > 0 ? " · needs " + string.Join(" + ", node.Prerequisites.Select(p => CampaignMeta.Passive(p)?.DisplayName ?? p)) : ""),
                72, 860, 1000, 28, 20, Muted, false, TextAnchor.MiddleLeft, 12);
            SafeText(root, node.Effect, 72, 892, 1000, 90, 24, White, false, TextAnchor.UpperLeft, 14);
            string id = node.Id;
            bool can = CanBuyNode(node, hero);
            string label = have ? "Unlocked" : can ? "Unlock for " + node.Cost : !PrerequisitesMet(node, hero) ? "Needs earlier passives" : "Not enough points";
            TableButton(label, 72, 986, 340, 50, () => BuyNode(id), can, true);
        }

        private void DrawRelics()
        {
            ChromePanel(root, "Relics", 1140, 800, 740, 250, "Panel");
            OutlinedText(root, "Relics · equip up to two", 1140, 808, 740, 40, 26, Gold, true, TextAnchor.MiddleCenter, 16);
            var relics = CampaignMeta.Relics;
            for (int i = 0; i < relics.Length; i++)
            {
                var relic = relics[i];
                bool have = profile.ownedRelics.Contains(relic.Id);
                bool equipped = profile.equippedRelics.Contains(relic.Id);
                float x = 1160 + (i % 2) * 360, y = 854 + (i / 2) * 96;
                ColorBox(root, "relic bg", x, y, 350, 88, new Color(0, 0, 0, have ? .3f : .16f));
                FitSafe(root, "Campaign/Relic" + (i + 1), x + 4, y + 4, 80, 80, have ? Color.white : new Color(.3f, .3f, .3f, 1));
                SafeText(root, relic.Name, x + 92, y + 4, 250, 28, 21, have ? Gold : Quiet, true, TextAnchor.MiddleLeft, 12);
                SafeText(root, have ? relic.Summary : "Not found yet", x + 92, y + 32, 250, 54, 15, have ? White : Quiet, false, TextAnchor.UpperLeft, 10);
                if (equipped) Label(root, "Equipped", x + 250, y + 4, 96, 24, 15, AllySage, true, TextAnchor.MiddleRight);
                if (have)
                {
                    string rid = relic.Id;
                    var area = Box(root, "relic click", x, y, 350, 88);
                    Click(area, () => ToggleRelic(rid)).transition = Selectable.Transition.None;
                }
            }
        }

        private void ToggleRelic(string relicId)
        {
            EnsureCampaignProfile();
            if (profile.equippedRelics.Contains(relicId))
            {
                profile.equippedRelics = profile.equippedRelics.Where(r => r != relicId).ToArray();
                Save();
            }
            else EquipRelic(relicId);
            UiSound("click");
            Render();
        }
    }
}
