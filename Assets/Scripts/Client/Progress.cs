using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private Ladder.SeasonState ladderState = new Ladder.SeasonState();

        private void SyncLadderFromProfile()
        {
            ladderState.AbsoluteRank = profile.ladderAbsolute;
            ladderState.Stars = profile.ladderStars;
            ladderState.StarBonus = Math.Max(1, profile.starBonus);
            ladderState.WinStreak = profile.winStreak;
            ladderState.PeakAbsolute = profile.peakAbsolute;
            ladderState.SeasonYear = profile.seasonYear;
            ladderState.SeasonMonth = profile.seasonMonth;
            ladderState.RankedMmr = profile.rankedMmr;
            ladderState.RankedRd = profile.rankedRd;
            ladderState.CasualMmr = profile.casualMmr;
            ladderState.CasualRd = profile.casualRd;
            ladderState.LegendPlace = profile.legendPlace;
            ladderState.RankedGames = profile.rankedGames;
            ladderState.ApprenticeGamesLeft = profile.apprenticeLeft;
            Ladder.EnsureSeason(ladderState, DateTime.UtcNow);
            WriteLadderToProfile();
        }

        private void WriteLadderToProfile()
        {
            profile.ladderAbsolute = ladderState.AbsoluteRank;
            profile.ladderStars = ladderState.Stars;
            profile.starBonus = ladderState.StarBonus;
            profile.winStreak = ladderState.WinStreak;
            profile.peakAbsolute = ladderState.PeakAbsolute;
            profile.seasonYear = ladderState.SeasonYear;
            profile.seasonMonth = ladderState.SeasonMonth;
            profile.rankedMmr = ladderState.RankedMmr;
            profile.rankedRd = ladderState.RankedRd;
            profile.casualMmr = ladderState.CasualMmr;
            profile.casualRd = ladderState.CasualRd;
            profile.legendPlace = ladderState.LegendPlace;
            profile.rankedGames = ladderState.RankedGames;
            profile.apprenticeLeft = ladderState.ApprenticeGamesLeft;
        }

        private void RefreshDailies()
        {
            string stamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (profile.dailyStamp == stamp && profile.dailies != null && profile.dailies.Count > 0) return;
            profile.dailyStamp = stamp;
            profile.dailies = new List<DailyQuest>
            {
                new DailyQuest{id="win_practice",label="Win 1 practice or ranked duel",target=1,rewardEmbers=40},
                new DailyQuest{id="claim_seals",label="Claim 3 Seals in battle",target=3,rewardEmbers=35},
                new DailyQuest{id="play_chapter",label="Complete a campaign battle",target=1,rewardEmbers=50}
            };
        }

        private void ProgressDaily(string id, int amount = 1)
        {
            RefreshDailies();
            var quest = profile.dailies.FirstOrDefault(q => q.id == id);
            if (quest == null || quest.claimed) return;
            quest.progress = Mathf.Min(quest.target, quest.progress + amount);
            if (quest.progress >= quest.target && !quest.claimed)
            {
                quest.claimed = true;
                profile.embers += quest.rewardEmbers;
                status = "Daily complete · +" + quest.rewardEmbers + " Embers";
                Toast("Quest complete", quest.label + " · +" + quest.rewardEmbers + " Embers", Gold);
                UnlockAchievement("daily_" + id);
            }
        }

        private void UnlockAchievement(string id)
        {
            if (profile.unlockedAchievements.Contains(id)) return;
            profile.unlockedAchievements = profile.unlockedAchievements.Concat(new[] { id }).ToArray();
            var named = AchievementList.FirstOrDefault(a => a.id == id);
            if (named.id != null) Toast("Achievement", named.name + " — " + named.text, AllySage);
        }

        private void RecordMatchEnd(bool won, bool wasDuel, int sealsDelta)
        {
            if (won) profile.wins++;
            else if (match != null && match.Winner != 2) profile.losses++;
            if (sealsDelta > 0)
            {
                profile.sealsClaimed += sealsDelta;
                ProgressDaily("claim_seals", sealsDelta);
            }
            profile.favoriteHero = profile.hero;
            if (won && wasDuel) ProgressDaily("win_practice");
            if (won && !wasDuel && !tutorialMatch) ProgressDaily("play_chapter");
            if (profile.progress >= 1) UnlockAchievement("first_chapter");
            if (profile.sealsClaimed >= 1) UnlockAchievement("first_seal");
            if (Hero.All.All(h => UnlockedHero(h.Id))) UnlockAchievement("all_heroes");
            if (tutorialMatch && won)
            {
                profile.tutorialDone = true;
                UnlockAchievement("tutorial_clear");
            }
            Save();
        }

        private void ApplyRatedResult(bool won, Net.QueueMode mode, double oppMmr, double oppRd)
        {
            int beforeRank = ladderState.AbsoluteRank;
            int beforeStars = ladderState.Stars;
            if (mode == Net.QueueMode.Ranked)
            {
                Glicko.Update(ref ladderState.RankedMmr, ref ladderState.RankedRd, oppMmr, oppRd, won);
                Ladder.ApplyRankedResult(ladderState, won);
                if (ladderState.AbsoluteRank > beforeRank) UiSound("rank-up");
                else if (ladderState.Stars > beforeStars) UiSound("star-gain");
                else if (ladderState.Stars < beforeStars || ladderState.AbsoluteRank < beforeRank) UiSound("star-loss");
            }
            else Glicko.Update(ref ladderState.CasualMmr, ref ladderState.CasualRd, oppMmr, oppRd, won);
            WriteLadderToProfile();
            Save();
        }
    }
}
