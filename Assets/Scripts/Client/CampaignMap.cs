using System;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private void Story() { }

        private void StartOptionalEncounter(string encounterId)
        {
            var enc = CampaignMeta.Encounters.FirstOrDefault(e => e.Id == encounterId);
            if (enc == null || !enc.IsOptional) return;
            if (!string.IsNullOrEmpty(enc.UnlockAfter) && !EncounterCleared(enc.UnlockAfter)) return;
            // Rules-only: construct match, no UI.
            StopAllCoroutines();
            if (!Campaign.LegalDeck(profile.deck, owned))
            {
                profile.deck = Campaign.Starter;
                Save();
            }
            ai = false;
            duel = false;
            tutorialMatch = false;
            activeEncounterId = enc.Id;
            var foe = Campaign.Chapters[Mathf.Clamp(chapter, 0, Campaign.Chapters.Length - 1)];
            var rules = SnapshotCampaignRules(enc.Id);
            match = new Match(profile.deck, Campaign.EnemyDeck(chapter), profile.hero, foe.Hero, foe.First, foe.Second,
                Environment.TickCount, foe.Health, rules);
            Go(Page.Battle);
        }
    }
}
